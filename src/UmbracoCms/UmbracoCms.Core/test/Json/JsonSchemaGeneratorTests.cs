using System.Text.Json;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.Json;

public sealed class JsonSchemaGeneratorTests
{
    [Fact]
    public Task Generate_String_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<string>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Integer_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<int>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Bool_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<bool>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Guid_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<Guid>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_DateTime_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<DateTime>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Empty_Class()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<EmptyClass>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Properties()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithProperties>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Nested_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithNested>(new JsonSchemaGeneratorOptions
        {
            ReferenceMode = ReferenceMode.Defs
        });

        return Verify(schema);
    }

    [Fact]
    public Task Generate_String_Array()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<string[]>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_List_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<List<string>>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Dictionary_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<Dictionary<string, int>>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Enum_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<TestValues>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Collections()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithCollections>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Collections_Defs()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithCollections>(new JsonSchemaGeneratorOptions
        {
            ReferenceMode = ReferenceMode.Defs
        });

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Collections_External()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithCollections>(new JsonSchemaGeneratorOptions
        {
            ReferenceMode = ReferenceMode.External
        });

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Nullable_Properties()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithNullable>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Guid_Property()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithGuid>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_DateTime_Properties()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithDateTime>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_With_CamelCase_Policy()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithProperties>(options);

        return Verify(schema);
    }

    [Fact]
    public Task Generate_With_Inline_Mode()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            ReferenceMode = ReferenceMode.Inline
        };

        JsonSchema schema = JsonSchemaGenerator.Generate<ClassWithNested>(options);

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Complex_Type()
    {
        JsonSchema schema = JsonSchemaGenerator.Generate<ComplexType>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_With_Handlers()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            ReferenceMode = ReferenceMode.External,
            Handlers =
            {
                new AddressHandler(),
                new AreaHandler()
            }
        };
        JsonSchemaGeneratorContext context = new(options);

        JsonSchemaGenerator.Generate<ClassWithHandledProperties>(context);

        return Verify(context.Schemas);
    }

    [Fact]
    public Task GenerateAll_Returns_Multiple_Schemas()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            ReferenceMode = ReferenceMode.External
        };
        JsonSchemaGeneratorContext context = new(options);

        JsonSchemaGenerator.Generate(typeof(ClassWithNested), context);

        return Verify(context.Schemas);
    }

    private sealed class EmptyClass;

    private sealed class ClassWithProperties
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private sealed class Address : IAddress
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public IArea Area { get; set; } = default!;
    }

    private sealed class Area : IArea
    {
        public string Country { get; set; } = default!;
    }

    private sealed class ClassWithNested
    {
        public string Name { get; set; } = null!;
        public Address? Address { get; set; }
    }

    private enum TestValues
    {
        Value1 = 0,
        Value2 = 1,
        Value3 = 2
    }

    private sealed class ClassWithCollections
    {
        public List<string> Tags { get; set; } = null!;
        public Dictionary<string, int> Metadata { get; set; } = null!;
        public string[] Names { get; set; } = null!;
        public IEnumerable<IAddress> Adresses { get; set; } = null!;
    }

    private sealed class ClassWithNullable
    {
        public string Name { get; set; } = null!;
        public string? Nickname { get; set; }
        public int? Age { get; set; }
    }

    private sealed class ClassWithGuid
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
    }

    private sealed class ClassWithDateTime
    {
        public DateTimeOffset CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateOnly BirthDate { get; set; }
        public TimeSpan Duration { get; set; }
    }

    private sealed class ComplexType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public List<string> Tags { get; set; } = null!;
        public Dictionary<string, object?> Metadata { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class ClassWithHandledProperties
    {
        public string Name { get; set; } = default!;
        public IAddress Address { get; set; } = default!;
    }

    private interface IArea
    {
        string Country { get; }
    }

    private interface IAddress
    {
        string Street { get; }
        string City { get; }
        IArea Area { get; }
    }

    private class AddressHandler : JsonSchemaTypeHandler<IAddress>
    {
        public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
            "Address";

        public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type) =>
            context.Generate<Address>();
    }

    private class AreaHandler : JsonSchemaTypeHandler<IArea>
    {
        public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
            "Area";

        public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type) =>
            JsonSchemaBuilder.Create()
                .Type(JsonPropertyType.Object)
                .Property("state", builder => builder.Type(JsonPropertyType.String))
                .Property("country", builder => builder.Type(JsonPropertyType.String))
                .Build();
    }
}
