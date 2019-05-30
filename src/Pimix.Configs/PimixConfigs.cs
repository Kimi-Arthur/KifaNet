using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Pimix.Configs {
    public static class PimixConfigs {
        static string configFilePath;

        static readonly Deserializer deserializer = new Deserializer();
        static string name => AppDomain.CurrentDomain.FriendlyName;

        static List<string> ConfigFilePaths
            => new List<string> {
                $"~/.{name}.yaml",
                "~/.pimix.yaml",
                $"/etc/{name}.yaml",
                "/etc/pimix.yaml"
            };

        static string ConfigFilePath {
            get {
                if (configFilePath == null) {
                    configFilePath = Environment.GetEnvironmentVariable("CONFIG");
                    if (configFilePath == null) {
                        foreach (var path in ConfigFilePaths) {
                            if (File.Exists(path)) {
                                configFilePath = path;
                                break;
                            }
                        }
                    }
                }

                return configFilePath;
            }
        }

        public static void LoadFromSystemConfigs(Assembly assembly = null) {
            var properties =
                assembly == null ? GetAllProperties() : GetProperties(assembly);
            if (ConfigFilePath != null) {
                LoadFromStream(File.OpenRead(ConfigFilePath), properties);
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
                foreach (var property in GetProperties(assembly)) {
                    properties[property.Key] = property.Value;
                }
            }

            return properties;
        }

        static Dictionary<string, PropertyInfo> GetProperties(Assembly assembly) {
            var properties = new Dictionary<string, PropertyInfo>();
            foreach (var t in assembly.GetTypes()) {
                if (t.Namespace?.StartsWith("Pimix") == true) {
                    foreach (var p in t.GetProperties()) {
                        if (p.GetSetMethod()?.IsStatic == true) {
                            properties[$"{t.Namespace}.{t.Name}.{p.Name}"] = p;
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
                    var value = deserializer.Deserialize(
                        new YamlNodeParser(YamlNodeToEventStreamConverter.ConvertToEventStream(p.Value)),
                        prop.PropertyType);
                    if (value == null) {
                        Console.WriteLine($"Cannot parse for {id}");
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
    }
}
