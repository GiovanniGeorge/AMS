using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Domain.Entities.Main;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AttachmentService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AttachmentInfo>> GetForAsync(string relatedEntityType, int relatedEntityId)
    {
        // The bytes are left out on purpose: a list of ten photos should not pull ten files.
        return await _db.Attachments
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

    public async Task<Attachment?> GetWithContentAsync(int id)
    {
        return await _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
    }

    public async Task<int> AddAsync(AttachmentInput input)
    {
        if (input.Content.Length == 0)
        {
            throw new InvalidOperationException("The file is empty.");
        }

        if (input.Content.Length > IAttachmentService.MaxBytes)
        {
            throw new InvalidOperationException("The file is larger than 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(input.FileName))
        {
            throw new InvalidOperationException("The file has no name.");
        }

        try
        {
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

            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync();
            return attachment.Id;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    public async Task DeactivateAsync(int id)
    {
        try
        {
            var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
            if (attachment is null)
            {
                return;
            }

            attachment.IsActive = false;
            await _db.SaveChangesAsync();
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }
}
