namespace UmbracoCompose.Cli;

internal readonly struct Emoji(string name, string? textColor = null)
{
    public string Name { get; } = name;
    public string? TextColor { get; } = textColor;

    public override string ToString() =>
        $"[{TextColor}]:{Name}:[/]  ";

    public static implicit operator string (Emoji emoji) =>
        emoji.ToString();
}
