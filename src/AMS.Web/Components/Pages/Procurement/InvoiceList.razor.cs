namespace AMS.Web.Components.Pages.Procurement;

public partial class InvoiceList
{
    [Inject] private InvoiceService Service { get; set; } = default!;
    [Inject] private CrudService<Invoice> InvoiceCrud { get; set; } = default!;
    [Inject] private CrudService<Supplier> Suppliers { get; set; } = default!;
    [Inject] private CrudService<Item> Items { get; set; } = default!;
    [Inject] private PurchaseOrderService Orders { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<Invoice>? _rows;
    private List<Supplier> _suppliers = new();
    private List<Item> _items = new();
    private List<PurchaseOrder> _orders = new();
    private Dictionary<int, string> _userNames = new();
    private bool _showInactive;
    private int _total;
    private int _page = 1;

    private bool _open;
    private int? _editId;
    private string? _number;
    private int? _supplierId;
    private int? _purchaseOrderId;
    private DateTime? _date = DateTime.Today;
    private decimal? _amount;
    private string _currency = Currencies.Default;
    private List<InvoiceLineInput> _lines = new();

    private List<Asset> _assets = new();
    private int? _lineItemId;
    private int? _lineAssetId;
    private string? _lineDescription;
    private decimal _lineQuantity = 1;
    private decimal? _lineUnitPrice;


    /// <summary>Shown inside the dialog.</summary>
    private string? _error;

    /// <summary>Shown on the page, for failures that happen with the dialog closed.</summary>
    private string? _pageError;

    private decimal Total => _lines.Sum(l => l.Quantity * (l.UnitPrice ?? 0m));

    private bool HasTypedLine =>
        _lineItemId is not null || _lineAssetId is not null || !string.IsNullOrWhiteSpace(_lineDescription);

    /// <summary>What a saved line is for: the asset, the item, or the free text.</summary>
    private string LineLabel(InvoiceLineInput line)
    {
        if (line.AssetId is int assetId)
        {
            var asset = _assets.FirstOrDefault(a => a.Id == assetId);
            return asset is null ? (line.Description ?? "Asset") : asset.AssetNumber + " — " + asset.Name;
        }

        return _items.FirstOrDefault(i => i.Id == line.ItemId)?.Name ?? line.Description ?? "";
    }


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _suppliers = await Suppliers.GetAllAsync(true);
            _items = await Items.GetAllAsync();
            _orders = await Orders.GetOpenAsync();
            _assets = await Service.GetBillableAssetsAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _pageError = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Service.GetAllAsync(_showInactive, _page);
            _rows = result.Items;
            _total = result.Total;
            _pageError = null;
        }
        catch (Exception ex) { _pageError = ex.Message; }
    }

    private async Task GoToAsync(int page)
    {
        _page = page;
        await LoadAsync();
    }

    private async Task FilterAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    // ------------------------------------------------------------------
    // Quick-add callbacks
    // ------------------------------------------------------------------

    private async Task SupplierAddedAsync(Supplier supplier)
    {
        _suppliers = await Suppliers.GetAllAsync(true);
        _supplierId = supplier.Id;
    }

    private async Task ItemAddedAsync(Item item)
    {
        _items = await Items.GetAllAsync();
        _lineItemId = item.Id;
    }

    private async Task AssetAddedAsync(Asset asset)
    {
        _assets = await Service.GetBillableAssetsAsync();
        _lineAssetId = asset.Id;
    }

    // ------------------------------------------------------------------
    // The dialog
    // ------------------------------------------------------------------

    private void OpenNew()
    {
        _editId = null;
        _number = null;
        _supplierId = null;
        _purchaseOrderId = null;
        _date = DateTime.Today;
        _amount = null;
        _currency = Currencies.Default;
        _lines = new();
        ClearLine();
        _error = null;
        _open = true;
    }

    private void OpenEdit(Invoice invoice)
    {
        // A closed order is not in the drop-down, so put this invoice's own order back in it.
        if (invoice.PurchaseOrder is not null && _orders.All(o => o.Id != invoice.PurchaseOrderId))
        {
            _orders = _orders.Append(invoice.PurchaseOrder).ToList();
        }

        _editId = invoice.Id;
        _number = invoice.InvoiceNumber;
        _supplierId = invoice.SupplierId;
        _purchaseOrderId = invoice.PurchaseOrderId;
        _date = invoice.InvoiceDate;
        _amount = invoice.Amount;
        _currency = Currencies.Normalise(invoice.Currency);
        _lines = invoice.Lines
            .Select(l => new InvoiceLineInput(l.ItemId, l.Description, l.Quantity, l.UnitPrice, l.AssetId))
            .ToList();

        // The assets already on this invoice must stay pickable while it is being edited.
        foreach (var onThisInvoice in invoice.Lines.Where(l => l.Asset is not null && _assets.All(a => a.Id != l.AssetId)))
        {
            _assets = _assets.Append(onThisInvoice.Asset!).ToList();
        }
        ClearLine();
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void ClearLine()
    {
        _lineItemId = null;
        _lineAssetId = null;
        _lineDescription = null;
        _lineQuantity = 1;
        _lineUnitPrice = null;
    }

    /// <summary>Moves what is in the line boxes into the list. False (with an error) if it cannot.</summary>
    private bool TakeTypedLine()
    {
        if (_lineItemId is null && _lineAssetId is null && string.IsNullOrWhiteSpace(_lineDescription))
        {
            _error = "Choose an item or an asset, or type a description.";
            return false;
        }

        if (_lineItemId is not null && _lineAssetId is not null)
        {
            _error = "A line bills either a store item or an asset, not both.";
            return false;
        }

        if (_lineAssetId is int already && _lines.Any(l => l.AssetId == already))
        {
            _error = "That asset is already on this invoice.";
            return false;
        }

        if (_lineQuantity <= 0)
        {
            _error = "The quantity must be greater than zero.";
            return false;
        }

        _lines.Add(new InvoiceLineInput(_lineItemId, _lineDescription, _lineQuantity, _lineUnitPrice, _lineAssetId));
        ClearLine();
        return true;
    }

    private void AddLine()
    {
        _error = null;
        TakeTypedLine();
    }

    private async Task SaveAsync()
    {
        _error = null;

        if (HasTypedLine && !TakeTypedLine())
        {
            return;
        }

        try
        {
            var input = new InvoiceInput(
                _number ?? string.Empty, _supplierId, _purchaseOrderId,
                _date, _amount, _currency, _lines);

            if (_editId is int id) await Service.UpdateAsync(id, input);
            else await Service.CreateAsync(input);

            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task ToggleAsync(Invoice invoice)
    {
        _pageError = null;
        try
        {
            if (invoice.IsActive) await InvoiceCrud.DeactivateAsync(invoice.Id);
            else await InvoiceCrud.ActivateAsync(invoice.Id);
            await LoadAsync();
        }
        catch (Exception ex) { _pageError = ex.InnerException?.Message ?? ex.Message; }
    }
}
