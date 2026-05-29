using System.Text;
using System.Text.Json;
using Spectre.Console;
using UmbracoCompose.Cli.Commands;

namespace UmbracoCompose.Cli.Services;

internal sealed class ResponseFormatter
{
    private readonly IConsole _console;

    public ResponseFormatter(IConsole console)
    {
        _console = console;
    }

    public void FormatResponse(JsonElement root, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            _console.DisplayRawText(root.ToString());
            return;
        }

        // Try to find and display an array as a table
        if (!FindAndDisplayFirstArray(root))
        {
            // Fallback: display as JSON
            _console.DisplayRawText(root.ToString());
        }
    }

    public void DisplayDataTable(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            _console.DisplayRawText(data.GetRawText());
            return;
        }

        List<JsonProperty> rootProperties = data.EnumerateObject().ToList();

        if (rootProperties.Count == 1)
        {
            // Single root field — recursively search for arrays
            JsonProperty field = rootProperties[0];
            FindAndDisplayFirstArray(field.Name, field.Value);
        }
        else
        {
            // Multiple root fields — show table
            Table table = new();
            table.AddColumn("Field");
            table.AddColumn("Type");
            table.AddColumn("Preview");
            table.Border(TableBorder.Rounded);

            foreach (var field in rootProperties)
            {
                table.AddRow(
                    field.Name.EscapeMarkup(),
                    GetValueKind(field.Value),
                    GetPreview(field.Value)
                );
            }

            _console.DisplayRenderable(table);
        }
    }

    private bool FindAndDisplayFirstArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            DisplayArrayAsTable(element);
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (FindAndDisplayFirstArray(prop.Value))
                {
                    return true; // Array found and displayed in a child — stop searching
                }
            }
        }

        // No array found at any depth — show scalar
        _console.DisplayRawText(GetPreview(element));
        return false;
    }

    private bool FindAndDisplayFirstArray(string path, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            DisplayArrayAsTable(path, element);
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var childPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                if (FindAndDisplayFirstArray(childPath, prop.Value))
                {
                    return true; // Array found and displayed in a child — stop searching
                }
            }
        }

        // No array found at any depth — show scalar
        _console.DisplayRawText($"{path}: {GetPreview(element)}");
        return false;
    }

    private void DisplayArrayAsTable(string name, JsonElement array)
    {
        int count = array.GetArrayLength();

        if (count == 0)
        {
            _console.DisplayRawText($"{name}: [] (empty array)");
            return;
        }

        // Check if items are objects to build a proper table
        JsonElement firstItem = array.EnumerateArray().FirstOrDefault();

        if (firstItem.ValueKind == JsonValueKind.Object)
        {
            // Always show objects as a table
            List<JsonProperty> sampleProps = firstItem.EnumerateObject().Take(8).ToList();

            Table table = new();
            table.AddColumn("#");
            foreach (JsonProperty prop in sampleProps)
            {
                table.AddColumn(prop.Name.EscapeMarkup());
            }
            table.Border(TableBorder.Rounded);

            foreach (JsonElement item in array.EnumerateArray())
            {
                List<string> rowValues = new List<string> { "#" + (table.Rows.Count + 1) };
                foreach (JsonProperty prop in sampleProps)
                {
                    if (item.TryGetProperty(prop.Name, out JsonElement v))
                    {
                        rowValues.Add(GetPreview(v));
                    }
                    else
                    {
                        rowValues.Add("?");
                    }
                }
                table.AddRow(rowValues.ToArray());
            }

            _console.DisplayRenderable(table);
        }
        else
        {
            // Simple array — show values
            _console.DisplayRawText($"{name}: [{string.Join(", ", array.EnumerateArray().Select(GetPreview))}]");
        }
    }

    private void DisplayArrayAsTable(JsonElement array)
    {
        int count = array.GetArrayLength();

        if (count == 0)
        {
            _console.DisplayRawText("[] (empty array)");
            return;
        }

        // Check if items are objects to build a proper table
        JsonElement firstItem = array.EnumerateArray().FirstOrDefault();

        if (firstItem.ValueKind == JsonValueKind.Object)
        {
            // Always show objects as a table
            List<JsonProperty> sampleProps = firstItem.EnumerateObject().Take(8).ToList();

            Table table = new();
            table.AddColumn("#");
            foreach (JsonProperty prop in sampleProps)
            {
                table.AddColumn(prop.Name.EscapeMarkup());
            }
            table.Border(TableBorder.Rounded);

            foreach (JsonElement item in array.EnumerateArray())
            {
                List<string> rowValues = new List<string> { "#" + (table.Rows.Count + 1) };
                foreach (JsonProperty prop in sampleProps)
                {
                    if (item.TryGetProperty(prop.Name, out JsonElement v))
                    {
                        rowValues.Add(GetPreview(v));
                    }
                    else
                    {
                        rowValues.Add("?");
                    }
                }
                table.AddRow(rowValues.ToArray());
            }

            _console.DisplayRenderable(table);
        }
        else
        {
            // Simple array — show values
            _console.DisplayRawText($"[{string.Join(", ", array.EnumerateArray().Select(GetPreview))}]");
        }
    }

    private static string GetPreview(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => EscapeJsonString(element.GetString()),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Object => $"{{...{element.GetPropertyCount()} props...}}",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            _ => element.GetRawText(),
        };
    }

    private static string EscapeJsonString(string? value)
    {
        if (value is null) return "null";
        StringBuilder sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (char c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c,
            });
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static string GetValueKind(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Null => "Null",
            JsonValueKind.Object => "Object",
            JsonValueKind.Array => "Array",
            _ => "Unknown",
        };
    }
}
