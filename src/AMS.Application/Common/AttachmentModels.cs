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
