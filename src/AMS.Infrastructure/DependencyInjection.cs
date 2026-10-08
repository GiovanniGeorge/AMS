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
        services.AddDbContext<ApplicationDbContext>(options =>
            // Compatibility level 120 = SQL Server 2014, which is what AMS runs on.
            // Without it EF Core 8 writes "OPENJSON(...) WITH (...)" for a list parameter
            // (e.g. Where(a => ids.Contains(a.Id))) and SQL Server 2014 rejects it with
            // "Incorrect syntax near the keyword 'WITH'". Raise it only with the server.
            options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(120)));

        // One registration covers ICrudService<Category>, ICrudService<Brand>, ... etc.
        services.AddScoped(typeof(ICrudService<>), typeof(CrudService<>));

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAssetIssuanceService, AssetIssuanceService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IDisposalService, DisposalService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IRevaluationService, RevaluationService>();
        services.AddScoped<IReceiveService, ReceiveService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IMaintenancePlanService, MaintenancePlanService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IDepreciationService, DepreciationService>();

        return services;
    }
}
