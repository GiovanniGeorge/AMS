namespace AMS.Domain.Enums;

/// <summary>
/// The kind of incident reported against an asset. Fixed list, so an enum.
/// Damaged / Malfunction  -> the approver decides: work order or disposal.
/// Missing / Stolen       -> no approval on the incident; it goes straight to a disposal request,
///                           and the approver is shown the asset's last stage and last custodian.
/// </summary>
public enum IncidentType
{
    Damaged = 1,
    Malfunction = 2,
    Missing = 3,
    Stolen = 4
}
