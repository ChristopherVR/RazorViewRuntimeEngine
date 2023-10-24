namespace DynamicRazorEngine.Factories;

internal static class DefaultReportConfiguration
{
    private static ReportingConfig Default() => ReportingConfig.Default;

    internal static ReportingConfig GetValueOrDefault(ReportingConfig? value)
    {
        if (value is null)
        {
            return Default();
        }

        if (string.IsNullOrWhiteSpace(value.BasePath))
        {
            return value.WithBasePath(Default().BasePath);
        }

        return value;
    }
}
