namespace Ambermoon;

using System;
using System.Threading.Tasks;

internal class ResourceProviderPromise<T>
{
    public event Action<T> ResultReady;

    private protected void OnProvideResult(T result)
    {
        ResultReady?.Invoke(result);
    }
}

file class InternalResourceProviderPromise<T> : ResourceProviderPromise<T>
{
    public InternalResourceProviderPromise(Func<T> provider)
    {
        Task.Run(() =>
        {
            var result = provider.Invoke();
            OnProvideResult(result);
        });
    }
}

internal static class ResourceProvider
{
    public static ResourceProviderPromise<T> GetResource<T>(Func<T> provider)
    {
        return new InternalResourceProviderPromise<T>(provider);
    }
}
