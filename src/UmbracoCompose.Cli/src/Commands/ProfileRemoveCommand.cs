using System.CommandLine;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileRemoveCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new("name")
    {
        Description = "Name of the profile to remove"
    };

    private static readonly Option<bool> s_forceOption = new("--force")
    {
        Description = "Skip confirmation prompt"
    };

    private readonly ProfileConfigService _profileConfigService;

    public ProfileRemoveCommand(IConsole console, ProfileConfigService profileConfigService)
        : base("remove", "Remove a profile", console)
    {
        _profileConfigService = profileConfigService;

        Arguments.Add(s_nameArgument);
        Options.Add(s_forceOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string name = parseResult.GetValue(s_nameArgument)!;
        bool force = parseResult.GetValue(s_forceOption);

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile name cannot be empty.");
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile name '{name}' contains invalid characters.");
        }

        // Load config
        ProfileConfig? config = _profileConfigService.Load();

        // Check if profile exists
        if (config is null || !config.Profiles.ContainsKey(name))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{name}' not found.");
        }

        // Confirm removal (unless --force)
        if (!force)
        {
            bool confirmed = await Console.ConfirmAsync(
                $"Are you sure you want to remove profile '{name}'?",
                defaultAnswer: false,
                cancellationToken: cancellationToken);

            if (!confirmed)
            {
                return CommandResult.Success();
            }
        }

        // Remove the profile
        bool success = _profileConfigService.Update(config =>
        {
            config.Profiles.Remove(name);

            if (config.Default == name)
            {
                config.Default = null;
            }

            return config;
        });

        if (!success)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to remove profile. The configuration file may be locked or unreadable.");
        }

        Console.DisplayMessage(Emojis.CheckMark, $"Profile '{name}' removed.");

        return CommandResult.Success();
    }
}
