using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace DynamicRazorEngine.Factories;

internal sealed class RazorViewFactory : IRazorViewFactory
{
    private readonly ILogger<RazorViewFactory> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IReportService _reportService;

    public RazorViewFactory(
        ILogger<RazorViewFactory> logger,
        IRazorViewEngine razorViewEngine,
        IHttpContextAccessor httpContextAccessor,
        IReportService reportService)
    {
        _contextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _razorViewEngine = razorViewEngine ?? throw new ArgumentNullException(nameof(razorViewEngine));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    }

    public async Task<IActionResult> ExecuteAsync(RazorViewFactoryRequest request)
    {
        var report = await _reportService.GetAsync(request.ReportId) ?? throw new ArgumentException(nameof(request.ReportId));

        _logger.LogDebug("Creating RazorView with path {pagePath}", report.Path);
        var viewPath = $"{report.Path}/{report.MainView ?? "Index"}.cshtml";

        var pageViewResult = _razorViewEngine.GetView(null, viewPath, isMainPage: true);

        var pageView = pageViewResult.View;

        // TODO: Need to compile the controller first in order to extract this info.
        //var actionProvider = context.RequestServices.GetService<IActionDescriptorCollectionProvider>()!;

        //// TODO: Cater for multiple endpoints with the same action name.
        //var actionDescripter = actionProvider.ActionDescriptors.Items
        //    .OfType<ControllerActionDescriptor>()
        //    .First(y => y.ActionName == "Index" && y.ControllerTypeInfo == type);

        var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext
        {
            HttpContext = _contextAccessor.HttpContext!,
            RouteData = _contextAccessor.HttpContext!.GetRouteData(),
            ViewData = new(new EmptyModelMetadataProvider(), new()),
            ActionDescriptor = new(),
            Writer = new StringWriter(),
        };

        if (pageView is not null)
        {
            await pageView.RenderAsync(viewContext);
        }

        var html = viewContext.Writer.ToString();

        return new ContentResult
        {
            ContentType = "text/html",
            StatusCode = 200,
            Content = html,
        };
    }
}
