using DynamicRazorEngine.Models;

namespace DynamicRazorEngine.Interfaces;

/// <summary>
/// The markup interface that needs to be registered by the application to retrieve a report's details.
/// </summary>
public interface IReportService
{
    Task<Report?> GetAsync(long id);
}
