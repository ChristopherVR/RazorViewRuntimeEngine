using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicRazorEngine.Middleware;

public record ReportingConfig
{
    public required string BasePath { get; init; }
    public TimeSpan DefaultRuntimeCache { get; init; } = TimeSpan.FromMinutes(1);
    public string? RoutePattern { get; init; }
    public string? BaseIndexRoutePattern { get; init; }
    public string[]? HttpMethods { get; init; }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseDynamicReporting(this WebApplication app) => UseDynamicReporting(app, config: null);

    public static IApplicationBuilder UseDynamicReporting(this WebApplication app, Action<ReportingConfig>? config)
    {
        var cfg = new ReportingConfig()
        {
            BasePath = "wwwroot/reports",
            RoutePattern = "/reports/{reportId:int}/{action}",
            BaseIndexRoutePattern = "/reports/{reportId:int}",
            HttpMethods = new[] { "PATCH", "POST", "GET", "DELETE", "PUT" },
        };

        config?.Invoke(cfg);
        app.MapGet(cfg.BaseIndexRoutePattern!, async (z) => await RenderReportViewAsync(z, app, cfg));
        
        app.MapMethods(cfg.RoutePattern!, cfg.HttpMethods!, async (context) => await HandleReportActionsAsync(context, app, cfg));
        return app;
    }


    private static async Task HandleReportActionsAsync(HttpContext context, IApplicationBuilder app, ReportingConfig config)
    {
        if (!context.Request.RouteValues.TryGetValue("reportId", out var id) || !context.Request.RouteValues.TryGetValue("action", out var action))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var factory = app.ApplicationServices.GetService<DynamicControllerFactory>()!;

        var response = await factory.ExecuteAsync($"{config.BasePath}/{id}/Report{nameof(Microsoft.AspNetCore.Mvc.Controller)}.cs", action!.ToString()!);

        if (response is not null)
        {
            await response.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext()
            {
                HttpContext = context,
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
                RouteData = context.GetRouteData(),
            });
        }
    }

    private static async Task RenderReportViewAsync(HttpContext context, IApplicationBuilder app, ReportingConfig config)
    {
        var factory = app.ApplicationServices.GetService<RazorViewFactory>()!;

        if (!context.Request.RouteValues.TryGetValue("reportId", out var id))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var response = await factory.ExecuteAsync($"{config.BasePath}/{id}/Index.cshtml");

        await response.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext()
        {
            HttpContext = context,
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
            RouteData = context.GetRouteData(),
        });
    }
}
