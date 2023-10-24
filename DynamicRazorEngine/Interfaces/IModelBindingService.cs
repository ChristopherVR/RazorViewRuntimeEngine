using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DynamicRazorEngine.Interfaces;

internal interface IModelBindingService
{
    Task<IList<ModelBindingResult>> BindControllerModelAsync(ControllerContext controllerContext);
    Task<IList<ModelBindingResult>> BindControllerModelAsync(ActionContext actionContext);
}
