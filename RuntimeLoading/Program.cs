using DynamicRazorEngine.Middleware;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RuntimeLoading;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages()
            .AddControllersAsServices();

        builder.Services.AddLogging();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddDynamicReportingServices();

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

        app.UseDynamicReporting();

        app.Run();
    }
}
