using Microsoft.EntityFrameworkCore;
using AMS.Application.Assets;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _db;

    public AssetService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    public async Task<AssetPage> SearchAsync(AssetFilter filter)
    {
        IQueryable<Asset> query = _db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Status)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Include(a => a.Custodian);

        if (!filter.IncludeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        if (filter.Stage is { } stage)
        {
            query = query.Where(a => a.LifecycleStage == stage);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(a => a.CategoryId == categoryId);
        }

        if (filter.LocationId is { } locationId)
        {
            query = query.Where(a => a.LocationId == locationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var text = filter.Search.Trim();
            query = query.Where(a =>
                a.AssetNumber.Contains(text) ||
                a.Name.Contains(text) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(text)) ||
                (a.TagOrBarcodeNumber != null && a.TagOrBarcodeNumber.Contains(text)) ||
                (a.LegacyAssetNumber != null && a.LegacyAssetNumber.Contains(text)));
        }

        var total = await query.CountAsync();

        query = query.OrderByDescending(a => a.Id);

        if (filter.PageSize > 0)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            query = query.Skip((page - 1) * filter.PageSize).Take(filter.PageSize);
        }

        return new AssetPage(await query.ToListAsync(), total);
    }

    public async Task<Asset?> GetDetailsAsync(int id)
    {
        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Brand)
            .Include(a => a.Supplier)
            .Include(a => a.Status)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Include(a => a.Custodian)
            .Include(a => a.ParentAsset)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Asset>> GetComponentsAsync(int parentId)
    {
        return await _db.Assets
            .AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.ParentAssetId == parentId && a.IsActive)
            .OrderBy(a => a.AssetNumber)
            .ToListAsync();
    }

    public async Task<List<AssetHistoryEntry>> GetHistoryAsync(int assetId)
    {
        var history = new List<AssetHistoryEntry>();

        // 1. Registration
        var asset = await _db.Assets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => new { a.AssetNumber, a.CreatedOn, a.CreatedById })
            .FirstOrDefaultAsync();

        if (asset is null)
        {
            return history;
        }

        history.Add(new AssetHistoryEntry(asset.CreatedOn, "Registered",
            $"Asset {asset.AssetNumber} registered", asset.CreatedById));

        // 2. Received
        var receives = await _db.ReceiveLines.AsNoTracking()
            .Where(l => l.AssetId == assetId && l.IsActive)
            .Select(l => new
            {
                Date = l.Receive!.Date ?? l.CreatedOn,
                Supplier = l.Receive.Supplier!.Name,
                Store = l.Receive.Store!.Name,
                l.Receive.Status,
                l.CreatedById
            })
            .ToListAsync();
        history.AddRange(receives.Select(r => new AssetHistoryEntry(r.Date, "Received",
            Join(r.Supplier is null ? null : "from " + r.Supplier, r.Store is null ? null : "into " + r.Store, r.Status),
            r.CreatedById)));

        // 3. Issued / assigned. An issuance is stored as an Issue + the AssetAssignment it opened,
        //    shown here as ONE line. Rejected requests stay visible with their reason.
        var assignments = await _db.AssetAssignments.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new
            {
                Date = x.AssignmentDate ?? x.CreatedOn,
                Custodian = x.Custodian!.Name,
                Department = x.Department!.Name,
                Location = x.Location!.Name,
                Store = x.Issue!.Store!.Name,
                IsIssuance = x.IssueId != null,
                x.ReturnDate,
                x.Status,
                Reason = x.Approval!.Comments,
                x.CreatedById
            })
            .ToListAsync();
        history.AddRange(assignments.Select(x => new AssetHistoryEntry(x.Date, x.IsIssuance ? "Issued" : "Assigned",
            Join(x.Store is null ? null : "from " + x.Store, x.Custodian is null ? null : "to " + x.Custodian,
                 x.Department, x.Location,
                 x.ReturnDate is null ? null : $"returned {x.ReturnDate:yyyy-MM-dd}",
                 x.Status,
                 x.Status == RequestStatus.Rejected && x.Reason is not null ? "reason: " + x.Reason : null),
            x.CreatedById)));

        // 4. An Issue without an assignment should not exist; listed only as a safety net.
        var issues = await _db.Issues.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive && !_db.AssetAssignments.Any(a => a.IssueId == x.Id))
            .Select(x => new
            {
                Date = x.Date ?? x.CreatedOn,
                Employee = x.Employee!.Name,
                Store = x.Store!.Name,
                x.Status,
                x.CreatedById
            })
            .ToListAsync();
        history.AddRange(issues.Select(x => new AssetHistoryEntry(x.Date, "Issued",
            Join(x.Store is null ? null : "from " + x.Store, x.Employee is null ? null : "to " + x.Employee, x.Status),
            x.CreatedById)));

        // 5. Returned
        var returns = await _db.Returns.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new
            {
                Date = x.Date ?? x.CreatedOn,
                Employee = x.Employee!.Name,
                Store = x.Store!.Name,
                x.Condition,
                x.Reason,
                x.Status,
                x.CreatedById
            })
            .ToListAsync();
        history.AddRange(returns.Select(x => new AssetHistoryEntry(x.Date, "Returned",
            Join(x.Employee is null ? null : "by " + x.Employee, x.Store is null ? null : "to " + x.Store,
                 x.Condition is null ? null : "condition: " + x.Condition, x.Reason, x.Status),
            x.CreatedById)));

        // 6. Transferred
        var transfers = await _db.TransferLines.AsNoTracking()
            .Where(l => l.AssetId == assetId && l.IsActive)
            .Select(l => new
            {
                Date = l.Transfer!.Date ?? l.CreatedOn,
                From = l.Transfer.FromLocation!.Name,
                To = l.Transfer.ToLocation!.Name,
                FromCustodian = l.Transfer.FromCustodian!.Name,
                ToCustodian = l.Transfer.ToCustodian!.Name,
                l.Transfer.Status,
                l.CreatedById
            })
            .ToListAsync();
        history.AddRange(transfers.Select(x => new AssetHistoryEntry(x.Date, "Transfer",
            Join(Arrow(x.From, x.To), Arrow(x.FromCustodian, x.ToCustodian), x.Status),
            x.CreatedById)));

        // 7. Incidents
        var incidents = await _db.Incidents.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new
            {
                Date = x.Date ?? x.CreatedOn,
                x.IncidentType,
                x.Description,
                x.Status,
                LastCustodian = x.LastCustodian!.Name,
                x.CreatedById
            })
            .ToListAsync();
        history.AddRange(incidents.Select(x => new AssetHistoryEntry(x.Date, "Incident",
            Join(x.IncidentType?.ToString(), x.Description,
                 x.LastCustodian is null ? null : "last custodian: " + x.LastCustodian, x.Status),
            x.CreatedById)));

        // 8. Maintenance
        var workOrders = await _db.WorkOrders.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new { Date = x.StartDate ?? x.CreatedOn, x.MaintenanceType, x.Problem, x.Status, x.CreatedById })
            .ToListAsync();
        history.AddRange(workOrders.Select(x => new AssetHistoryEntry(x.Date, "Work order",
            Join(x.MaintenanceType?.ToString(), x.Problem, x.Status), x.CreatedById)));

        // 9. Revaluation
        var revaluations = await _db.RevaluationEvents.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new { Date = x.Date ?? x.CreatedOn, x.OldValue, x.NewValue, x.Reason, x.Status, x.CreatedById })
            .ToListAsync();
        history.AddRange(revaluations.Select(x => new AssetHistoryEntry(x.Date, "Revaluation",
            Join($"{x.OldValue:N2} → {x.NewValue:N2}", x.Reason, x.Status), x.CreatedById)));

        // 10. Depreciation
        var depreciation = await _db.DepreciationEntries.AsNoTracking()
            .Where(x => x.AssetId == assetId && x.IsActive)
            .Select(x => new { x.PeriodDate, x.Amount, x.NetBookValue, x.CreatedById })
            .ToListAsync();
        history.AddRange(depreciation.Select(x => new AssetHistoryEntry(x.PeriodDate, "Depreciation",
            $"{x.Amount:N2} · book value {x.NetBookValue:N2}", x.CreatedById)));

        // 11. Disposal
        var disposals = await _db.DisposalRequestLines.AsNoTracking()
            .Where(l => l.AssetId == assetId && l.IsActive)
            .Select(l => new
            {
                Date = l.DisposalRequest!.RequestDate ?? l.CreatedOn,
                l.DisposalRequest.DisposalReason,
                l.DisposalRequest.Status,
                l.CreatedById
            })
            .ToListAsync();
        history.AddRange(disposals.Select(x => new AssetHistoryEntry(x.Date, "Disposal request",
            Join(x.DisposalReason, x.Status), x.CreatedById)));

        return history.OrderBy(h => h.Date).ToList();
    }

    // ------------------------------------------------------------------
    // Numbering
    // ------------------------------------------------------------------

    public async Task<string?> PreviewNextNumberAsync(int categoryId)
    {
        var prefix = await GetPrefixAsync(categoryId);
        return prefix is null ? null : await NextNumberAsync(prefix);
    }

    private async Task<string?> GetPrefixAsync(int categoryId)
    {
        var prefix = await _db.Categories
            .Where(c => c.Id == categoryId)
            .Select(c => c.NumberingPrefix)
            .FirstOrDefaultAsync();

        // "LAP-" and "LAP" both mean prefix "LAP".
        prefix = prefix?.Trim().TrimEnd('-');
        return string.IsNullOrEmpty(prefix) ? null : prefix;
    }

    /// <summary>
    /// Highest existing number for this prefix + 1, e.g. LAP-00007 → LAP-00008.
    /// Inactive assets are counted too, so a number is never reused.
    /// If two users register at the same instant, the unique index on AssetNumber
    /// rejects the second save and that user simply saves again.
    /// </summary>
    private async Task<string> NextNumberAsync(string prefix)
    {
        var start = prefix + "-";

        var existing = await _db.Assets
            .Where(a => a.AssetNumber.StartsWith(start))
            .Select(a => a.AssetNumber)
            .ToListAsync();

        var max = 0;
        foreach (var number in existing)
        {
            if (int.TryParse(number.Substring(start.Length), out var value) && value > max)
            {
                max = value;
            }
        }

        return $"{start}{max + 1:D5}";
    }

    // ------------------------------------------------------------------
    // Writing
    // ------------------------------------------------------------------

    public async Task<int> RegisterAsync(Asset asset)
    {
        var prefix = await GetPrefixAsync(asset.CategoryId);

        if (prefix is not null)
        {
            asset.AssetNumber = await NextNumberAsync(prefix);
        }
        else if (string.IsNullOrWhiteSpace(asset.AssetNumber))
        {
            throw new InvalidOperationException("Asset number is required because the category has no numbering prefix.");
        }

        asset.AssetNumber = asset.AssetNumber.Trim();
        asset.LifecycleStage = asset.CustodianId is null ? LifecycleStage.InStore : LifecycleStage.Assigned;
        asset.IsActive = true;

        try
        {
            _db.Assets.Add(asset);
            await _db.SaveChangesAsync();
            return asset.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task UpdateDetailsAsync(Asset asset)
    {
        if (asset.ParentAssetId == asset.Id)
        {
            throw new InvalidOperationException("An asset cannot be a component of itself.");
        }

        var existing = await _db.Assets.FirstOrDefaultAsync(a => a.Id == asset.Id)
                       ?? throw new InvalidOperationException("Asset not found.");

        // Editable fields - listed one by one on purpose, so it is obvious what can change.
        // NOT here: AssetNumber, LifecycleStage, LocationId, DepartmentId, CustodianId.
        existing.Name = asset.Name;
        existing.Description = asset.Description;
        existing.CategoryId = asset.CategoryId;
        existing.BrandId = asset.BrandId;
        existing.Model = asset.Model;
        existing.SerialNumber = asset.SerialNumber;
        existing.TagOrBarcodeNumber = asset.TagOrBarcodeNumber;
        existing.LegacyAssetNumber = asset.LegacyAssetNumber;
        existing.StatusId = asset.StatusId;
        existing.Condition = asset.Condition;
        existing.ParentAssetId = asset.ParentAssetId;
        existing.SupplierId = asset.SupplierId;
        existing.PurchaseOrderNumber = asset.PurchaseOrderNumber;
        existing.InvoiceNumber = asset.InvoiceNumber;
        existing.PurchaseDate = asset.PurchaseDate;
        existing.PurchaseCost = asset.PurchaseCost;
        existing.Currency = asset.Currency;
        existing.FundingSource = asset.FundingSource;
        existing.WarrantyStart = asset.WarrantyStart;
        existing.WarrantyEnd = asset.WarrantyEnd;
        existing.InsurancePolicyNumber = asset.InsurancePolicyNumber;
        existing.Insurer = asset.Insurer;
        existing.InsuredValue = asset.InsuredValue;
        existing.InsuranceExpiryDate = asset.InsuranceExpiryDate;
        existing.Notes = asset.Notes;

        try
        {
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    // ------------------------------------------------------------------
    // Helpers for the history text
    // ------------------------------------------------------------------

    private static string Join(params string?[] parts) =>
        string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string? Arrow(string? from, string? to) =>
        from is null && to is null ? null : $"{from ?? "—"} → {to ?? "—"}";
}
