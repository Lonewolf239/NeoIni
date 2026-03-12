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
                    var dict = new Dictionary<string, List<PropertyMeta>>();
                    foreach (var p in props)
                    {
                        if (!dict.TryGetValue(p.ContainingTypeName, out var list))
                        {
                            list = new List<PropertyMeta>();
                            dict[p.ContainingTypeName] = list;
                        }
                        list.Add(p);
                    }
                    return dict;
                });

            context.RegisterSourceOutput(groupedByType, static (spc, grouped) =>
            {
                if (grouped == null || grouped.Count == 0) return;
                foreach (var kvp in grouped) GenerateSourceForType(spc, kvp.Key, kvp.Value);
                GenerateReport(spc, grouped.SelectMany(x => x.Value).ToList());
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
            public string DefaultValueLiteral { get; private set; }

            public PropertyMeta(string containingTypeName, string propertyName, string propertyTypeName,
                    string section, string key, string defaultValueLiteral)
            {
                ContainingTypeName = containingTypeName;
                PropertyName = propertyName;
                PropertyTypeName = propertyTypeName;
                Section = section;
                Key = key;
                DefaultValueLiteral = defaultValueLiteral;
            }
        }

        private static PropertyMeta GetPropertyInfo(GeneratorSyntaxContext context)
        {
            var propertySyntax = context.Node as PropertyDeclarationSyntax;
            if (propertySyntax == null) return null;

            var model = context.SemanticModel;
            var propertySymbol = model.GetDeclaredSymbol(propertySyntax) as IPropertySymbol;
            if (propertySymbol == null) return null;

            var attr = propertySymbol
                .GetAttributes()
                .FirstOrDefault(a =>
                    a.AttributeClass != null &&
                    a.AttributeClass.ToDisplayString() == "NeoIni.Annotations.NeoIniKeyAttribute");

            if (attr == null) return null;

            string section = "";
            string key = "";
            string defaultValueLiteral = null;

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

        private static void GenerateSourceForType(SourceProductionContext context, string containingTypeName, List<PropertyMeta> properties)
        {
            if (properties == null || properties.Count == 0) return;

            string ns;
            string typeName;

            SplitTypeName(containingTypeName, out ns, out typeName);

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using System;");
            sb.AppendLine("using NeoIni;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            sb.AppendLine("    internal static partial class NeoIniGeneratedExtensions");
            sb.AppendLine("    {");

            sb.AppendLine("        public static void Set<TConfig>(this NeoIniReader reader, TConfig config)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader == null) throw new ArgumentNullException(\"reader\");");
            sb.AppendLine("            if (config == null) throw new ArgumentNullException(\"config\");");

            foreach (var p in properties)
            {
                var sectionLit = EscapeStringLiteral(p.Section);
                var keyLit = EscapeStringLiteral(p.Key);

                sb.Append("            reader.SetValue(")
                  .Append(sectionLit)
                  .Append(", ")
                  .Append(keyLit)
                  .Append(", ((TConfig)config).")
                  .Append(p.PropertyName)
                  .AppendLine(");");
            }

            sb.AppendLine("        }");

            sb.AppendLine();
            sb.AppendLine("        public static TConfig Get<TConfig>(this NeoIniReader reader) where TConfig : new()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader == null) throw new ArgumentNullException(\"reader\");");
            sb.AppendLine("            var cfg = new TConfig();");

            foreach (var p in properties)
            {
                var sectionLit = EscapeStringLiteral(p.Section);
                var keyLit = EscapeStringLiteral(p.Key);

                string defaultValue = p.DefaultValueLiteral ?? GetDefaultLiteralForType(p.PropertyTypeName);

                sb.Append("            ((TConfig)cfg).")
                  .Append(p.PropertyName)
                  .Append(" = reader.GetValue<")
                  .Append(p.PropertyTypeName)
                  .Append(">(")
                  .Append(sectionLit)
                  .Append(", ")
                  .Append(keyLit)
                  .Append(", ")
                  .Append(defaultValue)
                  .AppendLine(");");
            }

            sb.AppendLine("            return cfg;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(ns))
                sb.AppendLine("}");

            var hintName = MakeSafeHintName(containingTypeName) + ".NeoIniMapping.g.cs";
            context.AddSource(hintName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static string EscapeStringLiteral(string value)
        {
            if (value == null)
                return "null";

            return "@\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string GetDefaultLiteralForType(string fullyQualifiedTypeName)
        {
            return "default(" + fullyQualifiedTypeName + ")";
        }

        private static void SplitTypeName(string fullName, out string ns, out string name)
        {
            ns = null;
            name = fullName;

            if (string.IsNullOrEmpty(fullName))
                return;

            const string globalPrefix = "global::";
            if (fullName.StartsWith(globalPrefix, StringComparison.Ordinal))
                fullName = fullName.Substring(globalPrefix.Length);

            int lastDot = fullName.LastIndexOf('.');
            if (lastDot <= 0)
            {
                name = fullName;
                return;
            }

            ns = fullName.Substring(0, lastDot);
            name = fullName.Substring(lastDot + 1);
        }

        private static string MakeSafeHintName(string typeName)
        {
            if (typeName == null)
                return "NeoIniMapping";

            var sb = new StringBuilder(typeName.Length);
            foreach (var ch in typeName)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }

        private static void GenerateReport(SourceProductionContext context, IReadOnlyList<PropertyMeta> properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("namespace NeoIni");
            sb.AppendLine("{");
            sb.AppendLine("    internal static class NeoIniMappingReport");
            sb.AppendLine("    {");
            sb.AppendLine("        // This file is generated by NeoIniMappingGenerator");
            sb.AppendLine("        // Detected properties with [NeoIniKey]:");

            if (properties == null || properties.Count == 0)
            {
                sb.AppendLine("        //   (none)");
            }
            else
            {
                foreach (var p in properties)
                {
                    sb.Append("        //   Type: ")
                      .Append(p.ContainingTypeName)
                      .Append(", Property: ")
                      .Append(p.PropertyName)
                      .Append(" : ")
                      .Append(p.PropertyTypeName)
                      .Append(", Section=\"")
                      .Append(p.Section)
                      .Append("\", Key=\"")
                      .Append(p.Key)
                      .AppendLine("\"");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("NeoIniMappingReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
