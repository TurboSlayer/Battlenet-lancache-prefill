﻿using System;
using System.Diagnostics;
using System.Linq;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Structs;
using Konsole;
using Shared;
using Colors = Shared.Colors;

namespace BuildBackup
{
    /// <summary>
    /// Documentation :
    ///   https://wowdev.wiki/TACT
    ///   https://github.com/d07RiV/blizzget/wiki
    /// </summary>
    public class Program
    {
        private static TactProduct[] ProductsToProcess = new[]{ TactProducts.Starcraft1 };

        public static bool UseCdnDebugMode = true;
        
        public static void Main()
        {
            foreach (var product in ProductsToProcess)
            {
                ProcessProduct(product, new Writer(), UseCdnDebugMode);
            }
            Console.WriteLine("Pre-load Complete!\n");

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
        }

        public static ComparisonResult ProcessProduct(TactProduct product, IConsole console, bool useDebugMode)
        {
            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Now starting processing of : {Colors.Cyan(product.DisplayName)}");

            CDN cdn = new CDN(console) { DebugMode = useDebugMode };
            Logic logic = new Logic(cdn, Config.BattleNetPatchUri);

            // Loading CDNs
            var timer2 = Stopwatch.StartNew();
            CdnsFile cdns = logic.GetCDNs(product);
            Console.WriteLine($"GetCDNs loaded in {Colors.Yellow(timer2.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            // Initializing other classes, now that we have our CDN info loaded
            var patchLoader = new PatchLoader(cdn, cdns, console, product);
            var unarchivedFileHandler = new UnarchivedFileHandler(cdn, cdns, console);
            var ribbit = new Ribbit(cdn, cdns);
            var encodingFileHandler = new EncodingFileHandler(cdns, cdn);

            // Finding the latest version of the game
            VersionsEntry targetVersion = logic.GetVersionEntry(product);
            BuildConfigFile buildConfig = Requests.GetBuildConfig(cdns.entries[0].path, targetVersion, cdn);
            CDNConfigFile cdnConfig = logic.GetCDNconfig(cdns.entries[0].path, targetVersion);

            logic.GetBuildConfigAndEncryption(product, cdnConfig, targetVersion, cdn, cdns);

            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);
            var downloadFile = DownloadFileHandler.ParseDownloadFile(cdn, cdns.entries[0].path, encodingTable.downloadKey);
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdns.entries[0].path, cdnConfig, cdn, product, Config.BattleNetPatchUri);

            // Starting the download
            ribbit.HandleInstallFile(cdnConfig, encodingTable, cdn, cdns, archiveIndexDictionary);
            ribbit.HandleDownloadFile(cdn, cdns, downloadFile, archiveIndexDictionary, cdnConfig);

            unarchivedFileHandler.DownloadUnarchivedFiles(cdnConfig, encodingTable, product);

            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.DownloadPatchArchives(cdnConfig, patch);
            patchLoader.DownloadPatchFiles(cdnConfig);
            patchLoader.DownloadFullPatchArchives(cdnConfig);

            //DownloadFileHandler.DownloadFullArchives(cdnConfig, cdn, cdns);

            cdn.DownloadQueuedRequests();

            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var comparisonUtil = new ComparisonUtil(console);
            var result = comparisonUtil.CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product);
            
            return result;
        }
        
    }
}
