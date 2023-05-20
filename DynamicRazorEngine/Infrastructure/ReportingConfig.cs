namespace DynamicRazorEngine.Factories;

public readonly record struct ReportingConfig
{
    public ReportingConfig() {}

    public required string BasePath { get; init; }
    public TimeSpan DefaultRuntimeCache { get; init; } = TimeSpan.FromMinutes(1);
    public required string RoutePattern { get; init; }
    public required string BaseIndexRoutePattern { get; init; }
    public required string[] HttpMethods { get; init; }
}
