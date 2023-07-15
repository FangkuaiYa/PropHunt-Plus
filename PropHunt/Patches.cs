// Patches for PropHuntPlugin
// Copyright (C) 2022  ugackMiner
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PropHunt
{
    public class Patches
    {
        public static string NameSync = "";
        public static string NameState = "";
        public static bool Sync = false;
        public static bool IsCommand = false;

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
            ping.Append(string.Format(GetString(StringKey.Ping), AmongUsClient.Instance.Ping)).Append("</color>\n<size=130%>Prop Hunt Reactivited</size> v1.0\n<size=65%>By ugackMiner53&Jiege");
            __instance.text.text = ping.ToString();
        }

        // Main input loop for custom keys
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        public static void PlayerInputControlPatch(KeyboardJoystick __instance)
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            PlayerControl player = PlayerControl.LocalPlayer;
            if (Input.GetKeyDown(KeyCode.R) && !player.Data.Role.IsImpostor)
            {
                PropHuntPlugin.Logger.LogInfo("Key pressed");
                GameObject closestConsole = PropHuntPlugin.Utility.FindClosestConsole(player.gameObject, 3);
                if (closestConsole != null)
                {
                    player.Visible = false;
                    player.transform.localScale = closestConsole.transform.lossyScale;
                    player.GetComponent<SpriteRenderer>().sprite = closestConsole.GetComponent<SpriteRenderer>().sprite;
                    int t = 0;
                    foreach(var task in ShipStatus.Instance.AllConsoles)
                    {
                        t++;
                        if(task == closestConsole.GetComponent<Console>())
                        {
                            PropHuntPlugin.Logger.LogInfo("Task " + task.ToString() + " being sent out");
                            var writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RPC.PropSync, Hazel.SendOption.Reliable);
                            writer.Write(player.PlayerId);
                            writer.Write(t);
                            PropHuntPlugin.RPCHandler.RPCPropSync(player, t);
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
            if (__instance == PlayerControl.LocalPlayer) Sync = false;
        }

        // Make prop impostor on death

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        [HarmonyPostfix]
        public static void MakePropImpostorPatch(PlayerControl __instance)
        {
            if (!__instance.Data.IsDead) return;
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
                GameObject.Destroy(__instance.GetComponent<SpriteRenderer>());
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
            if (PropHuntPlugin.Instance.Debug.Value) return false;
            if (!GameData.Instance || TutorialManager.InstanceExists) return false;

            int crew = 0, impostors = 0, aliveImpostors = 0;
            
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
                    foreach (var pc in PlayerControl.AllPlayerControls) pc.Revive(); // =ShipStatus.ReviveEveryone()
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
                GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, false);
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
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            __instance.SetEnabled();
        }

        // Disable buttons
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        [HarmonyPostfix]
        public static void DisableButtonsPatch(HudManager __instance)
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            __instance.SabotageButton.gameObject.SetActive(false);
            __instance.ReportButton.SetActive(false);
            __instance.ImpostorVentButton.gameObject.SetActive(false);
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
                    NameState = "\n" + string.Format($"<color=#ff0000>{GetString(StringKey.RemainAttempt)}</color>", PropHuntPlugin.maxMissedKills - PropHuntPlugin.missedKills);
                }
                if (PropHuntPlugin.missedKills >= PropHuntPlugin.maxMissedKills)
                {
                    PlayerControl.LocalPlayer.RpcMurderPlayer(PlayerControl.LocalPlayer);
                    NameState = "";
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
            __instance.MinPlayers = PropHuntPlugin.Instance.Debug.Value ? 1 : 2;
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
            return AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
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
            NameState = "";
            PropHuntPlugin.Logger.LogInfo(PropHuntPlugin.hidingTime + " -- " + PropHuntPlugin.maxMissedKills);
        }

        // Change the role text
        public static void SetRoleTexts(IntroCutscene __instance)
        {
            
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    __instance.RoleText.text = GetString(StringKey.Seeker);
                    __instance.RoleBlurbText.text = string.Format(GetString(StringKey.SeekerDescription),PropHuntPlugin.hidingTime);
                }
                else
                {
                    __instance.RoleText.text = GetString(StringKey.Prop);
                    __instance.RoleBlurbText.text = GetString(StringKey.PropDescription);
                }
            
        }
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
        [HarmonyPrefix]
        public static bool RoleTextPatch(IntroCutscene __instance)
        {
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f, new Action<float>((p) => { SetRoleTexts(__instance); })));
            return true;
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
        [HarmonyPostfix]
        public static async void HidingTimePatch()
        {
            PropHuntPlugin.Logger.LogInfo("Game Started!");
            var lp = PlayerControl.LocalPlayer;
            
            int sec = PropHuntPlugin.hidingTime;
            lp.moveable = !lp.Data.Role.IsImpostor;
            
            for (int s = sec; s >= 0; s--)
            {
                NameState = "\n" + string.Format(GetString(StringKey.HidingTimeLeft), s);
                PropHuntPlugin.Logger.LogInfo(s);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            NameState = "";
            lp.moveable = true;
        }

        // Player dead check
        [HarmonyPatch(typeof(PlayerControl),nameof(PlayerControl.CheckMurder))]
        [HarmonyPostfix]
        public static void PlayerDeadPatch(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            var killer = __instance;
            if (killer == null || target == null) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (killer == target && target.Data.Role.IsImpostor)
            {
                PropHuntPlugin.Logger.LogInfo("Imp ded");
                DestroyableSingleton<ChatController>.Instance.AddChat(PlayerControl.LocalPlayer, GetString(StringKey.SeekerDead));
            }
            else
            {
                PropHuntPlugin.Logger.LogInfo("Crew ded/infcted");
                DestroyableSingleton<ChatController>.Instance.AddChat(PlayerControl.LocalPlayer, GetString(PropHuntPlugin.infection ? StringKey.PropInfected: StringKey.PropDead));
            }
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        [HarmonyPostfix]
        public static void PlayerNamePatch()
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) NameState = "";
            var lp = PlayerControl.LocalPlayer;
            if (!Sync)
            {
                NameSync = lp.Data.PlayerName;
                Sync = true;
            }
            lp.SetName(NameSync + NameState);
        }

        // Commands
        [HarmonyPatch(typeof(ChatController),nameof(ChatController.AddChat))]
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
                    sourcePlayer.RpcMurderPlayer(sourcePlayer);
                    break;
                case "/help":
                    __instance.AddChat(sourcePlayer, GetString(StringKey.CmdHelp));
                    break;
                // For testing
                case "/m1":
                    var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == Convert.ToInt32(cmd[1])).FirstOrDefault();
                    sourcePlayer.RpcMurderPlayer(player);
                    break;
                case "/m2":
                    var p = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == Convert.ToInt32(cmd[1])).FirstOrDefault();
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.MurderPlayer, Hazel.SendOption.Reliable);
                    writer.WritePacked(p.NetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "/pid":
                    string a = "";
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        a += pc.Data.PlayerName + " " + pc.PlayerId + "\r\n";
                    }
                    PropHuntPlugin.Logger.LogMessage(a);
                    break;
                case "/cid":
                    string i = "";
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        i += pc.Data.PlayerName + " " + pc.NetId + "\n";
                    }
                    PropHuntPlugin.Logger.LogMessage(i);
                    break;
                case "/kick":
                    AmongUsClient.Instance.KickPlayer(Convert.ToInt32(cmd[1]), Convert.ToBoolean(cmd[2]));
                    break;
                case "/role":
                    if (cmd[1] == "0")
                    {
                        DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                    }
                    else
                    {
                        DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
                    }
                    break;
            }
        }

        [HarmonyPatch(typeof(EmergencyMinigame),nameof(EmergencyMinigame.Update))]
        [HarmonyPostfix]
        public static void EmergencyButtonPatch(EmergencyMinigame __instance)
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            __instance.StatusText.text = GetString(StringKey.MeetingDisabled);
            __instance.NumberText.text = "";
            __instance.OpenLid.gameObject.SetActive(false);
            __instance.ClosedLid.gameObject.SetActive(true);
            __instance.ButtonActive = false;
        }

        [HarmonyPatch(typeof(ChatBubble),nameof(ChatBubble.SetText))]
        [HarmonyPostfix]
        public static void NameFix(ChatBubble __instance)
        {
            int line = __instance.NameText.text.Split("\n").Length;
            string el = "";
            if (line > 1)
            {
                for (int i = 0; i < line - 1; i++)
                {
                    el += "\n";
                }
                el += __instance.TextArea.text;
                __instance.TextArea.text = el;
            }
        }
    }
}