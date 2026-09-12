using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using SignalR.Documentation.Models;

namespace SignalR.Documentation.Reflection;

public class HubDocumentationScanner : IHubDocumentationScanner
{
        public IReadOnlyList<HubDocumentation> Scan(params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);
            if (assemblies.Length == 0)
                return [];

            var hubs = assemblies
                .Distinct()
                .SelectMany(GetLoadableTypes)
                .Where(IsHubType)
                .OrderBy(t => t.Name)
                .Select(BuildHubDocumentation)
                .ToList();

            return hubs;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null).Cast<Type>();
            }
        }

        private static bool IsHubType(Type type)
        {
            return type is
                   {
                       IsClass: true,
                       IsAbstract: false, ContainsGenericParameters: false
                   } &&
                   typeof(Hub).IsAssignableFrom(type);
        }

        private static HubDocumentation BuildHubDocumentation(Type hubType)
        {
            var hubDoc = hubType.GetCustomAttribute<HubDocAttribute>();
            var schemas = new SchemaBuilder();

            var methods = hubType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(ShouldIncludeMethod)
                .OrderBy(m => m.Name)
                .Select(m => BuildMethodDocumentation(m, schemas))
                .ToList();

            return new HubDocumentation
            {
                HubName = hubType.Name,
                FullName = hubType.FullName ?? hubType.Name,
                Namespace = hubType.Namespace,
                Summary = hubDoc?.Summary,
                Description = hubDoc?.Description,
                Methods = methods,
                Models = schemas.Models
            };
        }

        private static bool ShouldIncludeMethod(MethodInfo method)
        {
            if (method.DeclaringType == typeof(object) || method.DeclaringType == typeof(Hub) || method.IsSpecialName)
                return false;

            if (method.ContainsGenericParameters)
                return false;

            if (method.GetBaseDefinition().DeclaringType == typeof(Hub) || method.GetBaseDefinition().DeclaringType == typeof(object))
                return false;

            if (method.Name is nameof(Hub.OnConnectedAsync) or nameof(Hub.OnDisconnectedAsync))
                return false;

            return true;
        }

        private static HubMethodDocumentation BuildMethodDocumentation(MethodInfo method, SchemaBuilder schemas)
        {
            var methodSummary = GetMethodSummary(method);
            var parameters = method.GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken) && !p.GetCustomAttributes().Any(a => a is Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata))
                .Select(p => BuildParameterDocumentation(p, schemas))
                .ToList();

            var returnType = BuildReturnDocumentation(method, schemas);

            return new HubMethodDocumentation
            {
                MethodName = method.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? method.Name,
                Summary = methodSummary,
                DisplaySignature = BuildMethodSignature(method, parameters, returnType),
                IsAsync = IsAsyncType(method.ReturnType),
                ReturnType = returnType,
                Parameters = parameters
            };
        }

        private static string? GetMethodSummary(MethodInfo method)
        {
            var methodDoc = method.GetCustomAttribute<HubMethodDocAttribute>();
            if (!string.IsNullOrWhiteSpace(methodDoc?.Summary))
                return methodDoc.Summary;

            var hubDoc = method.GetCustomAttribute<HubDocAttribute>();
            if (!string.IsNullOrWhiteSpace(hubDoc?.Summary))
                return hubDoc.Summary;

            return null;
        }

        private static HubParameterDocumentation BuildParameterDocumentation(ParameterInfo parameter, SchemaBuilder schemas)
        {
            var parameterType = parameter.ParameterType;
            var nullableUnderlyingType = Nullable.GetUnderlyingType(parameterType);


            return new HubParameterDocumentation
            {
                Name = parameter.Name ?? string.Empty,
                TypeName = GetFriendlyTypeName(parameterType),
                FullTypeName = GetFriendlyFullTypeName(parameterType),
                IsNullable = nullableUnderlyingType is not null || new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable,
                Schema = schemas.Build(parameterType, new NullabilityInfoContext().Create(parameter)),
                IsOptional = parameter.IsOptional,
                DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null
            };
        }

        private static ReturnDocumentation BuildReturnDocumentation(MethodInfo method, SchemaBuilder schemas)
        {
            var returnType = method.ReturnType;
            var returnInfo = new NullabilityInfoContext().Create(method.ReturnParameter);
            var isAsyncWrapper = IsAsyncType(returnType);
            var unwrappedType = UnwrapAsyncReturnType(returnType);

            var hasReturnValue =
                returnType != typeof(void) &&
                returnType != typeof(Task) &&
                returnType != typeof(ValueTask);

            return new ReturnDocumentation
            {
                TypeName = GetFriendlyTypeName(returnType),
                FullTypeName = GetFriendlyFullTypeName(returnType),
                HasReturnValue = hasReturnValue,
                Schema = unwrappedType == null ? null : schemas.Build(unwrappedType, isAsyncWrapper ? returnInfo.GenericTypeArguments.FirstOrDefault() : returnInfo),
                IsAsyncWrapper = isAsyncWrapper,
                UnwrappedTypeName = unwrappedType is null ? null : GetFriendlyTypeName(unwrappedType),
                UnwrappedFullTypeName = unwrappedType is null ? null : GetFriendlyFullTypeName(unwrappedType)
            };
        }

        private static string BuildMethodSignature(
            MethodInfo method,
            IReadOnlyList<HubParameterDocumentation> parameters,
            ReturnDocumentation returnType)
        {
            var parametersText = string.Join(", ",
                parameters.Select(p =>
                {
                    var optionalText = p.IsOptional
                        ? $" = {FormatDefaultValue(p.DefaultValue)}"
                        : string.Empty;

                    return $"{p.TypeName} {p.Name}{optionalText}";
                }));

            return $"{returnType.TypeName} {method.Name}({parametersText})";
        }

        private static bool IsAsyncType(Type type)
        {
            if (type == typeof(Task) || type == typeof(ValueTask))
                return true;

            if (!type.IsGenericType)
                return false;

            var genericDefinition = type.GetGenericTypeDefinition();
            return genericDefinition == typeof(Task<>) || genericDefinition == typeof(ValueTask<>);
        }

        private static Type? UnwrapAsyncReturnType(Type type)
        {
            if (type == typeof(Task) || type == typeof(ValueTask))
                return null;

            if (!type.IsGenericType)
                return type == typeof(void) ? null : type;

            var genericDefinition = type.GetGenericTypeDefinition();

            if (genericDefinition == typeof(Task<>) || genericDefinition == typeof(ValueTask<>))
                return type.GetGenericArguments()[0];

            return type;
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var nullableUnderlying = Nullable.GetUnderlyingType(type);
            if (nullableUnderlying is not null)
                return $"{GetFriendlyTypeName(nullableUnderlying)}?";

            if (type == typeof(void)) return "void";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(object)) return "object";
            if (type == typeof(char)) return "char";
            if (type == typeof(Guid)) return "Guid";
            if (type == typeof(DateTime)) return "DateTime";
            if (type == typeof(DateTimeOffset)) return "DateTimeOffset";
            if (type == typeof(TimeSpan)) return "TimeSpan";

            if (type.IsArray)
                return $"{GetFriendlyTypeName(type.GetElementType()!)}[]";

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericName = genericTypeDefinition.Name;
                var tickIndex = genericName.IndexOf('`');
                if (tickIndex >= 0)
                    genericName = genericName[..tickIndex];

                var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericName}<{genericArguments}>";
            }

            return type.Name;
        }

        private static string GetFriendlyFullTypeName(Type type)
        {
            var nullableUnderlying = Nullable.GetUnderlyingType(type);
            if (nullableUnderlying is not null)
                return $"{GetFriendlyFullTypeName(nullableUnderlying)}?";

            if (type.IsArray)
                return $"{GetFriendlyFullTypeName(type.GetElementType()!)}[]";

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericName = genericTypeDefinition.FullName ?? genericTypeDefinition.Name;

                var tickIndex = genericName.IndexOf('`');
                if (tickIndex >= 0)
                    genericName = genericName[..tickIndex];

                var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyFullTypeName));
                return $"{genericName}<{genericArguments}>";
            }

            return type.FullName ?? type.Name;
        }

        private static string FormatDefaultValue(object? value)
        {
            if (value is null)
                return "null";

            return value switch
            {
                string s => $"\"{s}\"",
                char c => $"'{c}'",
                bool b => b ? "true" : "false",
                _ => value.ToString() ?? "null"
            };
        }
}
