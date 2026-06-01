using System.CommandLine;
using System.Text.Json.Nodes;
using Spectre.Console;
using Umbraco.Compose.Cli.Models;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class ProfileListCommand : BaseCommand
{
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (table or json)",
    };

    private readonly ProfileConfigService _profileConfigService;

    public ProfileListCommand(IConsole console, ProfileConfigService profileConfigService)
        : base("list", "List configured profiles", console)
    {
        _profileConfigService = profileConfigService;

        Options.Add(s_formatOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        OutputFormat format = parseResult.GetValue(s_formatOption);

        ProfileConfig? config = await _profileConfigService.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (!ProfileGuard.HasProfiles(config, Console))
        {
            return CommandResult.Success();
        }

        switch (format)
        {
            case OutputFormat.Table:
                DisplayTable(config);
                break;

            case OutputFormat.Json:
                DisplayJson(config);
                break;
        }

        return CommandResult.Success();
    }

    private void DisplayTable(ProfileConfig config)
    {
        var table = ProfileTableBuilder.CreateProfileTable(includeSecrets: false);
        table.AddColumn("[bold]Default[/]");

        foreach (var pair in config.Profiles)
        {
            bool isDefault = pair.Key == config.Default;
            ProfileTableBuilder.AddProfileRow(table, pair.Key, pair.Value, includeSecrets: false, isDefault: isDefault);
        }

        Console.DisplayRenderable(table);
    }

    private void DisplayJson(ProfileConfig config)
    {
        var obj = ProfileJsonBuilder.ToJsonObject(config.Profiles, includeSecrets: false);
        foreach (var pair in config.Profiles)
        {
            obj[pair.Key]!["isDefault"] = pair.Key == config.Default;
        }
        Console.DisplayRawText(ProfileJsonBuilder.ToJsonString(obj), ConsoleOutput.Standard);
    }
}
