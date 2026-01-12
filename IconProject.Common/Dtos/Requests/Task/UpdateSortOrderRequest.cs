namespace IconProject.Common.Dtos.Requests.Task;

public sealed record UpdateSortOrderRequest
{
    public required IReadOnlyList<SortOrderItem> Items { get; init; }
}

public sealed record SortOrderItem
{
    public int TaskId { get; init; }
    public int SortOrder { get; init; }
}
