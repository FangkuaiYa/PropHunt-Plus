using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using HudCoTaskComplete = HudManager._CoTaskComplete_d__64;

namespace PropHunt
{
    [HarmonyPatch]
    public static class Utils
    {
        public static GameObject FindClosestConsole(GameObject origin, float radius)
        {
            Collider2D bestCollider = null;

            float bestDist = 9999;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin.transform.position, radius))
            {
                if (collider.GetComponent<Console>() != null)
                {
                    float dist = Vector2.Distance(origin.transform.position, collider.transform.position);
                    if (dist < bestDist)
                    {
                        bestCollider = collider;
                        bestDist = dist;
                    }
                }
            }
            Main.Logger.LogInfo(bestCollider?.name);
            return bestCollider?.gameObject;
        }

        private static bool IsCustom = false;

        public static void ShowCustomTaskComplete(this HudManager hud, string text)
        {
            IsCustom = true;
            var tmp = hud.TaskCompleteOverlay.GetComponent<TextMeshPro>();
            hud.ShowTaskComplete();
            hud.TaskCompleteOverlay.DestroyComponent<TextTranslatorTMP>();
            tmp.text = text;
        }

        [HarmonyPatch(typeof(HudCoTaskComplete), nameof(HudCoTaskComplete.MoveNext))]
        [HarmonyPostfix]
        private static void OnShowingCustomCompletedOverlay(HudCoTaskComplete __instance, bool __result)
        {
            var hud = __instance.__4__this;
            var tmp = hud.TaskCompleteOverlay.GetComponent<TextMeshPro>();
            if (!IsCustom) tmp.text = TranslationController.Instance.GetString(StringNames.TaskComplete);
            if (!__result) IsCustom = false;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this List<T> collection)
        {
            var il2cppList = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in collection)
                il2cppList.Add(item);
            return il2cppList;
        }

        public static void Destroy(this Object obj) => Object.Destroy(obj);
        public static void DestroyImmediate(this Object obj) => Object.DestroyImmediate(obj);
        public static void DestroyComponent<T>(this Component comp) where T : Component => comp.gameObject.DestroyComponent<T>();
        public static void DestroyComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>().Destroy();
    }
}
