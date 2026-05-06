using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.Json;

public sealed class JsonSchemaTests
{
    [Fact]
    public void Create_Returns_New_Schema()
    {
        JsonSchema schema = JsonSchema.Create();

        Assert.NotNull(schema);
        Assert.IsType<JsonSchema>(schema);
    }

    [Fact]
    public void AddExtension_Adds_Extension()
    {
        JsonSchema schema = new();
        JsonSchema result = schema.AddExtension("x-custom", "value");

        Assert.NotNull(schema.Extensions);
        Assert.True(schema.Extensions.ContainsKey("x-custom"));
        Assert.Equal("value", schema.Extensions["x-custom"]);
        Assert.Same(schema, result);
    }

    [Fact]
    public void AddExtension_Initializes_Dictionary_If_Null()
    {
        JsonSchema schema = new();
        Assert.Null(schema.Extensions);

        schema.AddExtension("x-test", "test-value");

        Assert.NotNull(schema.Extensions);
    }

    [Fact]
    public void AddExtension_Allows_Null_Value()
    {
        JsonSchema schema = new();
        schema.AddExtension("x-null", null);

        Assert.NotNull(schema.Extensions);
        Assert.True(schema.Extensions.ContainsKey("x-null"));
        Assert.Null(schema.Extensions["x-null"]);
    }

    [Fact]
    public void AddExtension_Allows_Nested_Object()
    {
        JsonSchema schema = new();
        JsonObject nested = new()
        {
            ["key"] = "value"
        };

        schema.AddExtension("x-config", nested);

        Assert.NotNull(schema.Extensions);
        Assert.Equal(nested, schema.Extensions["x-config"]);
    }

    [Fact]
    public void AddExtension_Allows_JsonSchema_Object()
    {
        JsonSchema schema = new();
        JsonSchema nestedSchema = new() { Type = JsonPropertyType.String };

        schema.AddExtension("x-schema", nestedSchema);

        Assert.NotNull(schema.Extensions);
        Assert.Equal(nestedSchema, schema.Extensions["x-schema"]);
    }

    [Fact]
    public void All_Properties_Are_Settable()
    {
        JsonSchema schema = new()
        {
            Schema = "https://json-schema.org/draft/2020-12/schema",
            Id = "https://example.com/schema",
            Anchor = "myAnchor",
            Ref = "#/defs/MyType",
            Title = "My Schema",
            Description = "A test schema",
            Type = JsonPropertyType.String,
            Format = "email",
            Enum = ["option1", "option2"],
            Const = "fixed",
            MultipleOf = 10.0,
            Maximum = 100.0,
            ExclusiveMaximum = 50.0,
            Minimum = 0.0,
            ExclusiveMinimum = 5.0,
            MaxLength = 100,
            MinLength = 1,
            Pattern = "^[a-z]+$",
            MaxItems = 10,
            MinItems = 1,
            UniqueItems = true,
            Contains = new() { Type = JsonPropertyType.String },
            MaxProperties = 5,
            MinProperties = 1,
            Required = ["prop1"],
            Properties = new() { { "name", new JsonSchema { Type = JsonPropertyType.String } } },
            PatternProperties = new() { { "^[a-z]+$", new JsonSchema { Type = JsonPropertyType.String } } },
            AdditionalProperties = new() { Type = JsonPropertyType.Integer },
            Items = new() { Type = JsonPropertyType.String },
            PrefixItems = [new() { Type = JsonPropertyType.String }, new() { Type = JsonPropertyType.Integer }],
            AllOf = [new() { Type = JsonPropertyType.Object }],
            AnyOf = [new() { Type = JsonPropertyType.String }, new() { Type = JsonPropertyType.Integer }],
            OneOf = [new() { Type = JsonPropertyType.Boolean }],
            Not = new() { Const = "forbidden" },
            If = JsonSchemaBuilder.Create().Property("type", JsonSchemaBuilder.Create().Const("special").Build()).Build(),
            Then = new() { Type = JsonPropertyType.String },
            Else = new() { Type = JsonPropertyType.Integer }
        };

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema.Schema);
        Assert.Equal("https://example.com/schema", schema.Id);
        Assert.Equal("myAnchor", schema.Anchor);
        Assert.Equal("#/defs/MyType", schema.Ref);
        Assert.Equal("My Schema", schema.Title);
        Assert.Equal("A test schema", schema.Description);
        Assert.Equal(JsonPropertyType.String, schema.Type);
        Assert.Equal("email", schema.Format);
        Assert.NotNull(schema.Enum);
        Assert.Equal("fixed", schema.Const);
        Assert.Equal(10.0, schema.MultipleOf);
        Assert.Equal(100.0, schema.Maximum);
        Assert.Equal(50.0, schema.ExclusiveMaximum);
        Assert.Equal(0.0, schema.Minimum);
        Assert.Equal(5.0, schema.ExclusiveMinimum);
        Assert.Equal(100, schema.MaxLength);
        Assert.Equal(1, schema.MinLength);
        Assert.Equal("^[a-z]+$", schema.Pattern);
        Assert.Equal(10, schema.MaxItems);
        Assert.Equal(1, schema.MinItems);
        Assert.True(schema.UniqueItems.Value);
        Assert.NotNull(schema.Contains);
        Assert.Equal(5, schema.MaxProperties);
        Assert.Equal(1, schema.MinProperties);
        Assert.NotNull(schema.Required);
        Assert.NotNull(schema.Properties);
        Assert.NotNull(schema.PatternProperties);
        Assert.NotNull(schema.AdditionalProperties);
        Assert.NotNull(schema.Items);
        Assert.NotNull(schema.PrefixItems);
        Assert.Equal(2, schema.PrefixItems.Count);
        Assert.NotNull(schema.AllOf);
        Assert.NotNull(schema.AnyOf);
        Assert.Equal(2, schema.AnyOf.Count);
        Assert.NotNull(schema.OneOf);
        Assert.NotNull(schema.Not);
        Assert.NotNull(schema.If);
        Assert.NotNull(schema.Then);
        Assert.NotNull(schema.Else);
    }

    [Fact]
    public void Default_Constructor_Sets_Null_Values()
    {
        JsonSchema schema = new();

        Assert.Null(schema.Schema);
        Assert.Null(schema.Id);
        Assert.Null(schema.Anchor);
        Assert.Null(schema.Ref);
        Assert.Null(schema.Title);
        Assert.Null(schema.Description);
        Assert.Null(schema.Type);
        Assert.Null(schema.Format);
        Assert.Null(schema.Enum);
        Assert.Null(schema.Const);
        Assert.Null(schema.MultipleOf);
        Assert.Null(schema.Maximum);
        Assert.Null(schema.ExclusiveMaximum);
        Assert.Null(schema.Minimum);
        Assert.Null(schema.ExclusiveMinimum);
        Assert.Null(schema.MaxLength);
        Assert.Null(schema.MinLength);
        Assert.Null(schema.Pattern);
        Assert.Null(schema.MaxItems);
        Assert.Null(schema.MinItems);
        Assert.Null(schema.UniqueItems);
        Assert.Null(schema.Contains);
        Assert.Null(schema.MaxProperties);
        Assert.Null(schema.MinProperties);
        Assert.Null(schema.Required);
        Assert.Null(schema.Properties);
        Assert.Null(schema.PatternProperties);
        Assert.Null(schema.AdditionalProperties);
        Assert.Null(schema.Items);
        Assert.Null(schema.PrefixItems);
        Assert.Null(schema.AllOf);
        Assert.Null(schema.AnyOf);
        Assert.Null(schema.OneOf);
        Assert.Null(schema.Not);
        Assert.Null(schema.If);
        Assert.Null(schema.Then);
        Assert.Null(schema.Else);
        Assert.Null(schema.Extensions);
    }

    [Fact]
    public void Serialize_With_SystemTextJson()
    {
        JsonSchema schema = new()
        {
            Type = JsonPropertyType.Object,
            Title = "Test",
            Description = "A test schema",
            Required = ["name"],
            Properties = new()
            {
                ["name"] = new()
                { Type = JsonPropertyType.String }
            },
            AdditionalProperties = new()
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"type\":\"object\"", json);
        Assert.Contains("\"title\":\"Test\"", json);
        Assert.Contains("\"description\":\"A test schema\"", json);
    }

    [Fact]
    public void Serialize_Deserialize_Lossless()
    {
        JsonSchema original = new()
        {
            Type = JsonPropertyType.Object,
            Title = "User",
            Description = "A user entity",
            Required = ["id", "name"],
            Properties = new()
            {
                ["id"] = new()
                { Type = JsonPropertyType.Integer },
                ["name"] = new()
                { Type = JsonPropertyType.String }
            },
            AdditionalProperties = new()
        };

        string json = JsonSerializer.Serialize(original);
        JsonSchema restored = JsonSerializer.Deserialize<JsonSchema>(json) ?? throw new InvalidOperationException();

        Assert.Equal(original.Type, restored.Type);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.Description, restored.Description);
        Assert.NotNull(restored.Required);
        Assert.NotNull(restored.Properties);
        Assert.Equal(original.Required.Count, restored.Required.Count);
    }

    [Fact]
    public void Serialize_Nested_Schema()
    {
        JsonSchema schema = new()
        {
            Type = JsonPropertyType.Object,
            Properties = new()
            {
                ["nested"] = new()
                {
                    Type = JsonPropertyType.Object,
                    Properties = new()
                    {
                        ["value"] = new()
                        { Type = JsonPropertyType.Integer }
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"nested\"", json);
        Assert.Contains("\"value\"", json);
    }

    [Fact]
    public void Serialize_Definitions()
    {
        JsonSchema schema = new()
        {
            Type = JsonPropertyType.Object,
            Defs = new()
            {
                ["Address"] = new()
                {
                    Type = JsonPropertyType.Object,
                    Properties = new()
                    {
                        ["street"] = new()
                        { Type = JsonPropertyType.String }
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"$defs\"", json);
        Assert.Contains("\"Address\"", json);
    }

    [Fact]
    public void Serialize_Composition_Schemas()
    {
        JsonSchema schema = new()
        {
            OneOf =
            [
                new JsonSchema { Type = JsonPropertyType.String },
                new JsonSchema { Type = JsonPropertyType.Integer }
            ]
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"oneOf\"", json);
        Assert.Contains("\"string\"", json);
        Assert.Contains("\"integer\"", json);
    }

    [Fact]
    public void Serialize_Discriminator()
    {
        JsonSchema schema = new()
        {
            Type = JsonPropertyType.Object,
            Discriminator = new()
            {
                PropertyName = "type",
                Mapping = new()
                {
                    ["special"] = "#/$defs/SpecialType"
                }
            }
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"discriminator\"", json);
        Assert.Contains("\"propertyName\"", json);
    }

    [Fact]
    public void Serialize_IfThenElse()
    {
        JsonSchema schema = new()
        {
            If = JsonSchemaBuilder.Create().Property("type", JsonSchemaBuilder.Create().Const("special").Build()).Build(),
            Then = new()
            { Type = JsonPropertyType.String },
            Else = new()
            { Type = JsonPropertyType.Integer }
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"if\"", json);
        Assert.Contains("\"then\"", json);
        Assert.Contains("\"else\"", json);
    }

    [Fact]
    public void Serialize_Extensions()
    {
        JsonSchema schema = new();
        schema.AddExtension("x-custom", "value")
            .AddExtension("x-number", 42);

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"x-custom\"", json);
        Assert.Contains("\"x-number\"", json);
    }

    [Fact]
    public void Serialize_Nullable_Properties_Are_Omitted()
    {
        JsonSchema schema = new();

        string json = JsonSerializer.Serialize(schema);

        Assert.DoesNotContain("\"type\"", json);
        Assert.DoesNotContain("\"title\"", json);
    }

    [Fact]
    public void Serialize_Type_As_String()
    {
        JsonSchema schema = new()
        {
            Type = JsonPropertyType.String
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"type\":\"string\"", json);
    }

    [Fact]
    public void Serialize_Enum_As_Array()
    {
        JsonSchema schema = new()
        {
            Enum = ["option1", "option2", "option3"]
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"enum\"", json);
        Assert.Contains("\"option1\"", json);
    }

    [Fact]
    public void Serialize_Const_Single_Value()
    {
        JsonSchema schema = new()
        {
            Const = "fixedValue"
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"const\":\"fixedValue\"", json);
    }

    [Fact]
    public void Serialize_Bool_Values_As_Booleans()
    {
        JsonSchema schema = new()
        {
            UniqueItems = true,
            AdditionalProperties = new()
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"uniqueItems\":true", json);
        Assert.Contains("\"additionalProperties\":{}", json);
    }

    [Fact]
    public void Serialize_Nullable_Bool_As_Omitted_When_Null()
    {
        JsonSchema schema = new()
        {
            UniqueItems = null
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.DoesNotContain("\"uniqueItems\"", json);
    }

    [Fact]
    public void Serialize_Number_Values_As_Numbers()
    {
        JsonSchema schema = new()
        {
            MultipleOf = 10.5,
            Maximum = 100.0,
            Minimum = 0.0,
            MinLength = 5,
            MaxLength = 100
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"multipleOf\":10.5", json);
        Assert.Contains("\"maximum\":100", json);
        Assert.Contains("\"minimum\":0", json);
    }

    [Fact]
    public void Serialize_PrefixItems_As_Array()
    {
        JsonSchema schema = new()
        {
            PrefixItems =
            [
                new JsonSchema { Type = JsonPropertyType.String },
                new JsonSchema { Type = JsonPropertyType.Integer }
            ]
        };

        string json = JsonSerializer.Serialize(schema);

        Assert.Contains("\"prefixItems\"", json);
    }
}
