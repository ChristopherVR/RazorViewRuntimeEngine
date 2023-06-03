using DynamicRazorEngine.Factories;
using DynamicRazorEngine.Infrastructure.ModelBinder;
using DynamicRazorEngine.Interfaces;
using DynamicRazorEngine.Provider;
using DynamicRazorEngine.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicRazorEngine.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IServiceCollection AddDynamicReportingServices(this IServiceCollection services)
    {
        services = services
        .AddSingleton<CompilationService>()
        .AddSingleton<IDynamicReportService, DynamicReportService>()
        .AddSingleton<IActionDescriptorChangeProvider>(StaticDescripterChangeProvider.Instance)
        .AddSingleton(StaticDescripterChangeProvider.Instance)
        .AddSingleton<IModelBindingService, ModelBindingService>();

        return services;
    }

    public static IApplicationBuilder UseDynamicReporting(this WebApplication app) => app.UseDynamicReporting(config: null);

    public static IApplicationBuilder UseDynamicReporting(this WebApplication app, Action<ReportingConfig>? config)
    {
        var cfg = DefaultReportConfiguration.Default();

        config?.Invoke(cfg);
        app.MapMethods(cfg.RoutePattern, cfg.HttpMethods, async (context) => await HandleReportActionsAsync(context, app));
        return app;
    }

    private static async Task HandleReportActionsAsync(HttpContext context, IApplicationBuilder app)
    {
        if (!context.Request.RouteValues.TryGetValue("reportId", out var id))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        _ = context.Request.RouteValues.TryGetValue("action", out var action);
        _ = context.Request.RouteValues.TryGetValue("controller", out var controller);

        var factory = app.ApplicationServices.GetService<IDynamicReportService>()!;

        var (result, actionContext) = await factory.ExecuteAsync(new(long.Parse(id!.ToString()!), action?.ToString() ?? "Index", controller?.ToString(), null));

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
}
