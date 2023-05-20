using DynamicRazorEngine.Extensions;
using DynamicRazorEngine.Interfaces;
using DynamicRazorEngine.Provider;
using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Reflection;


namespace DynamicRazorEngine.Factories;

internal sealed class DynamicControllerFactory : IDynamicControllerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CompilationServices _compilationServices;
    private readonly ILogger<DynamicControllerFactory> _logger;
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly ApplicationPartManager _applicationPartManager;
    private readonly IReportService _reportService;
    private readonly IModelBindingService _modelBindingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicControllerFactory(
        IServiceProvider serviceProvider,
        CompilationServices compilationServices,
        ILogger<DynamicControllerFactory> logger,
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
    
    public async Task<IActionResult?> ExecuteAsync(ControllerFactoryRequest request)
    {
        _logger.LogDebug("Executing DynamicControllerFactor for {request}", request);
        var report = await _reportService.GetAsync(request.ReportId) ?? throw new ArgumentException(nameof(request.ReportId));

        var compilation = await LoadControllerTypeAsync(report.Path);
        
        if (compilation.Type is null)
        {
            return new NotFoundResult();
        }

        var controllerInstance = compilation.Type.CreateControllerInstance(compilation.ControllerAssembly, _serviceProvider);
  
        if (controllerInstance is not ControllerBase cb)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var actionResult = await InvokeControllerActionAsync(compilation.Type, compilation.ControllerAssembly, cb, request);

        return actionResult;
    }

    private async Task<(TypeInfo? Type, Assembly ControllerAssembly)> LoadControllerTypeAsync(string controllerPath)
    {
        // TODO: should not be hardcoded & support multiple files.
        var fileContent = await File.ReadAllTextAsync($"{controllerPath}/ReportController.cs");
        var compilation = _compilationServices.Compile(controllerPath, fileContent);
        return (compilation.CompiledType, compilation.Assembly);
    }

    private async Task<IActionResult> InvokeControllerActionAsync(TypeInfo type, Assembly assembly, ControllerBase instance, ControllerFactoryRequest request)
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
                .First(z => z.ActionName.Equals(request.Action, StringComparison.OrdinalIgnoreCase)
                && z.ControllerTypeInfo.FullName == type.FullName
                && z.EndpointMetadata.OfType<HttpMethodMetadata>().Any(y => y.HttpMethods.Contains(_httpContextAccessor.HttpContext!.Request.Method)));

            var actionContext = new ActionContext()
            {
                HttpContext = _httpContextAccessor.HttpContext!,
                ActionDescriptor = actionProvider,
                RouteData = _httpContextAccessor.HttpContext!.GetRouteData(),
            };

            instance.ControllerContext = request.ControllerContext ?? new ControllerContext(actionContext);

            var response = request.ControllerContext is not null
                ? await _modelBindingService.BindControllerModelAsync(instance, request.ControllerContext, actionProvider)
                : await _modelBindingService.BindControllerModelAsync(instance, actionContext);

            if (instance is IDisposable dp)
            {
                dp.Dispose();
                _applicationPartManager.ApplicationParts.Remove(assemblyPart);

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
        }
        catch
        {

        }
        finally
        {
            _applicationPartManager.ApplicationParts.Remove(assemblyPart);

            StaticDescripterChangeProvider.Instance.Refresh();
        }

        return new NotFoundResult();
    }
}
