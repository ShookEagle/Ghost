using CounterStrikeSharp.API.Modules.Commands;
using Ghost.utils;
using Microsoft.Extensions.Localization;

namespace Ghost.extensions;

public static class CommandExtensions {
  public static void ReplyLocalized(this CommandInfo cmd,
    IStringLocalizer localizer, string local, params object[] args) {
    string message = localizer[local, args];
    message = message.Replace("%prefix%", localizer["prefix"]);
    message = StringUtils.replaceChatColors(message);
    cmd.ReplyToCommand(message);
  }

}