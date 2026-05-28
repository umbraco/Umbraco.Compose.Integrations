using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal class ProfilesCommand : BaseCommand
{
    public ProfilesCommand(
        ProfileListCommand listCommand,
        ProfileAddCommand addCommand,
        ProfileRemoveCommand removeCommand,
        ProfileShowCommand showCommand,
        ProfileSetDefaultCommand setDefaultCommand,
        ProfileValidateCommand validateCommand,
        IConsole console
    ) : base("profiles", "Manage profiles", console)
    {
        Subcommands.Add(listCommand);
        Subcommands.Add(addCommand);
        Subcommands.Add(removeCommand);
        Subcommands.Add(showCommand);
        Subcommands.Add(setDefaultCommand);
        Subcommands.Add(validateCommand);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.DisplayHelp());
    }
}
