namespace DynamicRazorEngine.Factories;

public sealed class ReportingConfig
{
    public static ReportingConfig Default => new(
        "wwwroot\\Reports",
        TimeSpan.FromHours(6),
        "/reports/{reportId:int}/{action?}/{controller?}",
        new string[] { "GET", "POST", "PATCH", "PUT", "DELETE", "PATCH" });

    public const string Section = "Reporting";
    public ReportingConfig()
    {
        BasePath = RoutePattern = null!;
        HttpMethods = Array.Empty<string>();
    }
    private ReportingConfig(string basePath, TimeSpan defaultRuntimeCache, string routePattern, IReadOnlyCollection<string> httpMethods)
    {
        BasePath = basePath;
        DefaultRuntimeCache = defaultRuntimeCache;
        RoutePattern = routePattern;
        HttpMethods = httpMethods;
    }

    /// <summary>
    /// The base path where the reports are stored. This needs to be a static location that the service can access.
    /// Defaults to wwwroot\\Reports
    /// </summary>
    public string BasePath { get; private init; }
    /// <summary>
    /// The Default cache duration for generated assemblies.
    /// Default is one minute.
    /// </summary>
    public TimeSpan DefaultRuntimeCache { get; private init; } = TimeSpan.FromMinutes(1);
    /// <summary>
    /// Define the route used for the endpoints used by the Dynamic reports.
    /// Default is: /reports/{reportId:int}/{action?}/{controller?}
    /// </summary>
    public string RoutePattern { get; private init; }
    /// <summary>
    /// Valid options are GET, PATCH, PUT, POST, OPTIONS, and DELETE
    /// </summary>
    public IReadOnlyCollection<string> HttpMethods { get; private init; }

    public ReportingConfig WithBasePath(string basePath) => new(basePath, DefaultRuntimeCache, RoutePattern, HttpMethods);

    public ReportingConfig WithDefaultCache(TimeSpan defaultCache) => new(BasePath, defaultCache, RoutePattern, HttpMethods);

    public ReportingConfig WithHttpMethods(params string[] httpMethods) => new(BasePath, DefaultRuntimeCache, RoutePattern, httpMethods);
    public ReportingConfig WithHttpMethods(IReadOnlyCollection<string> httpMethods) => new(BasePath, DefaultRuntimeCache, RoutePattern, httpMethods);

    public ReportingConfig WithRoutePattern(string routePattern) => new(BasePath, DefaultRuntimeCache, routePattern, HttpMethods);
}
