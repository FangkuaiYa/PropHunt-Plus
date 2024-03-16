using HarmonyLib;
using Hazel;
using System.Data;
using System.Linq;
using static PropHunt.Main;

namespace PropHunt
{
    public enum RPC
    {
        PropSync = 200,
        SettingSync = 201,
        Handshake,
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCPatch
    {
        public static void Postfix([HarmonyArgument(0)] byte callId,[HarmonyArgument(1)] MessageReader reader)
        {
            var rpc = (RPC)callId;
            switch (rpc)
            {
                case RPC.PropSync:
                    byte id = reader.ReadByte();
                    PlayerControl player = null;
                    int idx = reader.ReadInt32();
                    player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
                    RpcHandler.RpcPropSync(player, idx);
                    break;
                case RPC.SettingSync:
                    byte pid = reader.ReadByte();
                    PlayerControl p = null;
                    var hidingTime = reader.ReadInt32();
                    var missedKills = reader.ReadInt32();
                    var infection = reader.ReadBoolean();
                    p = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == pid).FirstOrDefault();
                    RpcHandler.RpcSettingSync(p, hidingTime, missedKills, infection);
                    break;
                case RPC.Handshake:
                    byte playerId = reader.ReadByte();
                    ulong modInfo = reader.ReadUInt64();
                    PlayerControl sender;
                    sender = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == playerId).FirstOrDefault();
                    RpcHandler.RpcHandshake(sender, modInfo);
                    break;
            }
        }

    }
}
