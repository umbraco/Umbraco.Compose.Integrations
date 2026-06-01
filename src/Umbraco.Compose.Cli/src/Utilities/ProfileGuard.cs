using System.Diagnostics.CodeAnalysis;

namespace Umbraco.Compose.Cli.Utilities;

internal static class ProfileGuard
{
    public static bool HasProfiles([NotNullWhen(true)]Umbraco.Compose.Cli.Models.ProfileConfig? config, IConsole console)
    {
        if (config is null || config.Profiles.Count == 0)
        {
            console.DisplayMessage(Emojis.Information, "No profiles configured.");
            return false;
        }
        return true;
    }
}
