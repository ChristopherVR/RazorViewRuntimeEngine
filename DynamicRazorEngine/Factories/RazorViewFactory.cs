using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DynamicRazorEngine.Factories;

public class RazorViewFactory
{
    private readonly ILogger<RazorViewFactory> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IHttpContextAccessor _contextAccessor;

    public RazorViewFactory(
        ILogger<RazorViewFactory> logger,
        IRazorViewEngine razorViewEngine,
        IHttpContextAccessor httpContextAccessor)
    {
        _contextAccessor = httpContextAccessor;
        _logger = logger;
        _razorViewEngine = razorViewEngine;
    }

    public async Task<IActionResult> ExecuteAsync(string pagePath)
    {
        _logger.LogDebug("Creating RazorView with path {pagePath}", pagePath);

        var pageViewResult = _razorViewEngine.GetView(null, pagePath, true);
        var pageView = pageViewResult.View;

        var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext
        {
            HttpContext = _contextAccessor.HttpContext!,
            RouteData = _contextAccessor.HttpContext!.GetRouteData(),
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new EmptyModelMetadataProvider(), new()),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
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
