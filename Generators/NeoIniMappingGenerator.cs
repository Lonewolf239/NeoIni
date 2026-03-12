using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NeoIni.Generators
{
    [Generator]
    public sealed class NeoIniMappingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var propertyDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateProperty(node),
                    transform: static (ctx, _) => GetPropertyInfo(ctx))
                .Where(static info => info != null)!;
            var groupedByType = propertyDeclarations
                .Collect()
                .Select(static (props, _) =>
                {
                    Dictionary<string, List<PropertyMeta>> dict = new();
                    foreach (var p in props)
                    {
                        if (p is null) continue;
                        if (!dict.TryGetValue(p.ContainingTypeName, out var list))
                        {
                            list = new();
                            dict[p.ContainingTypeName] = list;
                        }
                        list.Add(p);
                    }
                    return dict;
                });
            context.RegisterSourceOutput(groupedByType, static (spc, grouped) =>
            {
                if (grouped == null || grouped.Count == 0) return;
                GenerateExtensions(spc, grouped);
            });
        }

        private static bool IsCandidateProperty(SyntaxNode node)
        {
            var property = node as PropertyDeclarationSyntax;
            if (property == null) return false;
            if (property.AttributeLists == null || property.AttributeLists.Count == 0) return false;
            return true;
        }

        private sealed class PropertyMeta
        {
            public string ContainingTypeName { get; private set; }
            public string PropertyName { get; private set; }
            public string PropertyTypeName { get; private set; }
            public string Section { get; private set; }
            public string Key { get; private set; }
            public string? DefaultValueLiteral { get; private set; }

            public PropertyMeta(string containingTypeName, string propertyName, string propertyTypeName,
                    string section, string key, string? defaultValueLiteral)
            {
                ContainingTypeName = containingTypeName;
                PropertyName = propertyName;
                PropertyTypeName = propertyTypeName;
                Section = section;
                Key = key;
                DefaultValueLiteral = defaultValueLiteral;
            }
        }

        private static PropertyMeta? GetPropertyInfo(GeneratorSyntaxContext context)
        {
            var propertySyntax = context.Node as PropertyDeclarationSyntax;
            if (propertySyntax == null) return null;
            var model = context.SemanticModel;
            var propertySymbol = model.GetDeclaredSymbol(propertySyntax) as IPropertySymbol;
            if (propertySymbol == null) return null;
            var attr = propertySymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass != null && a.AttributeClass.ToDisplayString() == "NeoIni.Annotations.NeoIniKeyAttribute");
            if (attr == null) return null;
            string section = "";
            string key = "";
            string? defaultValueLiteral = null;
            if (attr.ConstructorArguments.Length >= 2)
            {
                var argSection = attr.ConstructorArguments[0];
                var argKey = attr.ConstructorArguments[1];
                if (argSection.Value is string s1) section = s1;
                if (argKey.Value is string s2) key = s2;
            }
            var defaultValueArg = attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == "DefaultValue");
            if (defaultValueArg.Value.Value != null)
            {
                object dv = defaultValueArg.Value.Value;
                if (dv is string)
                    defaultValueLiteral = "@\"" + ((string)dv).Replace("\"", "\"\"") + "\"";
                else if (dv is bool)
                    defaultValueLiteral = ((bool)dv) ? "true" : "false";
                else if (dv is char)
                    defaultValueLiteral = "'" + dv.ToString().Replace("'", "\\'") + "'";
                else if (dv is IFormattable)
                    defaultValueLiteral = ((IFormattable)dv).ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                else
                    defaultValueLiteral = dv.ToString();
            }
            var containingType = propertySymbol.ContainingType;
            string typeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string propTypeName = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new PropertyMeta(typeName, propertySymbol.Name, propTypeName, section, key, defaultValueLiteral);
        }

        private static void GenerateExtensions(SourceProductionContext context, Dictionary<string, List<PropertyMeta>> grouped)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace NeoIni");
            sb.AppendLine("{");
            sb.AppendLine("    public static class NeoIniReaderExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        public static T Get<T>(this NeoIniReader reader) where T : new()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader == null) throw new ArgumentNullException(nameof(reader));");
            sb.AppendLine("            Type t = typeof(T);");
            sb.AppendLine();
            foreach (var kvp in grouped)
            {
                string typeName = kvp.Key;
                sb.AppendLine($"            if (t == typeof({typeName}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                var cfg = new {typeName}();");
                foreach (var p in kvp.Value)
                {
                    var sectionLit = EscapeStringLiteral(p.Section);
                    var keyLit = EscapeStringLiteral(p.Key);
                    string defaultValue = p.DefaultValueLiteral ?? GetDefaultLiteralForType(p.PropertyTypeName);
                    sb.AppendLine($"                cfg.{p.PropertyName} = reader.GetValue<{p.PropertyTypeName}>({sectionLit}, {keyLit}, {defaultValue});");
                }
                sb.AppendLine("                return (T)(object)cfg;");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return new T();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static void Set<T>(this NeoIniReader reader, T config)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader == null) throw new ArgumentNullException(nameof(reader));");
            sb.AppendLine("            if (config == null) throw new ArgumentNullException(nameof(config));");
            sb.AppendLine("            Type t = typeof(T);");
            sb.AppendLine();
            foreach (var kvp in grouped)
            {
                string typeName = kvp.Key;
                sb.AppendLine($"            if (t == typeof({typeName}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                var cfg = ({typeName})(object)config;");
                foreach (var p in kvp.Value)
                {
                    var sectionLit = EscapeStringLiteral(p.Section);
                    var keyLit = EscapeStringLiteral(p.Key);
                    sb.AppendLine($"                reader.SetValue({sectionLit}, {keyLit}, cfg.{p.PropertyName});");
                }
                sb.AppendLine("                return;");
                sb.AppendLine("            }");
            }

            sb.AppendLine("            throw new NotSupportedException($\"Type {t.FullName} is not registered in NeoIni source generator.\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("NeoIniReaderExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static string EscapeStringLiteral(string value)
        {
            if (value == null) return "null";
            return "@\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string GetDefaultLiteralForType(string fullyQualifiedTypeName) => "default(" + fullyQualifiedTypeName + ")";
    }
}
