using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Mvc;

internal interface IRazorViewFactory
{
    Task<IActionResult> ExecuteAsync(RazorViewFactoryRequest request);
}
