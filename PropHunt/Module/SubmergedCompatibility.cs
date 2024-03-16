using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PropHunt.Module
{
    public static class SubmergedCompatibility
    {
        public const string GUID = "Submerged";
        public static Assembly Assembly { get; set; }
        public static bool Loaded { get; set; }
        public static PluginInfo PluginInfo { get; set; }
        public static BasePlugin Plugin { get; set; }
        public static Type[] Types { get; set; }
        public static MethodInfo ShipStatusExtIsSubmerged { get; set; }
        public static Type SubmergedSpawnBehaviour { get; set; }
        public static void Start()
        {
            Loaded = IL2CPPChainloader.Instance.Plugins.TryGetValue(GUID, out var inf);
            if (!Loaded || inf == null) return;
            PluginInfo = inf;
            Plugin = PluginInfo.Instance as BasePlugin;
            Assembly = Plugin.GetType().Assembly;

            Types = AccessTools.GetTypesFromAssembly(Assembly);
            ShipStatusExtIsSubmerged = AccessTools.Method(Types.First(t => t.Name == "ShipStatusExtensions"), "IsSubmerged");
            SubmergedSpawnBehaviour = Types.First(t => t.Name == "SubmarineSelectSpawn");
        }

        public static bool IsSubmerged(this ShipStatus ship)
        {
            if (!Loaded) return false;
            if (ShipStatusExtIsSubmerged.Invoke(null, new object[] { ship }) is bool b) return b;
            return false;
        }
    }
}
