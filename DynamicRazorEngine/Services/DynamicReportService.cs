using DynamicRazorEngine.Extensions;
using DynamicRazorEngine.Factories;
using DynamicRazorEngine.Interfaces;
using DynamicRazorEngine.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DynamicRazorEngine.Services;

internal sealed class DynamicReportService : IDynamicReportService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CompilationService _compilationServices;
    private readonly ILogger<DynamicReportService> _logger;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly ApplicationPartManager _applicationPartManager;
    private readonly IReportService _reportService;
    private readonly IModelBindingService _modelBindingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicReportService(
        IServiceProvider serviceProvider,
        CompilationService compilationServices,
        ILogger<DynamicReportService> logger,
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        ApplicationPartManager applicationPartManager,
        IReportService reportService,
        IModelBindingService modelBindingService,
        IHttpContextAccessor httpContextAccessor)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _compilationServices = compilationServices ?? throw new ArgumentNullException(nameof(compilationServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider ?? throw new ArgumentNullException(nameof(actionDescriptorCollectionProvider));
        _applicationPartManager = applicationPartManager ?? throw new ArgumentNullException(nameof(applicationPartManager));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _modelBindingService = modelBindingService ?? throw new ArgumentNullException(nameof(modelBindingService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<(IActionResult? Result, ActionContext? ActionContext)> ExecuteAsync(ControllerFactoryRequest request)
    {
        LoggerMessage.Define<ControllerFactoryRequest>(LogLevel.Debug, new EventId(), "Executing DynamicControllerFactor for {Request}")(_logger, request, null);
        var report = await _reportService.GetAsync(request.ReportId).ConfigureAwait(false) ?? throw new ArgumentException(nameof(request.ReportId));

        var compilation = await _compilationServices.CompileAsync(report).ConfigureAwait(false);

        if (!compilation.Success || compilation.MainControllerType is null)
        {
            return (new StatusCodeResult(StatusCodes.Status500InternalServerError), null);
        }

        using var controllerInstance = compilation.MainControllerType.CreateInstance<Controller>(compilation.Assembly, _serviceProvider);

        if (controllerInstance is not Controller cb)
        {
            return (new StatusCodeResult(StatusCodes.Status500InternalServerError), null);
        }

        var actionResult = await InvokeControllerActionAsync(compilation.MainControllerType, compilation.Assembly, cb, request).ConfigureAwait(false);

        return actionResult;
    }

    private async Task<(IActionResult? Result, ActionContext ActionContext)> InvokeControllerActionAsync(TypeInfo type, Assembly assembly, Controller instance, ControllerFactoryRequest request)
    {
        var assemblyPart = new AssemblyPart(assembly);

        if (!_actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Any(z => z.ControllerTypeInfo.FullName == type.FullName))
        {
            _applicationPartManager.ApplicationParts.Add(assemblyPart);

            StaticDescripterChangeProvider.Instance.Refresh();
        }
        try
        {
            var actionProvider = _actionDescriptorCollectionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(z => z.ControllerTypeInfo.FullName == type.FullName && z.EndpointMetadata.OfType<HttpMethodMetadata>().Any(y => y.HttpMethods.Contains(_httpContextAccessor.HttpContext!.Request.Method)))
                .First(z => z.ActionName.Equals(request.Action, StringComparison.OrdinalIgnoreCase) || z.ActionName.StartsWith(request.Action, StringComparison.OrdinalIgnoreCase));

            var controllerContext = request.ControllerContext ?? new(new ActionContext()
            {
                HttpContext = _httpContextAccessor.HttpContext!,
                ActionDescriptor = actionProvider,
                RouteData = _httpContextAccessor.HttpContext!.GetRouteData(),
            });

            instance.ControllerContext = controllerContext;

            var modelBindingResult = request.ControllerContext is not null
                ? await _modelBindingService.BindControllerModelAsync(request.ControllerContext).ConfigureAwait(false)
                : await _modelBindingService.BindControllerModelAsync(controllerContext).ConfigureAwait(false);


            // Invoke the action method with the bound parameters
            var actionResult = actionProvider.MethodInfo.Invoke(instance, modelBindingResult.Select(x => x.Model).ToArray());

            if (actionResult is Task methodTask && !actionProvider.MethodInfo.ReturnType.IsGenericType)
            {
                await methodTask.ConfigureAwait(false);

                return (null, controllerContext);
            }

            var response = actionResult switch
            {
                Task d => await (dynamic)d,
                _ => actionResult,
            };

            if (instance is IDisposable dp)
            {
                dp.Dispose();
            }

            if (response is not null)
            {
                if (response is ViewResult view)
                {
                    return (view, instance.ControllerContext);
                }

                return (new OkObjectResult(response), instance.ControllerContext);
            }
        }
#pragma warning disable CA1031
        catch
        {

        }
#pragma warning restore CA1031
        finally
        {
            _applicationPartManager.ApplicationParts.Remove(assemblyPart);

            StaticDescripterChangeProvider.Instance.Refresh();
        }

        return (new NotFoundResult(), instance.ControllerContext);
    }
}
