using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PropHunt.Patch
{
    [HarmonyPatch]
    class CommandPatch
    {
        public static bool IsCommand = false;

        // Commands
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        [HarmonyPostfix]
        public static void ChatCommandsPatch(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] string chatText)
        {
            IsCommand = false;
            if (!chatText.StartsWith('/')) return;
            if (sourcePlayer != PlayerControl.LocalPlayer) return;
            string[] cmd = chatText.Split(" ");
            switch (cmd[0].ToLower())
            {
                case "/km":
                    sourcePlayer.RpcMurderPlayer(sourcePlayer, true);
                    break;
                case "/help":
                    __instance.AddChat(sourcePlayer, "[SYSMSG]" + GetString(StringKey.CmdHelp));
                    break;
                // For testing
                case "/m1":
                    var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == Convert.ToInt32(cmd[1])).FirstOrDefault();
                    sourcePlayer.RpcMurderPlayer(player, true);
                    break;
                case "/pid":
                    string a = "";
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        a += pc.Data.PlayerName + " " + pc.PlayerId + "\r\n";
                    }
                    Main.Logger.LogMessage(a);
                    break;
                case "/cid":
                    string i = "";
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        i += pc.Data.PlayerName + " " + pc.NetId + "\n";
                    }
                    Main.Logger.LogMessage(i);
                    break;
                case "/kick":
                    AmongUsClient.Instance.KickPlayer(Convert.ToInt32(cmd[1]), Convert.ToBoolean(cmd[2]));
                    break;
                case "/role":
                    if (cmd[1] == "0")
                        DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                    else
                        DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
                    break;
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
        [HarmonyPostfix]
        public static void ChatBubbbleFix(ChatBubble __instance)
        {
            int line = __instance.NameText.text.Split("\n").Length;
            string el = "";

            if (line > 1)
            {
                for (int i = 0; i < line - 1; i++) el += "\n";
                el += __instance.TextArea.text;
                __instance.TextArea.text = el;
            }
        }

        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
        [HarmonyPrefix]
        public static void SystemMessagePatch(ChatBubble __instance, ref string chatText)
        {
            if (chatText.StartsWith("[SYSMSG]"))
            {
                chatText.Replace("[SYSMSG]", "");
                __instance.SetName("SYSTEM", false, false, Color.green);
                __instance.SetLeft();
                __instance.SetCosmetics(new(0));
            }
        }
    }
}
