using System.CommandLine;
using System.CommandLine.Parsing;

namespace Umbraco.Compose.Cli.Utilities;

internal static class CommandLineValidators
{
    private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();

    public static Argument<string> AcceptValidProfileName(this Argument<string> argument)
    {
        argument.Validators.Add(ValidateProfileName);
        return argument;
    }

    private static void ValidateProfileName(ArgumentResult result)
    {
        foreach (Token token in result.Tokens)
        {
            string name = token.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddError("Profile name cannot be empty.");
                continue;
            }
            if (name.IndexOfAny(s_invalidFileNameChars) >= 0)
            {
                result.AddError($"Profile name '{name}' contains invalid characters.");
            }
        }
    }

    public static Argument<string> AcceptNonEmptyField(this Argument<string> argument, string fieldName)
    {
        argument.Validators.Add((result) => ValidateNonEmptyField(result, fieldName));
        return argument;
    }

    private static void ValidateNonEmptyField(ArgumentResult result, string fieldName)
    {
        foreach (Token token in result.Tokens)
        {
            if (string.IsNullOrWhiteSpace(token.Value))
            {
                result.AddError($"{fieldName} cannot be empty.");
            }
        }
    }
}
