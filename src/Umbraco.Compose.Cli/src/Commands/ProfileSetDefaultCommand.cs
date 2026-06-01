using System.CommandLine;
using Umbraco.Compose.Cli.Models;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class ProfileSetDefaultCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new Argument<string>("name") { Description = "Name of the profile to set as default" }.AcceptValidProfileName();
    private readonly ProfileConfigService _profileConfigService;

    public ProfileSetDefaultCommand(IConsole console, ProfileConfigService profileConfigService)
        : base("set-default", "Set the default profile", console)
    {
        _profileConfigService = profileConfigService;
        Arguments.Add(s_nameArgument);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string name = parseResult.GetValue(s_nameArgument)!;

        ProfileConfig? config = await _profileConfigService.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (config is null || !config.Profiles.ContainsKey(name))
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{name}' not found.");

        bool success = await _profileConfigService.UpdateAsync(config =>
        {
            config.Default = name;
            return config;
        }, cancellationToken).ConfigureAwait(false);

        if (!success)
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to save profile configuration. The configuration file may be locked or unreadable.");

        Console.DisplayMessage(Emojis.CheckMark, $"Profile '{name}' set as default.");
        return CommandResult.Success();
    }
}
