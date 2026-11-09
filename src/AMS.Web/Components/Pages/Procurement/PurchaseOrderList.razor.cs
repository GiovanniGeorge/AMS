namespace AMS.Web.Components.Pages.Procurement;

public partial class PurchaseOrderList
{
    [Inject] private PurchaseOrderService Orders { get; set; } = default!;
    [Inject] private CrudService<Supplier> Suppliers { get; set; } = default!;
    [Inject] private CrudService<Item> Items { get; set; } = default!;
    [Inject] private CrudService<User> Users { get; set; } = default!;

    private List<PurchaseOrder>? _rows;
    private List<Supplier> _suppliers = new();
    private List<Item> _items = new();
    private Dictionary<int, string> _userNames = new();
    private string _status = string.Empty;
    private int _total;
    private int _page = 1;

    private bool _open;
    private int? _editId;
    private string? _number;
    private int? _supplierId;
    private DateTime? _date = DateTime.Today;
    private DateTime? _expected;
    private string _currency = Currencies.Default;
    private List<PurchaseOrderLineInput> _lines = new();

    private int? _lineItemId;
    private string? _lineDescription;
    private decimal _lineQuantity = 1;
    private decimal? _lineUnitPrice;



    /// <summary>Shown inside the dialog.</summary>
    private string? _error;

    /// <summary>Shown on the page itself, for failures that happen with the dialog closed.</summary>
    private string? _pageError;

    private decimal Total => _lines.Sum(l => l.Quantity * (l.UnitPrice ?? 0m));

    /// <summary>True when the line boxes hold something worth keeping.</summary>
    private bool HasTypedLine =>
        _lineItemId is not null || !string.IsNullOrWhiteSpace(_lineDescription);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _suppliers = await Suppliers.GetAllAsync();
            _items = await Items.GetAllAsync();
            _userNames = (await Users.GetAllAsync(true)).ToDictionary(u => u.Id, u => u.Username);
        }
        catch (Exception ex) { _pageError = ex.Message; }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await Orders.GetAllAsync(string.IsNullOrEmpty(_status) ? null : _status, _page);
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
        _suppliers = await Suppliers.GetAllAsync();
        _supplierId = supplier.Id;
    }

    private async Task ItemAddedAsync(Item item)
    {
        _items = await Items.GetAllAsync();
        _lineItemId = item.Id;
    }

    // ------------------------------------------------------------------
    // The dialog
    // ------------------------------------------------------------------

    private async Task OpenNewAsync()
    {
        _editId = null;
        _supplierId = null;
        _date = DateTime.Today;
        _expected = null;
        _currency = Currencies.Default;
        _lines = new();
        ClearLine();
        _error = null;
        _open = true;

        try
        {
            _number = await Orders.PreviewNextNumberAsync();
        }
        catch (Exception ex)
        {
            _number = "—";
            _error = ex.Message;
        }
    }

    private void OpenEdit(PurchaseOrder order)
    {
        _editId = order.Id;
        _number = order.PurchaseOrderNumber;
        _supplierId = order.SupplierId;
        _date = order.PurchaseDate;
        _expected = order.ExpectedDeliveryDate;
        _currency = Currencies.Normalise(order.Currency);
        _lines = order.Lines
            .Select(l => new PurchaseOrderLineInput(l.ItemId, l.Description, l.Quantity, l.UnitPrice))
            .ToList();
        ClearLine();
        _error = null;
        _open = true;
    }

    private void Close() => _open = false;

    private void ClearLine()
    {
        _lineItemId = null;
        _lineDescription = null;
        _lineQuantity = 1;
        _lineUnitPrice = null;
    }

    /// <summary>Moves what is in the line boxes into the list. Returns false and sets the error if it cannot.</summary>
    private bool TakeTypedLine()
    {
        if (_lineItemId is null && string.IsNullOrWhiteSpace(_lineDescription))
        {
            _error = "Choose an item or type a description.";
            return false;
        }

        if (_lineQuantity <= 0)
        {
            _error = "The quantity must be greater than zero.";
            return false;
        }

        _lines.Add(new PurchaseOrderLineInput(_lineItemId, _lineDescription, _lineQuantity, _lineUnitPrice));
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

        // Pressing Save with a line still in the boxes should not throw it away.
        if (HasTypedLine && !TakeTypedLine())
        {
            return;
        }

        try
        {
            var input = new PurchaseOrderInput(_supplierId, _date, _expected, _currency, _lines);

            if (_editId is int id) await Orders.UpdateAsync(id, input);
            else await Orders.CreateAsync(input);

            _open = false;
            await LoadAsync();
        }
        catch (Exception ex) { _error = ex.InnerException?.Message ?? ex.Message; }
    }

    private async Task CloseOrderAsync(int id)
    {
        _pageError = null;
        try
        {
            await Orders.CloseAsync(id);
            await LoadAsync();
        }
        catch (Exception ex) { _pageError = ex.InnerException?.Message ?? ex.Message; }
    }
}
