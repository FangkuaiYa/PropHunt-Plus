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
    class LogicPatch
    {
        // Penalize the impostor if there is no prop killed
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
        [HarmonyPrefix]
        public static void KillButtonClickPatch(KillButton __instance)
        {
            if (__instance.currentTarget == null && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && !PlayerControl.LocalPlayer.inVent)
            {
                Main.missedKills++;
                if (AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    PlayerPatch.NameState = "\n" + string.Format($"<color=#ff0000>{GetString(StringKey.RemainAttempt)}</color>", Main.maxMissedKills - Main.missedKills);
                }
                if (Main.missedKills >= Main.maxMissedKills)
                {
                    PlayerControl.LocalPlayer.RpcMurderPlayer(PlayerControl.LocalPlayer, true);
                    PlayerPatch.NameState = "";
                    Main.missedKills = 0;
                }
                GameObject closestProp = Utils.FindClosestConsole(PlayerControl.LocalPlayer.gameObject, 
                    GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)]);
                if (closestProp != null)
                {
                    GameObject.Destroy(closestProp.gameObject);
                }
            }
        }

        // Make the game start with AT LEAST one impostor (happens if there are >4 players)
        [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
        [HarmonyPrefix]
        public static bool ForceNotZeroImps(ref int __result)
        {
            int numImpostors = GameOptionsManager.Instance.currentGameOptions.NumImpostors;
            int num = 3;
            if (GameData.Instance.PlayerCount < GameOptionsData.MaxImpostors.Length)
            {
                num = GameOptionsData.MaxImpostors[GameData.Instance.PlayerCount];
                if (num <= 0)
                {
                    num = 1;
                }
            }
            __result = Mathf.Clamp(numImpostors, 1, num);
            return false;
        }

        // Make it so that seekers only win if they got ALL the props
        [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
        [HarmonyPrefix]
        public static bool CheckEndPatch()
        {
            if (Main.Instance.Debug.Value) return false;
            if (!Main.IsModLobby) return true;

            int crew = 0, impostors = 0, aliveImpostors = 0;

            foreach (var pi in GameData.Instance.AllPlayers)
            {
                if (pi.Disconnected) continue;
                if (pi.Role.IsImpostor) impostors++;
                if (!pi.IsDead)
                    if (pi.Role.IsImpostor)
                        aliveImpostors++;
                    else
                        crew++;
            }
            if (crew <= 0)
            {
                GameOverReason endReason;
                switch (TempData.LastDeathReason)
                {
                    case DeathReason.Exile:
                        endReason = GameOverReason.ImpostorByVote;
                        break;
                    case DeathReason.Kill:
                        endReason = GameOverReason.ImpostorByKill;
                        break;
                    default:
                        endReason = GameOverReason.ImpostorByVote;
                        break;
                }
                GameManager.Instance.RpcEndGame(endReason, false);
                return false;
            }
            else if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                ShipStatus.Instance.enabled = false;
                GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                return false;
            }
            if (aliveImpostors <= 0)
            {
                GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, false);
                return false;
            }
            return false;
        }
    }
}
