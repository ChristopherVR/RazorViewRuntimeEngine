using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;
internal interface IDynamicPageController
{
    public IActionResult Index();
    public Task<IActionResult> IndexAsync();
}
