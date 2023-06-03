using DynamicRazorEngine.Interfaces;
using DynamicRazorEngine.Models;

namespace RuntimeLoading.Services;

internal class ReportingService : IReportService
{
    private const string Index = nameof(Index);
    public async Task<Report?> GetAsync(long id)
        => await Task.FromResult(new Report(Id: id, RelativePath: $"\\{id}", MainView: Index, nameof(Report), CacheCompilation: true, CacheDuration: TimeSpan.FromDays(1)));
}
