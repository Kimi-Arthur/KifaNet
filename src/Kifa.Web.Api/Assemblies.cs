using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace Kifa.Web.Api;

public static class Assemblies {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static List<string> Namespaces { get; set; } = new();

    public static void LoadAll() {
        var baseDir = AppContext.BaseDirectory;
        if (!Directory.Exists(baseDir)) {
            return;
        }

        Logger.Trace($"Preloading all assemblies and references from {baseDir}...");

        var queue = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        var visited = queue.Select(a => a.FullName).ExceptNull().ToHashSet();

        foreach (var file in Directory.EnumerateFiles(baseDir, "*.dll")) {
            try {
                var assembly = Assembly.LoadFrom(file);
                if (assembly.FullName != null && visited.Add(assembly.FullName)) {
                    queue.Enqueue(assembly);
                }
            } catch {
                // Ignore non-.NET or incompatible native files.
            }
        }

        while (queue.Count > 0) {
            var assembly = queue.Dequeue();
            foreach (var referenced in assembly.GetReferencedAssemblies()) {
                if (!visited.Add(referenced.FullName)) {
                    continue;
                }

                try {
                    queue.Enqueue(Assembly.Load(referenced));
                } catch {
                    // Ignore optional or unresolvable references.
                }
            }
        }
    }
}
