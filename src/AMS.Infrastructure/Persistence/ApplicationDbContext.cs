using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using AMS.Application.Common;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;

namespace AMS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Fills the audit fields of every BaseEntity before saving:
    /// new rows get CreatedOn + CreatedById, changed rows get ModifiedOn + ModifiedById.
    /// An update can never overwrite who created the row or when.
    ///
    /// It then writes one AuditLog row per changed entity. The rows are written in a
    /// SECOND save, because a new row has no Id until the first one has run.
    /// A Deactivate (IsActive true -> false) is logged as Delete: AMS has no hard delete.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        int? userId = null;
        var now = DateTime.UtcNow;
        var pending = new List<PendingAudit>();

        if (entries.Count > 0)
        {
            userId = await _currentUser.GetUserIdAsync();

            foreach (var entry in entries)
            {
                var isInsert = entry.State == EntityState.Added;

                if (isInsert)
                {
                    entry.Entity.CreatedOn = now;
                    entry.Entity.CreatedById = userId;
                }
                else
                {
                    entry.Entity.ModifiedOn = now;
                    entry.Entity.ModifiedById = userId;

                    entry.Property(e => e.CreatedOn).IsModified = false;
                    entry.Property(e => e.CreatedById).IsModified = false;
                }

                // Which properties changed, and their old values, can only be read HERE:
                // after the save every entry is Unchanged again.
                var changed = entry.Properties
                    .Where(property => isInsert || property.IsModified)
                    .Select(property => property.Metadata.Name)
                    .Where(Loggable)
                    .Take(40)
                    .ToList();

                pending.Add(new PendingAudit(
                    entry,
                    isInsert ? AuditAction.Insert
                        : IsDeactivation(entry) ? AuditAction.Delete : AuditAction.Update,
                    entry.Entity.GetType().Name,
                    changed,
                    isInsert ? null : Describe(entry, changed, original: true)));
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count > 0)
        {
            foreach (var item in pending)
            {
                AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = item.Action,
                    EntityName = item.Name,
                    RecordId = item.Entry.Entity.Id.ToString(),
                    DateTime = now,
                    OldValues = item.Old,
                    // Read AFTER the save, so a new row shows its real Id and foreign keys
                    // instead of the temporary negative ones EF uses before saving.
                    NewValues = Describe(item.Entry, item.Changed, original: false),
                    LogLevel = "Information"
                });
            }

            // base, not this: the log rows must not be audited themselves.
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private sealed record PendingAudit(
        EntityEntry<BaseEntity> Entry,
        AuditAction Action,
        string Name,
        List<string> Changed,
        string? Old);

    private static bool IsDeactivation(EntityEntry<BaseEntity> entry)
    {
        var property = entry.Property(e => e.IsActive);
        return property.IsModified
               && property.OriginalValue
               && !property.CurrentValue;
    }

    /// <summary>
    /// The row's Id is already in AuditLog.RecordId, the modified stamps are already in
    /// AuditLog.DateTime and UserId, and a password is never written anywhere.
    /// </summary>
    private static bool Loggable(string name) =>
        !name.Contains("Password", StringComparison.OrdinalIgnoreCase)
        && name != nameof(BaseEntity.Id)
        && name != nameof(BaseEntity.ModifiedOn)
        && name != nameof(BaseEntity.ModifiedById);

    /// <summary>The named properties as JSON - only what changed, so the log stays readable.</summary>
    private static string? Describe(EntityEntry<BaseEntity> entry, List<string> names, bool original)
    {
        if (names.Count == 0)
        {
            return null;
        }

        var values = new Dictionary<string, string?>();

        foreach (var name in names)
        {
            var property = entry.Property(name);
            var text = (original ? property.OriginalValue : property.CurrentValue)?.ToString();

            if (text is { Length: > 200 })
            {
                text = text[..200];
            }

            values[name] = text;
        }

        return JsonSerializer.Serialize(values);
    }

    // ==========================================
    // MAIN ENTITIES
    // ==========================================
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<AssetStatus> AssetStatuses => Set<AssetStatus>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans => Set<PreventiveMaintenancePlan>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<MaintenanceContract> MaintenanceContracts => Set<MaintenanceContract>();
    public DbSet<VerificationCampaign> VerificationCampaigns => Set<VerificationCampaign>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ==========================================
    // TRANSACTION ENTITIES
    // ==========================================
    public DbSet<Receive> Receives => Set<Receive>();
    public DbSet<ReceiveLine> ReceiveLines => Set<ReceiveLine>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferLine> TransferLines => Set<TransferLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<DisposalRequest> DisposalRequests => Set<DisposalRequest>();
    public DbSet<DisposalRequestLine> DisposalRequestLines => Set<DisposalRequestLine>();
    public DbSet<RevaluationEvent> RevaluationEvents => Set<RevaluationEvent>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();
    public DbSet<Approval> Approvals => Set<Approval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ------------------------------------------------------------------
        // 1. User / Role many-to-many through UserRole
        // ------------------------------------------------------------------
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId);

        // ------------------------------------------------------------------
        // 2. Max lengths - only where a column is indexed or is a short code.
        //    SQL Server cannot index nvarchar(max), so these are required.
        // ------------------------------------------------------------------
        modelBuilder.Entity<User>().Property(x => x.Username).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(x => x.PasswordHash).HasMaxLength(256);
        modelBuilder.Entity<Asset>().Property(x => x.AssetNumber).HasMaxLength(50);
        modelBuilder.Entity<Asset>().Property(x => x.TagOrBarcodeNumber).HasMaxLength(50);
        modelBuilder.Entity<Employee>().Property(x => x.EmployeeNumber).HasMaxLength(50);
        modelBuilder.Entity<PurchaseOrder>().Property(x => x.PurchaseOrderNumber).HasMaxLength(50);
        modelBuilder.Entity<Invoice>().Property(x => x.InvoiceNumber).HasMaxLength(50);
        modelBuilder.Entity<Attachment>().Property(x => x.RelatedEntityType).HasMaxLength(100);

        // Display names - shown in every list and drop-down.
        modelBuilder.Entity<Role>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Employee>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Department>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Location>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Category>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Brand>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Unit>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<AssetStatus>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Asset>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Item>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Store>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Supplier>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<Vendor>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<PreventiveMaintenancePlan>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<VerificationCampaign>().Property(x => x.Name).HasMaxLength(200);

        // ------------------------------------------------------------------
        // 3. Unique indexes
        // ------------------------------------------------------------------
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username).IsUnique();

        modelBuilder.Entity<Asset>()
            .HasIndex(x => x.AssetNumber).IsUnique();

        // Nullable column: filtered so many assets may have no barcode.
        modelBuilder.Entity<Asset>()
            .HasIndex(x => x.TagOrBarcodeNumber).IsUnique()
            .HasFilter("[TagOrBarcodeNumber] IS NOT NULL");

        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.EmployeeNumber).IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(x => x.PurchaseOrderNumber).IsUnique();

        // Invoice numbers come from the supplier, so they are only unique per supplier.
        modelBuilder.Entity<Invoice>()
            .HasIndex(x => new { x.SupplierId, x.InvoiceNumber }).IsUnique()
            .HasFilter("[SupplierId] IS NOT NULL");

        modelBuilder.Entity<StockBalance>()
            .HasIndex(x => new { x.StoreId, x.ItemId }).IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();

        modelBuilder.Entity<TransferLine>()
            .HasIndex(x => new { x.TransferId, x.AssetId }).IsUnique();

        modelBuilder.Entity<DepreciationEntry>()
            .HasIndex(x => new { x.AssetId, x.PeriodDate }).IsUnique();

        // ------------------------------------------------------------------
        // 4. Lookup index - attachments are found only by this pair (no FK).
        // ------------------------------------------------------------------
        modelBuilder.Entity<Attachment>()
            .HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });

        // ------------------------------------------------------------------
        // 5. "Exactly one of AssetId / ItemId" check constraints.
        //    (Issue has no ItemId any more - Asset Issuance only - so it has no check.)
        //    SQL Server has no usable boolean expression, hence the CASE form.
        // ------------------------------------------------------------------
        const string oneOfAssetOrItem =
            "(CASE WHEN [AssetId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ItemId] IS NULL THEN 0 ELSE 1 END) = 1";

        modelBuilder.Entity<Return>()
            .ToTable(t => t.HasCheckConstraint("CK_Return_AssetOrItem", oneOfAssetOrItem));

        modelBuilder.Entity<ReceiveLine>()
            .ToTable(t => t.HasCheckConstraint("CK_ReceiveLine_AssetOrItem", oneOfAssetOrItem));

        // ------------------------------------------------------------------
        // 6. CreatedById / ModifiedById -> User, for every BaseEntity.
        //    Configured here (not with navigation properties) so the entity
        //    classes stay clean. No navigation is created on either side.
        // ------------------------------------------------------------------
        var auditedTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in auditedTypes)
        {
            modelBuilder.Entity(clrType)
                .HasOne(typeof(User))
                .WithMany()
                .HasForeignKey(nameof(BaseEntity.CreatedById));

            modelBuilder.Entity(clrType)
                .HasOne(typeof(User))
                .WithMany()
                .HasForeignKey(nameof(BaseEntity.ModifiedById));
        }

        // ------------------------------------------------------------------
        // 7. decimal(18,2) for every decimal property.
        // ------------------------------------------------------------------
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // ------------------------------------------------------------------
        // 8. Restrict on every relationship. Must stay LAST so it also covers
        //    the relationships configured above. AMS never hard-deletes, and
        //    this prevents SQL Server error 1785 (multiple cascade paths).
        // ------------------------------------------------------------------
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
