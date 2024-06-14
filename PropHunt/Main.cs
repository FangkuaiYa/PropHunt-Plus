// Core Script of PropHuntPlugin
// Copyright (C) 2022  ugackMiner
global using static PropHunt.Language;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PropHunt.Module;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PropHunt;


[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInProcess("Among Us.exe")]
public partial class Main : BasePlugin
{
    // Mod Informations
    public const string ModGUID = "com.jiege.PropHuntPlus";
    public const string ModName = "Prop Hunt Plus";
    public const string ModVersion = "1.0";

    public static ulong ModNameHex => 0x5048506c7573; //"PHPlus" in Hexadecimal
    public static ulong ModVersionHex => Convert.ToUInt64(Main.ModVersion, 16);
    public static ulong ModInfoForHandshake => ModNameHex + ModVersionHex;

    // Backend Variables
    public Harmony Harmony { get; } = new(ModGUID);
    public ConfigEntry<int> HidingTime { get; set; }
    public ConfigEntry<int> MaxMissedKills { get; set; }
    public ConfigEntry<bool> Infection { get; set; }
    public ConfigEntry<bool> Debug { get; set; }

    internal static ManualLogSource Logger;

    // Gameplay Variables
    public static int hidingTime
    {
        get => Instance.HidingTime.Value;
        set
        {
            Instance.HidingTime.Value = value;
            Instance.Config.Save();
        }
    }
    public static int maxMissedKills
    {
        get => Instance.MaxMissedKills.Value;
        set
        {
            Instance.MaxMissedKills.Value = value;
            Instance.Config.Save();
        }
    }
    public static bool infection
    {
        get => Instance.Infection.Value;
        set
        {
            Instance.Infection.Value = value;
            Instance.Config.Save();
        }
    }

    public static int missedKills = 0;
    public static bool IsModLobby => true;//(AmongUsClient.Instance.AmHost || PlayerVersion.ContainsKey(AmongUsClient.Instance.GetHost().Character)) && 
    //    AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay && 
    //    GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.Normal;

    public static Dictionary<PlayerControl, ulong> PlayerVersion = new();
    public static Main Instance;


    public override void Load()
    {
        Logger = base.Log;
        HidingTime = Config.Bind("Prop Hunt", "Hiding Time", 30);
        MaxMissedKills = Config.Bind("Prop Hunt", "Max Misses", 3);
        Infection = Config.Bind("Prop Hunt", "Infection", true);
        Debug = Config.Bind("Prop Hunt", "Debug", false);

        Instance = this;

        Harmony.PatchAll();
        SubmergedCompatibility.Start();
        Logger.LogInfo("Loaded");
    }



    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    class ModStampPatch
    {
        public static void Postfix(ModManager __instance) => __instance.ShowModStamp();
    }

    public static class RpcHandler
    {
        public static void RpcPropSync(PlayerControl player, int propIndex)
        {
            GameObject prop = ShipStatus.Instance.AllConsoles[propIndex].gameObject;
            Logger.LogInfo($"{player.Data.PlayerName} changed sprite to: {prop.name}");
            player.GetComponent<SpriteRenderer>().sprite = prop.GetComponent<SpriteRenderer>().sprite;
            player.transform.localScale = prop.transform.lossyScale;
            player.Visible = false;
        }

        public static void RpcSettingSync(PlayerControl player, int _hidingTime, int _missedKills, bool _infection)
        {
            hidingTime = _hidingTime;
            maxMissedKills = _missedKills;
            infection = _infection;
            Logger.LogInfo("H: " + Main.hidingTime + ", M: " + Main.maxMissedKills + ", I: " + Main.infection);
            if (player == PlayerControl.LocalPlayer && (hidingTime != Instance.HidingTime.Value || maxMissedKills != Instance.MaxMissedKills.Value || infection != Instance.Infection.Value))
            {
                Instance.HidingTime.Value = hidingTime;
                Instance.MaxMissedKills.Value = maxMissedKills;
                Instance.Infection.Value = infection;
                Instance.Config.Save();
            }
        }

        public static void RpcHandshake(PlayerControl player, ulong modInfoHex)
        {
            PlayerVersion[player] = modInfoHex;
        }
    }
}
