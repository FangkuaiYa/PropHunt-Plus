using PropHunt.CustomOption;
using static PropHunt.Main;

namespace PropHunt.Module
{
    public static class ModData
    {
        public static bool IsTutorial => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
        public static bool IsLobby => LobbyBehaviour.Instance && !GameManager.Instance.GameHasStarted;
        public static int CurrentMiskills { get; set; } = 0;

        public static int HidingTime => (int)CustumOptions.HideTime.Get();
		public static int MaxMiskill => (int)CustumOptions.MaximumMissedKills.Get();
        public static bool Infection => CustumOptions.InfectionMode.Get();
    }
}