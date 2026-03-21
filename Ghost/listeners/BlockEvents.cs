using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using Ghost.extensions;

namespace Ghost.listeners;

public class BlockEvents {
  private readonly IGhost plugin;

  public BlockEvents(IGhost plugin) {
    this.plugin = plugin;
    plugin.GetBase()
     .RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Pre);
    plugin.GetBase().RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);

    VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnCanUse,
      HookMode.Pre);

    plugin.GetBase()
     .RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);

    plugin.GetBase().HookUserMessage(208, OnSoundEvent);
    plugin.GetBase().RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
  }

  private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info) {
    if (@event.Userid == null) return HookResult.Continue;
    if (plugin.WasGhostThisRound.Contains(@event.Userid.SteamID))
      info.DontBroadcast = true;
    return HookResult.Continue;
  }

  private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info) {
    var playersToClear = plugin.GhostPlayers;

    foreach (var playerController in playersToClear
     .Select(player
        => Utilities.GetPlayers().FirstOrDefault(p => p.SteamID == player))
     .OfType<CCSPlayerController>()) { plugin.TryExitGhost(playerController); }
    return HookResult.Continue;
  }

  // Ghost players cannot pick up weapons
  HookResult OnCanUse(DynamicHook hook) {
    var weaponServices = hook.GetParam<CCSPlayer_WeaponServices>(0);
    var player =
      new CCSPlayerController(
        weaponServices.Pawn.Value.Controller.Value!.Handle);

    if (plugin.GhostPlayers.Contains(player.SteamID)) {
      hook.SetReturn(false);
      return HookResult.Handled;
    }

    return HookResult.Continue;
  }

  // Ghost players take no damage
  private HookResult OnTakeDamage(CBaseEntity entity, CTakeDamageInfo info) {
    var pawn = Utilities.GetPlayers()
     .FirstOrDefault(p
        => p.IsReal() && p.PlayerPawn.Value?.Index == entity.Index);
    if (pawn != null && (pawn.DesignerName != "player"
      || pawn.OriginalControllerOfCurrentPawn.Value == null))
      return HookResult.Continue;

    var player = pawn?.OriginalControllerOfCurrentPawn.Value;

    if (player != null && !plugin.GhostPlayers.Contains(player.SteamID))
      return HookResult.Continue;
    info.Damage = 0;
    return HookResult.Handled;
  }

  // Other players don't hear ghost player footsteps/sounds
  HookResult OnSoundEvent(UserMessage um) {
    var entIndex  = um.ReadInt("source_entity_index");
    var entHandle = NativeAPI.GetEntityFromIndex(entIndex);
    var pPawn     = new CBasePlayerPawn(entHandle);
    if (!pPawn.IsValid || pPawn.DesignerName != "player")
      return HookResult.Continue;

    var controller = pPawn.Controller.Value?.As<CCSPlayerController>();
    if (controller == null || !controller.IsValid
      || !plugin.GhostPlayers.Contains(controller.SteamID))
      return HookResult.Continue;

    foreach (var player in Utilities.GetPlayers()
     .Where(player => player is {
        IsValid: true, Connected: PlayerConnectedState.PlayerConnected
      })
     .Where(player => player.Slot != controller.Slot)) {
      _ = um.Recipients.Remove(player);
    }

    return HookResult.Continue;
  }

  // If a ghost changes team, clean them up
  HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info) {
    var player = @event.Userid;
    if (player == null || !player.IsValid) return HookResult.Continue;

    if (plugin.GhostPlayers.Contains(player.SteamID)) {
      plugin.TryExitGhost(player);
    }

    return HookResult.Continue;
  }
}