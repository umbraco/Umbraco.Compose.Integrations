using System.CommandLine;
using System.CommandLine.Parsing;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileAddCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new Argument<string>("name") { Description = "Name of the profile" }.AcceptValidProfileName();

    private static readonly Argument<string> s_regionArgument = new Argument<string>("region") { Description = "Region" }.AcceptNonEmptyField("Region");

    private static readonly Argument<string> s_projectAliasArgument = new Argument<string>("project-alias") { Description = "Project alias" }.AcceptNonEmptyField("Project alias");

    private static readonly Argument<string> s_environmentAliasArgument = new Argument<string>("environment-alias") { Description = "Environment alias" }.AcceptNonEmptyField("Environment alias");

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
        string name = parseResult.GetValue(s_nameArgument)!;
        string region = parseResult.GetValue(s_regionArgument)!;
        string projectAlias = parseResult.GetValue(s_projectAliasArgument)!;
        string environmentAlias = parseResult.GetValue(s_environmentAliasArgument)!;

        // Check for duplicate before prompting for credentials
        ProfileConfig? existingConfig = await _profileConfigService.LoadAsync(cancellationToken).ConfigureAwait(false);
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

        bool success = await _profileConfigService.UpdateAsync(config =>
        {
            config.Profiles[name] = new(region, projectAlias, environmentAlias, clientId, clientSecret);

            if (config.Default is null)
            {
                config.Default = name;
            }

            return config;
        }, cancellationToken).ConfigureAwait(false);

        if (!success)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to save profile. Check that the config directory is writable.");
        }

        Console.DisplayMessage(Emojis.CheckMark, $"Profile '{name}' added successfully.");

        return CommandResult.Success();
    }
}
