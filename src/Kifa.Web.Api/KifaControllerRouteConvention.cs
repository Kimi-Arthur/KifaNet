using System.Reflection;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using NLog;

namespace Kifa.Web.Api;

public class KifaControllerRouteConvention : IControllerModelConvention {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Apply(ControllerModel controller) {
        var controllerType = controller.ControllerType;
        if (controllerType.BaseType?.IsGenericType ?? false) {
            controllerType = controllerType.BaseType.GetTypeInfo();
        }

        if (controllerType.IsGenericType && controllerType.GetGenericTypeDefinition() ==
            typeof(KifaDataController<,>)) {
            var endpoint = (controllerType.GetGenericArguments()[0].GetProperty("ModelId")
                .GetValue(null) as string)!;
            Logger.Info($"Found {controllerType}: {endpoint}");
            controller.Selectors.Add(new SelectorModel {
                AttributeRouteModel =
                    new AttributeRouteModel(new RouteAttribute("api/" + endpoint)),
            });
        }
    }
}
