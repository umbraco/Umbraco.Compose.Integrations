using System.Globalization;
using System.Text.Json;
using UmbracoCompose.Cli.Commands;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Services;

internal sealed class VariableParser
{
    public async Task<CommandResult?> ParseAsync(
        IList<string>? variableStrings,
        IList<string>? variablesStrings,
        CancellationToken cancellationToken,
        Dictionary<string, object?> variables)
    {
        // First, parse individual --variable flags
        if (variableStrings is not null)
        {
            foreach (string variable in variableStrings)
            {
                CommandResult? result = ParseVariable(variable, variables);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        // Then, parse --variables JSON (overrides individual variables for same keys)
        if (variablesStrings is not null)
        {
            foreach (string variablesEntry in variablesStrings)
            {
                string jsonContent;
                var (variablesContent, variablesError) = await FileReadHelper.ReadFromFileOrReturnAsync(variablesEntry, cancellationToken);
                if (variablesError != null)
                    return variablesError;
                jsonContent = variablesContent!;

                try
                {
                    Dictionary<string, object?>? parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonContent, AppJsonContext.Default.DictionaryStringObject);
                    if (parsed is not null)
                    {
                        foreach (KeyValuePair<string, object?> kvp in parsed)
                        {
                            variables[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    return CommandResult.Failure(ExitCodes.ValidationError, $"Failed to parse variables JSON: {ex.Message}");
                }
            }
        }

        return null;
    }

    private static CommandResult? ParseVariable(string variable, Dictionary<string, object?> variables)
    {
        int equalsIndex = variable.IndexOf('=');
        if (equalsIndex <= 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable format '{variable}'. Expected name=value or name:type=value.");
        }

        string keyWithType = variable[..equalsIndex];
        string value = variable[(equalsIndex + 1)..];

        string name;
        string type;

        int colonIndex = keyWithType.IndexOf(':');
        if (colonIndex > 0)
        {
            name = keyWithType[..colonIndex];
            type = keyWithType[(colonIndex + 1)..].ToLowerInvariant();
            if (string.IsNullOrEmpty(type))
            {
                return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable '{variable}'. Type specifier is empty. Valid types: string, int, float, bool, json.");
            }
        }
        else if (colonIndex == 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable '{variable}'. Variable name is empty.");
        }
        else
        {
            name = keyWithType;
            type = "string";
        }

        object? typedValue;
        try
        {
            typedValue = type switch
            {
                "int" => int.Parse(value, CultureInfo.InvariantCulture),
                "float" => double.Parse(value, CultureInfo.InvariantCulture),
                "bool" => bool.Parse(value),
                "json" => JsonSerializer.Deserialize<object?>(value, AppJsonContext.Default.Object) ?? throw new JsonException("JSON value deserialized to null. Ensure the JSON is a valid object or array."),
                _ => value, // string
            };
        }
        catch (FormatException ex) when (type is "int" or "float" or "bool")
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid {type} value '{value}' for variable '{name}': {ex.Message}");
        }
        catch (JsonException ex) when (type == "json")
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid JSON value for variable '{name}': {ex.Message}");
        }

        variables[name] = typedValue;
        return null;
    }
}
