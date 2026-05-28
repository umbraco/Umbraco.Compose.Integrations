using System.CommandLine;
using Spectre.Console;

using UmbracoCompose.Cli.Models;
using Profile = UmbracoCompose.Cli.Models.Profile;
using UmbracoCompose.Cli.Services;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileListCommand : BaseCommand
{
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (table or json)",
    };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || config.Profiles.Count == 0)
        {
            Console.DisplayMessage(Emojis.Information, "No profiles configured.");
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
        Table table = new();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Region[/]");
        table.AddColumn("[bold]Project Alias[/]");
        table.AddColumn("[bold]Environment Alias[/]");
        table.AddColumn("[bold]Default[/]");

        foreach (KeyValuePair<string, Profile> pair in config.Profiles)
        {
            string profileName = pair.Key;
            Profile profile = pair.Value;
            string isDefault = pair.Key == config.Default ? "*" : " ";

            table.AddRow(
                $"[yellow]{profileName.EscapeMarkup()}[/]",
                $"[yellow]{profile.Region.EscapeMarkup()}[/]",
                $"[yellow]{profile.ProjectAlias.EscapeMarkup()}[/]",
                $"[yellow]{profile.EnvironmentAlias.EscapeMarkup()}[/]",
                $"[yellow]{isDefault.EscapeMarkup()}[/]");
        }

        Console.DisplayRenderable(table);
    }

    private void DisplayJson(ProfileConfig config)
    {
        JsonArray arr = new();

        foreach (KeyValuePair<string, Profile> pair in config.Profiles)
        {
            arr.Add((JsonNode)new JsonObject
            {
                ["name"] = pair.Key,
                ["region"] = pair.Value.Region,
                ["projectAlias"] = pair.Value.ProjectAlias,
                ["environmentAlias"] = pair.Value.EnvironmentAlias,
                ["isDefault"] = pair.Key == config.Default,
            });
        }

        string json = arr.ToJsonString(s_jsonOptions);
        Console.DisplayRawText(json, ConsoleOutput.Standard);
    }
}
