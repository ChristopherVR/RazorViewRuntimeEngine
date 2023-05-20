using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace DynamicRazorEngine.Interfaces;

internal interface IModelBindingService
{
    Task<object?> BindControllerModelAsync(ControllerBase instance, ControllerContext controllerContext, ControllerActionDescriptor actionDescriptor);
    Task<object?> BindControllerModelAsync(ControllerBase instance, ActionContext actionContext);
}
