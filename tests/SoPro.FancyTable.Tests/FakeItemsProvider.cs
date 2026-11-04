/// <summary>
/// Records every server mode request and answers with configurable pages. A gate lets a test hold one call open.
/// </summary>
public sealed class FakeItemsProvider<TItem>
{
    private readonly Func<FancyItemsRequest, FancyItemsResult<TItem>> answer;

    public FakeItemsProvider(Func<FancyItemsRequest, FancyItemsResult<TItem>> answer)
    {
        this.answer = answer;
    }

    public List<FancyItemsRequest> Requests { get; } = [];

    public List<CancellationToken> Tokens { get; } = [];

    public TaskCompletionSource? Gate { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public FancyItemsRequest LastRequest => Requests[^1];

    public async ValueTask<FancyItemsResult<TItem>> LoadAsync(FancyItemsRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Tokens.Add(cancellationToken);
        FancyItemsResult<TItem> result = answer(request);

        TaskCompletionSource? gate = Gate;
        Gate = null;
        if (gate is not null)
        {
            await gate.Task;
        }

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return result;
    }
}
