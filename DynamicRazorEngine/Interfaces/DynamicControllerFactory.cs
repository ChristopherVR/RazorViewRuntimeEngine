using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;
public interface IDynamicPageController<TD> : IDynamicPageController, IDynamicController<TD>  where TD : ControllerBase 
{
 
}

public interface IDynamicPageController
{
    public IActionResult Index();
    public Task<IActionResult> IndexAsync();
}

public interface IDynamicController<T>: IDynamicController, IDisposable where T : ControllerBase {}

public interface IDynamicController { }

public interface IDynamicViewModel<T> where T : struct, IDisposable 
{ 

}

/// <summary>
/// The lifetime for this Controller & Its Assembly is only valid for this scope.
/// </summary>
public interface IScopedController { }

public interface ISingletonController { }

/// <summary>
/// Caches the Controller & Its Assembly for a while.
/// </summary>
public interface ICachedScopedController  : IScopedController
{
    public TimeSpan CacheDuration { get; set; }
}
