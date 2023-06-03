using DynamicRazorEngine.Extensions;
using DynamicRazorEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace DynamicRazorEngine.Infrastructure.ModelBinder;

internal sealed class ModelBindingService : IModelBindingService
{
    private readonly MvcOptions _options;
    private readonly ParameterBinder _parameterBinder;
    private readonly IModelBinderFactory _modelBinderFactory;
    private readonly IModelMetadataProvider _metadataProvider;

    public ModelBindingService(
        IOptions<MvcOptions> options, 
        ParameterBinder parameterBinder, 
        IModelBinderFactory modelBinderFactory, 
        IModelMetadataProvider metadataProvider)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _parameterBinder = parameterBinder ?? throw new ArgumentNullException(nameof(parameterBinder));
        _modelBinderFactory = modelBinderFactory ?? throw new ArgumentNullException(nameof(modelBinderFactory));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
    }

    public async Task<IList<ModelBindingResult>> BindControllerModelAsync(ControllerBase instance, ControllerContext controllerContext, Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor)
        => await BindModelAsync(instance, controllerContext, actionDescriptor);

    public async Task<IList<ModelBindingResult>> BindControllerModelAsync(ControllerBase instance, ActionContext actionContext)
    {
        var controllerContext = new ControllerContext(actionContext)
        {
            ValueProviderFactories = _options.ValueProviderFactories,
        };

        return await BindModelAsync(instance, controllerContext, (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)actionContext.ActionDescriptor);
    }

    private async Task<IList<ModelBindingResult>> BindModelAsync(ControllerBase instance, ControllerContext controllerContext, Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor)
    {
        var valueProvider = await CompositeValueProvider.CreateAsync(controllerContext);

        var parameters = controllerContext.ActionDescriptor.Parameters;

        var parameterBindingInfo = controllerContext.ActionDescriptor.GetParameterBindingInfo(_modelBinderFactory, _metadataProvider);

        var modelBindingResult = new List<ModelBindingResult>();

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var bindingInfo = parameterBindingInfo![i];
            var modelMetadata = bindingInfo.ModelMetadata;

            if (!modelMetadata.IsBindingAllowed)
            {
                continue;
            }

            var model = await _parameterBinder.BindModelAsync(
                controllerContext,
                bindingInfo.ModelBinder,
                valueProvider,
                parameter,
                modelMetadata,
                value: modelMetadata.ModelType.IsValueType ? Activator.CreateInstance(modelMetadata.ModelType) : default);

            if (!model.IsModelSet && modelMetadata.ModelType.IsValueType)
            {
                model = ModelBindingResult.Success(Activator.CreateInstance(modelMetadata.ModelType));
            }

            modelBindingResult.Add(model);
        }

        return modelBindingResult;
    }
}
