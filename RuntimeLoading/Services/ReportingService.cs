using DynamicRazorEngine.Models;
using DynamicRazorEngine.Services;

namespace RuntimeLoading.Services;

internal class ReportingService : IReportService
{
    public async Task<Report?> GetAsync(long id)
        => await Task.FromResult(new Report(id, $"wwwroot/reports/{id}", "Index", false, true));
}
