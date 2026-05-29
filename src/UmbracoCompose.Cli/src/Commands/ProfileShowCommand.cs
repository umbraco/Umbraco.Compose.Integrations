using System.CommandLine;
using Spectre.Console;

using UmbracoCompose.Cli.Models;
using Profile = UmbracoCompose.Cli.Models.Profile;
using UmbracoCompose.Cli.Services;
using UmbracoCompose.Cli.Utilities;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileShowCommand : BaseCommand
{
    private static readonly Argument<string> s_nameArgument = new("name")
    {
        Description = "The name of the profile to show"
    };
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (table or json)",
    };

    private static readonly Option<bool> s_showSecretsOption = new("--show-secrets")
    {
        Description = "Show sensitive values (client secret)",
    };

    private readonly ProfileConfigService _profileConfigService;

    public ProfileShowCommand(IConsole console, ProfileConfigService profileConfigService)
        : base("show", "Show a profile by name", console)
    {
        _profileConfigService = profileConfigService;

        Arguments.Add(s_nameArgument);
        Options.Add(s_formatOption);
        Options.Add(s_showSecretsOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        string name = parseResult.GetValue(s_nameArgument)!;
        OutputFormat format = parseResult.GetValue(s_formatOption);
        bool showSecrets = parseResult.GetValue(s_showSecretsOption) && !Console.IsOutputRedirected;

        ProfileConfig? config = await _profileConfigService.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (!ProfileGuard.HasProfiles(config, Console))
        {
            return CommandResult.Success();
        }

        if (!config!.Profiles.TryGetValue(name, out Profile? profile))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{name}' not found.");
        }

        switch (format)
        {
            case OutputFormat.Table:
                DisplayTable(name, profile, config, showSecrets);
                break;

            case OutputFormat.Json:
                DisplayJson(name, profile, config, showSecrets);
                break;
        }

        return CommandResult.Success();
    }

    private void DisplayTable(string name, Profile profile, ProfileConfig config, bool showSecrets)
    {
        var table = ProfileTableBuilder.CreatePropertyTable();
        ProfileTableBuilder.AddPropertyRow(table, "Name", name);
        ProfileTableBuilder.AddPropertyRow(table, "Region", profile.Region);
        ProfileTableBuilder.AddPropertyRow(table, "Project Alias", profile.ProjectAlias);
        ProfileTableBuilder.AddPropertyRow(table, "Environment Alias", profile.EnvironmentAlias);
        ProfileTableBuilder.AddPropertyRow(table, "Client ID", profile.ClientId);

        if (showSecrets)
        {
            ProfileTableBuilder.AddPropertyRow(table, "Client Secret", profile.ClientSecret);
        }

        string isDefault = name == config.Default ? "Yes" : "No";
        ProfileTableBuilder.AddPropertyRow(table, "Default", isDefault);

        Console.DisplayRenderable(table);
    }

    private void DisplayJson(string name, Profile profile, ProfileConfig config, bool showSecrets)
    {
        var obj = ProfileJsonBuilder.ToJsonObject(name, profile, showSecrets);
        obj["clientId"] = profile.ClientId;
        obj["isDefault"] = name == config.Default;
        if (showSecrets)
        {
            obj["clientSecret"] = profile.ClientSecret;
        }
        Console.DisplayRawText(ProfileJsonBuilder.ToJsonString(obj), ConsoleOutput.Standard);
    }
}
