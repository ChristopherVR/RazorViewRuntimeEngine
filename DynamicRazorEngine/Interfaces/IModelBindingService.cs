using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DynamicRazorEngine.Interfaces;

internal interface IModelBindingService
{
    Task<IList<ModelBindingResult>> BindControllerModelAsync(ControllerBase instance, ControllerContext controllerContext, ControllerActionDescriptor actionDescriptor);
    Task<IList<ModelBindingResult>> BindControllerModelAsync(ControllerBase instance, ActionContext actionContext);
}
