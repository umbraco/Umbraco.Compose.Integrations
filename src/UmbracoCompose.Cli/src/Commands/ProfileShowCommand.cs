using System.CommandLine;
using Spectre.Console;

using UmbracoCompose.Cli.Models;
using Profile = UmbracoCompose.Cli.Models.Profile;
using UmbracoCompose.Cli.Services;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Encodings.Web;

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

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
        bool showSecrets = parseResult.GetValue(s_showSecretsOption);

        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || config.Profiles.Count == 0)
        {
            Console.DisplayMessage(Emojis.Information, "No profiles configured.");
            return CommandResult.Success();
        }

        if (!config.Profiles.TryGetValue(name, out Profile? profile))
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
        Table table = new();
        table.AddColumn("[bold]Property[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("[cyan]Name[/]", $"[yellow]{name.EscapeMarkup()}[/]");
        table.AddRow("[cyan]Region[/]", $"[yellow]{profile.Region.EscapeMarkup()}[/]");
        table.AddRow("[cyan]Project Alias[/]", $"[yellow]{profile.ProjectAlias.EscapeMarkup()}[/]");
        table.AddRow("[cyan]Environment Alias[/]", $"[yellow]{profile.EnvironmentAlias.EscapeMarkup()}[/]");
        table.AddRow("[cyan]Client ID[/]", $"[yellow]{profile.ClientId.EscapeMarkup()}[/]");

        // TODO: Find a better way to do this (i.e. not include secrets if output is redirected)
        if (showSecrets && !System.Console.IsOutputRedirected)
        {
            table.AddRow("[cyan]Client Secret[/]", $"[yellow]{profile.ClientSecret.EscapeMarkup()}[/]");
        }

        string isDefault = name == config.Default ? "*" : " ";
        table.AddRow("[cyan]Default[/]", $"[yellow]{isDefault.EscapeMarkup()}[/]");


        Console.DisplayRenderable(table);
    }

    private void DisplayJson(string name, Profile profile, ProfileConfig config, bool showSecrets)
    {
        JsonObject obj = new()
        {
            ["name"] = name,
            ["region"] = profile.Region,
            ["projectAlias"] = profile.ProjectAlias,
            ["environmentAlias"] = profile.EnvironmentAlias,
            ["clientId"] = profile.ClientId,
            ["isDefault"] = name == config.Default,
        };

        if (showSecrets && !System.Console.IsOutputRedirected)
        {
            obj["clientSecret"] = profile.ClientSecret;
        }

        string json = obj.ToJsonString(s_jsonOptions);
        Console.DisplayRawText(json, ConsoleOutput.Standard);
    }
}
