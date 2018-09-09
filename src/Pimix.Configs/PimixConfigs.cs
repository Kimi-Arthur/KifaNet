using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.RepresentationModel;

namespace Pimix.Configs {
    public class PimixConfigs {
        public static readonly List<string> ConfigFilePaths = new List<string>
            {"pimix.yaml", "~/.pimix.yaml", "/etc/pimix.yaml"};

        public static void LoadFromSystemConfigs() {
            foreach (var filePath in ConfigFilePaths) {
                if (File.Exists(filePath)) {
                    var properties = GetAllProperties();
                    LoadFromStream(File.OpenRead(filePath), properties);
                    break;
                }
            }
        }

        public static void LoadFromStream(Stream stream,
            Dictionary<string, PropertyInfo> properties) {
            var yaml = new YamlStream();

            using (var sr = new StreamReader(stream)) {
                yaml.Load(sr);
            }

            Apply((YamlMappingNode) yaml.Documents[0].RootNode, "", properties);
        }

        public static Dictionary<string, PropertyInfo> GetAllProperties() {
            var properties = new Dictionary<string, PropertyInfo>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var t in assembly.GetTypes()) {
                    if (t.Namespace?.StartsWith("Pimix") == true) {
                        foreach (var p in t.GetProperties()) {
                            if (p.GetSetMethod()?.IsStatic == true) {
                                properties[$"{t.Namespace}.{t.Name}.{p.Name}"] = p;
                            }
                        }
                    }
                }
            }

            return properties;
        }

        static void Apply(YamlMappingNode node, string prefix,
            IReadOnlyDictionary<string, PropertyInfo> properties) {
            foreach (var p in node) {
                if (p.Value == null) {
                    continue;
                }

                var id = $"{prefix}{((YamlScalarNode) p.Key).Value}";
                if (properties.TryGetValue(id, out var prop)) {
                    var value = Parse(p.Value, prop.PropertyType);
                    if (value == null) {
                        Console.WriteLine($" for {id}");
                        continue;
                    }

                    prop.SetValue(null, value);
                    continue;
                }

                if (p.Value.NodeType == YamlNodeType.Mapping) {
                    Apply((YamlMappingNode) p.Value, $"{id}.", properties);
                }
            }
        }

        static object Parse(YamlNode node, Type propertyType) {
            if (propertyType == typeof(string)) {
                if (node.NodeType == YamlNodeType.Scalar) {
                    return (string) node;
                }

                Console.Write("Want a string, got {0}", node.NodeType);
            }

            if (propertyType == typeof(int)) {
                if (node.NodeType == YamlNodeType.Scalar) {
                    return int.Parse((string) node);
                }

                Console.Write("Want an int, got {0}", node.NodeType);
            }

            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                if (node.NodeType == YamlNodeType.Sequence) {
                    var itemType = propertyType.GetGenericArguments()[0];
                    var items = ((YamlSequenceNode) node).Select(n => Parse(n, itemType));

                    if (itemType == typeof(int)) {
                        return items.Cast<int>().ToList();
                    }

                    if (itemType == typeof(string)) {
                        return items.Cast<string>().ToList();
                    }

                    return items.ToList();
                }

                Console.Write("Want a list, got {0}", node.NodeType);
            }

            if (propertyType.IsGenericType &&
                propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                if (node.NodeType == YamlNodeType.Mapping) {
                    var itemType = propertyType.GetGenericArguments();
                    var keyType = itemType[0];
                    var valueType = itemType[1];

                    var data = ((YamlMappingNode) node).Select(n
                        => new KeyValuePair<object, object>(
                            Parse(n.Key, keyType),
                            Parse(n.Value, valueType)));

                    if (keyType == typeof(string) && valueType == typeof(string)) {
                        return data.ToDictionary(p => (string) p.Key, p => (string) p.Value);
                    }

                    if (keyType == typeof(string) && valueType == typeof(int)) {
                        return data.ToDictionary(p => (string) p.Key, p => (int) p.Value);
                    }

                    if (keyType == typeof(int) && valueType == typeof(string)) {
                        return data.ToDictionary(p => (int) p.Key, p => (string) p.Value);
                    }

                    if (keyType == typeof(int) && valueType == typeof(int)) {
                        return data.ToDictionary(p => (int) p.Key, p => (int) p.Value);
                    }
                }

                Console.Write("Want a dict, got {0}", node.NodeType);
            }

            return null;
        }
    }
}
