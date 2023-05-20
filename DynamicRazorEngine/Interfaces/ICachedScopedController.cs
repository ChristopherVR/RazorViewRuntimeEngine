using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;

#pragma warning disable IDE1006 // Naming Styles
internal interface ICachedScopedController<T> where T: ControllerBase
#pragma warning restore IDE1006 // Naming Styles
{
    public TimeSpan CacheDuration { get; set; }
}
