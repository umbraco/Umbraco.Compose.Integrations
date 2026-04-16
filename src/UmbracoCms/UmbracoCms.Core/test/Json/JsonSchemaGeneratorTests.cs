using System.Text.Json;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.Json;

public sealed class JsonSchemaGeneratorTests
{
    [Fact]
    public Task Generate_String_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<string>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Integer_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<int>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Bool_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<bool>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Guid_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<Guid>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_DateTime_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<DateTime>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Empty_Class()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<EmptyClass>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Properties()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithProperties>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Nested_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithNested>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_String_Array()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<string[]>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_List_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<List<string>>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Dictionary_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<Dictionary<string, int>>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Enum_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<TestValues>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Collections()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithCollections>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Nullable_Properties()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithNullable>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_Guid_Property()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithGuid>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Class_With_DateTime_Properties()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ClassWithDateTime>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_With_CamelCase_Policy()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        JsonSchemaGenerator generator = new(options);
        JsonSchema schema = generator.Generate<ClassWithProperties>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_With_Inline_Mode()
    {
        JsonSchemaGeneratorOptions options = new()
        {
            ReferenceMode = ReferenceMode.Inline
        };

        JsonSchemaGenerator generator = new(options);
        JsonSchema schema = generator.Generate<ClassWithNested>();

        return Verify(schema);
    }

    [Fact]
    public Task Generate_Complex_Type()
    {
        JsonSchemaGenerator generator = new();
        JsonSchema schema = generator.Generate<ComplexType>();

        return Verify(schema);
    }

    [Fact]
    public Task GenerateAll_Returns_Multiple_Schemas()
    {
        JsonSchemaGenerator generator = new();
        Dictionary<string, JsonSchema> schemas = generator.GenerateAll(typeof(ClassWithNested));

        return Verify(schemas);
    }

    private sealed class EmptyClass;

    private sealed class ClassWithProperties
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private sealed class Address
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
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
}
