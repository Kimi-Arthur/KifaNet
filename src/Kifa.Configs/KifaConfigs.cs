using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Kifa.Configs;

public static class KifaConfigs {
    static string configFilePath;

    static readonly IDeserializer deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties().Build();

    static string name => AppDomain.CurrentDomain.FriendlyName;

    static List<string> ConfigFilePaths
        => new() {
            $"~/.{name}.yaml",
            "~/.kimily.yaml",
            $"/etc/{name}.yaml",
            "/etc/kimily.yaml"
        };

    static string ConfigFilePath {
        get {
            if (configFilePath == null) {
                configFilePath = Environment.GetEnvironmentVariable("KIFA_CONFIG");
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
        var properties = assembly == null ? GetAllProperties() : GetProperties(assembly);
        var assemblyName = assembly == null
            ? string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().Select(ass => ass.FullName))
            : assembly.FullName;
        Log($"Configure the following {properties.Count} properties in {assemblyName}:");
        foreach (var property in properties) {
            Log($"\t{property.Key}");
        }

        if (ConfigFilePath != null) {
            var localConfig = ConfigFilePath;
            var remoteConfig = localConfig.Replace(".yaml", ".remote.yaml");
            if (File.Exists(remoteConfig)) {
                Log($"Load configs from {remoteConfig}...");
                LoadFromStream(File.OpenRead(remoteConfig), properties);
            }

            Log($"Load configs from {localConfig}...");
            LoadFromStream(File.OpenRead(localConfig), properties);
        }
    }

    public static void LoadFromStream(Stream stream, Dictionary<string, PropertyInfo> properties) {
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
            if (t.Namespace?.StartsWith("Kifa") ?? false) {
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
            var id = $"{prefix}{((YamlScalarNode) p.Key).Value}";
            Log($"Apply config for {id}");
            if (properties.TryGetValue(id, out var prop)) {
                Log($"Property found for {id}");
                var value = deserializer.Deserialize(
                    new YamlNodeParser(
                        YamlNodeToEventStreamConverter.ConvertToEventStream(p.Value)),
                    prop.PropertyType);
                Log($"Value for {id}: {value}");

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

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static bool loggerConfigured;
    static bool loggingNeeded;

    static readonly List<string> PendingLogs = new();

    static void Log(string message) {
        if (!loggingNeeded) {
            return;
        }

        if (loggerConfigured) {
            Logger.Trace(message);
        } else {
            PendingLogs.Add(message);
        }
    }

    public static void Init(bool logEvents = false) {
        // Workaround that YamlDotNet may fail to initialize Regex in TagDirective.
        if (Constants.DefaultTagDirectives.Length != 2) {
            Console.WriteLine("YamlDotNet is Broken.");
        }

        loggingNeeded = logEvents;
        AppDomain.CurrentDomain.AssemblyLoad += (sender, eventArgs)
            => LoadFromSystemConfigs(eventArgs.LoadedAssembly);
        LoadFromSystemConfigs();
    }

    public static void LoggerConfigured() {
        loggerConfigured = true;
        Logger.Trace("Logger configured. Logging pending logs...");
        foreach (var log in PendingLogs) {
            Logger.Trace(log);
        }

        PendingLogs.Clear();
    }
}
