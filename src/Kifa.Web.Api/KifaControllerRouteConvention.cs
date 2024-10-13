using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using NLog;

namespace Kifa.Web.Api;

public class KifaControllerRouteConvention : IControllerModelConvention {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Apply(ControllerModel controller) {
        Logger.Debug($"Adding route for {controller.ControllerType.FullName}.");
        var controllerType = controller.ControllerType.GetDataControllerType();
        if (controllerType == null) {
            return;
        }

        var endpoint = (controllerType.GetGenericArguments()[0].GetProperty("ModelId")
            .GetValue(null) as string)!;
        Logger.Debug($"Adding endpoint /api/{endpoint} for {controllerType}.");
        controller.Selectors.Add(new SelectorModel {
            AttributeRouteModel = new AttributeRouteModel(new RouteAttribute("api/" + endpoint)),
        });
    }
}
