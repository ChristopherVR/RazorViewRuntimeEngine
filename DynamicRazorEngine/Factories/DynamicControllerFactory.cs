﻿using DynamicRazorEngine.Provider;
using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DynamicRazorEngine.Factories;

internal sealed class DynamicControllerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly CompilationServices _compilationServices;
    private readonly ILogger<DynamicControllerFactory> _logger;

    public DynamicControllerFactory(
        IServiceProvider serviceProvider,
        CompilationServices compilationServices,
        IHttpContextAccessor contextAccessor,
        ILogger<DynamicControllerFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _compilationServices = compilationServices ?? throw new ArgumentNullException(nameof(compilationServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult?> ExecuteAsync(HttpContext context, string controllerPath, string endpoint)
    {
        var compilation = await LoadControllerTypeAsync(controllerPath);
        
        if (compilation.Type is null)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var controllerInstance = ActivatorUtilities.CreateInstance(_serviceProvider, compilation.Type);
  
        if (controllerInstance is null)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var actionResult = await InvokeControllerActionAsync(context, compilation.Type, compilation.ControllerAssembly, controllerInstance, endpoint);

        return actionResult;
    }

    private async Task<(TypeInfo? Type, Assembly ControllerAssembly)> LoadControllerTypeAsync(string controllerPath)
    {
        var fileContent = await File.ReadAllTextAsync(controllerPath);
        var compilation = _compilationServices.Compile(controllerPath, fileContent);
        return (compilation.CompiledType, compilation.Assembly);
    }

    private async Task<IActionResult> InvokeControllerActionAsync(HttpContext context, TypeInfo type, Assembly assembly, object instance, string actionName)
    {
        if (instance is not ControllerBase cb) 
        {
            return new NotFoundResult();
        }
    
        var actionProvider = context.RequestServices.GetService<IActionDescriptorCollectionProvider>()!;

        var assemblyPart = new AssemblyPart(assembly);

        var partManager = context.RequestServices.GetService<ApplicationPartManager>()!;


        if (!actionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Any(z => z.ControllerTypeInfo.FullName == type.FullName))
        {
            partManager.ApplicationParts.Add(assemblyPart);

            // Notify change
            StaticDescripterChangeProvider.Instance.Refresh();
        } 
        else
        {
            // TODO: do this better
            type = actionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .First(z => z.ControllerTypeInfo.FullName == type.FullName).ControllerTypeInfo;

            var controllerInstance = ActivatorUtilities.CreateInstance(context.RequestServices, type);

            instance = controllerInstance;
        }

        // TODO: Cater for multiple endpoints with the same action name.
        var actionDescripter = actionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .First(y => y.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase) && y.ControllerTypeInfo.FullName == type.FullName);

        var actionContext = new ActionContext(
            context,
            context.GetRouteData(),
            actionDescripter);

        var mvcOptions = context.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
        var parameterBinder = context.RequestServices.GetRequiredService<ParameterBinder>()!;
        var modelBinderFactory = context.RequestServices.GetRequiredService<IModelBinderFactory>()!;
        var modelMetadataProvider = context.RequestServices.GetRequiredService<IModelMetadataProvider>()!;

        var controllerContext = new ControllerContext(actionContext)
        {
            ValueProviderFactories = mvcOptions.ValueProviderFactories,
        };
       
        var valueProvider = await CompositeValueProvider.CreateAsync(controllerContext);
        
        var parameters = controllerContext.ActionDescriptor.Parameters;
        
        var parameterBindingInfo = GetParameterBindingInfo(
                        modelBinderFactory,
                        modelMetadataProvider,
                        controllerContext.ActionDescriptor);

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

            var model = await parameterBinder.BindModelAsync(
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
        var actionResult = actionDescripter.MethodInfo.Invoke(instance, modelBindingResult.Select(x => x.Model).ToArray());

        var response = actionResult switch
        {
            // TODO: Cater for void responses.
            Task d => await (dynamic)d,
            _ => actionResult,
        };

        if (instance is IDisposable dp)
        {
            dp.Dispose();
            partManager.ApplicationParts.Remove(assemblyPart);

            // Notify change
            StaticDescripterChangeProvider.Instance.Refresh();
        }

        if (response is not null)
        {
            if (response is ViewResult view)
            {
                return view;
            }

            return new OkObjectResult(response);
        }

        return new NotFoundResult();
    }

    private static BinderItem[]? GetParameterBindingInfo(
     IModelBinderFactory modelBinderFactory,
     IModelMetadataProvider modelMetadataProvider,
     ControllerActionDescriptor actionDescriptor)
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

            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            parameterBindingInfo[i] = new BinderItem(binder, metadata);
        }

        return parameterBindingInfo;
    }

    private readonly record struct BinderItem(IModelBinder ModelBinder, ModelMetadata ModelMetadata);

    /// <summary>
    /// Gets the constructor or method paramater value. The server's <see cref="IServiceProvider"/> will be used to resolve any dependency injection services.
    /// <br/> If a <see cref="HttpContext"/> is available, this will be used to resolve any request data being posted alongside the generated method.
    /// </summary>
    /// <param name="propInfo">The <see cref="ParameterInfo"/> information for the given method or constructor.</param>
    /// <param name="assembly">The <see cref="Assembly"/> that will be used to determine if the <see cref="Type"/> exists within it.</param>
    /// <param name="token">Cancellation token that can be passed as the parameter value.</param>
    /// <returns></returns>
    private async Task<object?> GetParameterValueAsync(ParameterInfo propInfo, Assembly assembly, CancellationToken token)
    {
        IServiceProvider serviceProvider = (_contextAccessor.HttpContext?.RequestServices ?? _serviceProvider)!;

        string? name = propInfo.Name?.ToUpperInvariant();

        if (propInfo.GetCustomAttributes(typeof(FromServicesAttribute), false).Any() || serviceProvider.GetService(propInfo.ParameterType) is not null)
        {
            return serviceProvider.GetService(propInfo.ParameterType);
        }

        if (propInfo.ParameterType == typeof(ILogger<>)
            || propInfo.ParameterType.GetInterface(nameof(ILogger)) is not null)
        {
            using var loggerFactory = serviceProvider.GetService<ILoggerFactory>()!;
            return CreateGenericLogger(loggerFactory, propInfo.ParameterType);
        }

        if (propInfo.ParameterType == typeof(CancellationToken))
        {
            return token;
        }

        if (TryGetReferencedTypeValue(propInfo, assembly, token, out var var))
        {
            return var;
        }

        if (propInfo.Name is not null && _contextAccessor.HttpContext is not null)
        {
            var httpContext = _contextAccessor.HttpContext;
            if (propInfo.GetCustomAttributes(typeof(FromFormAttribute), false).Any()
                   && httpContext.Request.HasFormContentType
                   && httpContext.Request.Form.Any(x => x.Key.ToUpperInvariant() == name))
            {
                return httpContext.Request.Form.TryGetValue(propInfo.Name, out var val) ? val : default;
            }

            if (httpContext.Request.Query.Any(x => x.Key.ToUpperInvariant() == name))
            {
                return Convert
                   .ChangeType(httpContext.Request.Query
                   .First(x => x.Key.ToUpperInvariant() == name).Value.ToString(), propInfo.ParameterType, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (httpContext.Request.Cookies.Any(x => x.Key.ToUpperInvariant() == name))
            {
                return httpContext.Request.Cookies
                   .First(x => x.Key.ToUpperInvariant() == name)
                   .Value
                   .ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (httpContext.Request.Headers.Any(x => x.Key.ToUpperInvariant() == name))
            {
                return httpContext.Request.Headers
                   .First(x => x.Key.ToUpperInvariant() == name)
                   .Value
                   .ToString();
            }

            if (propInfo.GetCustomAttributes(typeof(FromBodyAttribute), false).Any()
               && httpContext.Request.Body is not null)
            {
                using var sr = new StreamReader(httpContext.Request.Body);
                string body = await sr.ReadToEndAsync(token);
                object? data = System.Text.Json.JsonSerializer.Deserialize(body, propInfo.ParameterType);

                if (data is not null)
                {
                    return data;
                }
            }

            if (propInfo.ParameterType == typeof(IFormCollection))
            {
                return httpContext.Request.Form;
            }
        }

        if (!propInfo.ParameterType.IsValueType)
        {
            try
            {
                var instance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, propInfo.ParameterType);
                return instance;
            }
            catch
            {
                _logger.LogDebug("Failed to get or create an instance for {Name}", propInfo.ParameterType.FullName);
            }
        }

        if (propInfo.HasDefaultValue)
        {
            return propInfo.DefaultValue;
        }

        if (propInfo.ParameterType.IsValueType)
        {
            return Activator.CreateInstance(propInfo.ParameterType);
        }

        // See if there is any current assembly loaded into the AppDomain that satisfies the type
        foreach (var ase in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (TryGetReferencedTypeValue(propInfo, ase, token, out var value))
            {
                return value;
            }
        }

        return default;
    }

    /// <summary>
    /// Tries to get a refenced value for the <see cref="ParameterInfo"/> in the specified <see cref="Assembly"/>.
    /// </summary>
    /// <param name="propInfo"></param>
    /// <param name="assembly"></param>
    /// <param name="token"></param>
    /// <param name="value"></param>
    /// <returns>True if successful.</returns>
    private bool TryGetReferencedTypeValue(ParameterInfo propInfo, Assembly assembly, CancellationToken token, out object? value)
    {
        // This is for cases where the dynamic code injects a service that is defined in the dynamic code.
        if (!propInfo.ParameterType.IsValueType)
        {
            try
            {
                Type? objectType = assembly.GetType(propInfo.ParameterType.FullName!, throwOnError: false, ignoreCase: true);
                if (objectType is not null)
                {
                    var constructors = objectType.GetConstructors();
                    // assume we will have only one constructor
                    var firstConstrutor = constructors.FirstOrDefault() ?? objectType.GetConstructor(Type.EmptyTypes);
                    var parameters = firstConstrutor?.GetParameters().Select(y => GetParameterValueAsync(y, assembly, token)).ToArray();
                    value = firstConstrutor?.Invoke(parameters);
                    return true;
                }
            }
            catch
            {
                _logger.LogError("Failed to invoke type when attempted to find a reference for {PropInfo} in Assembly {Assembly}", propInfo.ParameterType.FullName, assembly.FullName);
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Creates a generic <see cref="Logger{T}"/> logger using reflection.
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="declaredType"></param>
    /// <returns></returns>
    internal static object CreateGenericLogger(ILoggerFactory factory, Type declaredType)
    {
        Type genericClass = typeof(ILogger<>).MakeGenericType(declaredType);
        const string CreateLogger = nameof(CreateLogger);
        var genericType = declaredType.GetGenericArguments().First();
        var mi = typeof(LoggerFactoryExtensions).GetMethods().Single(m => m.Name == CreateLogger && m.IsGenericMethodDefinition);
        var gi = mi.MakeGenericMethod(declaredType.GetGenericArguments().First());
        return gi.Invoke(null, new[] { factory })!;
    }
}