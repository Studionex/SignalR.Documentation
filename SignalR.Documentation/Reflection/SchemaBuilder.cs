using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SignalR.Documentation.Models;

namespace SignalR.Documentation.Reflection;

internal sealed class SchemaBuilder
{
    private readonly Dictionary<Type, ModelDocumentation> models = [];
    private readonly NullabilityInfoContext nullability = new();
    public List<ModelDocumentation> Models => models.Values.OrderBy(m => m.FullName, StringComparer.Ordinal).ToList();

    public TypeDocumentation Build(Type type, NullabilityInfo? info = null)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var nullable = underlying != null || info?.ReadState == NullabilityState.Nullable;
        type = underlying ?? type;
        var name = FriendlyName(type);
        if (type.IsEnum)
            return new() { TypeName = name, Kind = "enum", IsNullable = nullable, EnumValues = Enum.GetNames(type) };
        if (IsScalar(type))
            return new() { TypeName = name, IsNullable = nullable };

        var dictionary = FindGeneric(type, typeof(IDictionary<,>)) ?? FindGeneric(type, typeof(IReadOnlyDictionary<,>));
        if (dictionary != null)
        {
            var arguments = dictionary.GetGenericArguments();
            return new() { TypeName = name, Kind = "dictionary", IsNullable = nullable,
                Keys = Build(arguments[0], GenericInfo(type, info, arguments[0], 0)),
                Values = Build(arguments[1], GenericInfo(type, info, arguments[1], 1)) };
        }
        var sequence = FindGeneric(type, typeof(IEnumerable<>)) ?? FindGeneric(type, typeof(IAsyncEnumerable<>));
        if (type.IsArray || sequence != null)
        {
            var element = type.IsArray ? type.GetElementType()! : sequence!.GetGenericArguments()[0];
            return new() { TypeName = name, Kind = "array", IsNullable = nullable,
                Items = Build(element, type.IsArray ? info?.ElementType : GenericInfo(type, info, element, 0)) };
        }

        if (!models.TryGetValue(type, out var model))
        {
            model = new() { Id = "model-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(type.AssemblyQualifiedName ?? name))).ToLowerInvariant(),
                Name = name, FullName = type.FullName ?? name,
                Description = type.GetCustomAttribute<DescriptionAttribute>()?.Description };
            models.Add(type, model);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                if (property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0 ||
                    property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always) continue;
                model.Properties.Add(new() {
                    Name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name),
                    Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    IsRequired = property.IsDefined(typeof(RequiredAttribute)) || property.IsDefined(typeof(RequiredMemberAttribute)) || property.IsDefined(typeof(JsonRequiredAttribute)),
                    Type = Build(property.PropertyType, nullability.Create(property))
                });
            }
        }
        return new() { TypeName = name, Kind = "object", IsNullable = nullable, ModelId = model.Id };
    }

    private static NullabilityInfo? GenericInfo(Type type, NullabilityInfo? info, Type argument, int index) =>
        type.IsGenericType && type.GetGenericArguments().Length > index && type.GetGenericArguments()[index] == argument && info?.GenericTypeArguments.Length > index
            ? info.GenericTypeArguments[index] : null;

    private static Type? FindGeneric(Type type, Type definition) =>
        type.GetInterfaces().Prepend(type).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == definition);

    private static bool IsScalar(Type type) => type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) ||
        type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Uri) || type == typeof(object) ||
        type == typeof(JsonElement) || type == typeof(JsonDocument) || type == typeof(void);

    internal static string FriendlyName(Type type)
    {
        if (type.IsEnum) return type.Name;
        if (Nullable.GetUnderlyingType(type) is { } underlying) return FriendlyName(underlying) + "?";
        if (type.IsArray) return FriendlyName(type.GetElementType()!) + "[]";
        if (type.IsGenericType) return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(FriendlyName)) + ">";
        return Type.GetTypeCode(type) switch {
            TypeCode.String => "string", TypeCode.Boolean => "bool", TypeCode.Int32 => "int", TypeCode.Int64 => "long",
            TypeCode.Decimal => "decimal", TypeCode.Double => "double", TypeCode.Single => "float",
            _ => type == typeof(void) ? "void" : type.Name
        };
    }
}
