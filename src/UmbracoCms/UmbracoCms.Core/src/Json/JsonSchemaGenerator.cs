using System.Reflection;
using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Generates JSON Schemas from .NET types using reflection to analyze type structure, properties,
/// and constraints. The generator supports multiple reference modes for handling complex type graphs,
/// custom type handlers for extending schema generation to non-standard types, and automatic detection
/// of nullable types and required properties based on C# nullability annotations. Use this class
/// when you need to automatically produce JSON Schemas that accurately represent .NET type contracts.
/// </summary>
public static class JsonSchemaGenerator
{
    private static readonly NullabilityInfoContext NullabilityInfoContext = new();

    /// <summary>
    /// Generates a JSON Schema for the specified generic type T. This convenience method infers
    /// the type from the generic parameter and delegates to the Type-based Generate method. Use
    /// this when you have a compile-time known type and want concise syntax for schema generation.
    /// </summary>
    /// <typeparam name="T">The .NET type to generate a schema for. Can be any serializable type including classes, structs, enums, and collections.</typeparam>
    /// <param name="options">Optional JsonSchemaGeneratorOptions for customizing generation behavior. If null, default options are used.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of type T</returns>
    public static JsonSchema Generate<T>(JsonSchemaGeneratorOptions? options = default) =>
        Generate(typeof(T), options);

    /// <summary>
    /// Generates a JSON Schema for the specified runtime type with optional reference mode override.
    /// This method analyzes the type structure including properties, nested types, and collections,
    /// then produces a comprehensive schema with appropriate references based on the current or
    /// specified reference mode. The generated schema validates data against the .NET type contract.
    /// </summary>
    /// <param name="type">The .NET Type object to generate a schema for. Must be a valid serializable type.</param>
    /// <param name="options">Optional JsonSchemaGeneratorOptions for customizing generation behavior. If null, default options are used.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of the specified type</returns>
    public static JsonSchema Generate(Type type, JsonSchemaGeneratorOptions? options = default) =>
        Generate(type, new JsonSchemaGeneratorContext(options));

    /// <summary>
    /// Generates a JSON Schema for the specified generic type T using the provided context. This
    /// convenience method delegates to the Type-based Generate method with context by inferring
    /// the type from the generic parameter. Use this when you have a compile-time known type and
    /// want to reuse an existing JsonSchemaGeneratorContext for generation state management.
    /// </summary>
    /// <typeparam name="T">The .NET type to generate a schema for. Can be any serializable type.</typeparam>
    /// <param name="context">The JsonSchemaGeneratorContext to use for generation state and configuration.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of type T.</returns>
    public static JsonSchema Generate<T>(JsonSchemaGeneratorContext context) =>
        Generate(typeof(T), context);

    /// <summary>
    /// Generates a JSON Schema for the specified runtime type using the provided context. This method
    /// analyzes the type structure including properties, nested types, and collections, then produces
    /// a comprehensive schema with appropriate references based on the context's configuration. The
    /// generated schema validates data against the .NET type contract and is automatically registered
    /// in the context for future reference resolution. This overload provides full control over generation
    /// state by allowing reuse of an existing context instance.
    /// </summary>
    /// <param name="type">The .NET Type object to generate a schema for. Must be a valid serializable type.</param>
    /// <param name="context">The JsonSchemaGeneratorContext to use for generation state and configuration.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type or context is null.</exception>
    public static JsonSchema Generate(Type type, JsonSchemaGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        return GenerateInternal(context, type.GetTypeInfo());
    }

    private static JsonSchema GenerateInternal(JsonSchemaGeneratorContext context, TypeInfo type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
        {
            type = underlyingType.GetTypeInfo();
        }

        JsonSchemaBuilder builder;
        if (context.Options.TypeMapping.TryGetValue(type, out Action<JsonSchemaGeneratorContext, JsonSchemaBuilder>? mapHandler))
        {
            builder = context.CreateBuilder(JsonPropertyType.Object);
            mapHandler(context, builder);
            return builder.Build();
        }

        if (context.TryGetSchema(type, out JsonSchema? schema))
        {
            return schema;
        }

        string typeName = context.GetTypeName(type);
        if (context.TryGetSchema(type, out schema))
        {
            return schema;
        }

        if (context.CurrentHandling != type && context.TryFindHandler(type, out IJsonSchemaTypeHandler? handler))
        {
            if (context.TryGetSchema(type, out schema))
            {
                return schema;
            }

            context.CurrentHandling = type;
            schema = handler.Handle(context, type);
            schema.TypeName = typeName;
            context.TryRegisterSchema(schema, type, typeName);
            context.CurrentHandling = null;

            return schema;
        }

        if (type.IsEnum)
        {
            return HandleEnumInternal(context, type);
        }

        if (TryGetIEnumerableInterface(type) is { } enumerableInterface)
        {
            TypeInfo itemType = (type.GetElementType() ?? enumerableInterface.GenericTypeArguments[0]).GetTypeInfo();

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return HandleDictionaryInternal(context, itemType.GetTypeInfo());
            }

            return context.CreateBuilder(JsonPropertyType.Array)
                .Items(GenerateInternal(context, itemType))
                .Build();
        }

        builder = context.CreateBuilder(JsonPropertyType.Object);

        HandleObjectInternal(context, type, builder);

        return builder.Build();
    }

    private static void HandleObjectInternal(JsonSchemaGeneratorContext context, TypeInfo type, JsonSchemaBuilder builder)
    {
        string typeName = context.GetTypeName(type);
        builder.JsonSchema.TypeName = typeName;
        builder.JsonSchema.ClrType = type;

        builder.Type(JsonPropertyType.Object);

        if (type == typeof(Object))
        {
            return;
        }

        builder
            .Title(typeName)
            .TypeName(typeName)
            .CustomKeyword("csharpType", type.ToString());

        if (type.IsAbstract || type.IsInterface)
        {
            builder.CustomKeyword("x-abstract", true);
        }

        foreach (PropertyInfo property in type.DeclaredProperties)
        {
            TypeInfo propertyType = property.PropertyType.GetTypeInfo();
            if (propertyType.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                continue;
            }

            string propertyName;
            if (propertyType.GetCustomAttribute<JsonPropertyNameAttribute>() is { } propertyNameAttribute)
            {
                propertyName = propertyNameAttribute.Name;
            }
            else
            {
                propertyName = context.Options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
            }

            if (NullabilityInfoContext.Create(property).WriteState is not NullabilityState.Nullable)
            {
                builder.Required(propertyName);
            }

            JsonSchema propertySchema = GenerateInternal(context, propertyType);
            if (propertySchema.Type is JsonPropertyType.Array or JsonPropertyType.Object
                && context.Options.ReferenceMode is ReferenceMode.Defs or ReferenceMode.External)
            {
                string? GetPath()
                {
                    string? propertyTypeName = propertySchema.Type switch
                    {
                        JsonPropertyType.Array when propertySchema.Items is not null => propertySchema.Items.TypeName,
                        JsonPropertyType.Object => propertySchema.TypeName,
                        _ => null
                    };

                    if (propertyTypeName is null)
                    {
                        return null;
                    }

                    return context.Options.ReferenceMode switch
                    {
                        ReferenceMode.Defs => $"#/defs/{propertyTypeName}",
                        ReferenceMode.External => $"./{propertyTypeName}",
                        _ => throw new InvalidOperationException("unreachable")
                    };
                }

                string? path = GetPath();

                if (propertySchema.Type is JsonPropertyType.Array && path is not null)
                {
                    builder.Property(propertyName, builder => builder.Type(JsonPropertyType.Array).Items(builder => builder.Ref(path)));
                }
                else if (propertySchema.Type is JsonPropertyType.Object && path is not null)
                {
                    builder.Property(propertyName, builder => builder.Type(JsonPropertyType.Object).Ref(path));
                }
                else
                {
                    builder.Property(propertyName, propertySchema);
                }
            }
            else
            {
                builder.Property(propertyName, propertySchema);
            }
        }

        context.TryRegisterSchema(builder.JsonSchema, type, typeName);
    }

    private static JsonSchema HandleEnumInternal(JsonSchemaGeneratorContext context, TypeInfo type) =>
       context
           .CreateBuilder(JsonPropertyType.String)
           .Enum([.. type.GetEnumValues()])
           .Build();

    private static JsonSchema HandleDictionaryInternal(JsonSchemaGeneratorContext context, TypeInfo type) =>
       context
           .CreateBuilder(JsonPropertyType.Object)
           .AdditionalProperties(GenerateInternal(context, type.GenericTypeArguments[1].GetTypeInfo()))
           .Build();

    private static TypeInfo? TryGetIEnumerableInterface(TypeInfo type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type;
        }

        return type.ImplementedInterfaces
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
            .GetTypeInfo();
    }
}
