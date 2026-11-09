namespace AMS.Web.Components.Pages.Account;

public partial class Setup
{
    [Inject] private AccountService Account { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm]
    private SetupModel Input { get; set; } = new();

    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Reached without HTTP (in-app navigation): AuthLayout reloads the page.
        if (HttpContext is null) return;

        if (await Account.AnyUsersExistAsync())
        {
            Nav.NavigateTo("account/login");
        }
    }

    private async Task CreateAsync()
    {
        var username = Input.Username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(Input.Password))
        {
            _error = "Username and password are required.";
            return;
        }

        if (username.Length > 100)
        {
            _error = "Username can be at most 100 characters.";
            return;
        }

        // The only account in the system: a typo here would lock everyone out.
        if (Input.Password != Input.ConfirmPassword)
        {
            _error = "The two passwords do not match.";
            return;
        }

        var created = await Account.CreateFirstUserAsync(username, Input.Password);

        // created == false: another user was created in the meantime. Either way, go to Login.
        Nav.NavigateTo(created ? "account/login?created=1" : "account/login");
    }

    private sealed class SetupModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
