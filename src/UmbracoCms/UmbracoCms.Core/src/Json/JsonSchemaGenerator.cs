using System.Reflection;
using System.Text.RegularExpressions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Generates JSON Schemas from .NET types using reflection to analyze type structure, properties,
/// and constraints. The generator supports multiple reference modes for handling complex type graphs,
/// custom type handlers for extending schema generation to non-standard types, and automatic detection
/// of nullable types and required properties based on C# nullability annotations. Use this class
/// when you need to automatically produce JSON Schemas that accurately represent .NET type contracts.
/// </summary>
/// <remarks>
/// <para>
/// Thread Safety: This class is not thread-safe. Each thread should use a separate instance of JsonSchemaGenerator,
/// or external synchronization must be applied to Generate() calls. The generator maintains internal state (type definitions,
/// processed types, handler schemas) that is cleared and rebuilt on each generation call. Concurrent access to the same
/// instance will result in race conditions and corrupted schema output. For high-concurrency scenarios, consider using
/// a pool of JsonSchemaGenerator instances or creating new instances per request.
/// </para>
/// <para>
/// Performance: The generator caches PropertyInfo arrays per type to avoid repeated reflection calls. This cache persists
/// across Generate() calls but may grow unbounded in long-running applications that generate schemas for many different types.
/// For applications generating schemas for thousands of types, call ClearCache() periodically or create a new JsonSchemaGenerator
/// instance to reset the cache and prevent memory leaks.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new JsonSmechemaGenerator instance with optional custom configuration. When no
/// options are provided, default settings are used including Defs reference mode and an empty
/// handler list. Custom options allow configuring reference behavior and registering type handlers.
/// </remarks>
/// <param name="options">Optional JsonSchemaGeneratorOptions for customizing generation behavior. If null, default options are used.</param>
public sealed partial class JsonSchemaGenerator(JsonSchemaGeneratorOptions? options = null)
{
    private const string OptionalAttributeName = "OptionalAttribute";
    private const string CanBeNullAttributeName = "CanBeNullAttribute";

    /// <summary>
    /// Cache of types to their generated names for use in references and definitions. This dictionary
    /// is populated during type collection and reused throughout schema generation to ensure consistent
    /// naming across all references to the same type.
    /// </summary>
    private readonly Dictionary<Type, string> _typeDefinitions = [];

    /// <summary>
    /// Cache of types to their pre-generated schemas from custom handlers. This allows reusing
    /// handler-generated schemas across multiple references to the same type without regenerating.
    /// </summary>
    private readonly Dictionary<Type, JsonSchema> _handlerSchemas = [];

    /// <summary>
    /// Set of types that have been fully processed into schemas. This prevents infinite recursion
    /// when processing circular type references and ensures each type is only generated once.
    /// </summary>
    private readonly HashSet<Type> _processedTypes = [];

    /// <summary>
    /// Set of types that were skipped during collection, typically because custom handlers returned
    /// null schemas. These types are tracked to avoid attempting regeneration and provide debugging
    /// information about which types couldn't be schema-converted.
    /// </summary>
    private readonly HashSet<Type> _skippedTypes = [];

    /// <summary>
    /// Cache for reflection results to improve performance. Stores PropertyInfo arrays keyed by type
    /// to avoid repeatedly calling GetProperties() on the same types during schema generation.
    /// </summary>
    private readonly Dictionary<Type, PropertyInfo[]> _propertyCache = [];

    /// <summary>
    /// Current reference mode being used for schema generation. This is updated during generation
    /// to match the selected mode and determines how type references are resolved and expressed.
    /// </summary>
    private ReferenceMode _currentMode;

    /// <summary>
    /// Generator configuration options including reference mode and custom type handlers. These
    /// options control the behavior of schema generation and can be customized for specific use cases.
    /// </summary>
    private readonly JsonSchemaGeneratorOptions _options = options ?? new();

    /// <summary>
    /// Clears the internal property cache to free memory. This method should be called periodically
    /// in long-running applications that generate schemas for many different types to prevent unbounded
    /// memory growth. The cache stores PropertyInfo arrays from reflection, and while this improves
    /// performance, it can consume significant memory over time. For most use cases, this is not
    /// necessary as the cache is bounded by the number of unique types encountered.
    /// </summary>
    public void ClearCache()
    {
        _propertyCache.Clear();
    }

    /// <summary>
    /// Generates a JSON Schema for the specified generic type T. This convenience method infers
    /// the type from the generic parameter and delegates to the Type-based Generate method. Use
    /// this when you have a compile-time known type and want concise syntax for schema generation.
    /// </summary>
    /// <typeparam name="T">The .NET type to generate a schema for. Can be any serializable type including classes, structs, enums, and collections.</typeparam>
    /// <returns>A JsonSchema instance representing the structure and constraints of type T</returns>
    public JsonSchema Generate<T>() =>
        Generate(typeof(T));

    /// <summary>
    /// Generates a JSON Schema for the specified runtime type with optional reference mode override.
    /// This method analyzes the type structure including properties, nested types, and collections,
    /// then produces a comprehensive schema with appropriate references based on the current or
    /// specified reference mode. The generated schema validates data against the .NET type contract.
    /// </summary>
    /// <param name="type">The .NET Type object to generate a schema for. Must be a valid serializable type.</param>
    /// <param name="mode">Optional ReferenceMode override for this generation. If null, uses the mode from generator options.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of the specified type</returns>
    public JsonSchema Generate(Type type, ReferenceMode? mode = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        _currentMode = mode ?? _options.ReferenceMode;
        _options.ReferenceMode = _currentMode;
        _typeDefinitions.Clear();
        _handlerSchemas.Clear();
        _skippedTypes.Clear();

        CollectAllTypes(type);

        _processedTypes.Clear();
        foreach (Type t in _typeDefinitions.Keys.Where(t => t != type))
        {
            _processedTypes.Add(t);
        }

        JsonSchema schema = GenerateSchemaWithRefs(type);

        if (_currentMode == ReferenceMode.Defs)
        {
            Dictionary<string, JsonSchema> defsSchemas = GenerateDefsSchemas(type);
            if (defsSchemas.Count > 0)
            {
                schema.Defs = defsSchemas;
            }
        }

        return schema;
    }

    /// <summary>
    /// Generates JSON Schemas for all types reachable from the specified root type. This method
    /// produces a complete set of schemas where each type is represented as a separate entry in
    /// the returned dictionary, keyed by its type name. Use this when you need to generate
    /// multiple related schemas for cross-referencing or external schema storage.
    /// </summary>
    /// <param name="type">The root .NET Type to start collection from. All reachable types will be included.</param>
    /// <param name="schema">The JSON Schema version URI</param>
    /// <returns>A dictionary mapping type names to their corresponding JsonSchema instances. Each key is a unique type name, and each value is the generated schema for that type.</returns>
    public Dictionary<string, JsonSchema> GenerateAll(Type type, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        return GenerateAllInternal(type, schema);
    }

    /// <summary>
    /// Internal implementation of GenerateAll that produces schemas for all collected types in
    /// external reference mode. Each type is generated with its own ID and references to other
    /// types are expressed as external URIs. This method handles the complex logic of generating
    /// multiple interrelated schemas while maintaining proper reference resolution.
    /// </summary>
    /// <param name="type">The root type to start collection from</param>
    /// <param name="schema">The JSON Schema version URI</param>
    /// <returns>A dictionary mapping type names to their generated schemas</returns>
    private Dictionary<string, JsonSchema> GenerateAllInternal(Type type, string? schema = null)
    {
        _typeDefinitions.Clear();

        CollectAllTypes(type);

        Dictionary<string, JsonSchema> result = [];

        foreach (KeyValuePair<Type, string> kvp in _typeDefinitions)
        {
            string typeName = kvp.Value;

            _processedTypes.Clear();
            foreach (Type t in _typeDefinitions.Keys)
            {
                if (t != kvp.Key)
                {
                    _processedTypes.Add(t);
                }
            }

            _currentMode = ReferenceMode.External;
            JsonSchema jsonSchema = GenerateSchemaInternal(kvp.Key, schema, withRefs: true);
            jsonSchema.Id = typeName;
            result[typeName] = jsonSchema;
        }

        return result;
    }

    /// <summary>
    /// Collects all types reachable from the specified root type, building a complete map of
    /// types and their relationships. This method initiates recursive type collection to discover
    /// nested types, generic arguments, array elements, and property types. The collected types
    /// are stored in _typeDefinitions for later schema generation.
    /// </summary>
    /// <param name="type">The root type to start collection from. All reachable types will be added to the type definitions cache.</param>
    private void CollectAllTypes(Type type)
    {
        CollectTypesRecursive(type);
    }

    /// <summary>
    /// Searches through registered custom handlers to find one that can handle the specified type.
    /// Handlers are checked in registration order, and the first handler that returns true from
    /// CanHandle is used for schema generation. This enables extensibility for non-standard types.
    /// </summary>
    /// <param name="type">The Type to find a handler for</param>
    /// <returns>The first IJsonSchemaTypeHandler that can handle the type, or null if no registered handler supports it</returns>
    private IJsonSchemaTypeHandler? FindHandler(Type type) =>
        _options.Handlers.FirstOrDefault(x => x.CanHandle(type));

    private void CollectTypesRecursive(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is not null)
        {
            Type? underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is not null)
            {
                CollectTypesRecursive(underlyingType);
                return;
            }
        }

        if (type.IsEnum || _options.PrimitiveTypeHandlers.ContainsKey(type) || type == typeof(object))
        {
            return;
        }

        // TODO: use ITypeNameGenerator class instead
        string typeName = GetTypeName(type);
        if (string.IsNullOrEmpty(typeName))
        {
            return;
        }

        IJsonSchemaTypeHandler? handler = FindHandler(type);
        if (handler is not null)
        {
            JsonSchema? handledSchema = handler.Handle(type);
            if (handledSchema is null)
            {
                _skippedTypes.Add(type);
            }
            else
            {
                _typeDefinitions[type] = typeName;
                _handlerSchemas[type] = handledSchema;
            }
            return;
        }

        if (_typeDefinitions.ContainsKey(type))
        {
            return;
        }

        if (type.IsGenericType)
        {
            foreach (Type arg in type.GetGenericArguments())
            {
                CollectTypesRecursive(arg);
            }
            return;
        }

        if (type.IsArray)
        {
            CollectTypesRecursive(type.GetElementType()!);
            return;
        }

        _typeDefinitions[type] = typeName;

        if (type.IsClass)
        {
            foreach (PropertyInfo prop in GetProperties(type))
            {
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                CollectTypesRecursive(prop.PropertyType);
            }
        }

        if (type.IsInterface)
        {
            foreach (PropertyInfo prop in GetProperties(type))
            {
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                CollectTypesRecursive(prop.PropertyType);
            }
        }
    }

    private JsonSchema BuildItemSchema(Type elementType, bool withRefs)
    {
        if (withRefs
            && _typeDefinitions.ContainsKey(elementType)
            && _processedTypes.Contains(elementType)
            && !_options.PrimitiveTypeHandlers.ContainsKey(elementType))
        {
            return GetReferenceOrGenerate(elementType);
        }

        return GenerateSchemaInternal(elementType, null, withRefs);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (GetDictionaryValueType(type) is not null)
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        Type genericType = type.GetGenericTypeDefinition();

        if (genericType == typeof(List<>)
            || genericType == typeof(IList<>)
            || genericType == typeof(ICollection<>)
            || genericType == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        Type? baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return baseType.GetGenericArguments()[0];
            }
            baseType = baseType.BaseType;
        }

        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static Type? GetDictionaryValueType(Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        Type genericType = type.GetGenericTypeDefinition();

        if (genericType == typeof(Dictionary<,>)
            || genericType == typeof(IDictionary<,>)
            || genericType == typeof(IReadOnlyDictionary<,>))
        {
            return type.GetGenericArguments()[1];
        }

        Type? baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return baseType.GetGenericArguments()[1];
            }
            baseType = baseType.BaseType;
        }

        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return iface.GetGenericArguments()[1];
            }
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            {
                return iface.GetGenericArguments()[1];
            }
        }

        return null;
    }

    private void HandleArrayCollectionType(Type elementType, JsonSchemaBuilder builder, bool withRefs)
    {
        JsonSchema itemSchema = BuildItemSchema(elementType, withRefs);

        if (elementType == typeof(byte))
        {
            itemSchema.ContentEncoding = "base64";
        }

        builder.Items(itemSchema);
    }

    private void HandleDictionaryType(Type valueType, JsonSchemaBuilder builder, bool withRefs)
    {
        JsonSchema propSchema;

        if (valueType == typeof(object))
        {
            propSchema = new() { Type = JsonValueType.Object };
        }
        else
        {
            propSchema = BuildItemSchema(valueType, withRefs);
        }

        builder.AdditionalProperties(propSchema);
    }

    private JsonSchema GetReferenceOrGenerate(Type type)
    {
        string typeName = GetTypeName(type);
        string? refValue = _currentMode switch
        {
            ReferenceMode.Defs => $"#/defs/{typeName}",
            ReferenceMode.External => typeName,
            _ => null
        };

        if (refValue is not null)
        {
            return new() { Ref = refValue };
        }

        return GenerateSchemaInternal(type, null, withRefs: true);
    }

    private JsonSchema GenerateSchemaWithRefs(Type type)
    {
        bool withRefs = _currentMode != ReferenceMode.Inline;
        return GenerateSchemaInternal(type, null, withRefs);
    }

    private Dictionary<string, JsonSchema> GenerateDefsSchemas(Type rootType)
    {
        Dictionary<string, JsonSchema> defs = [];

        foreach (KeyValuePair<Type, string> kvp in _typeDefinitions)
        {
            if (kvp.Key == rootType)
            {
                continue;
            }

            string typeName = kvp.Value;
            if (!string.IsNullOrEmpty(typeName) && !defs.ContainsKey(typeName))
            {
                JsonSchema schema;
                if (_handlerSchemas.TryGetValue(kvp.Key, out JsonSchema? handlerSchema))
                {
                    schema = handlerSchema;
                }
                else
                {
                    _processedTypes.Clear();
                    foreach (Type t in _typeDefinitions.Keys)
                    {
                        if (t != kvp.Key)
                        {
                            _processedTypes.Add(t);
                        }
                    }

                    schema = GenerateSchemaInternal(kvp.Key, null, withRefs: true);
                }
                defs[typeName] = schema;
            }
        }

        return defs;
    }

    private JsonSchema GenerateSchemaInternal(Type type, string? schema, bool withRefs = false)
    {
        if (_handlerSchemas.TryGetValue(type, out JsonSchema? handlerSchema))
        {
            return handlerSchema;
        }

        JsonSchemaBuilder builder = JsonSchemaBuilder.Create().Schema(schema);

        if (Nullable.GetUnderlyingType(type) is not null)
        {
            Type? underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is not null)
            {
                return GenerateSchemaInternal(underlyingType, schema, withRefs);
            }
        }

        if (type.IsEnum)
        {
            return builder
                .Type(JsonValueType.String)
                .Enum([.. Enum.GetValues(type).Cast<object>()])
                .Build();
        }

        if (_options.PrimitiveTypeHandlers.TryGetValue(type, out Action<JsonSchemaBuilder>? handler))
        {
            handler(builder);
        }
        else if (GetDictionaryValueType(type) is { } dictionaryValueType)
        {
            builder.Type(JsonValueType.Object);
            HandleDictionaryType(dictionaryValueType, builder, withRefs);
        }
        else if (GetEnumerableElementType(type) is { } elementType)
        {
            builder.Type(JsonValueType.Array);
            HandleArrayCollectionType(elementType, builder, withRefs);
        }
        else
        {
            builder.Type(JsonValueType.Object);
            if (type.IsClass && !_options.PrimitiveTypeHandlers.ContainsKey(type))
            {
                GenerateObjectSchemaInternal(builder, type, withRefs);
            }
            else if (type.IsInterface && !type.IsGenericType)
            {
                GenerateInterfaceSchemaInternal(builder, type, withRefs);
            }
        }

        return builder.Build();
    }

    private void GenerateObjectSchemaInternal(JsonSchemaBuilder builder, Type type, bool withRefs)
    {
        GenerateObjectOrInterfaceSchemaInternal(builder, type, withRefs, isInterface: false);
    }

    private void GenerateInterfaceSchemaInternal(JsonSchemaBuilder builder, Type type, bool withRefs)
    {
        GenerateObjectOrInterfaceSchemaInternal(builder, type, withRefs, isInterface: true);
    }

    private void GenerateObjectOrInterfaceSchemaInternal(JsonSchemaBuilder builder, Type type, bool withRefs, bool isInterface)
    {
        PropertyInfo[] properties = GetProperties(type);
        List<string> requiredProperties = [];

        string typeName = GetTypeName(type);
        builder.Title(typeName);

        if (withRefs && _typeDefinitions.ContainsKey(type) && _processedTypes.Contains(type) && !string.IsNullOrEmpty(typeName))
        {
            string? refValue = _currentMode switch
            {
                ReferenceMode.Defs => $"#/defs/{typeName}",
                ReferenceMode.External => typeName,
                _ => null
            };

            if (refValue is not null)
            {
                builder.Ref(refValue);
                return;
            }
        }

        _processedTypes.Add(type);

        foreach (PropertyInfo property in properties)
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            string propertyName = TransformPropertyName(property.Name);
            JsonSchema propertySchema = GenerateSchemaInternal(property.PropertyType, null, withRefs);

            builder.Property(propertyName, propertySchema);

            bool isRequired = !IsNullable(property.PropertyType, property) &&
                !property.CustomAttributes.Any(a =>
                    a.AttributeType.Name == OptionalAttributeName ||
                        a.AttributeType.Name == CanBeNullAttributeName);

            if (isRequired || IsNonNullableReferenceType(property))
            {
                requiredProperties.Add(propertyName);
            }
        }

        if (requiredProperties.Count > 0)
        {
            builder.Required([.. requiredProperties]);
        }

        if (string.IsNullOrEmpty(typeName))
        {
            return;
        }

        builder.CustomKeyword("csharpType", type.FullName);
        if (!isInterface)
        {
            return;
        }

        builder.CustomKeyword("x-abstract", true);
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        if (!_propertyCache.TryGetValue(type, out PropertyInfo[]? cached))
        {
            cached = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propertyCache[type] = cached;
        }
        return cached;
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            string name = TypeNameRegex().Replace(type.Name, string.Empty);
            IEnumerable<string> args = type.GetGenericArguments().Select(GetTypeName);
            return $"{name}<{string.Join(",", args)}>";
        }
        return type.Name;
    }

    private static bool IsNullable(Type type, PropertyInfo? property = null)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        if (property is null || type.IsValueType)
        {
            return false;
        }

        NullabilityInfo nullabilityInfo = new NullabilityInfoContext().Create(property);
        return nullabilityInfo.ReadState != NullabilityState.NotNull;
    }

    private static bool IsNonNullableReferenceType(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        NullabilityInfo nullabilityInfo = new NullabilityInfoContext().Create(property);
        return nullabilityInfo.ReadState == NullabilityState.NotNull;
    }

    private string TransformPropertyName(string propertyName)
    {
        if (_options.PropertyNamingPolicy is null)
        {
            return propertyName;
        }

        return _options.PropertyNamingPolicy.ConvertName(propertyName);
    }

    [GeneratedRegex(@"`\d+")]
    private static partial Regex TypeNameRegex();
}
