using HarmonyLib;
using Hazel;
using System.Data;
using System.Linq;
using static PropHunt.Main;

namespace PropHunt
{
    public enum CustomRpc
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
            var rpc = (CustomRpc)callId;
            switch (rpc)
            {
                case CustomRpc.PropSync:
                    byte id = reader.ReadByte();
                    PlayerControl player = null;
                    int idx = reader.ReadInt32();
                    player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
                    RpcHandler.PropSync(player, idx);
                    break;
                case CustomRpc.SettingSync:
                    var hidingTime = reader.ReadInt32();
                    var missedKills = reader.ReadInt32();
                    var infection = reader.ReadBoolean();
                    RpcHandler.SettingSync(hidingTime, missedKills, infection);
                    break;
                case CustomRpc.Handshake:
                    byte playerId = reader.ReadByte();
                    ulong modInfo = reader.ReadUInt64();
                    PlayerControl sender;
                    sender = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == playerId).FirstOrDefault();
                    RpcHandler.Handshake(sender, modInfo);
                    break;
            }
        }

    }
}
