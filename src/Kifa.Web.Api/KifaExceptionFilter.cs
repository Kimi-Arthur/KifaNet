using System;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kifa.Web.Api;

public class KifaExceptionFilter : ExceptionFilterAttribute {
    public override void OnException(ExceptionContext context) {
        if (context.Exception is DataModelNotFoundException) {
            context.Result = new NotFoundResult();
        } else if (context.Exception is ArgumentException argEx) {
            context.Result = new BadRequestObjectResult(new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = argEx.Message
            });
            context.ExceptionHandled = true;
        }
    }
}
