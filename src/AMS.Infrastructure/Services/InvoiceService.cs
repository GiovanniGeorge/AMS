using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Procurement;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Supplier invoices and their lines. No approval is involved.
/// Deactivate / Activate still use CrudService&lt;Invoice&gt;.
/// </summary>
public class InvoiceService
{
    private readonly ApplicationDbContext _db;

    public InvoiceService(ApplicationDbContext db)
    {
        _db = db;
    }

    private IQueryable<Invoice> Invoices() =>
        _db.Invoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Item)
            .Include(i => i.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Asset);

    public async Task<Paged<Invoice>> GetAllAsync(bool includeInactive = false, int page = 1)
    {
        var query = Invoices();

        if (!includeInactive)
        {
            query = query.Where(i => i.IsActive);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(i => i.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<Invoice>.Size)
            .Take(Paged<Invoice>.Size)
            .ToListAsync();

        return new Paged<Invoice>(items, total);
    }

    public async Task<Invoice?> GetByIdAsync(int id) =>
        await Invoices().FirstOrDefaultAsync(i => i.Id == id);

    /// <summary>The invoices that bill this asset, newest first. Usually none or one.</summary>
    public async Task<List<Invoice>> GetForAssetAsync(int assetId) =>
        await Invoices()
            .Where(i => i.IsActive && i.Lines.Any(l => l.IsActive && l.AssetId == assetId))
            .OrderByDescending(i => i.Id)
            .ToListAsync();

    /// <summary>
    /// An asset belongs on one invoice, so the ones already billed are left out -
    /// except the one the line being edited is pointing at.
    /// </summary>
    public async Task<List<Asset>> GetBillableAssetsAsync(int? includeAssetId = null)
    {
        var billed = _db.InvoiceLines
            .Where(l => l.IsActive && l.AssetId != null && l.Invoice!.IsActive)
            .Select(l => l.AssetId!.Value);

        return await _db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && (!billed.Contains(a.Id) || a.Id == includeAssetId))
            .OrderByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(InvoiceInput input)
    {
        Validate(input);

        try
        {
            await EnsureNumberIsFreeAsync(input.InvoiceNumber, input.SupplierId, exceptId: null);

            var invoice = new Invoice
            {
                InvoiceNumber = input.InvoiceNumber.Trim(),
                SupplierId = input.SupplierId,
                PurchaseOrderId = input.PurchaseOrderId,
                InvoiceDate = input.InvoiceDate,
                Currency = input.Currency,
                Amount = AmountOf(input)
            };

            foreach (var line in input.Lines)
            {
                invoice.Lines.Add(NewLine(line));
            }

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            await FillAssetPurchaseFactsAsync(invoice);
            return invoice.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>Replaces the header and the lines.</summary>
    public async Task UpdateAsync(int id, InvoiceInput input)
    {
        Validate(input);

        try
        {
            var invoice = await _db.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new InvalidOperationException("Invoice not found.");

            await EnsureNumberIsFreeAsync(input.InvoiceNumber, input.SupplierId, exceptId: id);

            invoice.InvoiceNumber = input.InvoiceNumber.Trim();
            invoice.SupplierId = input.SupplierId;
            invoice.PurchaseOrderId = input.PurchaseOrderId;
            invoice.InvoiceDate = input.InvoiceDate;
            invoice.Currency = input.Currency;
            invoice.Amount = AmountOf(input);

            // The old lines are deactivated, never deleted.
            foreach (var old in invoice.Lines.Where(l => l.IsActive))
            {
                old.IsActive = false;
            }

            foreach (var line in input.Lines)
            {
                invoice.Lines.Add(NewLine(line));
            }

            await _db.SaveChangesAsync();

            await FillAssetPurchaseFactsAsync(invoice);
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Puts one asset on an invoice, creating the invoice when no existing one is given.
    /// Returns the invoice id.
    /// </summary>
    public async Task<int> RecordAssetPurchaseAsync(AssetPurchaseInput input)
    {
        try
        {
            var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AssetId)
                        ?? throw new InvalidOperationException("Asset not found.");

            var alreadyBilled = await _db.InvoiceLines
                .AnyAsync(l => l.IsActive && l.AssetId == input.AssetId && l.Invoice!.IsActive);
            if (alreadyBilled)
            {
                throw new InvalidOperationException("This asset is already on an invoice.");
            }

            var line = new InvoiceLine
            {
                AssetId = input.AssetId,
                Description = asset.AssetNumber + " — " + asset.Name,
                Quantity = 1,
                UnitPrice = input.Amount
            };

            Invoice invoice;

            if (input.InvoiceId is int existingId)
            {
                invoice = await _db.Invoices
                    .Include(i => i.Lines)
                    .FirstOrDefaultAsync(i => i.Id == existingId && i.IsActive)
                    ?? throw new InvalidOperationException("Invoice not found.");

                invoice.Lines.Add(line);
                invoice.Amount = invoice.Lines.Where(l => l.IsActive).Sum(l => l.Quantity * (l.UnitPrice ?? 0m));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input.InvoiceNumber))
                {
                    throw new InvalidOperationException("The invoice number is required.");
                }

                await EnsureNumberIsFreeAsync(input.InvoiceNumber, input.SupplierId, exceptId: null);

                invoice = new Invoice
                {
                    InvoiceNumber = input.InvoiceNumber.Trim(),
                    SupplierId = input.SupplierId,
                    InvoiceDate = input.InvoiceDate,
                    Currency = input.Currency,
                    Amount = input.Amount ?? 0m
                };

                invoice.Lines.Add(line);
                _db.Invoices.Add(invoice);
            }

            await _db.SaveChangesAsync();
            return invoice.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// An asset billed on an invoice learns its purchase facts from it - but only the
    /// blanks. Anything already recorded on the asset is left exactly as it is, because
    /// the asset register is the book of record for an asset, not the invoice.
    /// </summary>
    private async Task FillAssetPurchaseFactsAsync(Invoice invoice)
    {
        var assetIds = invoice.Lines
            .Where(l => l.IsActive && l.AssetId is not null)
            .Select(l => l.AssetId!.Value)
            .Distinct()
            .ToList();

        if (assetIds.Count == 0)
        {
            return;
        }

        var assets = await _db.Assets.Where(a => assetIds.Contains(a.Id)).ToListAsync();

        foreach (var asset in assets)
        {
            var line = invoice.Lines.First(l => l.IsActive && l.AssetId == asset.Id);

            asset.SupplierId ??= invoice.SupplierId;
            asset.PurchaseDate ??= invoice.InvoiceDate;
            asset.PurchaseCost ??= line.Quantity * (line.UnitPrice ?? 0m);
            asset.Currency ??= invoice.Currency;

            if (string.IsNullOrWhiteSpace(asset.InvoiceNumber))
            {
                asset.InvoiceNumber = invoice.InvoiceNumber;
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// The database holds one invoice number per supplier. Saying it here keeps the
    /// SQL Server message off the screen.
    /// </summary>
    private async Task EnsureNumberIsFreeAsync(string invoiceNumber, int? supplierId, int? exceptId)
    {
        if (supplierId is null)
        {
            return;
        }

        var number = invoiceNumber.Trim();

        var clash = await _db.Invoices.AnyAsync(i =>
            i.Id != exceptId
            && i.SupplierId == supplierId
            && i.InvoiceNumber == number);

        if (clash)
        {
            throw new InvalidOperationException("This supplier already has an invoice with that number.");
        }
    }

    private static void Validate(InvoiceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.InvoiceNumber))
        {
            throw new InvalidOperationException("The invoice number is required.");
        }

        foreach (var line in input.Lines)
        {
            if (line.ItemId is null && line.AssetId is null && string.IsNullOrWhiteSpace(line.Description))
            {
                throw new InvalidOperationException("Every line needs an item, an asset or a description.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Every line needs a quantity greater than zero.");
            }
        }

        var assets = input.Lines.Where(l => l.AssetId is not null).Select(l => l.AssetId!.Value).ToList();
        if (assets.Count != assets.Distinct().Count())
        {
            throw new InvalidOperationException("The same asset cannot be on two lines of one invoice.");
        }
    }

    /// <summary>
    /// With lines, the amount is their sum. Without them it is whatever was typed, so
    /// an invoice recorded before lines existed keeps its figure when it is edited.
    /// </summary>
    private static decimal? AmountOf(InvoiceInput input) =>
        input.Lines.Count > 0
            ? input.Lines.Sum(l => l.Quantity * (l.UnitPrice ?? 0m))
            : input.Amount;

    private static InvoiceLine NewLine(InvoiceLineInput line) => new()
    {
        ItemId = line.ItemId,
        AssetId = line.AssetId,
        Description = line.Description,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice
    };
}
