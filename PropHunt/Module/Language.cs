using HarmonyLib;
using System.Collections.Generic;

namespace PropHunt
{
	public enum StringKey
	{
		PropHuntSettings,
		HidingTime,
		MaxMiskill,
		Infection,
		Ping,
		RemainingAttempt,
		Seeker,
		SeekerDescription,
		Prop,
		PropDescription,
		HidingTimeLeft,
		CmdHelp,
		SystemMessage,
		SeekerDead,
		PropDead,
		PropInfected,
		MeetingDisabled,
		HandshakeNoMod,
		HandshakeOVMod
	}
	public static class Language
	{
		public static Dictionary<StringKey, string> langDic = new();

		[HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize))]
		[HarmonyPostfix]
		public static void Init(TranslationController __instance)
		{
			langDic = GetLang(__instance.currentLanguage.languageID);
		}

		[HarmonyPatch(typeof(TranslationController), nameof(TranslationController.SetLanguage))]
		[HarmonyPostfix]
		public static void SetLangPatch([HarmonyArgument(0)] TranslatedImageSet lang)
		{
			langDic = GetLang(lang.languageID);
		}

		public static string GetString(StringKey key)
		{
			string result = "";
			try
			{
				result = langDic[key];
			}
			catch
			{
				result = "<ERR_GET_TRSLATION:" + key.ToString() + ">";
			}
			return result;
		}

		private static Dictionary<StringKey, string> GetLang(SupportedLangs lang)
		{
			switch (lang)
			{
				default:
				case SupportedLangs.English:
					return new()
					{
						[StringKey.PropHuntSettings] = "Prop Hunt Settings",
						[StringKey.HidingTime] = "Hiding Time",
						[StringKey.MaxMiskill] = "Maximum Missed Kills",
						[StringKey.Infection] = "Infection Mode",
						[StringKey.Ping] = "Ping: {0} ms",
						[StringKey.RemainingAttempt] = "Remaining Attempts: {0}",
						[StringKey.Seeker] = "Seeker",
						[StringKey.SeekerDescription] = "Find and kill the props\nYour game will be unfrozen after {0} seconds",
						[StringKey.Prop] = "Prop",
						[StringKey.PropDescription] = "Turn into props to hide from the seekers",
						[StringKey.HidingTimeLeft] = "{0} seconds left for hiding!",
						[StringKey.CmdHelp] = "<b>How to play</b>:\n</b>R</b>: (Prop only) Turn into nearest task\n<b>Shift</b>: Noclip through walls\n<b>Note: Noclip is a temporary solution for getting stuck, not for hiding!</b>",
						[StringKey.SystemMessage] = "System Message",
						[StringKey.SeekerDead] = "Seeker {0} was dead!\n{1} Seeker(s) remaining, {2} Prop(s) remaining.",
						[StringKey.PropDead] = "Prop {0} was dead!\n{1} Seeker(s) remaining, {2} Prop(s) remaining.",
						[StringKey.PropInfected] = "Prop {0} was infected into seeker!\n{1} Seeker(s) remaining, {2} Prop(s) remaining.",
						[StringKey.MeetingDisabled] = "Meeting was disabled when playing Prop Hunt mode",
						[StringKey.HandshakeNoMod] = "Player {0} has a different mod or no mod.",
						[StringKey.HandshakeOVMod] = "Player {0} has a different version Prop Hunt Plus"
					};
				case SupportedLangs.SChinese:
					return new()
					{
						[StringKey.PropHuntSettings] = "道具躲猫猫设置",
						[StringKey.HidingTime] = "躲藏时间",
						[StringKey.MaxMiskill] = "最多击杀失误次数",
						[StringKey.Infection] = "道具死后变为内鬼",
						[StringKey.Ping] = "延迟：{0} 毫秒",
						[StringKey.RemainingAttempt] = "剩余击杀次数：{0}",
						[StringKey.Seeker] = "寻找者",
						[StringKey.SeekerDescription] = "找出道具们\n你在 {0} 秒后才能行动",
						[StringKey.Prop] = "道具",
						[StringKey.PropDescription] = "变成道具，戏弄寻找者",
						[StringKey.HidingTimeLeft] = "剩余 {0} 秒躲藏",
						[StringKey.CmdHelp] = "<b>玩法</b>：\n<b>R</b>: （道具使用） 变成离你最近的任务的样子\n<b>Shift</b>： 穿墙\n<b>注意：它只是一个你卡住时的解决方法，不是用来作弊的！</b>",
						[StringKey.SystemMessage] = "【系统消息】",
						[StringKey.SeekerDead] = "寻找者 {0} 已死亡！\n剩余 {1} 位寻找者， 剩余 {2} 个道具。",
						[StringKey.PropDead] = "道具 {0} 已死亡！\n剩余 {1} 位寻找者， 剩余 {2} 个道具。",
						[StringKey.PropInfected] = "道具 {0} 已被感染为寻找者！\n剩余 {1} 位寻找者， 剩余 {2} 个道具。",
						[StringKey.MeetingDisabled] = "在道具躲猫猫模式下，您无法开启会议",
						[StringKey.HandshakeNoMod] = "玩家 {0} 未安装模组或安装了其它模组",
						[StringKey.HandshakeOVMod] = "玩家 {0} 安装了其它版本的 Prop Hunt Plus"
					};
			}
		}
	}
}
