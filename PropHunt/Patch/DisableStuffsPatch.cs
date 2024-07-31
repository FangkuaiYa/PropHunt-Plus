// Patches for PropHuntPlugin
// Copyright (C) 2022  ugackMiner
using HarmonyLib;
using PropHunt.Module;

namespace PropHunt
{
    [HarmonyPatch]
    public class DisableStuffsPatch
    {
        // Make it so that the kill button doesn't light up when near a player
        [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
        [HarmonyPostfix]
        public static void KillButtonHighlightPatch(ActionButton __instance)
        {
            if (ModData.IsTutorial) return;
            __instance.SetEnabled();
        }

        // Disable buttons
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void DisableButtonsPatch(HudManager __instance)
        {
            if (ModData.IsTutorial) return;
            if (!Main.IsModLobby) return;
            __instance.SabotageButton.gameObject.SetActive(false);
            __instance.ReportButton.SetActive(false);
            __instance.ImpostorVentButton.gameObject.SetActive(false);
        }

        // Change the minimum amount of players to start a game
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        [HarmonyPostfix]
        public static void MinPlayerPatch(GameStartManager __instance)
        {
            __instance.MinPlayers = Main.Instance.Debug.Value ? 1 : 2;
        }
        
        // Disable a lot of stuffs
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcEnterVent))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
        [HarmonyPrefix]
        public static bool DisableFunctions() => ModData.IsTutorial;

        [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
        [HarmonyPrefix]
        public static bool DisableVent(ref bool canUse, ref bool couldUse) // Stop player from entering vent by pressing hotkey
        {
            canUse = couldUse = false;
            return false;
        }
    }
}