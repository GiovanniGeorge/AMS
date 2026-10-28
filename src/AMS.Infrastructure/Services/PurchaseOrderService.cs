using Microsoft.EntityFrameworkCore;
using AMS.Application.Procurement;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    /// <summary>Every purchase order number looks like PO-00001.</summary>
    private const string NumberPrefix = "PO-";

    private readonly ApplicationDbContext _db;

    public PurchaseOrderService(ApplicationDbContext db)
    {
        _db = db;
    }

    private IQueryable<PurchaseOrder> Orders() =>
        _db.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Lines.Where(l => l.IsActive))
                .ThenInclude(l => l.Item)
            .Where(p => p.IsActive);

    public async Task<Paged<PurchaseOrder>> GetAllAsync(string? status = null, int page = 1)
    {
        var query = Orders();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var total = await query.CountAsync();

        var items = await query.OrderByDescending(p => p.Id)
            .Skip((Math.Max(page, 1) - 1) * Paged<PurchaseOrder>.Size)
            .Take(Paged<PurchaseOrder>.Size)
            .ToListAsync();

        return new Paged<PurchaseOrder>(items, total);
    }

    public async Task<List<PurchaseOrder>> GetOpenAsync()
    {
        return await Orders()
            .Where(p => p.Status == PurchaseOrderStatus.Open)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await Orders().FirstOrDefaultAsync(p => p.Id == id);
    }

    // ------------------------------------------------------------------
    // Numbering
    // ------------------------------------------------------------------

    public Task<string> PreviewNextNumberAsync() => NextNumberAsync();

    /// <summary>
    /// Highest existing number + 1, e.g. PO-00007 -> PO-00008. Closed and deactivated
    /// orders count too, so a number is never reused. If two people save at the same
    /// instant the unique index rejects the second, and that user simply saves again.
    /// </summary>
    private async Task<string> NextNumberAsync()
    {
        var existing = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PurchaseOrderNumber.StartsWith(NumberPrefix))
            .Select(p => p.PurchaseOrderNumber)
            .ToListAsync();

        var max = 0;
        foreach (var number in existing)
        {
            if (int.TryParse(number.Substring(NumberPrefix.Length), out var value) && value > max)
            {
                max = value;
            }
        }

        return $"{NumberPrefix}{max + 1:D5}";
    }

    // ------------------------------------------------------------------
    // Writing
    // ------------------------------------------------------------------

    public async Task<int> CreateAsync(PurchaseOrderInput input)
    {
        Validate(input);

        try
        {
            var order = new PurchaseOrder
            {
                PurchaseOrderNumber = await NextNumberAsync(),
                SupplierId = input.SupplierId,
                PurchaseDate = input.PurchaseDate,
                ExpectedDeliveryDate = input.ExpectedDeliveryDate,
                Currency = input.Currency,
                Status = PurchaseOrderStatus.Open,
                TotalCost = Total(input.Lines)
            };

            foreach (var line in input.Lines)
            {
                order.Lines.Add(NewLine(line));
            }

            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync();
            return order.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task UpdateAsync(int id, PurchaseOrderInput input)
    {
        Validate(input);

        try
        {
            var order = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
                ?? throw new InvalidOperationException("Purchase order not found.");

            if (order.Status == PurchaseOrderStatus.Closed)
            {
                throw new InvalidOperationException("A closed purchase order cannot be changed.");
            }

            // The number stays as it was issued; only the contents are editable.
            order.SupplierId = input.SupplierId;
            order.PurchaseDate = input.PurchaseDate;
            order.ExpectedDeliveryDate = input.ExpectedDeliveryDate;
            order.Currency = input.Currency;
            order.TotalCost = Total(input.Lines);

            // The old lines are deactivated, never deleted.
            foreach (var old in order.Lines.Where(l => l.IsActive))
            {
                old.IsActive = false;
            }

            foreach (var line in input.Lines)
            {
                order.Lines.Add(NewLine(line));
            }

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task CloseAsync(int id)
    {
        try
        {
            var order = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && p.IsActive)
                        ?? throw new InvalidOperationException("Purchase order not found.");

            order.Status = PurchaseOrderStatus.Closed;
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// An order with no lines is allowed on purpose: it is raised first and filled in as
    /// the quotes arrive, and its total is simply zero until then. A line that IS there
    /// still has to make sense.
    /// </summary>
    private static void Validate(PurchaseOrderInput input)
    {
        foreach (var line in input.Lines)
        {
            if (line.ItemId is null && string.IsNullOrWhiteSpace(line.Description))
            {
                throw new InvalidOperationException("Every line needs an item or a description.");
            }

            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Every line needs a quantity greater than zero.");
            }
        }
    }

    private static PurchaseOrderLine NewLine(PurchaseOrderLineInput line) => new()
    {
        ItemId = line.ItemId,
        Description = line.Description,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice
    };

    private static decimal Total(List<PurchaseOrderLineInput> lines) =>
        lines.Sum(l => l.Quantity * (l.UnitPrice ?? 0m));
}
