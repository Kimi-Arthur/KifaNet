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
    static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties().Build();

    static string ConfigFilePath {
        get {
            if (field == null) {
                field = Environment.GetEnvironmentVariable("KIFA_CONFIG");
                if (field == null) {
                    Console.WriteLine(
                        "You should specify your config either with environment variable 'KIFA_CONFIG' or via command line argument '--config'.");
                    Environment.Exit(1);
                }
            }

            return field;
        }
        set;
    }

    static readonly object ConfigLock = new();
    static readonly HashSet<Assembly> ProcessedAssemblies = new();
    static bool isConfiguring;
    static bool assemblyLoadHooked;

    static bool ShouldProcessAssembly(Assembly assembly) {
        var name = assembly.GetName().Name;
        return name != null && (name.StartsWith("Kifa") || name.StartsWith("Mito"));
    }

    const string LoadPrefix = "# Load ";

    public static void LoadFromSystemConfigs(Assembly? assembly = null) {
        lock (ConfigLock) {
            if (isConfiguring) {
                return;
            }

            if (assembly != null && (!ShouldProcessAssembly(assembly) || ProcessedAssemblies.Contains(assembly))) {
                return;
            }

            isConfiguring = true;
            try {
                while (true) {
                    var newAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !ProcessedAssemblies.Contains(a) && ShouldProcessAssembly(a))
                        .ToList();
                    if (newAssemblies.Count == 0) {
                        break;
                    }

                    foreach (var a in newAssemblies) {
                        ProcessedAssemblies.Add(a);
                    }

                    var properties = new Dictionary<string, PropertyInfo>();
                    foreach (var a in newAssemblies) {
                        foreach (var property in GetProperties(a)) {
                            properties[property.Key] = property.Value;
                        }
                    }

                    if (properties.Count > 0) {
                        var assemblyNames = string.Join(", ", newAssemblies.Select(ass => ass.FullName));
                        Log($"Configure the following {properties.Count} properties in {assemblyNames}:");
                        foreach (var property in properties) {
                            Log($"\t{property.Key}");
                        }

                        LoadConfig(ConfigFilePath, properties);
                    }
                }
            } finally {
                isConfiguring = false;
            }
        }
    }

    static void LoadConfig(string configPath, Dictionary<string, PropertyInfo> properties) {
        foreach (var line in File.ReadLines(configPath)) {
            if (!line.StartsWith(LoadPrefix)) {
                break;
            }

            var path = line[LoadPrefix.Length..];
            LoadConfig(Path.Combine(Directory.GetParent(configPath)!.FullName, path), properties);
        }

        Log($"Loading configs from {configPath}...");
        LoadFromStream(File.OpenRead(configPath), properties);
    }

    public static void LoadFromStream(Stream stream, Dictionary<string, PropertyInfo> properties) {
        var yaml = new YamlStream();
        using (var sr = new StreamReader(stream)) {
            yaml.Load(sr);
        }

        if (yaml.Documents.Count == 0) {
            Log("No config found.");
            return;
        }

        Apply((YamlMappingNode) yaml.Documents[0].RootNode, "", properties);
    }

    public static Dictionary<string, PropertyInfo> GetAllProperties() {
        var properties = new Dictionary<string, PropertyInfo>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (!ShouldProcessAssembly(assembly)) {
                continue;
            }

            foreach (var property in GetProperties(assembly)) {
                properties[property.Key] = property.Value;
            }
        }

        return properties;
    }

    static Dictionary<string, PropertyInfo> GetProperties(Assembly assembly) {
        var properties = new Dictionary<string, PropertyInfo>();
        if (!ShouldProcessAssembly(assembly)) {
            return properties;
        }

        Type[] types;
        try {
            types = assembly.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            types = ex.Types.Where(t => t != null).ToArray()!;
        } catch {
            types = Type.EmptyTypes;
        }

        foreach (var t in types) {
            // TODO: Temp workaround as KifaConfigs itself cannot be properly configured.
            if ((t.Namespace?.StartsWith("Kifa") ?? false) ||
                (t.Namespace?.StartsWith("Mito") ?? false)) {
                PropertyInfo[] typeProperties;
                try {
                    typeProperties = t.GetProperties();
                } catch {
                    typeProperties = Array.Empty<PropertyInfo>();
                }

                foreach (var p in typeProperties) {
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
                var value = Deserializer.Deserialize(
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

    public static void Init(string? configFile = null, bool logEvents = false) {
        if (configFile != null) {
            ConfigFilePath = configFile;
        }

        // Workaround that YamlDotNet may fail to initialize Regex in TagDirective.
        if (Constants.DefaultTagDirectives.Length != 2) {
            Console.WriteLine("YamlDotNet is Broken.");
        }

        loggingNeeded = logEvents;

        lock (ConfigLock) {
            if (!assemblyLoadHooked) {
                AppDomain.CurrentDomain.AssemblyLoad += (_, eventArgs)
                    => LoadFromSystemConfigs(eventArgs.LoadedAssembly);
                assemblyLoadHooked = true;
            }
        }

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
