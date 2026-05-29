using Spectre.Console;

namespace UmbracoCompose.Cli.Utilities;

internal static class ProfileTableBuilder
{
    private static readonly string[] s_columns = ["Name", "Region", "Project Alias", "Environment Alias"];

    public static void AddProfileRow(Table table, string name, UmbracoCompose.Cli.Models.Profile profile, bool includeSecrets = false, bool isDefault = false)
    {
        table.AddRow(
            $"[yellow]{name.EscapeMarkup()}[/]",
            $"[yellow]{profile.Region.EscapeMarkup()}[/]",
            $"[yellow]{profile.ProjectAlias.EscapeMarkup()}[/]",
            $"[yellow]{profile.EnvironmentAlias.EscapeMarkup()}[/]",
            isDefault ? "*" : " ");

        if (includeSecrets)
        {
            table.AddRow("", "", "", "", $"[dim]{profile.ClientId.EscapeMarkup()}[/]", $"[dim]{profile.ClientSecret.EscapeMarkup()}[/]");
        }
    }

    public static Table CreateProfileTable(bool includeSecrets = false)
    {
        var table = new Table();
        foreach (var col in s_columns)
        {
            table.AddColumn($"[bold]{col}[/]");
        }
        if (includeSecrets)
        {
            table.AddColumn("[bold]Client ID[/]");
            table.AddColumn("[bold]Client Secret[/]");
        }
        return table;
    }

    public static Table CreatePropertyTable()
    {
        var table = new Table();
        table.AddColumn("[bold]Property[/]");
        table.AddColumn("[bold]Value[/]");
        return table;
    }

    public static void AddPropertyRow(Table table, string property, string value)
    {
        table.AddRow($"[cyan]{property.EscapeMarkup()}[/]", $"[yellow]{value.EscapeMarkup()}[/]");
    }
}
