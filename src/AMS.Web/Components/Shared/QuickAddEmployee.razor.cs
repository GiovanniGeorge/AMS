namespace AMS.Web.Components.Shared;

public partial class QuickAddEmployee
{
    [Inject] private CrudService<Department> Departments { get; set; } = default!;

    [Parameter] public EventCallback<Employee> OnAdded { get; set; }

    private List<Department> _departments = new();
    private string? _number;
    private int? _departmentId;

    protected override async Task OnInitializedAsync()
    {
        try { _departments = await Departments.GetAllAsync(); }
        catch { /* the list stays empty; the department is optional anyway */ }
    }

    private string? Check() =>
        string.IsNullOrWhiteSpace(_number) ? "Type the employee number." : null;

    private void Fill(Employee employee)
    {
        employee.EmployeeNumber = _number!.Trim();
        employee.DepartmentId = _departmentId;
    }

    private async Task AddedAsync(Employee employee)
    {
        _number = null;
        _departmentId = null;
        await OnAdded.InvokeAsync(employee);
    }
}
