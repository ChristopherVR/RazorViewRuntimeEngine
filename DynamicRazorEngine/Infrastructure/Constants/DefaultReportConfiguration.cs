namespace DynamicRazorEngine.Factories;

internal static class DefaultReportConfiguration
{
    internal static ReportingConfig Default() => new()
    {
        BasePath = "wwwroot\\Reports",
        HttpMethods = new[] { "GET", "POST", "PATCH", "PUT", "DELETE", "PATCH" },
        RoutePattern = "/reports/{reportId:int}/{action?}/{controller?}",
        DefaultRuntimeCache = TimeSpan.FromHours(6),
    };
}
