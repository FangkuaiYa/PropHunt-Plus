using AmongUs.GameOptions;
using HarmonyLib;
using PropHunt.Module;
using System;
using System.Collections;
using System.Linq;
using Object = UnityEngine.Object;
using UnityEngine;
using System.Reflection;

namespace PropHunt
{
    [HarmonyPatch]
    class PlayerPatch
    {
        public static string NameSync = "";
        public static string NameState = "";
        public static bool Sync = false;

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
            lp.cosmetics.nameText.text = NameSync + NameState;
        }

        // Change the role text
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
        [HarmonyPrefix]
        public static bool RoleTextPatch(IntroCutscene __instance)
        {
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f, new Action<float>((p) => { SetRoleTexts(__instance); })));
            return true;
        }
        static void SetRoleTexts(IntroCutscene __instance)
        {
            if (!Main.IsModLobby) return;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                __instance.RoleText.text = GetString(StringKey.Seeker);
                __instance.RoleBlurbText.text = string.Format(GetString(StringKey.SeekerDescription), Main.hidingTime);
            }
            else
            {
                __instance.RoleText.text = GetString(StringKey.Prop);
                __instance.RoleBlurbText.text = GetString(StringKey.PropDescription);
            }

        }

        // Main input loop for custom keys
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        public static void PlayerInputControlPatch(KeyboardJoystick __instance)
        {
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) return;
            PlayerControl player = PlayerControl.LocalPlayer;
            var impPlayer = PlayerControl.AllPlayerControls.ToArray().Where(p => p && p && !p.Data.Disconnected && p.Data.Role.IsImpostor).FirstOrDefault();
            // For testing only
            if (Input.GetKeyDown(KeyCode.F2)) GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByVote, false);
            if (Input.GetKeyDown(KeyCode.F1)) GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, false);
            if (Input.GetKeyDown(KeyCode.F3)) GameManager.Instance.RpcEndGame(GameOverReason.HideAndSeek_ByKills, false);
            if (Input.GetKeyDown(KeyCode.F4)) GameManager.Instance.RpcEndGame(GameOverReason.HideAndSeek_ByTimer, false);
            if (Input.GetKeyDown(KeyCode.F9)) DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
            if (Input.GetKeyDown(KeyCode.F10)) DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
            if (Input.GetKeyDown(KeyCode.F11)) impPlayer.RpcMurderPlayer(impPlayer, true);
            if (Input.GetKeyDown(KeyCode.F12)) player.RpcMurderPlayer(player, true);

            if (Input.GetKeyDown(KeyCode.R) && !player.Data.Role.IsImpostor)
            {
                Main.Logger.LogInfo("Key pressed");
                GameObject closestConsole = Utils.FindClosestConsole(player.gameObject, GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)]);
                if (closestConsole != null)
                {
                    player.Visible = false;
                    player.transform.localScale = closestConsole.transform.lossyScale;
                    player.GetComponent<SpriteRenderer>().sprite = closestConsole.GetComponent<SpriteRenderer>().sprite;
                    int t = 0;
                    foreach (var task in ShipStatus.Instance.AllConsoles)
                    {
                        t++;
                        if (task == closestConsole.GetComponent<Console>())
                        {
                            Main.Logger.LogInfo("Task " + task.ToString() + " being sent out");
                            var writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RPC.PropSync, Hazel.SendOption.Reliable);
                            writer.Write(player.PlayerId);
                            writer.Write(t);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            Main.RpcHandler.RpcPropSync(player, t);
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
            if (!Main.IsModLobby) return;
            __instance.gameObject.AddComponent<SpriteRenderer>();
            __instance.GetComponent<CircleCollider2D>().radius = 0.00001f;
            if (__instance == PlayerControl.LocalPlayer) PlayerPatch.Sync = false;
        }

        //[HarmonyPatch(typeof(AmongUsClient),nameof(AmongUsClient.OnGameJoined))]
        //[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
        //[HarmonyPostfix]
        //public static void HandshakePatch()
        //{
        //    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RPC.Handshake, Hazel.SendOption.Reliable);
        //    writer.Write(PlayerControl.LocalPlayer.PlayerId);
        //    writer.Write(Main.ModInfoForHandshake);
        //    AmongUsClient.Instance.FinishRpcImmediately(writer);
        //    Main.RpcHandler.RpcHandshake(PlayerControl.LocalPlayer, Main.ModInfoForHandshake);
        //}

        // Make prop impostor on death
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        [HarmonyPostfix]
        public static void MakePropImpostorPatch(PlayerControl __instance)
        {
            if (!Main.IsModLobby) return;
            if (!__instance.Data.IsDead) return;
            if (!__instance.Data.Role.IsImpostor && Main.infection)
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

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
        [HarmonyPostfix]
        public static void HidingTimePatch(GameManager __instance)
        {
            Main.Logger.LogInfo("Game Started!");
            var lp = PlayerControl.LocalPlayer;

            lp.moveable = !lp.Data.Role.IsImpostor;

            if (!ShipStatus.Instance.IsSubmerged())
                __instance.StartCoroutine((Il2CppSystem.Collections.IEnumerator)CoStartTimer(Main.hidingTime));

            NameState = "";
            lp.moveable = true;
        }

        private static int Timer;
        private static IEnumerator CoStartTimer(int time)
        {
            Timer = time;
            for (int i = 0; i < time; i++)
            {
                NameState = "\n" + string.Format(GetString(StringKey.HidingTimeLeft), Timer);
                Main.Logger.LogInfo(Timer);
                yield return new WaitForSeconds(1);
                Timer = time - i;
            }
        }

        // Player dead check
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
        [HarmonyPostfix]
        public static void PlayerDeadPatch(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            var killer = __instance;
            if (killer == null || target == null) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (killer == target && target.Data.Role.IsImpostor)
            {
                Main.Logger.LogInfo("Imp ded");
                HudManager.Instance.ShowCustomTaskComplete(GetString(StringKey.SeekerDead));
            }
            else
            {
                Main.Logger.LogInfo("Crew ded/infcted");
                HudManager.Instance.ShowCustomTaskComplete(GetString(Main.infection ? StringKey.PropInfected : StringKey.PropDead));
            }
        }

        // Fix submerged timer
        [HarmonyPatch]
        public class SubmergedHidingTimerFix
        {
            public static MethodBase TargetMethod() => SubmergedCompatibility.SubmergedSpawnBehaviour.GetMethod("OnDestroy");
            public static void Postfix() 
            {
                if (!ShipStatus.Instance.IsSubmerged()) return;
                GameManager.Instance.StartCoroutine((Il2CppSystem.Collections.IEnumerator)CoStartTimer(Main.hidingTime)); 
            }

        }
    }
}
