using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// Files attached to a record (asset photo, invoice scan, contract...).
/// There is no file system and no download endpoint: the bytes live in Attachments.Content
/// and the page hands them to the browser as a data link.
/// </summary>
public class AttachmentService
{
    /// <summary>The largest file AMS accepts, in bytes (5 MB).</summary>
    public const int MaxBytes = 5 * 1024 * 1024;

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;

    public AttachmentService(IDbContextFactory<ApplicationDbContext> dbFactory, ICurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    /// <summary>The attachments of one record, without their bytes, so the list stays light.</summary>
    public async Task<List<AttachmentInfo>> GetForAsync(string relatedEntityType, int relatedEntityId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // The bytes are left out on purpose: a list of ten photos should not pull ten files.
        return await db.Attachments
            .AsNoTracking()
            .Where(a => a.IsActive
                        && a.RelatedEntityType == relatedEntityType
                        && a.RelatedEntityId == relatedEntityId)
            .OrderByDescending(a => a.Id)
            .Select(a => new AttachmentInfo(
                a.Id,
                a.FileName ?? "file",
                a.FileType,
                a.DocumentType,
                a.FileSize,
                a.UploadedOn,
                a.UploadedById))
            .ToListAsync();
    }

    /// <summary>One attachment with its bytes, for downloading.</summary>
    public async Task<Attachment?> GetWithContentAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
    }

    public async Task<int> AddAsync(AttachmentInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (input.Content.Length == 0)
        {
            throw new InvalidOperationException("The file is empty.");
        }

        if (input.Content.Length > AttachmentService.MaxBytes)
        {
            throw new InvalidOperationException("The file is larger than 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(input.FileName))
        {
            throw new InvalidOperationException("The file has no name.");
        }

        var attachment = new Attachment
        {
            RelatedEntityType = input.RelatedEntityType,
            RelatedEntityId = input.RelatedEntityId,
            FileName = input.FileName,
            FileType = input.FileType,
            DocumentType = input.DocumentType,
            FileSize = input.Content.Length,
            Content = input.Content,
            UploadedById = await _currentUser.GetUserIdAsync(),
            UploadedOn = DateTime.UtcNow
        };

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();
        return attachment.Id;
    }

    /// <summary>Soft delete, like everything else in AMS.</summary>
    public async Task DeactivateAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
        if (attachment is null)
        {
            return;
        }

        attachment.IsActive = false;
        await db.SaveChangesAsync();
    }
}
