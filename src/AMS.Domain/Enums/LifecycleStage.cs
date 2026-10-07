namespace AMS.Domain.Enums;

/// <summary>
/// Where an asset is in its life. Set by the system only:
/// at registration (InStore, or Assigned when a custodian is given) and afterwards by transactions.
/// InStore = 0 so any existing asset row automatically becomes InStore.
/// </summary>
public enum LifecycleStage
{
    InStore = 0,
    Assigned = 1,
    InTransit = 2,
    UnderMaintenance = 3,
    Disposed = 4
}
