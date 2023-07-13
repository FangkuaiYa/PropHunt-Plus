using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using static PropHunt.PropHuntPlugin;

namespace PropHunt
{
    public enum RPC
    {
        PropSync = 200,
        SettingSync = 201,
    }
    [HarmonyPatch(typeof(PlayerControl),nameof(PlayerControl.HandleRpc))]
    class RPCPatch
    {
        public static void Postfix([HarmonyArgument(0)] byte callId,[HarmonyArgument(1)] MessageReader reader)
        {
            var rpc = (RPC)callId;
            switch (rpc)
            {
                case RPC.PropSync:
                    var id = reader.ReadByte();
                    PlayerControl player = null;
                    var idx = reader.ReadString();
                    foreach(var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.PlayerId == id)
                        {
                            player = pc;
                            break;
                        }
                    }
                    RPCHandler.RPCPropSync(player, idx);
                    break;
                case RPC.SettingSync:
                    var pid = reader.ReadByte();
                    PlayerControl p = null;
                    var hidingTime = reader.ReadInt32();
                    var missedKills = reader.ReadInt32();
                    var infection = reader.ReadBoolean();
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.PlayerId == pid)
                        {
                            p = pc;
                            break;
                        }
                    }
                    RPCHandler.RPCSettingSync(p, hidingTime, missedKills, infection);
                    break;
            }
        }

    }
}
