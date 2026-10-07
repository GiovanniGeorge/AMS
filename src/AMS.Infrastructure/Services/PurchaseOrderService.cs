using Microsoft.EntityFrameworkCore;
using AMS.Application.Procurement;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;
using AMS.Application.Common;

namespace AMS.Infrastructure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
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

    public async Task<int> CreateAsync(PurchaseOrderInput input)
    {
        Validate(input);

        try
        {
            var duplicate = await _db.PurchaseOrders
                .AnyAsync(p => p.PurchaseOrderNumber == input.PurchaseOrderNumber);
            if (duplicate)
            {
                throw new InvalidOperationException("This purchase order number already exists.");
            }

            var order = new PurchaseOrder
            {
                PurchaseOrderNumber = input.PurchaseOrderNumber.Trim(),
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

            var duplicate = await _db.PurchaseOrders
                .AnyAsync(p => p.Id != id && p.PurchaseOrderNumber == input.PurchaseOrderNumber);
            if (duplicate)
            {
                throw new InvalidOperationException("This purchase order number already exists.");
            }

            order.PurchaseOrderNumber = input.PurchaseOrderNumber.Trim();
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

    private static void Validate(PurchaseOrderInput input)
    {
        if (string.IsNullOrWhiteSpace(input.PurchaseOrderNumber))
        {
            throw new InvalidOperationException("The purchase order number is required.");
        }

        if (input.Lines.Count == 0)
        {
            throw new InvalidOperationException("A purchase order needs at least one line.");
        }

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
