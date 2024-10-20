using System;

namespace PropHunt.CustomOption
{
    public class CustumOptions
    {
        public static CustomHeaderOption PropHuntSeeting;
        public static CustomNumberOption HideTime;
        public static CustomNumberOption MaximumMissedKills;
		public static CustomToggleOption InfectionMode;

		private static Func<object, string> CooldownFormat { get; } = value => $"{value:#}" == "" ? $"0" : $"{value:#}s";

        public static void Load()
        {
            var num = 0;

			PropHuntSeeting = new CustomHeaderOption(num++, MultiMenu.main, "Prop Hunt Settings");
			HideTime = new CustomNumberOption(num++, MultiMenu.main, "Hide Time", 5f, 0f, 120f, 5f,
				CooldownFormat);
			MaximumMissedKills = new CustomNumberOption(num++, MultiMenu.main, "Maximum Missed Kills", 1f, 0f, 35f, 1f,
				CooldownFormat);
			InfectionMode = new CustomToggleOption(num++, MultiMenu.main, "Infection Mode", false);
        }
    }
}