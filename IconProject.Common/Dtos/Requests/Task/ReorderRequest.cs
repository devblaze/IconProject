namespace IconProject.Common.Dtos.Requests.Task;

public sealed record ReorderRequest
{
    public required IReadOnlyList<int> TaskIds { get; init; }
}
