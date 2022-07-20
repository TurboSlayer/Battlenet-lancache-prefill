
# Battlenet-lancache-prefill

[![](https://dcbadge.vercel.app/api/server/BKnBS4u?style=flat-square)](https://discord.com/invite/BKnBS4u)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tpill90_Battlenet-lancache-prefill&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=tpill90_Battlenet-lancache-prefill)

Automatically fills a [Lancache](https://lancache.net/) with games from Battle.net, so that subsequent downloads for the same content will be served from the Lancache, improving speeds and reducing load on your internet connection.

![Prefilling game](docs/screenshot1-prefill.png)

Inspired by the [lancache-autofill](https://github.com/zeropingheroes/lancache-autofill) project for Steam games.

# Features
* Downloads specific games by product ID
* Incredibly fast, can easily saturate a 10gbe line!
* Game install writes no data to disk,  no unnecessary wear-and-tear to SSDs!
* Multi-platform support (Windows, Linux, MacOS)
* Self-contained application, no installation required!

# Installation
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/Battlenet-lancache-prefill/releases) page.
2.  Unzip to a directory of your choice
3.  (**Linux / OSX Only**)  Give the downloaded executable permissions to be run with `chmod +x .\BattleNetPrefill`
4.  (**Windows Only**)  Configure your terminal to use Unicode, for much nicer looking UI output.
    - Unicode on Windows is not enabled by default, however adding the following to your Powershell `profile.ps1` will enable it.
    - `[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()`
    - If you do not already have a Powershell profile created, follow this step-by-step guide https://lazyadmin.nl/powershell/powershell-profile/

# Basic Usage

A single game can be downloaded by specifying a single product code
```powershell
.\BattleNetPrefill.exe prefill --products s1
```

Multiple games can be downloaded by specifying as many product codes as desired
```powershell
.\BattleNetPrefill.exe prefill --products s1 d3 zeus
```

Optional flags can be used to bulk preload products, without having to specify each product code individually
```powershell
.\BattleNetPrefill.exe prefill --all
.\BattleNetPrefill.exe prefill --blizzard 
.\BattleNetPrefill.exe prefill --activision 
```

The list of currently supported products to download can be displayed using the following
```powershell
.\BattleNetPrefill.exe list-products
```

# Detailed Usage

## list-products
Displays a table of all currently supported Activision and Blizzard games.  Only currently supports retail products, and does not include any PTR or beta products. 

These product IDs can then be used with the `prefill` command to specify which games to be prefilled.

## prefill
Fills a Lancache by downloading the exact same files from Blizzard's CDN as the official Battle.Net client.  Expected initial download speeds should be the speed of your internet connection.

Subsequent runs of this command should be hitting the Lancache, and as such should be dramatically faster than the initial run.  

### -p|--products
If a list of products is supplied, only these products will be downloaded.  This parameter is ideally used when only interested in a small number of games.

### --all, --activision, --blizzard
Downloads multiple products, useful for prefilling a completely empty cache.  Can be combined with `--products`.

### --nocache
By default, **BattleNetPrefill** will cache copies of certain files on disk, in order to dramatically speed up future runs (in some cases 3X faster).  These cache files will be stored in the `/cache` directory in the same directory as **BattleNetPrefill**.
However, in some scenarios this disk cache can potentially take up a non-trivial amount of storage (~1gb), which may not be ideal for all use cases.

By running with the additional flag `--nocache`, **BattleNetPrefill** will no longer cache any files locally, at the expense of slower runtime.

### -f|--force
By default, **BattleNetPrefill** will keep track of the most recently prefilled product, and will only attempt to prefill if there it determines there a newer version available for download.  This default behavior will work best for most use cases, as no time will be wasted re-downloading files that have been previously prefilled.

Running with the flag `--force` will override this behavior, and instead will always run the prefill, re-downloading all files for the specified product.  This flag may be useful for diagnostics, or benchmarking network performance.

# Need Help?
If you are running into any issues, feel free to open up a Github issue on this repository.

You can also find us at the [**LanCache.NET** Discord](https://discord.com/invite/BKnBS4u), in the `#battlenet-prefill` channel.

# Other Docs
* [Development Configuration](/docs/Development.md)

# External Docs
* https://wowdev.wiki/TACT
* https://github.com/d07RiV/blizzget/wiki

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget