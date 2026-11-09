using Microsoft.EntityFrameworkCore;
using AMS.Application.Common;
using AMS.Application.Procurement;
using AMS.Domain.Common;
using AMS.Domain.Entities.Main;
using AMS.Domain.Entities.Transactions;
using AMS.Domain.Enums;
using AMS.Infrastructure.Persistence;

namespace AMS.Infrastructure.Services;

/// <summary>
/// A made-up dataset for trying the system out: categories, locations, people,
/// assets, one custody, a purchase order and an invoice.
///
/// It is invented, not a copy of anyone's register, and it contains no login
/// accounts - whoever installs AMS still creates their own on the setup page.
/// </summary>
public class SampleDataService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public SampleDataService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>True while the register is still empty, which is the only time it may be loaded.</summary>
    public async Task<bool> CanLoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return !await db.Assets.AnyAsync();
    }

    /// <summary>Writes the dataset and returns a one-line summary of what it added.</summary>
    public async Task<string> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (!await CanLoadAsync())
        {
            throw new InvalidOperationException(
                "The register already has assets. The sample data is only for an empty system.");
        }

        // ---------------------------------------------------------- lookups
        var piece = new Unit { Name = "Piece" };
        var box = new Unit { Name = "Box" };
        db.Units.AddRange(piece, box);

        var laptops = new Category { Name = "Laptops", NumberingPrefix = "LAP", UsefulLife = 4, SalvageValue = 1000 };
        var furniture = new Category { Name = "Furniture", NumberingPrefix = "FUR", UsefulLife = 10, SalvageValue = 200 };
        var printers = new Category { Name = "Printers", NumberingPrefix = "PRN", UsefulLife = 5, SalvageValue = 300 };
        db.Categories.AddRange(laptops, furniture, printers);

        var isNew = new AssetStatus { Name = "New" };
        var good = new AssetStatus { Name = "Good" };
        var damaged = new AssetStatus { Name = "Damaged" };
        db.AssetStatuses.AddRange(isNew, good, damaged);

        var dell = new Brand { Name = "Dell" };
        var hp = new Brand { Name = "HP" };
        db.Brands.AddRange(dell, hp);

        var head = new Location { Name = "Head office" };
        var branch = new Location { Name = "Maadi branch" };
        db.Locations.AddRange(head, branch);

        var it = new Department { Name = "IT" };
        var finance = new Department { Name = "Finance" };
        db.Departments.AddRange(it, finance);

        var mainStore = new Store { Name = "Main store" };
        db.Stores.Add(mainStore);

        var techno = new Supplier { Name = "TechnoSupply" };
        var nile = new Supplier { Name = "Nile Office Supplies" };
        db.Suppliers.AddRange(techno, nile);

        var careCo = new Vendor { Name = "CareCo Maintenance" };
        db.Vendors.Add(careCo);

        await db.SaveChangesAsync();

        // ---------------------------------------------------------- store items
        var toner = new Item { Name = "Printer toner", UnitOfMeasureId = piece.Id, MinimumLevel = 4, ReorderPoint = 6 };
        var cable = new Item { Name = "Network cable 3m", UnitOfMeasureId = piece.Id, MinimumLevel = 20, ReorderPoint = 30 };
        db.Items.AddRange(toner, cable);

        var hoda = new Employee { EmployeeNumber = "E-001", Name = "Hoda Samir", DepartmentId = it.Id };
        var karim = new Employee { EmployeeNumber = "E-002", Name = "Karim Fouad", DepartmentId = finance.Id };
        var sara = new Employee { EmployeeNumber = "E-003", Name = "Sara Nabil", DepartmentId = it.Id };
        db.Employees.AddRange(hoda, karim, sara);

        await db.SaveChangesAsync();

        db.StockBalances.AddRange(
            new StockBalance { StoreId = mainStore.Id, ItemId = toner.Id, Quantity = 9 },
            new StockBalance { StoreId = mainStore.Id, ItemId = cable.Id, Quantity = 45 });

        // ---------------------------------------------------------- assets
        var bought = DateTime.Today.AddMonths(-7);

        var laptop1 = Asset("LAP-00001", "Dell Latitude 5440", laptops, isNew, dell, techno, head, it, bought, 21500);
        var laptop2 = Asset("LAP-00002", "Dell Latitude 5440", laptops, isNew, dell, techno, head, it, bought, 21500);
        var laptop3 = Asset("LAP-00003", "HP ProBook 450", laptops, good, hp, techno, branch, finance, bought.AddMonths(2), 18900);
        var printer = Asset("PRN-00001", "HP LaserJet M428", printers, good, hp, techno, head, it, bought.AddMonths(1), 9400);
        var printer2 = Asset("PRN-00002", "HP LaserJet M428", printers, damaged, hp, techno, branch, finance, bought.AddMonths(1), 9400);
        var desk1 = Asset("FUR-00001", "Meeting room table", furniture, good, null, nile, head, null, bought.AddMonths(3), 7200);
        var desk2 = Asset("FUR-00002", "Reception desk", furniture, good, null, nile, head, null, bought.AddMonths(3), 5400);
        var chair = Asset("FUR-00003", "Office chair", furniture, isNew, null, nile, branch, null, DateTime.Today.AddDays(-20), 2100);

        db.Assets.AddRange(laptop1, laptop2, laptop3, printer, printer2, desk1, desk2, chair);
        await db.SaveChangesAsync();

        // ---------------------------------------------------------- one approved custody
        var approval = new Approval
        {
            Decision = RequestStatus.Approved,
            Status = RequestStatus.Approved,
            Comments = "Sample data",
            DecisionDate = DateTime.UtcNow
        };

        var issue = new Issue
        {
            Date = bought.AddMonths(1),
            AssetId = laptop1.Id,
            EmployeeId = hoda.Id,
            StoreId = mainStore.Id,
            Status = RequestStatus.Approved
        };

        db.AssetAssignments.Add(new AssetAssignment
        {
            AssetId = laptop1.Id,
            CustodianId = hoda.Id,
            DepartmentId = it.Id,
            LocationId = head.Id,
            AssignmentDate = issue.Date,
            Status = RequestStatus.Approved,
            Notes = "Sample data",
            Issue = issue,
            Approval = approval
        });

        laptop1.CustodianId = hoda.Id;
        laptop1.DepartmentId = it.Id;
        laptop1.LocationId = head.Id;
        laptop1.LifecycleStage = LifecycleStage.Assigned;

        // One waiting for a decision, so the approval screens have something in them.
        var pendingIssue = new Issue
        {
            Date = DateTime.Today,
            AssetId = laptop2.Id,
            EmployeeId = karim.Id,
            StoreId = mainStore.Id,
            Status = RequestStatus.Pending
        };

        db.AssetAssignments.Add(new AssetAssignment
        {
            AssetId = laptop2.Id,
            CustodianId = karim.Id,
            DepartmentId = finance.Id,
            LocationId = branch.Id,
            AssignmentDate = DateTime.Today,
            Status = RequestStatus.Pending,
            Notes = "Sample data — waiting for approval",
            Issue = pendingIssue
        });

        // ---------------------------------------------------------- procurement
        var order = new PurchaseOrder
        {
            PurchaseOrderNumber = "PO-00001",
            SupplierId = techno.Id,
            PurchaseDate = bought.AddDays(-10),
            ExpectedDeliveryDate = bought,
            Currency = Currencies.Default,
            Status = PurchaseOrderStatus.Closed,
            TotalCost = 52900
        };
        order.Lines.Add(new PurchaseOrderLine { Description = "Dell Latitude 5440", Quantity = 2, UnitPrice = 21500 });
        order.Lines.Add(new PurchaseOrderLine { ItemId = toner.Id, Quantity = 6, UnitPrice = 1650 });
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-2026-0041",
            SupplierId = techno.Id,
            PurchaseOrderId = order.Id,
            InvoiceDate = bought,
            Currency = Currencies.Default,
            Amount = 43000
        };
        invoice.Lines.Add(new InvoiceLine { AssetId = laptop1.Id, Description = "LAP-00001", Quantity = 1, UnitPrice = 21500 });
        invoice.Lines.Add(new InvoiceLine { AssetId = laptop2.Id, Description = "LAP-00002", Quantity = 1, UnitPrice = 21500 });
        db.Invoices.Add(invoice);

        // ---------------------------------------------------------- maintenance
        db.PreventiveMaintenancePlans.Add(new PreventiveMaintenancePlan
        {
            Name = "Quarterly laptop service",
            CategoryId = laptops.Id,
            Frequency = "Quarterly",
            StartDate = DateTime.Today.AddMonths(-3),
            EndDate = DateTime.Today.AddMonths(9)
        });

        db.MaintenanceContracts.Add(new MaintenanceContract
        {
            ContractNumber = "AMC-2026-01",
            VendorId = careCo.Id,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddMonths(10),
            CoverageDetails = "Printers and laptops, 48h response"
        });

        await db.SaveChangesAsync();

        return "Added 3 categories, 3 conditions, 2 locations, 2 departments, 1 store, "
               + "2 suppliers, 1 service vendor, 2 store items, 3 employees, 8 assets "
               + "(one in custody, one waiting for approval), 1 purchase order, 1 invoice, "
               + "1 preventive plan and 1 maintenance contract.";
    }

    private static Asset Asset(
        string number, string name, Category category, AssetStatus status, Brand? brand,
        Supplier supplier, Location location, Department? department,
        DateTime purchased, decimal cost) => new()
    {
        AssetNumber = number,
        Name = name,
        CategoryId = category.Id,
        StatusId = status.Id,
        BrandId = brand?.Id,
        SupplierId = supplier.Id,
        LocationId = location.Id,
        DepartmentId = department?.Id,
        PurchaseDate = purchased,
        PurchaseCost = cost,
        Currency = Currencies.Default,
        LifecycleStage = LifecycleStage.InStore
    };
}
