// Patches for PropHuntPlugin
// Copyright (C) 2022  ugackMiner
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PropHunt
{
    public class Patches
    {
        public static PingTracker GameStatShower;

        [HarmonyPatch(typeof(ModManager),nameof(ModManager.LateUpdate))]
        [HarmonyPostfix]
        public static void ModStampPatch(ModManager __instance)
        {
            __instance.ShowModStamp();
        }

        [HarmonyPatch(typeof(PingTracker),nameof(PingTracker.Update))]
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
            ping.Append(string.Format("{0}: {1}{2}", TranslationController.Instance.currentLanguage.languageID == SupportedLangs.SChinese ? "—”≥Ÿ" : "Ping" ,AmongUsClient.Instance.Ping, TranslationController.Instance.currentLanguage.languageID == SupportedLangs.SChinese ? "∫¡√Î" : "ms")).Append("</color>\n<size=130%>Prop Hunt Reactivited</size> v1.0");
            __instance.text.text = ping.ToString();
        }
        // Main input loop for custom keys
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        public static void PlayerInputControlPatch(KeyboardJoystick __instance)
        {
            PlayerControl player = PlayerControl.LocalPlayer;
            if (Input.GetKeyDown(KeyCode.R) && !player.Data.Role.IsImpostor)
            {
                PropHuntPlugin.Logger.LogInfo("Key pressed");
                GameObject closestConsole = PropHuntPlugin.Utility.FindClosestConsole(player.gameObject, 3);
                if (closestConsole != null)
                {
                    player.transform.localScale = closestConsole.transform.lossyScale;
                    player.GetComponent<SpriteRenderer>().sprite = closestConsole.GetComponent<SpriteRenderer>().sprite;
                    for (int i = 0; i < ShipStatus.Instance.AllConsoles.Length; i++)
                    {
                        if (ShipStatus.Instance.AllConsoles[i] == closestConsole.GetComponent<Console>())
                        {
                            PropHuntPlugin.Logger.LogInfo("Task of index " + i + " being sent out");
                            var writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RPC.PropSync, Hazel.SendOption.Reliable);
                            writer.Write(player.PlayerId);
                            writer.Write(i + "");
                            PropHuntPlugin.RPCHandler.RPCPropSync(player, i + "");
                        }
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                player.Collider.enabled = false;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                player.Collider.enabled = true;
            }
        }

        // Runs when the player is created
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
        [HarmonyPostfix]
        public static void PlayerControlStartPatch(PlayerControl __instance)
        {
            __instance.gameObject.AddComponent<SpriteRenderer>();
            __instance.GetComponent<CircleCollider2D>().radius = 0.00001f;
        }


        // Runs periodically, resets animation data for players
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPostfix]
        public static void PlayerPhysicsAnimationPatch(PlayerPhysics __instance)
        {
            if (!AmongUsClient.Instance.IsGameStarted)
                return;
            if (__instance.GetComponent<SpriteRenderer>().sprite != null && !__instance.myPlayer.Data.Role.IsImpostor)
            {
                __instance.myPlayer.Visible = false;
            }
            if (__instance.myPlayer.Data.IsDead)
            {
                __instance.myPlayer.Visible = true;
                GameObject.Destroy(__instance.GetComponent<SpriteRenderer>());
            }
        }

        // Make prop impostor on death
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        [HarmonyPostfix]
        public static void MakePropImpostorPatch(PlayerControl __instance)
        {
            if (!__instance.Data.Role.IsImpostor && PropHuntPlugin.infection)
            {
                foreach (GameData.TaskInfo task in __instance.Data.Tasks)
                {
                    task.Complete = true;
                }
                GameData.Instance.RecomputeTaskCounts();
                __instance.Revive();
                __instance.Data.Role.TeamType = RoleTeamTypes.Impostor;
                DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, RoleTypes.Impostor);
                __instance.transform.localScale = new Vector3(0.7f, 0.7f, 1);
                __instance.Visible = true;
                foreach (SpriteRenderer rend in __instance.GetComponentsInChildren<SpriteRenderer>())
                {
                    rend.sortingOrder += 5;
                }
            }
        }

        // Make it so that seekers only win if they got ALL the props
        [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
        [HarmonyPrefix]
        public static bool CheckEndPatch(LogicGameFlowNormal __instance)
        {
            if (true) return false;
            if (!GameData.Instance || TutorialManager.InstanceExists)
            {
                return false;
            }
            int crew = 0;
            int aliveImpostors = 0;
            int impostors = 0;
            foreach(var pi in GameData.Instance.AllPlayers)
            {
                if (pi.Disconnected) continue;
                if (pi.Role.IsImpostor) impostors++;
                if (!pi.IsDead)
                {
                    if (pi.Role.IsImpostor)
                    {
                        aliveImpostors++;
                    }
                    else
                    {
                        crew++;
                    }   
                }
            }
            if (crew <= 0)
            {
                if (DestroyableSingleton<TutorialManager>.InstanceExists)
                {
                    DestroyableSingleton<HudManager>.Instance.ShowPopUp(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameOverImpostorKills, System.Array.Empty<Il2CppSystem.Object>()));
                    foreach (var pc in PlayerControl.AllPlayerControls) pc.Revive(); // =ShipStatus.ReviveEveryone
                    return false;
                }
                if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.Normal)
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
            }
            else if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.Normal && GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
                {
                    ShipStatus.Instance.enabled = false;
                    GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                    return false;
                }
            }
            else
            {
                bool allComplete = true;
                foreach (PlayerTask t in PlayerControl.LocalPlayer.myTasks)
                {
                    if (!t.IsComplete)
                    {
                        allComplete = false;
                    }
                }
                if (allComplete)
                {
                    DestroyableSingleton<HudManager>.Instance.ShowPopUp(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameOverTaskWin, System.Array.Empty<Il2CppSystem.Object>()));
                    ShipStatus.Instance.Begin();
                }

            }
            if (aliveImpostors <= 0)
            {
                GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, !DataManager.Player.Ads.HasPurchasedAdRemoval);
                return false;
            }
            return false;
        }

        // Make it so that the kill button doesn't light up when near a player
        [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
        [HarmonyPostfix]
        public static void KillButtonHighlightPatch(ActionButton __instance)
        {
            __instance.SetEnabled();
        }


        // Penalize the impostor if there is no prop killed
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
        [HarmonyPrefix]
        public static void KillButtonClickPatch(KillButton __instance)
        {
            if (__instance.currentTarget == null && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && !PlayerControl.LocalPlayer.inVent)
            {
                PropHuntPlugin.missedKills++;
                if (AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    TMPro.TextMeshPro pingText = GameObject.FindObjectOfType<PingTracker>().text;
                    pingText.text = string.Format("{0}: {1}", TranslationController.Instance.currentLanguage.languageID == SupportedLangs.SChinese ? " £”‡¥Œ ˝" : "Remaining Attempts", PropHuntPlugin.maxMissedKills - PropHuntPlugin.missedKills);
                    pingText.color = Color.red;
                }
                if (PropHuntPlugin.missedKills >= PropHuntPlugin.maxMissedKills)
                {
                    PlayerControl.LocalPlayer.CmdCheckMurder(PlayerControl.LocalPlayer);
                    PropHuntPlugin.missedKills = 0;
                }
                GameObject closestProp = PropHuntPlugin.Utility.FindClosestConsole(PlayerControl.LocalPlayer.gameObject, GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)]);
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



        // Change the minimum amount of players to start a game
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        [HarmonyPostfix]
        public static void MinPlayerPatch(GameStartManager __instance)
        {
            __instance.MinPlayers = 1;
        }

        // Disable a lot of stuff
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
        [HarmonyPrefix]
        public static bool DisableFunctions()
        {
            return false;
        }


        // Reset variables on game start
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
        [HarmonyPostfix]
        public static void IntroCuscenePatch()
        {
            PropHuntPlugin.missedKills = 0;
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
            PropHuntPlugin.Logger.LogInfo(PropHuntPlugin.hidingTime + " -- " + PropHuntPlugin.maxMissedKills);
        }

        // Change the role text
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
        [HarmonyPostfix]
        public static void IntroCutsceneRolePatch(IntroCutscene __instance)
        {
            
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    __instance.RoleText.text = "Seeker";
                    __instance.RoleBlurbText.text = "Find and kill the props\nYour game will be unfrozen after " + PropHuntPlugin.hidingTime + " seconds";
                }
                else
                {
                    __instance.RoleText.text = "Prop";
                    __instance.RoleBlurbText.text = "Turn into props to hide from the seekers";
                }
            
        }

        [HarmonyPatch(typeof(GameStartManager),nameof(GameStartManager.FinallyBegin))]
        [HarmonyPostfix]
        public static async void HidingTimePatch()
        {
            PropHuntPlugin.Logger.LogInfo("Game Started!");
            var lp = PlayerControl.LocalPlayer;
            if (!lp.Data.Role.IsImpostor) return;
            lp.moveable = false;
            await Task.Delay(TimeSpan.FromSeconds(PropHuntPlugin.hidingTime));
            lp.moveable = true;
        }
        [HarmonyPatch(typeof(TaskPanelBehaviour),nameof(TaskPanelBehaviour.Update))]
        [HarmonyPostfix]
        public static void GameStatShowerPatch(TaskPanelBehaviour __instance)
        {
            string text = __instance.taskText.text;
            if (text == "None") return;
            int imp = int.MinValue, crew = int.MinValue;
            StringBuilder sb = new();
            foreach (var pc in PlayerControl.AllPlayerControls) if (pc != null && !pc.Data.Disconnected && pc.Data.Role.IsImpostor) imp++; else crew++;
            sb.Append(string.Format("Impostors Count: {0}\nCrewmates Count: {1}", imp, crew)).Append(text);
            __instance.taskText.text = sb.ToString();
        }
    }
}
