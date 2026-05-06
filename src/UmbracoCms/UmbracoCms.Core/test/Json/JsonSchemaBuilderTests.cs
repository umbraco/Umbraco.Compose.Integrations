using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.Json;

public sealed class JsonSchemaBuilderTests
{
    [Fact]
    public Task Simple_String_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create().Type(JsonPropertyType.String).Build();

        return Verify(schema);
    }

    [Fact]
    public Task Simple_Integer_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create().Type(JsonPropertyType.Integer).Build();

        return Verify(schema);
    }

    [Fact]
    public Task Object_With_Properties_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Object)
            .Title("User")
            .Description("A user entity")
            .Required("id", "name")
            .Property("id", _ => _.Type(JsonPropertyType.Integer).Minimum(1))
            .Property("name", _ => _.Type(JsonPropertyType.String).MinLength(1).MaxLength(100))
            .Property("email", _ => _.Type(JsonPropertyType.String).Format("email"))
            .AdditionalProperties(false)
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Array_Of_Items_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Array)
            .Title("Item List")
            .MinItems(1)
            .MaxItems(100)
            .UniqueItems(true)
            .Items(_ => _.Type(JsonPropertyType.String))
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Object_With_Nested_Object_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Object)
            .Title("Address")
            .Required("street", "city")
            .Property("street", _ => _.Type(JsonPropertyType.String))
            .Property("city", _ => _.Type(JsonPropertyType.String))
            .Property("postalCode", _ => _.Type(JsonPropertyType.String).Pattern(@"^\d{5}$"))
            .AdditionalProperties(false)
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Union_Type_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .OneOf(
                _ => _.Type(JsonPropertyType.String),
                _ => _.Type(JsonPropertyType.Integer)
            )
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Intersection_Type_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .AllOf(
                _ => _.Type(JsonPropertyType.Object),
                _ => _.Required("name"),
                _ => _.Property("name", _ => _.Type(JsonPropertyType.String))
            )
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Conditional_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .IfThenElse(
                ifSchema: _ => _.Property("type", new JsonSchema { Const = "special" }),
                thenSchema: _ => _.Type(JsonPropertyType.String),
                elseSchema: _ => _.Type(JsonPropertyType.Integer)
            )
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Schema_With_Definitions()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Object)
            .Def("Address", _ => _.Type(JsonPropertyType.Object).Required("street", "city"))
            .Def("Name", _ => _.Type(JsonPropertyType.String).MinLength(1))
            .Property("address", new JsonSchema { Ref = "#/$defs/Address" })
            .Property("name", new JsonSchema { Ref = "#/$defs/Name" })
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Complex_Nested_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Title("ComplexType")
            .Description("A complex nested type example")
            .Id("https://example.com/schemas/complex-type")
            .Type(JsonPropertyType.Object)
            .Required("id", "name", "tags")
            .Property("id", _ => _.Type(JsonPropertyType.Integer).Minimum(1))
            .Property("name", _ => _.Type(JsonPropertyType.String).MinLength(1).MaxLength(255))
            .Property("age", _ => _.Type(JsonPropertyType.Integer).Minimum(0).Maximum(150))
            .Property("isActive", _ => _.Type(JsonPropertyType.Boolean))
            .Property("tags", _ => _.Type(JsonPropertyType.Array).MinItems(0).UniqueItems(true).Items(_ => _.Type(JsonPropertyType.String)))
            .Property("metadata", _ => _.Type(JsonPropertyType.Object).AdditionalProperties(true))
            .Def("Tag", _ => _.Type(JsonPropertyType.String).MinLength(1))
            .CustomKeyword("x-version", "1.0.0")
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Tuple_Array_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Array)
            .PrefixItems(
                _ => _.Type(JsonPropertyType.String),
                _ => _.Type(JsonPropertyType.Integer),
                _ => _.Type(JsonPropertyType.Boolean)
            )
            .MinItems(3)
            .MaxItems(3)
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Pattern_Properties_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Object)
            .Required("name")
            .Property("name", _ => _.Type(JsonPropertyType.String))
            .PatternProperty("^[a-z]+$", _ => _.Type(JsonPropertyType.Integer))
            .MinProperties(1)
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Schema_With_Discriminator()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Object)
            .Required("type")
            .Property("type", _ => _.Enum("special", "regular"))
            .Discriminator(
                "type",
                new()
                {
                    ["special"] = "#/$defs/SpecialType",
                    ["regular"] = "#/$defs/RegularType"
                })
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Numeric_Constraints_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Number)
            .MultipleOf(0.01)
            .Minimum(0.0)
            .Maximum(100.0)
            .ExclusiveMinimum(0.0)
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task String_Constraints_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.String)
            .MinLength(8)
            .MaxLength(128)
            .Pattern("^[a-zA-Z0-9]+$")
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Array_Contains_Constraint_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.Array)
            .MinItems(1)
            .Contains(_ => _.Type(JsonPropertyType.String).MinLength(5))
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Not_Constraint_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Type(JsonPropertyType.String)
            .Not(_ => _.Const("forbidden"))
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task AnyOf_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .AnyOf(
                _ => _.Type(JsonPropertyType.String),
                _ => _.Type(JsonPropertyType.Integer),
                _ => _.Const(null)
            )
            .Build();

        return Verify(schema);
    }

    [Fact]
    public Task Full_Complex_Object_Schema()
    {
        JsonSchema schema = JsonSchemaBuilder.Create()
            .Title("Product")
            .Description("A product in the catalog")
            .Type(JsonPropertyType.Object)
            .Required("id", "name", "price")
            .MinProperties(2)
            .MaxProperties(10)
            .Property("id", _ => _.Type(JsonPropertyType.String).MinLength(1))
            .Property("name", _ => _.Type(JsonPropertyType.String).MinLength(1).MaxLength(200))
            .Property("description", _ => _.Type(JsonPropertyType.String))
            .Property("price", _ => _.Type(JsonPropertyType.Number).Minimum(0).MultipleOf(0.01))
            .Property("inStock", _ => _.Type(JsonPropertyType.Boolean))
            .Property("tags", _ => _.Type(JsonPropertyType.Array).MinItems(0).UniqueItems(true).Items(_ => _.Type(JsonPropertyType.String)))
            .Property("metadata", _ => _.AdditionalProperties(_ => _.Type(JsonPropertyType.Object)))
            .Def(
                "PriceRange",
                _ => _.Type(JsonPropertyType.Object).Property("min", _ => _.Type(JsonPropertyType.Number))
                    .Property("max", _ => _.Type(JsonPropertyType.Number)))
            .CustomKeyword("x-internal", false)
            .Build();

        return Verify(schema);
    }
}
