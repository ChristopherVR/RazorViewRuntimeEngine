using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace DynamicRazorEngine.Provider;

internal sealed class StaticDescripterChangeProvider : IActionDescriptorChangeProvider
{
    public static StaticDescripterChangeProvider Instance { get; } = new StaticDescripterChangeProvider();

    public CancellationTokenSource? TokenSource { get; private set; }

    public bool HasChanged { get; set; }

    public IChangeToken GetChangeToken()
    {
        TokenSource = new CancellationTokenSource();
        return new CancellationChangeToken(TokenSource.Token);
    }

    public void Refresh()
    {
        HasChanged = true;
        TokenSource?.Cancel();
    }
}
