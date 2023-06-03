namespace RuntimeLoading.Services;

public interface IExampleService
{
    Task<string> GetHelloWorldAsync();
}

internal sealed class ExampleService : IExampleService
{
    public async Task<string> GetHelloWorldAsync() => await Task.FromResult("Hello world!");
}
