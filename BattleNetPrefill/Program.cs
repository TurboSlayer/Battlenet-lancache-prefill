using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Feature - Implement support for beta apps.  Add a --all-betas  flag
    // TODO - Feature - Possibly add a clear cache command?
    // TODO - Feature - Consider implementing a multi-select command for interactively choosing which products to prefill.  Similar to steamPrefill.
    // TODO - Tech Debt - Cleanup trim/single file warnings
    // TODO - Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO - Documentation - Add battle.net slow download to known issues page ? https://lancache.net/docs/common-issues/
    // TODO - Add link to Lancache discord + bnet prefill channel in readme
    // TODO - Resolve issues on Github issues
    // TODO - General - Promote this app on r/lanparty and discord
    // TODO - Test out https://github.com/microsoft/infersharpaction
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Documentation on website needs to be updated to include changes
    // TODO - interface - Remove "Default : false" output from CliFx help text
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    // TODO - Document process for updating app
    // TODO - readme - Update readme to match style of steam prefill readme
    public static class Program
    {
        public static async Task<int> Main()
        {
            var description = "Automatically fills a Lancache with games from Battle.net, so that subsequent downloads will be \n" +
                              "  served from the Lancache, improving speeds and reducing load on your internet connection.";
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName($"BattleNetPrefill{(OperatingSystem.IsWindows() ? ".exe" : "")}")
                         .SetDescription(description)
                         .Build()
                         .RunAsync();
        }
    }
}