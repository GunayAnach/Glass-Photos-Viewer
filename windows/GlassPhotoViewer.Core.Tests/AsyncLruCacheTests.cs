using GlassPhotoViewer.Core;

namespace GlassPhotoViewer.Core.Tests;

public sealed class AsyncLruCacheTests
{
    [Fact]
    public async Task ConcurrentRequestsForTheSameKeyShareOneLoad()
    {
        var loads = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new AsyncLruCache<string, string>(capacity: 3);

        Task<string> Load(string key)
        {
            Interlocked.Increment(ref loads);
            return CompleteAsync();

            async Task<string> CompleteAsync()
            {
                await gate.Task;
                return $"decoded:{key}";
            }
        }

        var first = cache.GetAsync("photo.jpg", Load);
        var second = cache.GetAsync("photo.jpg", Load);
        gate.SetResult();

        Assert.Equal("decoded:photo.jpg", await first);
        Assert.Equal("decoded:photo.jpg", await second);
        Assert.Equal(1, loads);
    }

    [Fact]
    public async Task RemovingAKeyForcesAFreshLoad()
    {
        var loads = 0;
        var cache = new AsyncLruCache<string, int>(capacity: 3);

        Task<int> Load(string _) => Task.FromResult(Interlocked.Increment(ref loads));

        Assert.Equal(1, await cache.GetAsync("photo.jpg", Load));
        Assert.True(cache.Remove("photo.jpg"));
        Assert.Equal(2, await cache.GetAsync("photo.jpg", Load));
    }
}
