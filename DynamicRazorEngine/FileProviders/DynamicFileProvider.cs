using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections;

namespace DynamicRazorEngine.Factories;

public sealed class DynamicFileInfo : IFileInfo
{
    private readonly string _viewPath;
    private readonly byte[]? _viewContent;
    private readonly DateTimeOffset _lastModified;
    private bool _exists;

    public DynamicFileInfo(string viewPath)
    {
        _viewPath = viewPath;
        GetView(viewPath);
    }
    public bool Exists => _exists;

    public bool IsDirectory => false;

    public DateTimeOffset LastModified => _lastModified;

    public long Length
    {
        get
        {
            if (_viewContent is null)
            {
                return 0;
            }
            using var stream = new MemoryStream(_viewContent);
            return stream.Length;
        }
    }

    public string Name => Path.GetFileName(_viewPath);

    public string? PhysicalPath => null;

    public Stream CreateReadStream() => _viewContent is null ? new MemoryStream() : new MemoryStream(_viewContent);

    private void GetView(string viewPath)
    {
        if (File.Exists(viewPath))
        {
            try
            {
                _exists = true;

                File.Open(viewPath, FileMode.Open, FileAccess.Read, FileShare.Read).Write(_viewContent);
            }
            catch
            {
                _exists = false;
            }
        }
    }
}

public class DummyContent : IDirectoryContents
{
    public bool Exists => false;

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
public sealed class DynamicFileProvider : IFileProvider
{
    public IDirectoryContents GetDirectoryContents(string subpath) => null;
    public IFileInfo GetFileInfo(string subpath)
    {
        var result = new DynamicFileInfo(subpath);
        return result.Exists ? result as IFileInfo : new NotFoundFileInfo(subpath);
    }

    public IChangeToken Watch(string filter) => new DynamicChangeToken(filter);
}

public class DynamicChangeToken : IChangeToken
{
    private readonly string _viewPath;

    public DynamicChangeToken(string viewPath) => _viewPath = viewPath;

    public bool ActiveChangeCallbacks => false;

    public bool HasChanged => false; // TODO: Implement this.

    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => EmptyDisposable.Instance;
}

internal class EmptyDisposable : IDisposable
{
    public static EmptyDisposable Instance { get; } = new EmptyDisposable();
    private EmptyDisposable() { }
    public void Dispose() { }
}