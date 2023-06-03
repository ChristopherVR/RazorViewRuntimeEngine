namespace DynamicRazorEngine.Models;
public sealed record Report(long Id, string RelativePath, string? MainView, string? MainController, bool CacheCompilation, TimeSpan? CacheDuration = default);
