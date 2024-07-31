using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using PropHunt.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PropHunt
{
    [HarmonyPatch]
    class CustomRoleSettings
    {
        public static NumberOption HidingTimeOption { get; set; }
        public static NumberOption MaxMiskillOption { get; set; }
        public static ToggleOption InfectionModeOption { get; set; }

        public static FloatGameSetting HidingSetting => new()
        {
            ValidRange = new(5, 120),
            Increment = 5,
            FormatString = "",
            ZeroIsInfinity = false,
            SuffixType = NumberSuffixes.Seconds,
            Type = OptionTypes.Float,
            Value = ModData.HidingTime,
            OptionName = (FloatOptionNames)100000,
            Title = (StringNames)HidingOptionId
        };
        public static FloatGameSetting MaxMissSetting => new()
        {
            ValidRange = new(1, 35),
            Increment = 1,
            FormatString = "",
            ZeroIsInfinity = false,
            SuffixType = NumberSuffixes.None,
            Type = OptionTypes.Int,
            Value = ModData.MaxMiskill,
            OptionName = (FloatOptionNames)100000,
            Title = (StringNames)MaxMissOptionId
        };
        public static CheckboxGameSetting InfectionModeSetting => new()
        {
            Type = OptionTypes.Checkbox,
            OptionName = (BoolOptionNames)100000,
            Title = (StringNames)InfectionModeOptionId
        };
        public static List<BaseGameSetting> Settings => new()
        {
            HidingSetting,
            MaxMissSetting,
            InfectionModeSetting
        };

        public const int HidingOptionId = 9999;
        public const int MaxMissOptionId = 10000;
        public const int InfectionModeOptionId = 10001;

        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
        [HarmonyPostfix]
        public static void PropOptionsMenuPatch(RolesSettingsMenu __instance)
        {
            var headers = __instance.transform.FindChild("HeaderButtons");
            for (int i = 0; i < headers.childCount; i++)
            {
                var current = headers.GetChild(i);
                if (current.name.StartsWith("RoleSettingsTabButton")) current.gameObject.SetActive(false);
            }

            Object.FindObjectOfType<GameSettingMenu>().RoleSettingsButton.SelectButton(true);
            OpenModMenu(__instance);

            void InitNumberOptionDelayed(NumberOption option, StringKey id, float value)
            {
                option.StartCoroutine(Coroutine().WrapToIl2Cpp());
                IEnumerator Coroutine()
                {
                    yield return new WaitForSeconds(0.1f);
                    option.oldValue = option.Value = value;
                    option.TitleText.text = GetString(id);
                    option.ValueText.text = option.Data.GetValueString(value);
                }
            }

            void InitToggleOptionDelayed(ToggleOption option, StringKey id, bool value)
            {
                option.StartCoroutine(Coroutine().WrapToIl2Cpp());
                IEnumerator Coroutine()
                {
                    yield return new WaitForSeconds(0.1f);
                    option.CheckMark.enabled = option.oldValue = value;
                    option.TitleText.text = GetString(id);
                }
            }

            var options = __instance.advancedSettingChildren;
            foreach (var option in options)
            {
                switch ((int)option.Data.Title)
                {
                    case HidingOptionId:
                        {
                            HidingTimeOption = option.Cast<NumberOption>();
                            HidingTimeOption.ValidRange = HidingSetting.ValidRange;
                            HidingTimeOption.Increment = HidingSetting.Increment;
                            InitNumberOptionDelayed(HidingTimeOption, StringKey.HidingTime, ModData.HidingTime);
                        }
                        break;
                    case MaxMissOptionId:
                        {
                            MaxMiskillOption = option.Cast<NumberOption>();
                            MaxMiskillOption.ValidRange = HidingSetting.ValidRange;
                            MaxMiskillOption.Increment = HidingSetting.Increment;
                            InitNumberOptionDelayed(MaxMiskillOption, StringKey.MaxMiskill, ModData.MaxMiskill);
                        }
                        break;
                    case InfectionModeOptionId:
                        {
                            InfectionModeOption = option.Cast<ToggleOption>();
                            InitToggleOptionDelayed(InfectionModeOption, StringKey.Infection, ModData.Infection);
                        }
                        break;
                }

                option.OnValueChanged = new Action<OptionBehaviour>((o) =>
                {
                    switch ((int)o.Data.Title)
                    {
                        case HidingOptionId:
                            ModData.HidingTime = o.GetInt();
                            break;
                        case MaxMissOptionId:
                            ModData.MaxMiskill = o.GetInt();
                            break;
                        case InfectionModeOptionId:
                            ModData.Infection = o.GetBool();
                            break;
                    }

                    SyncCustomSettings();
                });

                var oldPos = option.transform.localPosition;
                option.transform.localPosition = new Vector3(0, oldPos.y, oldPos.z) + Vector3.up;
            }
        }

        public static void OpenModMenu(RolesSettingsMenu __instance)
        {
            __instance.ChangeTab(new()
            {
                Role = RoleManager.Instance.AllRoles.FirstOrDefault(r => r.Role == RoleTypes.Crewmate),
                AllGameSettings = Settings.ToIl2CppList()
            }, __instance.AllButton);
        }

        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.ChangeTab))]
        [HarmonyPostfix]
        public static void OnChangeTab(RolesSettingsMenu __instance)
        {
            __instance.AllButton.SelectButton(true);
            __instance.AllButton.Destroy();

            var advancedTab = __instance.transform.FindChild("Scroller").FindChild("SliderInner").FindChild("AdvancedTab");
            advancedTab.FindChild("InfoLabelBackground").gameObject.Destroy();
            advancedTab.FindChild("DescBackground").gameObject.Destroy();
            advancedTab.FindChild("Imagebackground").gameObject.Destroy();
            __instance.roleScreenshot.gameObject.Destroy();
            __instance.roleTitleText.text = GetString(StringKey.PropHunt);
        }

        [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.OpenMenu))]
        [HarmonyPostfix]
        public static void OnOpenMenu(RolesSettingsMenu __instance)
        {
            if (!__instance.AdvancedRolesSettings.active) OpenModMenu(__instance);
        }

        public static void SyncCustomSettings()
        {
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRpc.SettingSync, Hazel.SendOption.Reliable);
                writer.Write(ModData.HidingTime);
                writer.Write(ModData.MaxMiskill);
                writer.Write(ModData.Infection);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                Main.RpcHandler.SettingSync(ModData.HidingTime, ModData.MaxMiskill, ModData.Infection);
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Awake))]
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
        [HarmonyPostfix]
        public static void SyncSettingsPatch(PlayerControl __instance)
        {
            if (!GameManager.Instance) return; // Stop synchronizing when player prefab is loaded
            SyncCustomSettings();
        }
    }
}
