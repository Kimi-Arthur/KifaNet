using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using NLog;

namespace Kifa.Web.Api;

public class KifaDataControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        var candidates = GetAllDataModels().ToHashSet();
        var implemented = GetImplementedDataModels().ToList();
        candidates.ExceptWith(implemented.Select(c => c.BaseType.GetGenericArguments()[0]));

        foreach (var controller in implemented.Where(t => !t.IsAbstract)) {
            controller.BaseType.GetGenericArguments()[0].GetProperty("Client").SetValue(null,
                Activator.CreateInstance(controller.BaseType.GetGenericArguments()[1]));
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

    static IEnumerable<Type> GetImplementedDataModels() {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly
            => assembly.GetExportedTypes().Where(x
                => (x.BaseType?.IsGenericType ?? false) && x.BaseType?.GetGenericTypeDefinition() ==
                typeof(KifaDataController<,>)));
    }
}
