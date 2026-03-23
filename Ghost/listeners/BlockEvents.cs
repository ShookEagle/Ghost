using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace Ghost.listeners;

public class BlockEvents {
  private readonly IGhost plugin;

  public BlockEvents(IGhost plugin) {
    this.plugin = plugin;

    // Disable button use for Ghost players
    plugin.GetBase().HookEntityOutput("func_button", "OnIn", onButtonUse);
    plugin.GetBase().HookEntityOutput("func_button", "OnOut", onButtonUse);

    // Remove sound from Ghost players
    plugin.GetBase().HookUserMessage(208, cMsgSosStartSoundEvent);

    // Remove weapon usage for ghosts
    VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(onCanUse,
      HookMode.Pre);

    // Exit Ghost on team change
    plugin.GetBase().RegisterEventHandler<EventPlayerTeam>(onPlayerTeam);

    // Cleanup Ghost Death Message
    plugin.GetBase()
     .RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);

    // Cleanup Ghosts and Lists On Round End
    plugin.GetBase()
     .RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Pre);

    // Replace TP's On Map Start
    plugin.GetBase().RegisterEventHandler<EventRoundStart>(OnRoundStart);

    // Ghost Tp Fix
    VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnTriggerStartTouch,
      HookMode.Pre);

    // Prevent Ghost Damage
    plugin.GetBase()
     .RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
  }

  public void Unhook() {
    plugin.GetBase().UnhookEntityOutput("func_button", "OnIn", onButtonUse);
    plugin.GetBase().UnhookEntityOutput("func_button", "OnOut", onButtonUse);
    plugin.GetBase().UnhookUserMessage(208, cMsgSosStartSoundEvent);
    VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(onCanUse,
      HookMode.Pre);
    plugin.GetBase().DeregisterEventHandler<EventPlayerTeam>(onPlayerTeam);
    plugin.GetBase()
     .DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
    plugin.GetBase()
     .DeregisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Pre);
    plugin.GetBase().DeregisterEventHandler<EventRoundStart>(OnRoundStart);
    VirtualFunctions.CBaseTrigger_StartTouchFunc.Unhook(OnTriggerStartTouch,
      HookMode.Pre);
    plugin.GetBase()
     .RemoveListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
  }

  private HookResult onButtonUse(CEntityIOOutput output, string name,
    CEntityInstance activator, CEntityInstance caller, CVariant value,
    float delay) {
    if (activator.DesignerName != "player") return HookResult.Continue;
    var pawn = activator.EntityHandle.Value!.As<CCSPlayerPawn>();
    if (!pawn.IsValid) return HookResult.Continue;
    var baseController = pawn.Controller.Value;
    if (baseController == null || !baseController.IsValid)
      return HookResult.Continue;
    var controller = baseController.As<CCSPlayerController>();
    if (!controller.IsValid
      || !plugin.GhostPlayers.Contains(controller.SteamID))
      return HookResult.Continue;

    return HookResult.Handled;
  }

  private HookResult cMsgSosStartSoundEvent(UserMessage um) {
    var entIndex  = um.ReadInt("source_entity_index");
    var entHandle = NativeAPI.GetEntityFromIndex(entIndex);
    var pPawn     = new CBasePlayerPawn(entHandle);
    if (!pPawn.IsValid || pPawn.DesignerName != "player")
      return HookResult.Continue;
    var pController = pPawn.Controller.Value?.As<CCSPlayerController>();
    if (pController == null || !pController.IsValid
      || !plugin.GhostPlayers.Contains(pController.SteamID))
      return HookResult.Continue;
    var players = Utilities.GetPlayers();
    foreach (var player in players
     .Where(player => player is {
        IsValid: true, Connected: PlayerConnectedState.PlayerConnected
      })
     .Where(player => player.SteamID != pController.SteamID)) {
      _ = um.Recipients.Remove(player);
    }

    return HookResult.Continue;
  }

  private HookResult onCanUse(DynamicHook hook) {
    var weaponServices = hook.GetParam<CCSPlayer_WeaponServices>(0);
    var player =
      new CCSPlayerController(
        weaponServices.Pawn.Value.Controller.Value!.Handle);

    if (!plugin.GhostPlayers.Contains(player.SteamID))
      return HookResult.Continue;
    hook.SetReturn(false);
    return HookResult.Handled;
  }

  private HookResult onPlayerTeam(EventPlayerTeam @event, GameEventInfo info) {
    var player = @event.Userid;
    if (player == null || !player.IsValid) return HookResult.Continue;

    if (plugin.GhostPlayers.Contains(player.SteamID)) {
      plugin.TryExitGhost(player);
    }

    return HookResult.Continue;
  }

  private HookResult
    OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info) {
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

    plugin.WasGhostThisRound.Clear();
    plugin.GhostPlayers.Clear();

    return HookResult.Continue;
  }

  private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info) {
    plugin.GetMapTeleportsFix().ReplaceMapTeleports();

    plugin.WasGhostThisRound.Clear();
    plugin.GhostPlayers.Clear();

    return HookResult.Continue;
  }

  private HookResult OnTriggerStartTouch(DynamicHook hook) {
    var entity = hook.GetParam<CBaseEntity>(1);
    if (!entity.IsValid || entity.DesignerName != "player")
      return HookResult.Continue;

    var pawn = new CCSPlayerPawn(entity.Handle);
    if (!pawn.IsValid || pawn.Controller.Value == null)
      return HookResult.Continue;

    var player = new CCSPlayerController(pawn.Controller.Value.Handle);
    if (!player.IsValid) return HookResult.Continue;

    var trigger = hook.GetParam<CTriggerMultiple>(0);
    if (trigger is not { IsValid: true } || trigger.Entity == null)
      return HookResult.Continue;

    var triggerName = trigger.Entity.Name;
    if (string.IsNullOrWhiteSpace(triggerName)) return HookResult.Continue;

    var name = triggerName.Split(';')[0];

    if (name == "redactedtpto") {
      if (plugin.GetMapTeleportsFix()
       .TeleportsList.TryGetValue(trigger, out var value))
        player.PlayerPawn.Value?.Teleport(value);
    }

    hook.SetReturn(true);
    return HookResult.Continue;
  }

  private HookResult OnTakeDamage(CBaseEntity entity, CTakeDamageInfo info) {
    if (!entity.IsPlayerPawn()) return HookResult.Continue;
    var pawn = entity.As<CCSPlayerPawn>();
    if (pawn.DesignerName != "player" || pawn.Controller.Value == null)
      return HookResult.Continue;
    var player = pawn.Controller.Value?.As<CCSPlayerController>();

    if (player == null || !plugin.GhostPlayers.Contains(player.SteamID))
      return HookResult.Continue;

    info.Damage = 0;
    return HookResult.Changed;
  }
}