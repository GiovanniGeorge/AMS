// Usings for every .cs file in AMS.Web.
// _Imports.razor does the same job for .razor files only - it never reaches a .cs file -
// so the code-behind files (Page.razor.cs) get their namespaces from here.
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Routing;
global using Microsoft.AspNetCore.Components.Web;
global using static Microsoft.AspNetCore.Components.Web.RenderMode;
global using Microsoft.JSInterop;
global using AMS.Web.Components.Shared;
global using AMS.Application.Assets;
global using AMS.Application.Audit;
global using AMS.Application.Common;
global using AMS.Application.Inventory;
global using AMS.Application.Maintenance;
global using AMS.Application.MasterData;
global using AMS.Application.Reports;
global using AMS.Application.Procurement;
global using AMS.Application.Transactions;
global using AMS.Domain.Common;
global using AMS.Domain.Enums;
global using AMS.Domain.Entities.Main;
global using AMS.Domain.Entities.Transactions;
global using AMS.Infrastructure.Services;
