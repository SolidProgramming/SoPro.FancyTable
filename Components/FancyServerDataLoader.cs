/// <summary>
/// Runs server mode requests for a table: cancels the previous request when a new one starts and
/// discards results of requests that were superseded in the meantime.
/// </summary>
internal sealed class FancyServerDataLoader<TItem> : IDisposable
{
    private CancellationTokenSource? currentSource;
    private int requestSequence;

    public bool IsLoading { get; private set; }

    /// <summary>
    /// Starts a request. Returns null if the request was cancelled or superseded by a newer one;
    /// any other exception of the provider is rethrown.
    /// </summary>
    public async Task<FancyItemsResult<TItem>?> LoadAsync(FancyItemsProvider<TItem> provider, FancyItemsRequest request)
    {
        currentSource?.Cancel();
        CancellationTokenSource source = new();
        currentSource = source;
        int sequence = ++requestSequence;
        IsLoading = true;

        try
        {
            FancyItemsResult<TItem> result = await provider(request, source.Token);
            return IsCurrent(sequence, source) ? result : null;
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            return null;
        }
        finally
        {
            if (sequence == requestSequence)
            {
                IsLoading = false;
            }
        }
    }

    public void Dispose()
    {
        currentSource?.Cancel();
        currentSource?.Dispose();
        currentSource = null;
    }

    private bool IsCurrent(int sequence, CancellationTokenSource source) =>
        sequence == requestSequence && !source.IsCancellationRequested;
}
