using AMS.Domain.Entities.Main;

namespace AMS.Application.Common;

/// <summary>What is uploaded. The bytes are stored in the database, as decided for AMS.</summary>
public record AttachmentInput(
    string RelatedEntityType,
    int RelatedEntityId,
    string FileName,
    string? FileType,
    string? DocumentType,
    byte[] Content);

/// <summary>One attachment in a list - everything except the bytes.</summary>
public record AttachmentInfo(
    int Id,
    string FileName,
    string? FileType,
    string? DocumentType,
    long? FileSize,
    DateTime? UploadedOn,
    int? UploadedById);

/// <summary>
/// Files attached to a record (asset photo, invoice scan, contract...).
/// There is no file system and no download endpoint: the bytes live in Attachments.Content
/// and the page hands them to the browser as a data link.
/// </summary>
public interface IAttachmentService
{
    /// <summary>The largest file AMS accepts, in bytes (5 MB).</summary>
    const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>The attachments of one record, without their bytes, so the list stays light.</summary>
    Task<List<AttachmentInfo>> GetForAsync(string relatedEntityType, int relatedEntityId);

    /// <summary>One attachment with its bytes, for downloading.</summary>
    Task<Attachment?> GetWithContentAsync(int id);

    Task<int> AddAsync(AttachmentInput input);

    /// <summary>Soft delete, like everything else in AMS.</summary>
    Task DeactivateAsync(int id);
}
