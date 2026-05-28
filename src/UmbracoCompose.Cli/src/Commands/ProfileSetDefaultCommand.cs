using System.CommandLine;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileSetDefaultCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new("name") { Description = "Name of the profile to set as default" };
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

        if (string.IsNullOrWhiteSpace(name))
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile name cannot be empty.");

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile name '{name}' contains invalid characters.");

        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || !config.Profiles.ContainsKey(name))
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{name}' not found.");

        bool success = _profileConfigService.Update(config =>
        {
            config.Default = name;
            return config;
        });

        if (!success)
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to save profile configuration. The configuration file may be locked or unreadable.");

        Console.DisplayMessage(Emojis.CheckMark, $"Profile '{name}' set as default.");
        return CommandResult.Success();
    }
}
