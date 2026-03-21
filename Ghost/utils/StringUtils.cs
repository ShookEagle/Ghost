using CounterStrikeSharp.API.Modules.Utils;

namespace Ghost.utils;

internal class StringUtils {
  private static readonly List<string> stringsToRemove = [];

  static StringUtils() {
    ChatColorUtils.ALL_COLORS.ToList()
     .ForEach(c => stringsToRemove.Add(c.ToString()));
    typeof(ChatColors).GetFields()
     .Select(f => f.Name)
     .ToList()
     .ForEach(c => stringsToRemove.Add($"{{{c}}}"));
  }

  internal static string replaceChatColors(string message) {
    if (!message.Contains('{')) return message;
    var modifiedValue = message;
    foreach (var field in typeof(ChatColors).GetFields()) {
      var pattern = $"{{{field.Name}}}";
      if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
        modifiedValue = modifiedValue.Replace(pattern,
          field.GetValue(null)!.ToString(),
          StringComparison.OrdinalIgnoreCase);
    }

    return modifiedValue;

  }

  internal static string removeStrings(string message,
    List<string> stringsToRemove) {
    var modifiedValue = message;
    foreach (var s in stringsToRemove)
      modifiedValue = modifiedValue.Replace(s, string.Empty,
        StringComparison.OrdinalIgnoreCase);

    return modifiedValue;
  }

  public static string StripChatColors(string message) {
    return removeStrings(message, stringsToRemove);
  }
}
