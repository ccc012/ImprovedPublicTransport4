using System.Collections.Generic;
using JetBrains.Annotations;

namespace ImprovedPublicTransport.Data
{
  public struct LineData
  {
    public int TargetVehicleCount { get; set; }

    public float NextSpawnTime { get; set; }

    public bool BudgetControl { get; set; }

    public bool Unbunching { get; set; }

    public ushort Depot { get; set; }

    [CanBeNull]
    public HashSet<string> Prefabs { get; set; }

    [CanBeNull]
    public Queue<string> QueuedVehicles { get; set; }
  }
}
