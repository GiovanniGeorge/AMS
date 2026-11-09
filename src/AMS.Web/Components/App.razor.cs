namespace AMS.Web.Components;

public partial class App
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    // Pages under /account (login, logout, setup) run as plain HTTP pages, because only an
    // HTTP response can write or remove the login cookie. Every other page stays interactive.
    private IComponentRenderMode? PageRenderMode =>
        HttpContext.Request.Path.StartsWithSegments("/account") ? null : InteractiveServer;
}
