using DynamicRazorEngine.Extensions;
using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using RuntimeLoading.Services;

namespace RuntimeLoading;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

        builder.Services.AddLogging();
        builder.Services.AddHttpContextAccessor();

        // To Configure default options

        builder.Services.Configure<ReportingConfig>(builder.Configuration.GetSection(ReportingConfig.Section));

        builder.Services.AddDynamicReportingServices<ReportingService>();
        builder.Services.AddSingleton<IExampleService, ExampleService>();

        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();
        var options = builder.Configuration.GetSection(ReportingConfig.Section).Get<ReportingConfig>();

        app.UseDynamicReporting(c =>
        {
            if (options is not null)
            {
                c.WithDefaultCache(options.DefaultRuntimeCache)
                .WithRoutePattern(options.RoutePattern)
                .WithBasePath(options.BasePath)
                .WithHttpMethods(options.HttpMethods);
            }
        });

        app.Run();
    }
}
