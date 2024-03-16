using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PropHunt
{
    [HarmonyPatch]
    class UserInterfacePatch
    {
        public static PingTracker GameStateShower;

        [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
        [HarmonyPostfix]
        public static void EmergencyButtonPatch(EmergencyMinigame __instance)
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            if (!Main.IsModLobby) return;
            __instance.StatusText.text = GetString(StringKey.MeetingDisabled);
            __instance.NumberText.text = "";
            __instance.OpenLid.gameObject.SetActive(false);
            __instance.ClosedLid.gameObject.SetActive(true);
            __instance.ButtonActive = false;
        }

        //[HarmonyPatch(typeof(GameStartManager),nameof(GameStartManager.Update))]
        //[HarmonyPostfix]
        //public static void ShowMsgForHandshake(GameStartManager __instance)
        //{
        //    if (!AmongUsClient.Instance.AmHost) return;
        //    string message = "";
        //    bool error = false;
        //    foreach(var pc in PlayerControl.AllPlayerControls)
        //    {
        //        if (Main.PlayerVersion.ContainsKey(pc))
        //        {
        //            if (Main.PlayerVersion[pc] != Main.ModInfoForHandshake)
        //            {
        //                message += $"<color=#FF0000>{string.Format(GetString(StringKey.HandshakeOVMod), pc.Data.PlayerName)}</color>\n";
        //                error = true;
        //            }
        //        }
        //        else
        //        {
        //            message += $"<color=#FF0000>{string.Format(GetString(StringKey.HandshakeNoMod), pc.Data.PlayerName)}</color>\n";
        //            error = true;
        //        }
        //    }

        //    __instance.GameStartText.text = message;
        //    if (error)
        //        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 2;
        //    else
        //        __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
            
        //}

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        [HarmonyPostfix]
        public static void PingTrackerPatch(PingTracker __instance)
        {
            StringBuilder ping = new();
            ping.Append("\n<color=");
            if (AmongUsClient.Instance.Ping < 100)
            {
                ping.Append("#00ff00>");
            }
            else if (AmongUsClient.Instance.Ping < 300)
            {
                ping.Append("#ffff00>");
            }
            else if (AmongUsClient.Instance.Ping > 300)
            {
                ping.Append("#ff0000>");
            }
            ping.Append(string.Format(GetString(StringKey.Ping), AmongUsClient.Instance.Ping)).Append("</color>\n<size=130%>Prop Hunt Plus</size> v1.0\n<size=65%>By ugackMiner53&Jiege");
            __instance.text.text = ping.ToString();
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
        [HarmonyPostfix]
        public static void ShowGameInfoShower()
        {
            var shower = GameObject.FindObjectOfType<PingTracker>();
            GameStateShower = GameObject.Instantiate(shower, shower.transform.parent);
        }

        // Reset variables on game start
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
        [HarmonyPostfix]
        public static void IntroCuscenePatch()
        {
            Main.missedKills = 0;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                foreach (SpriteRenderer rend in PlayerControl.LocalPlayer.GetComponentsInChildren<SpriteRenderer>())
                {
                    rend.sortingOrder += 5;
                }
            }
            HudManager hud = DestroyableSingleton<HudManager>.Instance;
            hud.ImpostorVentButton.gameObject.SetActiveRecursively(false);
            hud.SabotageButton.gameObject.SetActiveRecursively(false);
            hud.ReportButton.gameObject.SetActiveRecursively(false);
            hud.Chat.SetVisible(true);
            PlayerPatch.NameState = "";
            Main.Logger.LogInfo(Main.hidingTime + " -- " + Main.maxMissedKills);
        }
    }
}
