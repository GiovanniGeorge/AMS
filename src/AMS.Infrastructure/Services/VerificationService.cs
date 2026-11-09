using Microsoft.EntityFrameworkCore;
using AMS.Application.Assets;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// A physical count of the assets that match a scope (location / department / category).
/// The approved entity list has no line table, so the campaign keeps the expected assets
/// as a snapshot of asset numbers, and the progress and findings as plain text.
/// </summary>
public class VerificationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public VerificationService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<VerificationCampaign>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.VerificationCampaigns
            .AsNoTracking()
            .Include(c => c.Location)
            .Include(c => c.Department)
            .Include(c => c.Category)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(VerificationInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new InvalidOperationException("The campaign name is required.");
        }

        var campaign = new VerificationCampaign
        {
            Name = input.Name.Trim(),
            LocationId = input.LocationId,
            DepartmentId = input.DepartmentId,
            CategoryId = input.CategoryId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Status = VerificationStatus.Draft
        };

        db.VerificationCampaigns.Add(campaign);
        await db.SaveChangesAsync();
        return campaign.Id;
    }

    public async Task UpdateAsync(int id, VerificationInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new InvalidOperationException("The campaign name is required.");
        }

        var campaign = await Load(db, id);

        if (campaign.Status == VerificationStatus.Closed)
        {
            throw new InvalidOperationException("A closed campaign cannot be changed.");
        }

        campaign.Name = input.Name.Trim();
        campaign.LocationId = input.LocationId;
        campaign.DepartmentId = input.DepartmentId;
        campaign.CategoryId = input.CategoryId;
        campaign.StartDate = input.StartDate;
        campaign.EndDate = input.EndDate;

        await db.SaveChangesAsync();
    }

    /// <summary>How many active assets match the scope right now.</summary>
    public async Task<int> CountScopeAsync(int? locationId, int? departmentId, int? categoryId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await Scope(db, locationId, departmentId, categoryId).CountAsync();
    }

    /// <summary>
    /// Writes the asset numbers that match the scope into ExpectedSnapshot and starts the campaign.
    /// Re-running it replaces the snapshot.
    /// </summary>
    public async Task TakeSnapshotAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var campaign = await Load(db, id);

        if (campaign.Status == VerificationStatus.Closed)
        {
            throw new InvalidOperationException("A closed campaign cannot be changed.");
        }

        var numbers = await Scope(db, campaign.LocationId, campaign.DepartmentId, campaign.CategoryId)
            .OrderBy(a => a.AssetNumber)
            .Select(a => a.AssetNumber)
            .ToListAsync();

        if (numbers.Count == 0)
        {
            throw new InvalidOperationException("No active asset matches this scope.");
        }

        campaign.ExpectedSnapshot = string.Join(", ", numbers);
        campaign.Status = VerificationStatus.InProgress;
        campaign.Progress = 0m;

        await db.SaveChangesAsync();
    }

    /// <summary>Saves what was found: progress as a percentage and the findings as text.</summary>
    public async Task SaveResultsAsync(int id, decimal? progress, string? results, bool close)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (progress is < 0 or > 100)
        {
            throw new InvalidOperationException("Progress is a percentage between 0 and 100.");
        }

        var campaign = await Load(db, id);

        if (campaign.Status == VerificationStatus.Draft)
        {
            throw new InvalidOperationException("Take the expected snapshot before recording results.");
        }

        campaign.Progress = progress;
        campaign.Results = results;

        if (close)
        {
            campaign.Status = VerificationStatus.Closed;
            campaign.EndDate ??= DateTime.UtcNow.Date;
        }

        await db.SaveChangesAsync();
    }

    private IQueryable<Asset> Scope(ApplicationDbContext db, int? locationId, int? departmentId, int? categoryId)
    {
        var query = db.Assets
            .AsNoTracking()
            .Where(a => a.IsActive && a.LifecycleStage != LifecycleStage.Disposed);

        if (locationId is not null)
        {
            query = query.Where(a => a.LocationId == locationId);
        }

        if (departmentId is not null)
        {
            query = query.Where(a => a.DepartmentId == departmentId);
        }

        if (categoryId is not null)
        {
            query = query.Where(a => a.CategoryId == categoryId);
        }

        return query;
    }

    private async Task<VerificationCampaign> Load(ApplicationDbContext db, int id)
    {
        return await db.VerificationCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.IsActive)
               ?? throw new InvalidOperationException("Campaign not found.");
    }
}
