using Microsoft.EntityFrameworkCore;
using AMS.Application.Assets;
using AMS.Domain.Entities.Main;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class VerificationService : IVerificationService
{
    private readonly ApplicationDbContext _db;

    public VerificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<VerificationCampaign>> GetAllAsync()
    {
        return await _db.VerificationCampaigns
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
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new InvalidOperationException("The campaign name is required.");
        }

        try
        {
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

            _db.VerificationCampaigns.Add(campaign);
            await _db.SaveChangesAsync();
            return campaign.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task UpdateAsync(int id, VerificationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new InvalidOperationException("The campaign name is required.");
        }

        try
        {
            var campaign = await Load(id);

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

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task<int> CountScopeAsync(int? locationId, int? departmentId, int? categoryId)
    {
        return await Scope(locationId, departmentId, categoryId).CountAsync();
    }

    public async Task TakeSnapshotAsync(int id)
    {
        try
        {
            var campaign = await Load(id);

            if (campaign.Status == VerificationStatus.Closed)
            {
                throw new InvalidOperationException("A closed campaign cannot be changed.");
            }

            var numbers = await Scope(campaign.LocationId, campaign.DepartmentId, campaign.CategoryId)
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

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task SaveResultsAsync(int id, decimal? progress, string? results, bool close)
    {
        if (progress is < 0 or > 100)
        {
            throw new InvalidOperationException("Progress is a percentage between 0 and 100.");
        }

        try
        {
            var campaign = await Load(id);

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

            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    private IQueryable<Asset> Scope(int? locationId, int? departmentId, int? categoryId)
    {
        var query = _db.Assets
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

    private async Task<VerificationCampaign> Load(int id)
    {
        return await _db.VerificationCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.IsActive)
               ?? throw new InvalidOperationException("Campaign not found.");
    }
}
