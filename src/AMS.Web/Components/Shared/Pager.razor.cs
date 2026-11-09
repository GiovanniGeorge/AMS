namespace AMS.Web.Components.Shared;

public partial class Pager
{
    [Parameter] public int Page { get; set; } = 1;
    [Parameter] public int Total { get; set; }
    [Parameter] public int Size { get; set; } = AMS.Application.Common.Paged<object>.Size;

    /// <summary>Called with the page the user asked for.</summary>
    [Parameter] public EventCallback<int> PageChanged { get; set; }

    private int Pages => Total == 0 || Size <= 0 ? 1 : (Total + Size - 1) / Size;

    private Task PreviousAsync() => Page <= 1 ? Task.CompletedTask : PageChanged.InvokeAsync(Page - 1);

    private Task NextAsync() => Page >= Pages ? Task.CompletedTask : PageChanged.InvokeAsync(Page + 1);
}
