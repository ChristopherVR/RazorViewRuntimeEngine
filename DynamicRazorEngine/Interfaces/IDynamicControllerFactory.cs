using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;

internal interface IDynamicControllerFactory 
{
    Task<IActionResult?> ExecuteAsync(ControllerFactoryRequest request);
}
