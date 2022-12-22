using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Kifa.Web.Api;

public class KifaDataControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature> {
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        var candidates = GetAllDataModels().ToHashSet();
        candidates.ExceptWith(GetImplementedDataModels());

        foreach (var candidate in candidates) {
            feature.Controllers.Add(typeof(KifaDataController<,>).MakeGenericType(candidate,
                typeof(KifaServiceJsonClient<>).MakeGenericType(candidate)).GetTypeInfo());
        }
    }

    static IEnumerable<Type> GetAllDataModels() {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly
            => assembly.GetExportedTypes().Where(x => x.GetInterface(nameof(WithModelId)) != null));
    }

    static IEnumerable<Type> GetImplementedDataModels() {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly
            => assembly.GetExportedTypes().Where(x
                => (x.BaseType?.IsGenericType ?? false) && x.BaseType?.GetGenericTypeDefinition() ==
                typeof(KifaDataController<,>)).Select(c => c.BaseType.GetGenericArguments()[0]));
    }
}
