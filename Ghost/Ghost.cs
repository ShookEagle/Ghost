using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Ghost.extensions;
using Ghost.listeners;

namespace Ghost;

public class Ghost : BasePlugin, IGhost {
  public override string ModuleName => "Ghost";
  public override string ModuleVersion => "1.0.0";
  public HashSet<ulong> GhostPlayers { get; set; } = [];
  public HashSet<ulong> WasGhostThisRound { get; set; } = [];
  public HashSet<ulong> JustJoined { get; set; } = [];

  public override void Load(bool hotReload) {
    AddCommand("css_ghost", "Allows DS players to turn into a ghost while dead",
      OnGhostCommand);

    _ = new BlockEvents(this);

    base.Load(hotReload);
  }

  public BasePlugin GetBase() => this;

  private void OnGhostCommand(CCSPlayerController? player,
    CommandInfo commandInfo) {
    if (player == null || !player.IsDedicatedSupporter()) {
      commandInfo.ReplyLocalized(Localizer, "no_permission_needs_ds_generic");
      return;
    }

    // Must be dead and on a real team to enter ghost mode
    if (player.PawnIsAlive) {
      commandInfo.ReplyLocalized(Localizer, "command_must_be_dead");
      return;
    }

    if (player.Team is CsTeam.Spectator or CsTeam.None) {
      commandInfo.ReplyLocalized(Localizer, "command_must_on_team");
      return;
    }

    if (!GhostPlayers.Contains(player.SteamID)) {
      TryEnterGhost(player);
    } else {
      TryExitGhost(player);
    }
    commandInfo.ReplyLocalized(Localizer, "command_ghost_confirm");
  }

  public bool TryEnterGhost(CCSPlayerController player) {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null) return false;

    if (!GhostPlayers.Add(player.SteamID)) return false;

    player.Respawn();
    player.RemoveWeapons();


    Server.NextFrame(() => {
      pawn.Collision.CollisionAttribute.CollisionGroup =
        (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock
      pawn.Collision.CollisionGroup =
        (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock
      pawn.ShadowStrength = 0f;
      pawn.Render         = Color.Transparent; //for ragdoll if player unredie
      Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
      
      //The Tick after NextFrame players can still pick up weapons at their feet
      //remove these weapons
      AddTickTimer(2, player.RemoveWeapons);

      //timer to avoid blackscreen due to Weapon Removal.
      // Only takes 4 ticks after RemoveWeapons but lag may increase this
      AddTickTimer(8, () => { pawn.LifeState = (byte)LifeState_t.LIFE_DYING; });
    });
    return true;
  }

  public bool TryExitGhost(CCSPlayerController player) {
    var pawn = player.PlayerPawn.Value;
    if (pawn == null) return false;

    if (!GhostPlayers.Remove(player.SteamID)) return false;
    WasGhostThisRound.Add(player.SteamID);

    Server.NextFrame(() => {
      pawn.LifeState = (byte)LifeState_t.LIFE_ALIVE;
      player.CommitSuicide(false, true);
    });
    return true;
  }
}