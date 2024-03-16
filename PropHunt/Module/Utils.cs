using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PropHunt
{
    public static class Utils
    {
        public static GameObject FindClosestConsole(GameObject origin, float radius)
        {
            try
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
                return bestCollider.gameObject;
            }
            catch
            {
                Main.Logger.LogError("Error getting nearest console");
                return null;
            }
        }

        public static void ShowCustomTaskComplete(this HudManager hud, string text)
        {
            hud.ShowTaskComplete();
            GameObject.Find("Main Camera/Hud/TaskCompleteOverlay_TMP").GetComponent<TextMeshPro>().text = text;
        }
    }
}
