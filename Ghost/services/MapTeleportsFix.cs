using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Ghost.api.services;

namespace Ghost.services;

public class MapTeleportsFix : IMapTeleportsFix {
  public Dictionary<CTriggerMultiple, Vector> TeleportsList { get; set; } = [];

  public void LoadReplacedTeleportsFromMap() {
    var triggers =
      Utilities.FindAllEntitiesByDesignerName<CTriggerMultiple>(
        "trigger_multiple");
    var destinations =
      Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(
        "info_teleport_destination");

    foreach (var trigger in triggers) {
      if (trigger.Entity == null) continue;

      var teleportData = trigger.Entity.Name.Split(";");

      if (teleportData[0] != "redactedtpto") continue;
      // ReSharper disable once PossibleMultipleEnumeration
      foreach (var destination in destinations) {
        if (destination.Entity == null) continue;
        if (destination.AbsOrigin == null) continue;

        if (destination.Entity!.Name == teleportData[1]) {
          TeleportsList.Add(trigger, destination.AbsOrigin);
        }
      }
    }
  }

  public void ReplaceMapTeleports() {
    TeleportsList.Clear();
    var teleports =
      Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>(
        "trigger_teleport");
    var destinations =
      Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(
        "info_teleport_destination");

    foreach (var teleport in teleports) {
      var trigger =
        Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");

      if (trigger == null || !trigger.IsValid) continue;
      trigger.Entity!.Name = "redactedtpto" + ";" + teleport.Target;
      trigger.Spawnflags   = 1;
      trigger.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &=
        ~(uint)(1 << 2);
      trigger.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
      trigger.Collision.SolidFlags = 0;
      trigger.Collision.CollisionGroup = 2;
      trigger.Collision.CollisionAttribute.CollisionGroup = 2;
      trigger.SetModel(teleport.CBodyComponent!.SceneNode!.GetSkeletonInstance()
       .ModelState.ModelName);
      trigger.DispatchSpawn();
      trigger.Teleport(teleport.AbsOrigin, teleport.AbsRotation);
      trigger.AcceptInput("Enable");

      // ReSharper disable once PossibleMultipleEnumeration
      foreach (var destination in destinations) {
        if (destination.Entity!.Name == teleport.Target) {
          TeleportsList.Add(trigger, destination.AbsOrigin!);
        }
      }

      teleport.Remove();
    }
  }
}