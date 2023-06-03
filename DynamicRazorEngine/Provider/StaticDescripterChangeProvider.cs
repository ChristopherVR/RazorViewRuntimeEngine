using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace DynamicRazorEngine.Provider;

internal sealed class StaticDescripterChangeProvider : IActionDescriptorChangeProvider
{
    internal static StaticDescripterChangeProvider Instance { get; } = new();

    internal CancellationTokenSource? TokenSource { get; private set; }

    internal bool HasChanged { get; set; }

    public IChangeToken GetChangeToken()
    {
        TokenSource = new();
        return new CancellationChangeToken(TokenSource.Token);
    }

    internal void Refresh()
    {
        HasChanged = true;
        TokenSource?.Cancel();
    }
}
