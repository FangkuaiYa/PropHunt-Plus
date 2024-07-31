using static PropHunt.Main;

namespace PropHunt.Module
{
    public static class ModData
    {
        public static bool IsTutorial => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
        public static bool IsLobby => LobbyBehaviour.Instance && !GameManager.Instance.GameHasStarted;
        public static int CurrentMiskills { get; set; } = 0;

        public static int HidingTime
        {
            get => Instance.HidingTimeConfig.Value;
            set
            {
                Instance.HidingTimeConfig.Value = value;
                Instance.Config.Save();
            }
        }
        public static int MaxMiskill
        {
            get => Instance.MaxMiskillConfig.Value;
            set
            {
                Instance.MaxMiskillConfig.Value = value;
                Instance.Config.Save();
            }
        }
        public static bool Infection
        {
            get => Instance.InfectionConfig.Value;
            set
            {
                Instance.InfectionConfig.Value = value;
                Instance.Config.Save();
            }
        }
    }
}