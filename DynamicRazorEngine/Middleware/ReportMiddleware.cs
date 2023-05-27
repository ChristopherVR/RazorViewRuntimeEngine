using DynamicRazorEngine.Factories;
using DynamicRazorEngine.Infrastructure.ModelBinder;
using DynamicRazorEngine.Interfaces;
using DynamicRazorEngine.Provider;
using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicRazorEngine.Middleware;

public static class ApplicationBuilderExtensions
{
    public static IServiceCollection AddDynamicReportingServices(this IServiceCollection services)
        => services
        .AddSingleton<CompilationServices>()
        .AddSingleton<IRazorViewFactory, RazorViewFactory>()
        .AddSingleton<IDynamicControllerFactory, DynamicControllerFactory>()
        .AddSingleton<IActionDescriptorChangeProvider>(StaticDescripterChangeProvider.Instance)
        .AddSingleton(StaticDescripterChangeProvider.Instance)
        .AddSingleton<IModelBindingService, ModelBindingService>();

    public static IApplicationBuilder UseDynamicReporting(this WebApplication app) => UseDynamicReporting(app, config: null);

    public static IApplicationBuilder UseDynamicReporting(this WebApplication app, Action<ReportingConfig>? config)
    {
        var cfg = new ReportingConfig()
        {
            BasePath = "wwwroot/reports",
            RoutePattern = "/reports/{reportId:int}/{action}/{controller:?}",
            BaseIndexRoutePattern = "/reports/{reportId:int}",
            HttpMethods = new[] { "PATCH", "POST", "GET", "DELETE", "PUT" },
        };

        config?.Invoke(cfg);
        app.MapGet(cfg.BaseIndexRoutePattern, async (z) => await RenderReportViewAsync(z, app, cfg));

        app.MapMethods(cfg.RoutePattern, cfg.HttpMethods, async (context) => await HandleReportActionsAsync(context, app, cfg));
        return app;
    }

    private static async Task HandleReportActionsAsync(HttpContext context, IApplicationBuilder app, ReportingConfig config)
    {
        if (!context.Request.RouteValues.TryGetValue("reportId", out var id)
            || !context.Request.RouteValues.TryGetValue("action", out var action))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        _ = context.Request.RouteValues.TryGetValue("controller", out var controller);

        var factory = app.ApplicationServices.GetService<IDynamicControllerFactory>()!;

        var (result, actionContext) = await factory.ExecuteAsync(new(long.Parse(id!.ToString()!), action!.ToString()!, controller?.ToString(), null));

        if (result is not null)
        {
            await result.ExecuteResultAsync(actionContext ?? new()
            {
                HttpContext = context,
                ActionDescriptor = new(),
                RouteData = context.GetRouteData(),
            });
        }
    }

    private static async Task RenderReportViewAsync(HttpContext context, IApplicationBuilder app, ReportingConfig config)
    {
        var factory = app.ApplicationServices.GetService<IRazorViewFactory>()!;

        if (!context.Request.RouteValues.TryGetValue("reportId", out var id))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var response = await factory.ExecuteAsync(new(long.Parse(id!.ToString()!), null));

        await response.ExecuteResultAsync(new()
        {
            HttpContext = context,
            ActionDescriptor = new(),
            RouteData = context.GetRouteData(),
        });
    }
}
