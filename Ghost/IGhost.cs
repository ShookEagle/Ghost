using CounterStrikeSharp.API.Core;

namespace Ghost;

public interface IGhost {
  HashSet<ulong> GhostPlayers { get; }
  HashSet<ulong> WasGhostThisRound { get; }
  BasePlugin GetBase();
  bool TryEnterGhost(CCSPlayerController player);
  bool TryExitGhost(CCSPlayerController player);
}