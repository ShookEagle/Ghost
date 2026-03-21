using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace Ghost.extensions;

public static class PlayerExtensions {
  public static bool IsReal(this CCSPlayerController? player, bool bot = true) {
    //  Do nothing else before this:
    //  Verifies the handle points to an entity within the global entity list.
    if (player == null || !player.IsValid) return false;

    if (player.Connected != PlayerConnectedState.PlayerConnected) return false;

    return (!player.IsBot && !player.IsHLTV) || bot;
  }

  public static bool IsDedicatedSupporter(this CCSPlayerController player) {
    return player.IsReal()
      && AdminManager.PlayerHasPermissions(player, "@ego/ds");
  }
}