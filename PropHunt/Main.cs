// Core Script of PropHuntPlugin
// Copyright (C) 2022  ugackMiner
global using static PropHunt.Language;
global using Object = UnityEngine.Object;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PropHunt.CustomOption;
using PropHunt.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PropHunt;


[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInProcess("Among Us.exe")]
public partial class Main : BasePlugin
{
    // Mod Information
    public const string ModGUID = "com.modlaboratory.prophuntplus";
    public const string ModName = "Prop Hunt Plus";
    public const string ModVersion = "1.0";

    public static ulong ModNameHex => 0x5048506c7573; //"PHPlus" in Hexadecimal
    public static ulong ModVersionHex => Convert.ToUInt64(ModVersion, 16);
    public static ulong ModInfoForHandshake => ModNameHex + ModVersionHex;

    // Backend Variables
    public Harmony Harmony { get; } = new(ModGUID);
    public ConfigEntry<int> HidingTimeConfig { get; set; }
    public ConfigEntry<int> MaxMiskillConfig { get; set; }
    public ConfigEntry<bool> InfectionConfig { get; set; }
    public ConfigEntry<bool> Debug { get; set; }

    internal static ManualLogSource Logger;

    // Gameplay Variables
    
    public static bool IsModLobby => true;//(AmongUsClient.Instance.AmHost || PlayerVersion.ContainsKey(AmongUsClient.Instance.GetHost().Character)) && 
    //    AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay && 
    //    GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.Normal;

    public static Dictionary<PlayerControl, ulong> PlayerVersion = new();
    public static Main Instance { get; private set; }


    public override void Load()
    {
        Logger = Log;
        HidingTimeConfig = Config.Bind("Prop Hunt", "Hiding Time", 30);
        MaxMiskillConfig = Config.Bind("Prop Hunt", "Max Misses", 3);
        InfectionConfig = Config.Bind("Prop Hunt", "Infection", true);
        Debug = Config.Bind("Prop Hunt", "Debug", false);

        Instance = this;

        SubmergedCompatibility.Start();
        if (SubmergedCompatibility.Loaded)
            Harmony.PatchAll();
        else
            foreach (var type in typeof(Main).Assembly.GetTypes().Where(t => !t.Equals(typeof(PlayerPatch.SubmergedHidingTimerFix))))
                Harmony.PatchAll(type);
        CustumOptions.Load();
        Harmony.PatchAll(typeof(Language));
        Logger.LogInfo("Loaded successfully!");
    }



    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    static class ModStampPatch
    {
        public static void Postfix(ModManager __instance) => __instance.ShowModStamp();
    }

    public static class RpcHandler
    {
        public static void PropSync(PlayerControl player, int propIndex)
        {
            GameObject prop = ShipStatus.Instance.AllConsoles[propIndex].gameObject;
            Logger.LogInfo($"{player.Data.PlayerName} changed sprite to: {prop.name}");
            player.GetComponent<SpriteRenderer>().sprite = prop.GetComponent<SpriteRenderer>().sprite;
            player.transform.localScale = prop.transform.lossyScale;
            player.Visible = false;
        }

        public static void Handshake(PlayerControl player, ulong modInfoHex)
        {
            PlayerVersion[player] = modInfoHex;
        }
    }
}
