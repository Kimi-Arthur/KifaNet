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
        Logger.Trace(
            $"Loading extra assemblies with names starting with {string.Join(", ", Namespaces)} from {AppDomain.CurrentDomain.BaseDirectory}...");

        var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.Location).ToHashSet();

        Logger.Trace("Already loaded assemblies");
        foreach (var ass in loaded) {
            Logger.Trace($"\t{ass}");
        }

        foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                     .Where(f => !loaded.Contains(f))) {
            var fileName = file[(file.LastIndexOf("/") + 1)..];
            if (Namespaces.Any(ns => fileName.StartsWith(ns + "."))) {
                Logger.Trace($"Loading assembly {file}...");
                Assembly.LoadFile(file);
            }
        }
    }
}
