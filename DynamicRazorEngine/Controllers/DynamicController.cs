//using DynamicRazorEngine.Interfaces;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Routing;

//namespace DynamicRazorEngine.Controllers;

/////// <summary>
/////// Controller alternative that can be registered if Minimal API approach doesn't seem appropriate.
/////// </summary>
//[Route("[controller]")]
//public sealed class DynamicReportController : Controller
//{
//    private readonly IDynamicControllerFactory _dynamicControllerFactory;
//    private readonly IRazorViewFactory _razorViewFactory;

//    internal DynamicReportController(IDynamicControllerFactory dynamicControllerFactory, IRazorViewFactory razorViewFactory)
//    {
//        _dynamicControllerFactory = dynamicControllerFactory ?? throw new ArgumentNullException(nameof(dynamicControllerFactory));
//        _razorViewFactory = razorViewFactory ?? throw new ArgumentNullException(nameof(razorViewFactory));
//    }

//    [HttpGet("{reportId:long}")]
//    public async Task<IActionResult> IndexAsync(long reportId) => await _razorViewFactory.ExecuteAsync(new(reportId, ControllerContext));

//    [Route("{reportId:long}/{controller:string?}/{action}/{controller?}")]
//    public async Task<IActionResult> EverythingElseAsync(long reportId, string? controller, string action) 
//        => await _dynamicControllerFactory.ExecuteAsync(new(reportId, action, controller, ControllerContext)) ?? NoContent();
//}
