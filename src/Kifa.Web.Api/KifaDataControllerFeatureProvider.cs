using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kifa.Web.Api.Controllers;
using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using NLog;

namespace Kifa.Web.Api;

public class KifaDataControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        var candidates = GetAllDataModels().ToHashSet();
        Logger.Debug($"Candidate data models ({candidates.Count}):");
        foreach (var candidate in candidates) {
            Logger.Debug($"\t{candidate.FullName}");
        }

        var implemented = GetImplementingControllers().ToList();
        Logger.Debug($"Implemented data models ({implemented.Count}):");
        foreach (var imp in implemented) {
            Logger.Debug($"\t{imp.GetGenericArguments()[0]}: {imp.GetGenericArguments()[1]}");
        }

        candidates.ExceptWith(implemented.Select(c => c.GetGenericArguments()[0]));

        foreach (var controller in implemented) {
            Logger.Debug($"Processing {controller.FullName}");
            controller.GetGenericArguments()[0].GetProperty("Client").SetValue(null,
                Activator.CreateInstance(controller.GetGenericArguments()[1]));
        }

        Logger.Debug($"To be added data models ({candidates.Count}):");
        foreach (var candidate in candidates) {
            Logger.Debug($"\t{candidate.FullName}");
        }

        foreach (var candidate in candidates.Where(t => t.CustomAttributes.All(a
                     => a.AttributeType != typeof(SkipControllerAttribute)))) {
            Logger.Debug($"Adding client and controller for {candidate}...");
            feature.Controllers.Add(typeof(KifaDataController<,>).MakeGenericType(candidate,
                typeof(KifaServiceJsonClient<>).MakeGenericType(candidate)).GetTypeInfo());
            candidate.GetProperty("Client").SetValue(null,
                Activator.CreateInstance(
                    typeof(KifaServiceJsonClient<>).MakeGenericType(candidate)));
        }
    }

    static IEnumerable<Type> GetAllDataModels() {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly
            => assembly.GetExportedTypes().Where(x
                => x.GetInterfaces().Any(i
                    => i.FullName?.StartsWith("Kifa.Service.WithModelId`1[") ?? false)));
    }

    static IEnumerable<Type> GetImplementingControllers() {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly
            .GetExportedTypes().Select(x => x.GetDataControllerType())).ExceptNull();
    }
}
