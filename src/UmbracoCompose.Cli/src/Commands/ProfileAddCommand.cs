using System.CommandLine;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileAddCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new("name")
    {
        Description = "Name of the profile"
    };

    private static readonly Argument<string> s_regionArgument = new("region")
    {
        Description = "Region"
    };

    private static readonly Argument<string> s_projectAliasArgument = new("project-alias")
    {
        Description = "Project alias"
    };

    private static readonly Argument<string> s_environmentAliasArgument = new("environment-alias")
    {
        Description = "Environment alias"
    };

    private readonly ProfileConfigService _profileConfigService;

    public ProfileAddCommand(IConsole console, ProfileConfigService profileConfigService)
        : base("add", "Add a new profile", console)
    {
        _profileConfigService = profileConfigService;

        Arguments.Add(s_nameArgument);
        Arguments.Add(s_regionArgument);
        Arguments.Add(s_projectAliasArgument);
        Arguments.Add(s_environmentAliasArgument);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? name = parseResult.GetValue(s_nameArgument);
        string? region = parseResult.GetValue(s_regionArgument);
        string? projectAlias = parseResult.GetValue(s_projectAliasArgument);
        string? environmentAlias = parseResult.GetValue(s_environmentAliasArgument);

        // Validate all required arguments
        if (string.IsNullOrWhiteSpace(name))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Region cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(projectAlias))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Project alias cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(environmentAlias))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Environment alias cannot be empty.");
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile name '{name}' contains invalid characters.");
        }

        // Check for duplicate before prompting for credentials
        var existingConfig = _profileConfigService.Load();
        if (existingConfig?.Profiles.ContainsKey(name) == true)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, $"A profile with the name '{name}' already exists.");
        }

        string? clientId = await Console.ReadLineAsync("Client ID: ", cancellationToken: cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Client ID cannot be empty.");
        }

        string? clientSecret = await Console.ReadLineAsync("Client Secret: ", masked: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Client Secret cannot be empty.");
        }

        bool success = _profileConfigService.Update(config =>
        {
            config.Profiles[name] = new(region, projectAlias, environmentAlias, clientId, clientSecret);

            if (config.Default is null)
            {
                config.Default = name;
            }

            return config;
        });

        if (!success)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to save profile. Check that the config directory is writable.");
        }

        Console.DisplayMessage(Emojis.CheckMark, $"Profile '{name}' added successfully.");

        return CommandResult.Success();
    }
}
