using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DynamicRazorEngine.Factories;

internal sealed class RazorViewFactory
{
    private readonly ILogger<RazorViewFactory> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IHttpContextAccessor _contextAccessor;

    public RazorViewFactory(
        ILogger<RazorViewFactory> logger,
        IRazorViewEngine razorViewEngine,
        IHttpContextAccessor httpContextAccessor)
    {
        _contextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _razorViewEngine = razorViewEngine ?? throw new ArgumentNullException(nameof(razorViewEngine));
    }

    public async Task<IActionResult> ExecuteAsync(HttpContext context, string pagePath)
    {
        _logger.LogDebug("Creating RazorView with path {pagePath}", pagePath);

        var pageViewResult = _razorViewEngine.GetView(null, pagePath, true);
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
