﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Structs;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class Ribbit
    {
        private CDN _cdn;
        private CdnsFile _cdns;

        public Ribbit(CDN cdn, CdnsFile cdns)
        {
            _cdn = cdn;

            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
        }

        //TODO comment
        public void HandleInstallFile(CDNConfigFile cdnConfig, EncodingTable encodingTable, CDN cdn, CdnsFile cdns,
            Dictionary<string, IndexEntry> archiveIndexDictionary)
        {
            Console.Write("Parsing install file list...");
            var timer = Stopwatch.StartNew();

            var installFile = ParseInstallFile(cdns.entries[0].path, encodingTable.installKey);

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var archiveIndexDownloads = new List<InstallFileMatch>();
            var fileIndexDownloads = new List<InstallFileMatch>();

            var reverseLookupDictionary = encodingTable.EncodingDictionary.ToDictionary(e => e.Value, e => e.Key);

            foreach (var file in installFile.entries)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (!reverseLookupDictionary.ContainsKey(file.contentHashString.FromHexString().ToMD5()))
                {
                    continue;
                }

                //TODO make multi region
                if (!file.tags.Contains("1=enUS"))
                {
                    continue;
                }

                // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                var upperHash = reverseLookupDictionary[file.contentHashString.FromHexString().ToMD5()].ToString().ToUpper();

                if (archiveIndexDictionary.ContainsKey(upperHash))
                {
                    IndexEntry archiveIndex = archiveIndexDictionary[upperHash];
                    archiveIndexDownloads.Add(new InstallFileMatch { IndexEntry = archiveIndex, InstallFileEntry = file });
                    //Debugger.Break();
                }
                else if (fileIndexList.ContainsKey(upperHash))
                {
                    IndexEntry indexMatch = fileIndexList[upperHash];
                    fileIndexDownloads.Add(new InstallFileMatch() { IndexEntry = indexMatch, InstallFileEntry = file });
                    //TODO Not sure what needs to be done here
                    //Debugger.Break();
                }
            }

            var requests = archiveIndexDownloads.Select(e => new Request
            {
                Uri = e.IndexEntry.IndexId,
                LowerByteRange = (int)e.IndexEntry.offset,
                // Need to subtract 1, since the byte range is "inclusive"
                UpperByteRange = ((int)e.IndexEntry.offset + (int)e.IndexEntry.size - 1)
            }).ToList();

            foreach (var indexDownload in requests)
            {
                cdn.QueueRequest($"{cdns.entries[0].path}/data/", indexDownload.Uri, indexDownload.LowerByteRange, indexDownload.UpperByteRange, true);
            }
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        public InstallFile ParseInstallFile(string url, string hash)
        {
            var install = new InstallFile();

            byte[] content = _cdn.Get($"{url}/data/", hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN")
                {
                    throw new Exception("Error while parsing install file. Did BLTE header size change?");
                }

                bin.ReadByte();

                install.hashSize = bin.ReadByte();
                if (install.hashSize != 16) throw new Exception("Unsupported install hash size!");

                install.numTags = bin.ReadUInt16(true);
                install.numEntries = bin.ReadUInt32(true);

                int bytesPerTag = ((int)install.numEntries + 7) / 8;

                install.tags = new InstallTagEntry[install.numTags];

                for (var i = 0; i < install.numTags; i++)
                {
                    install.tags[i].name = bin.ReadCString();
                    install.tags[i].type = bin.ReadUInt16(true);

                    var filebits = bin.ReadBytes(bytesPerTag);

                    for (int j = 0; j < bytesPerTag; j++)
                        filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

                    install.tags[i].files = new BitArray(filebits);
                }

                install.entries = new InstallFileEntry[install.numEntries];

                for (var i = 0; i < install.numEntries; i++)
                {
                    install.entries[i].name = bin.ReadCString();
                    install.entries[i].contentHash = bin.ReadBytes(install.hashSize);
                    install.entries[i].contentHashString = BitConverter.ToString(install.entries[i].contentHash).Replace("-", "");
                    install.entries[i].size = bin.ReadUInt32(true);
                    install.entries[i].tags = new List<string>();
                    for (var j = 0; j < install.numTags; j++)
                    {
                        if (install.tags[j].files[i] == true)
                        {
                            install.entries[i].tags.Add(install.tags[j].type + "=" + install.tags[j].name);
                        }
                    }
                }
            }

            return install;
        }

        public void HandleDownloadFile(CDN cdn, CdnsFile cdns, DownloadFile download, Dictionary<string, IndexEntry> archiveIndexDictionary, CDNConfigFile cdnConfigFile)
        {
            Console.Write("Parsing download file list...");
            var timer = Stopwatch.StartNew();

            var indexDownloads = 0;
            var totalBytes = 0L;

            //TODO make this more flexible.  Perhaps pass in the region by name?
            var tagToUse = download.tags.Single(e => e.Name.Contains("enUS"));
            var tagToUse2 = download.tags.Single(e => e.Name.Contains("Windows"));

            for (var i = 0; i < download.entries.Length; i++)
            {
                var current = download.entries[i];

                // Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                if (tagToUse.Bits[i] == false || tagToUse2.Bits[i] == false)
                {
                    continue;
                }
                if (!archiveIndexDictionary.ContainsKey(current.hash))
                {
                    continue;
                }

                IndexEntry e = archiveIndexDictionary[current.hash];
                uint blockSize = 1048576;

                uint offset = e.offset;
                uint size = e.size;
                uint blockStart = offset / blockSize;
                uint blockEnd = (offset + size + blockSize - 1) / blockSize;

                uint getStart = blockEnd;
                uint getEnd = blockStart;

                var mask = cdnConfigFile.archives[e.index].mask;

                //TODO fix this.  sometimes happens when running all unit tests.  Might not even be needed
                if (mask != null)
                {
                    for (int j = (int)blockStart; j < blockEnd; ++j)
                    {
                        var condition2 = (mask[j / 8] & (1 << (j & 7))) != 0;
                        if (j / 8 >= mask.Length || condition2)
                        {
                            getStart = (uint)Math.Max(getStart, i);
                            getEnd = (uint)Math.Max(getEnd, i + 1);
                        }
                    }
                }
                

                //if (getStart < getEnd)
                //{
                //    var startBytes = getStart * blockSize;
                //    var endBytes = (getEnd * blockSize) - 1;
                //    //cdn.QueueRequest($"{cdns.entries[0].path}/data/", e.IndexId, startBytes, endBytes, writeToDevNull: true);
                //    indexDownloads++;
                //}
                if (blockStart < blockEnd)
                {
                    var excessBlocks = 1;
                    var startBytesBlock = Math.Max(((int)blockStart - excessBlocks) * blockSize, 0);
                    var endBytesBlock = ((blockEnd + excessBlocks) * blockSize) - 1;
                    cdn.QueueRequest($"{cdns.entries[0].path}/data/", e.IndexId, startBytesBlock, endBytesBlock, writeToDevNull: true);
                }
                else
                {
                    Debugger.Break();
                }
                


                var startBytes = e.offset;
                // Need to subtract 1, since the byte range is "inclusive"
                int upperByteRange = ((int)e.offset + (int)e.size - 1);
                //cdn.QueueRequest($"{cdns.entries[0].path}/data/", e.IndexId, startBytes, upperByteRange, writeToDevNull: true);
            }
            
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
    }
}
