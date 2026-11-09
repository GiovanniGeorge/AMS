namespace AMS.Web.Components.Shared;

public partial class UserName
{
    [Parameter] public int? Id { get; set; }

    /// <summary>User Id → Username, loaded once by the page.</summary>
    [Parameter] public IReadOnlyDictionary<int, string> Names { get; set; } = new Dictionary<int, string>();

    private string Text => Id is int id && Names.TryGetValue(id, out var name) ? name : "—";
}
