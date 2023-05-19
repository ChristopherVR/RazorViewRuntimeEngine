using DynamicRazorEngine.Factories;
using DynamicRazorEngine.Provider;
using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DynamicRazorEngine.Middleware;

public record ReportingConfig
{
    public required string BasePath { get; init; }
    public TimeSpan DefaultRuntimeCache { get; init; } = TimeSpan.FromMinutes(1);
    public required string RoutePattern { get; init; }
    public required string BaseIndexRoutePattern { get; init; }
    public required string[] HttpMethods { get; init; }
}

[Route("[controller]")]
public class MainReportController : Controller
{
    [HttpGet("{reportId:int}")]
    public async Task<IActionResult> IndexAsync(int reportId) => await RenderReportViewAsync(reportId);

    [Route("{reportId:int}/{action}")]
    public async Task<IActionResult> EverythingElseAsync(int reportId, string action) => await HandleReportActionsAsync(reportId, action);

    private async Task<IActionResult> HandleReportActionsAsync(int reportId, string action)
    {
        var factory = HttpContext.RequestServices.GetService<DynamicControllerFactory>()!;

        var response = await factory.ExecuteAsync(HttpContext, $"{"wwwroot/reports"}/{reportId}/Asd.cs", action!.ToString()!);

        return response!;
    }

    private async Task<IActionResult> RenderReportViewAsync(int reportId)
    {
        var factory = HttpContext.RequestServices.GetService<RazorViewFactory>()!;

        var response = await factory.ExecuteAsync(HttpContext, $"{"wwwroot/reports"}/{reportId}/Index.cshtml");

        return response!;
    }
}

public static class ApplicationBuilderExtensions
{
    public static IServiceCollection AddDynamicReportingServices(this IServiceCollection services)
        => services
        .AddSingleton<CompilationServices>()
        .AddSingleton<RazorViewFactory>()
        .AddSingleton<DynamicControllerFactory>()
        .AddSingleton<IActionDescriptorChangeProvider>(StaticDescripterChangeProvider.Instance)
        .AddSingleton(StaticDescripterChangeProvider.Instance);

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
        app.MapGet(cfg.BaseIndexRoutePattern, async (z) => await RenderReportViewAsync(z, app, cfg));
        
        app.MapMethods(cfg.RoutePattern, cfg.HttpMethods, async (context) => await HandleReportActionsAsync(context, app, cfg));
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

        var response = await factory.ExecuteAsync(context, $"{config.BasePath}/{id}/Asd.cs", action!.ToString()!);

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

        var response = await factory.ExecuteAsync(context, $"{config.BasePath}/{id}/Index.cshtml");

        await response.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext()
        {
            HttpContext = context,
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
            RouteData = context.GetRouteData(),
        });
    }
}
