using DynamicRazorEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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

    public async Task<object?> BindControllerModelAsync(ControllerBase instance, ControllerContext controllerContext, ControllerActionDescriptor actionDescriptor)
        => await BindModelAsync(instance, controllerContext, actionDescriptor);

    public async Task<object?> BindControllerModelAsync(ControllerBase instance, ActionContext actionContext)
    {
        var controllerContext = new ControllerContext(actionContext)
        {
            ValueProviderFactories = _options.ValueProviderFactories,
        };

        return await BindModelAsync(instance, controllerContext, (ControllerActionDescriptor)actionContext.ActionDescriptor);
    }

    private async Task<object?> BindModelAsync(ControllerBase instance, ControllerContext controllerContext, ControllerActionDescriptor actionDescriptor)
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

        // Invoke the action method with the bound parameters
        var actionResult = actionDescriptor.MethodInfo.Invoke(instance, modelBindingResult.Select(x => x.Model).ToArray());

        //if (actionResult is Task methodTask)
        //{
        //    await methodTask.ConfigureAwait(false);

        //    if (!actionDescriptor.MethodInfo.ReturnType.IsGenericType)
        //    {
        //        return null;
        //    }
        //}

        var response = actionResult switch
        {
            // TODO: Cater for void responses.
            Task d => await (dynamic)d,
            _ => actionResult,
        };

        return response;
    }
}
