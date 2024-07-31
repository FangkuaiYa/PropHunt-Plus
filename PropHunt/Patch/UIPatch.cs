using HarmonyLib;
using PropHunt.Module;
using System.Text;
using TMPro;
using UnityEngine;

namespace PropHunt
{
    [HarmonyPatch]
    class UIPatch
    {
        public static PingTracker ModVersionShower { get; set; }
        public static TextMeshPro AbilityInfoShower { get; set; }

        [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
        [HarmonyPostfix]
        public static void EmergencyButtonPatch(EmergencyMinigame __instance)
        {
            if (ModData.IsTutorial) return;
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
            StringBuilder pingText = new();
            pingText.Append("\n<color=");
            if (AmongUsClient.Instance.Ping < 100)
            {
                pingText.Append("#00ff00>");
            }
            else if (AmongUsClient.Instance.Ping < 300)
            {
                pingText.Append("#ffff00>");
            }
            else if (AmongUsClient.Instance.Ping > 300)
            {
                pingText.Append("#ff0000>");
            }
            pingText.Append(string.Format(GetString(StringKey.Ping), AmongUsClient.Instance.Ping)).Append("</color>");
            __instance.text.alignment = TextAlignmentOptions.Center;
            __instance.text.text = pingText.ToString();
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
        [HarmonyPostfix]
        public static void SetUpInfoShower()
        {
            if (ModData.IsTutorial) return;

            // Version Info Shower
            var pingPrefab = Object.FindObjectOfType<PingTracker>();
            ModVersionShower = Object.Instantiate(pingPrefab, HudManager.Instance.transform);
            ModVersionShower.transform.localPosition = new(1.35f, 2.8f, 0);
            ModVersionShower.DestroyComponent<AspectPosition>();
            ModVersionShower.DestroyComponent<PingTracker>();

            var tmp = ModVersionShower.GetComponent<TextMeshPro>();
            tmp.text = "<size=130%>Prop Hunt Plus</size> v1.0.1\n<size=65%>By ugackMiner53\nReactived by JieGeLovesDengDuaLang</size>";
            tmp.alignment = TextAlignmentOptions.TopRight;

            // Ability Info Shower
            var abilityPrefab = HudManager.Instance.AbilityButton.buttonLabelText;
            AbilityInfoShower = Object.Instantiate(abilityPrefab, HudManager.Instance.transform);
            AbilityInfoShower.transform.localPosition = new(0, -1.5f, 0);
            AbilityInfoShower.text = "";
        }

        // Reset variables on game start
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
        [HarmonyPostfix]
        public static void IntroCuscenePatch()
        {
            ModData.CurrentMiskills = 0;

            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                foreach (SpriteRenderer rend in PlayerControl.LocalPlayer.GetComponentsInChildren<SpriteRenderer>())
                    rend.sortingOrder += 5;
                
            HudManager hud = DestroyableSingleton<HudManager>.Instance;
            hud.ImpostorVentButton.gameObject.SetActiveRecursively(false);
            hud.SabotageButton.gameObject.SetActiveRecursively(false);
            hud.ReportButton.gameObject.SetActiveRecursively(false);
            hud.Chat.SetVisible(true);
            Main.Logger.LogInfo(ModData.HidingTime + " -- " + ModData.MaxMiskill);
        }
    }
}
