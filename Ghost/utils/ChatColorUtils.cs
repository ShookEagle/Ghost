using CounterStrikeSharp.API.Modules.Utils;

namespace Ghost.utils;

internal class ChatColorUtils {
  public static readonly char[] AVAILABLE_COLORS = {
    ChatColors.White, ChatColors.Green, ChatColors.Yellow, ChatColors.Olive,
    ChatColors.Lime, ChatColors.LightPurple, ChatColors.Purple, ChatColors.Grey,
    ChatColors.Gold, ChatColors.Silver, ChatColors.DarkBlue
  };

  public static readonly char[] ALL_COLORS = {
    ChatColors.White, ChatColors.DarkRed, ChatColors.Green, ChatColors.Yellow,
    ChatColors.Olive, ChatColors.Lime, ChatColors.Red, ChatColors.LightPurple,
    ChatColors.Purple, ChatColors.Grey, ChatColors.Gold, ChatColors.Silver,
    ChatColors.Blue, ChatColors.DarkBlue, ChatColors.LightRed
  };

  public static bool IsValidColor(char color, bool allColors = false) {
    return allColors ?
      ALL_COLORS.Contains(color) :
      AVAILABLE_COLORS.Contains(color);
  }

  public static bool IsValidColor(string color, bool allColors = false) {
    return allColors ?
      ALL_COLORS.Contains(StringToColor(color)) :
      AVAILABLE_COLORS.Contains(StringToColor(color));
  }

  public static string PrettyColorName(char color) {
    return color switch {
      '\u0001' => "White",
      '\u0002' => "DarkRed",
      '\u0004' => "Green",
      '\t'     => "Yellow",
      '\u0005' => "Olive",
      '\u0006' => "Lime",
      '\a'     => "Red",
      '\u0003' => "LightPurple",
      '\u000e' => "Purple",
      '\b'     => "Grey",
      '\u0010' => "Gold",
      '\n'     => "Silver",
      '\v'     => "Blue",
      '\f'     => "DarkBlue",
      '\u000f' => "LightRed",
      _        => "White"
    };
  }

  public static char StringToColor(string color) {
    return color.ToLower() switch {
      "white"       => ChatColors.White,
      "darkred"     => ChatColors.DarkRed,
      "green"       => ChatColors.Green,
      "lightyellow" => ChatColors.LightYellow,
      "lightblue"   => ChatColors.LightBlue,
      "olive"       => ChatColors.Olive,
      "lime"        => ChatColors.Lime,
      "red"         => ChatColors.Red,
      "lightpurple" => ChatColors.LightPurple,
      "purple"      => ChatColors.Purple,
      "grey"        => ChatColors.Grey,
      "yellow"      => ChatColors.Yellow,
      "gold"        => ChatColors.Gold,
      "silver"      => ChatColors.Silver,
      "blue"        => ChatColors.Blue,
      "darkblue"    => ChatColors.DarkBlue,
      "bluegrey"    => ChatColors.BlueGrey,
      "magenta"     => ChatColors.Magenta,
      "lightred"    => ChatColors.LightRed,
      "orange"      => ChatColors.Orange,
      _             => ChatColors.White
    };
  }
}
