using DynamicRazorEngine.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DynamicRazorEngine.Extensions;

internal static class ControllerActionDescriptorExtensions
{
    internal static BinderItem[]? GetParameterBindingInfo(
        this Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor,
        IModelBinderFactory modelBinderFactory,
        IModelMetadataProvider modelMetadataProvider)
    {
        var parameters = actionDescriptor.Parameters;
        if (parameters.Count == 0)
        {
            return null;
        }

        var parameterBindingInfo = new BinderItem[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            ModelMetadata metadata;
            if (
                modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase &&
                parameter is ControllerParameterDescriptor controllerParameterDescriptor)
            {
                // The default model metadata provider derives from ModelMetadataProvider
                // and can therefore supply information about attributes applied to parameters.
                metadata = modelMetadataProviderBase.GetMetadataForParameter(controllerParameterDescriptor.ParameterInfo);
            }
            else
            {
                // For backward compatibility, if there's a custom model metadata provider that
                // only implements the older IModelMetadataProvider interface, access the more
                // limited metadata information it supplies. In this scenario, validation attributes
                // are not supported on parameters.
                metadata = modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            }

            var binder = modelBinderFactory.CreateBinder(new()
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            parameterBindingInfo[i] = new(binder, metadata);
        }

        return parameterBindingInfo;
    }
}
