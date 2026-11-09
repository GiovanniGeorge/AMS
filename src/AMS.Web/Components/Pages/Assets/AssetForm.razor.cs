namespace AMS.Web.Components.Pages.Assets;

public partial class AssetForm
{
    [Inject] private AssetService Assets { get; set; } = default!;
    [Inject] private CrudService<Category> Categories { get; set; } = default!;
    [Inject] private CrudService<Brand> Brands { get; set; } = default!;
    [Inject] private CrudService<AssetStatus> Statuses { get; set; } = default!;
    [Inject] private CrudService<Supplier> Suppliers { get; set; } = default!;
    [Inject] private CrudService<Location> Locations { get; set; } = default!;
    [Inject] private CrudService<Department> Departments { get; set; } = default!;
    [Inject] private CrudService<Employee> Employees { get; set; } = default!;
    [Inject] private InvoiceService Invoices { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    private bool IsNew => Id is null;

    private Asset? _asset;
    private string? _preview;

    /// <summary>Raising the invoice is a decision, so it is a tick box, never automatic.</summary>
    private bool _createInvoice;
    private int? _invoiceId;
    private List<Invoice> _invoices = new();
    private bool _notFound;
    private bool _saving;
    private string? _error;

    private List<Category> _categories = new();
    private List<Brand> _brands = new();
    private List<AssetStatus> _statuses = new();
    private List<Supplier> _suppliers = new();
    private List<Location> _locations = new();
    private List<Department> _departments = new();
    private List<Employee> _employees = new();
    private List<Asset> _parents = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _categories = await Categories.GetAllAsync();
            if (IsNew) await LoadInvoicesAsync();
            _brands = await Brands.GetAllAsync();
            _statuses = await Statuses.GetAllAsync();
            _suppliers = await Suppliers.GetAllAsync();
            _locations = await Locations.GetAllAsync();
            _departments = await Departments.GetAllAsync();
            _employees = await Employees.GetAllAsync();

            if (IsNew)
            {
                // The drop-down shows EGP from the start, so the model has to agree with it;
                // otherwise an untouched currency box saves nothing at all.
                _asset = new Asset { Currency = Currencies.Default };
                // PageSize 0 = no paging: the "Part of" list needs every asset.
                _parents = (await Assets.SearchAsync(new AssetFilter(PageSize: 0))).Items;
            }
            else
            {
                _asset = await Assets.GetDetailsAsync(Id!.Value);
                _notFound = _asset is null;

                // An asset saved before the currency became a list has none, or has it in
                // another spelling; put it on the list so the box is not silently blank.
                if (_asset is not null) _asset.Currency = Currencies.Normalise(_asset.Currency);
                _parents = (await Assets.SearchAsync(new AssetFilter(PageSize: 0))).Items
                    .Where(a => a.Id != Id)
                    .ToList();
            }
        }
        catch (Exception ex) { _error = ex.Message; }
    }

    // ------------------------------------------------------------------
    // Quick-add: a lookup added here is reloaded and selected straight away,
    // so a missing brand or location never costs you the form you are filling in.
    // ------------------------------------------------------------------

    private async Task StatusAddedAsync(AssetStatus status)
    {
        _statuses = await Statuses.GetAllAsync();
        _asset.StatusId = status.Id;
    }

    private async Task BrandAddedAsync(Brand brand)
    {
        _brands = await Brands.GetAllAsync();
        _asset.BrandId = brand.Id;
    }

    private async Task SupplierAddedAsync(Supplier supplier)
    {
        _suppliers = await Suppliers.GetAllAsync();
        _asset.SupplierId = supplier.Id;
    }

    private async Task LocationAddedAsync(Location location)
    {
        _locations = await Locations.GetAllAsync();
        _asset.LocationId = location.Id;
    }

    private async Task DepartmentAddedAsync(Department department)
    {
        _departments = await Departments.GetAllAsync();
        _asset.DepartmentId = department.Id;
    }

    private async Task UpdatePreviewAsync()
    {
        if (!IsNew || _asset is null) return;

        _preview = _asset.CategoryId == 0 ? null : await Assets.PreviewNextNumberAsync(_asset.CategoryId);
    }

    private async Task LoadInvoicesAsync()
    {
        try { _invoices = (await Invoices.GetAllAsync(false, 1)).Items; }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task SaveAsync()
    {
        if (_asset is null) return;
        _error = null;

        if (string.IsNullOrWhiteSpace(_asset.Name)) { _error = "Name is required."; return; }
        if (_asset.CategoryId == 0) { _error = "Category is required."; return; }
        if (_asset.StatusId == 0) { _error = "Status is required."; return; }
        _saving = true;
        try
        {
            if (IsNew)
            {
                var newId = await Assets.RegisterAsync(_asset);

                // The asset exists first; the invoice line points at it.
                if (_createInvoice)
                {
                    await Invoices.RecordAssetPurchaseAsync(new AssetPurchaseInput(
                        newId, _invoiceId, _asset.InvoiceNumber, _asset.SupplierId,
                        _asset.PurchaseDate, _asset.PurchaseCost, _asset.Currency));
                }

                Nav.NavigateTo($"assets/{newId}");
            }
            else
            {
                await Assets.UpdateDetailsAsync(_asset);
                Nav.NavigateTo($"assets/{_asset.Id}");
            }
        }
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => Nav.NavigateTo(IsNew ? "assets" : $"assets/{Id}");

    private async Task QuickCategoryAddedAsync(Category created)
    {
        _categories = await Categories.GetAllAsync();
        _asset.CategoryId = created.Id;

        // The asset number comes from the category prefix, so the preview has to catch up.
        await UpdatePreviewAsync();
    }

    private async Task QuickEmployeeAddedAsync(Employee created)
    {
        _employees = await Employees.GetAllAsync();
        _asset.CustodianId = created.Id;
    }
}
