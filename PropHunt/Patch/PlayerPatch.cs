using AmongUs.GameOptions;
using HarmonyLib;
using PropHunt.Module;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Reflection;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace PropHunt
{
    [HarmonyPatch]
    class PlayerPatch
    {
        // Change the role text
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
        [HarmonyPrefix]
        public static bool RoleTextPatch(IntroCutscene __instance)
        {
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f, new Action<float>((p) => { SetRoleTexts(__instance); })));
            return true;
        }
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowTeam))]
        [HarmonyPostfix]
        public static void RoleTeamPatch(IntroCutscene __instance)
        {
            if (!Main.IsModLobby) return;

            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                __instance.TeamTitle.text = GetString(StringKey.Seeker);
            else
                __instance.TeamTitle.text = GetString(StringKey.Prop);
            
        }

        static void SetRoleTexts(IntroCutscene __instance)
        {
            if (!Main.IsModLobby) return;

            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                __instance.RoleText.text = GetString(StringKey.Seeker);
                __instance.RoleBlurbText.text = string.Format(GetString(StringKey.SeekerDescription), ModData.HidingTime);
            }
            else
            {
                __instance.RoleText.text = GetString(StringKey.Prop);
                __instance.RoleBlurbText.text = GetString(StringKey.PropDescription);
            }
        }

        [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        [HarmonyPostfix]
        public static void RoleAssignPatch(RoleManager __instance)
        {
            foreach (var p in PlayerControl.AllPlayerControls)
                __instance.SetRole(p, p.Data.Role.IsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate);
        }

        // Main input loop for custom keys
        [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
        [HarmonyPostfix]
        public static void PlayerInputControlPatch(KeyboardJoystick __instance)
        {
            PlayerControl player = PlayerControl.LocalPlayer;
            var impPlayer = PlayerControl.AllPlayerControls.ToArray().Where(p => p && p && !p.Data.Disconnected && p.Data.Role.IsImpostor).FirstOrDefault();
            // For testing only
            if (Input.GetKeyDown(KeyCode.F2)) GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByVote, false);
            if (Input.GetKeyDown(KeyCode.F1)) GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, false);
            if (Input.GetKeyDown(KeyCode.F11)) impPlayer.RpcMurderPlayer(impPlayer, true);
            if (Input.GetKeyDown(KeyCode.F12)) player.RpcMurderPlayer(player, true);

            if (Input.GetKeyDown(KeyCode.R) && !player.Data.Role.IsImpostor && !HudManager.Instance.Chat.IsOpenOrOpening)
            {
                Main.Logger.LogInfo("Key pressed");
                GameObject closestConsole = Utils.FindClosestConsole(player.gameObject, GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)]);
                if (closestConsole)
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
                            var writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRpc.PropSync, Hazel.SendOption.Reliable);
                            writer.Write(player.PlayerId);
                            writer.Write(t);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            Main.RpcHandler.PropSync(player, t);
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
        }

        //[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
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
            if (!__instance.Data.Role.IsImpostor && ModData.Infection)
            {
                foreach (var task in __instance.Data.Tasks)
                    task.Complete = true;
                
                GameData.Instance.RecomputeTaskCounts();
                __instance.Revive();
                Object.FindObjectsOfType<DeadBody>().FirstOrDefault(db => db.ParentId == __instance.PlayerId)?.gameObject.Destroy();
                if (__instance == PlayerControl.LocalPlayer) HudManager.Instance.PlayerCam.Locked = false;

                __instance.Data.Role.TeamType = RoleTeamTypes.Impostor;
                DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, RoleTypes.Impostor);

                __instance.transform.localScale = new Vector3(0.7f, 0.7f, 1);
                __instance.Visible = true;

                __instance.DestroyComponent<SpriteRenderer>();
                foreach (SpriteRenderer rend in __instance.GetComponentsInChildren<SpriteRenderer>())
                    rend.sortingOrder += 5;

                __instance.moveable = true;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
        [HarmonyPostfix]
        public static void HidingTimePatch(GameManager __instance)
        {
            Main.Logger.LogInfo("Game Started!");

            if (!ShipStatus.Instance.IsSubmerged())
                __instance.StartCoroutine(CoStartHidingTimer(ModData.HidingTime).WrapToIl2Cpp());
        }

        private static int Timer;
        private static IEnumerator CoStartHidingTimer(int time)
        {
            var lp = PlayerControl.LocalPlayer;
            lp.moveable = !lp.Data.Role.IsImpostor;

            Timer = time;
            for (int i = 0; i < time; i++)
            {
                Main.Logger.LogInfo(Timer);
                yield return null;
                UIPatch.AbilityInfoShower.text = string.Format(GetString(StringKey.HidingTimeLeft), Timer);
                yield return new WaitForSeconds(1);
                Timer = time - i;
            }

            UIPatch.AbilityInfoShower.text = "";
            PlayerControl.LocalPlayer.moveable = true;
        }

        // Player dead check
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
        [HarmonyPrefix]
        public static bool PlayerDeadPatch(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            var killer = __instance;
            if (killer == null || target == null) return false;
            if (!AmongUsClient.Instance.AmHost) return false;

            var impCount = PlayerControl.AllPlayerControls.ToArray().Count(pc => pc.Data.Role.IsImpostor);
            var crewCount = PlayerControl.AllPlayerControls.Count - impCount;
            if (killer == target && target.Data.Role.IsImpostor)
            {
                Main.Logger.LogInfo("Imp ded");
                HudManager.Instance.ShowCustomTaskComplete(string.Format(GetString(StringKey.SeekerDead), target, impCount, crewCount));
            }
            else
            {
                Main.Logger.LogInfo("Crew ded/infcted");
                HudManager.Instance.ShowCustomTaskComplete(string.Format(GetString(ModData.Infection ? StringKey.PropInfected : StringKey.PropDead), target, impCount, crewCount));
            }

            __instance.RpcMurderPlayer(target, true);
            return false;
        }

        // Fix submerged timer
        [HarmonyPatch]
        public class SubmergedHidingTimerFix
        {
            public static MethodBase TargetMethod() => SubmergedCompatibility.SubmergedSpawnBehaviour?.GetMethod("OnDestroy");
            public static void Postfix() 
            {
                if (!ShipStatus.Instance.IsSubmerged()) return;
                GameManager.Instance.StartCoroutine(CoStartHidingTimer(ModData.HidingTime).WrapToIl2Cpp()); 
            }
        }
    }
}
