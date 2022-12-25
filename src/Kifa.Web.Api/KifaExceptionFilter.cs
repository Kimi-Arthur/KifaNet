using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kifa.Web.Api;

public class KifaExceptionFilter : ExceptionFilterAttribute {
    public override void OnException(ExceptionContext context) {
        if (context.Exception is DataModelNotFoundException) {
            context.Result = new NotFoundResult();
        }
    }
}
