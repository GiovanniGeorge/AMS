using System;
using AMS.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities.Main;

public class Attachment : BaseEntity
{
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public string? DocumentType { get; set; }
    /// <summary>
    /// Kept for files that live outside the database. AMS stores the bytes in Content,
    /// so this is normally empty.
    /// </summary>
    public string? StorageReference { get; set; }

    /// <summary>The file itself. AMS keeps attachments inside the database (varbinary(max)).</summary>
    public byte[]? Content { get; set; }
    public int? UploadedById { get; set; }

    [ForeignKey(nameof(UploadedById))]
    public virtual User? UploadedBy { get; set; }
    public DateTime? UploadedOn { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
