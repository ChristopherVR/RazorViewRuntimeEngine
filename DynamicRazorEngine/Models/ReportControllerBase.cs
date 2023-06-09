namespace DynamicRazorEngine.Models;

public abstract class ReportControllerBase : Microsoft.AspNetCore.Mvc.Controller
{
    private long? _reportId;
    public long ReportId 
    { 
        get
        {
            if (_reportId is null)
            {
                var routeId = HttpContext.Request.RouteValues.GetValueOrDefault(nameof(ReportId), default(long?));
                _reportId = routeId is null ? HttpContext.Request.Query.TryGetValue(nameof(ReportId), out var val) ? long.Parse(val!) : default : long.Parse(routeId.ToString()!);
            }

            return _reportId.Value;
        } 
    }
}
