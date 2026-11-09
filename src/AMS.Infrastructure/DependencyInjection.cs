using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AMS.Application.Assets;
using AMS.Application.Audit;
using AMS.Application.Common;
using AMS.Application.Inventory;
using AMS.Application.Maintenance;
using AMS.Application.MasterData;
using AMS.Application.Reports;
using AMS.Application.Procurement;
using AMS.Application.Transactions;
using AMS.Infrastructure.Persistence;
using AMS.Infrastructure.Services;

namespace AMS.Infrastructure;

/// <summary>
/// Single place where Infrastructure wires itself up.
/// Program.cs calls this so the Web project never mentions EF Core.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // A factory, not one shared DbContext. In Blazor Server a "scope" is the whole browser tab,
        // so AddDbContext would hand the same context to every page for hours: two pages loading
        // at once collide on it, and it keeps every entity it ever loaded. Each service method
        // asks the factory for a fresh, short-lived context instead.
        // Scoped (not the default Singleton) because the context needs ICurrentUserService,
        // which belongs to the signed-in tab.
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            // Compatibility level 120 = SQL Server 2014, which is what AMS runs on.
            // Without it EF Core 8 writes "OPENJSON(...) WITH (...)" for a list parameter
            // (e.g. Where(a => ids.Contains(a.Id))) and SQL Server 2014 rejects it with
            // "Incorrect syntax near the keyword 'WITH'". Raise it only with the server.
            options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(120)),
            ServiceLifetime.Scoped);

        // One registration covers CrudService<Category>, CrudService<Brand>, ... etc.
        services.AddScoped(typeof(CrudService<>));

        services.AddScoped<Pbkdf2PasswordHasher>();
        services.AddScoped<AccountService>();
        services.AddScoped<AssetService>();
        services.AddScoped<DirectoryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<AssetIssuanceService>();
        services.AddScoped<ReturnService>();
        services.AddScoped<TransferService>();
        services.AddScoped<WorkOrderService>();
        services.AddScoped<DisposalService>();
        services.AddScoped<IncidentService>();
        services.AddScoped<RevaluationService>();
        services.AddScoped<ReceiveService>();
        services.AddScoped<StockService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<AssetImportService>();
        services.AddScoped<SampleDataService>();
        services.AddScoped<VerificationService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<MaintenancePlanService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<DepreciationService>();

        return services;
    }
}
