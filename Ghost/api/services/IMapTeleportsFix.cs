using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Ghost.api.services;

public interface IMapTeleportsFix {
  Dictionary<CTriggerMultiple, Vector> TeleportsList { get;
    set;
  }
  void LoadReplacedTeleportsFromMap();
  void ReplaceMapTeleports();
}