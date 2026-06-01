using System.CommandLine;
using Spectre.Console;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Commands;

internal sealed class AgentInitCommand : BaseCommand
{
    private static readonly Option<bool> s_forceOption = new("--force", "-f")
    {
        Description = "Overwrite existing skill files without prompting",
    };

    private readonly FileWriteHelper _fileWriteHelper;

    // Skill files to install (relative path within umbraco-compose-cli directory)
    private static readonly (string RelativePath, string ResourceName)[] s_skillFiles =
    [
        ("SKILL.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.SKILL.md"),
        ("references/cli-reference.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.cli-reference.md"),
        ("references/exit-codes.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.exit-codes.md"),
        ("references/filtering-and-sorting.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.filtering-and-sorting.md"),
        ("references/ingestion.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.ingestion.md"),
        ("references/quick-reference.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.quick-reference.md"),
        ("references/relay-connections.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.relay-connections.md"),
        ("references/schema-mappings.md", "UmbracoCompose.Cli.Resources.agents.skills.umbraco_compose.references.schema-mappings.md"),
    ];

    // Install locations with their display labels
    private static readonly (string Path, string Label)[] s_defaultLocations =
    [
        (".agents/skills", "Standard (.agents/skills) - Supported by  VS Code, GitHub Copilot and OpenCode"),
        (".claude/skills", "Claude Code (.claude/skills)"),
        (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agent/skills"), "Global (~/.agents/skills) - Same as Standard but globally"),
    ];

    public AgentInitCommand(IConsole console, FileWriteHelper fileWriteHelper)
        : base("init", "Install Umbraco Compose CLI skills for AI agents", console)
    {
        _fileWriteHelper = fileWriteHelper;
        Options.Add(s_forceOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        bool force = parseResult.GetValue(s_forceOption);

        // Present multi-select prompt
        MultiSelectionPrompt<string> multiSelect = new MultiSelectionPrompt<string>()
            .Title("[bold cyan]Select where to install Umbraco Compose CLI skills:[/]")
            .InstructionsText("[dim](Press <space> to select, <enter> to confirm)[/]")
            .UseConverter(label => label);

        multiSelect.AddChoices(s_defaultLocations.Select(l => l.Label).ToArray());
        multiSelect.Select(s_defaultLocations[0].Label);

        string[] selectedLabels = await Console.MultiSelectPromptAsync(multiSelect, cancellationToken).ConfigureAwait(false);

        // Map selected labels back to paths
        string[] selectedPaths = selectedLabels
            .Select(label => s_defaultLocations.First(l => l.Label == label).Path)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            Console.DisplayMessage(Emojis.Information, "No locations selected. Installation cancelled.");
            return CommandResult.Success();
        }

        int filesCopied = 0;
        int filesFailed = 0;

        // Get the skill files from embedded resources
        var skillFiles = GetSkillFiles();

        if (skillFiles.Count == 0)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, "No skill files found in the CLI package.");
        }

        foreach (string path in selectedPaths)
        {
            string location = Path.Combine(path , "umbraco-compose");

            // Create directory if it doesn't exist
            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }

            Console.DisplayMessage(Emojis.Package, $"Installing to [cyan]{location}[/]...");

            foreach (var (relativePath, content) in skillFiles)
            {
                string targetPath = Path.Combine(location, relativePath);
                string? targetDir = Path.GetDirectoryName(targetPath);

                if (targetDir is not null && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                bool success;
                if (force)
                {
                    success = await _fileWriteHelper.WriteAtomicAsync(targetPath, content, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Check if file exists
                    if (File.Exists(targetPath))
                    {
                        bool overwrite = await Console.ConfirmAsync(
                            $"  File already exists at [cyan]{targetPath}[/]. Overwrite?",
                            defaultAnswer: false,
                            cancellationToken).ConfigureAwait(false);

                        if (!overwrite)
                        {
                            continue;
                        }
                    }

                    success = await _fileWriteHelper.WriteAtomicAsync(targetPath, content, cancellationToken).ConfigureAwait(false);
                }

                if (success)
                {
                    filesCopied++;
                }
                else
                {
                    filesFailed++;
                    Console.DisplayError($"  Failed to write: {relativePath}");
                }
            }
        }

        if (filesCopied > 0)
        {
            string pluralizedFiles = filesCopied == 1 ? "file" : "files";
            Console.DisplayMessage(Emojis.Sparkles, $"Successfully installed {filesCopied} {pluralizedFiles}.");
        }

        if (filesFailed > 0)
        {
            string pluralizedFiles = filesFailed == 1 ? "file" : "files";
            Console.DisplayError($"Failed to install {filesFailed} {pluralizedFiles}. Check permissions.");
            return CommandResult.Failure(ExitCodes.RuntimeError, $"Failed to install {filesFailed} {pluralizedFiles}.");
        }

        return CommandResult.Success();
    }

    private static List<(string RelativePath, string Content)> GetSkillFiles()
    {
        var skillFiles = new List<(string RelativePath, string Content)>();
        var assembly = typeof(AgentInitCommand).Assembly;

        foreach (var (relativePath, resourceName) in s_skillFiles)
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                skillFiles.Add((relativePath, content));
            }
        }

        return skillFiles;
    }
}
