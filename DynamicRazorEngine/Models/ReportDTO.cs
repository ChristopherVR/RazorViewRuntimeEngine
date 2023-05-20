namespace DynamicRazorEngine.Models;
public sealed record Report(long Id, string Path, string? MainView, bool LoadFromDll, bool CacheCompilation, TimeSpan? CacheDuration = default);
