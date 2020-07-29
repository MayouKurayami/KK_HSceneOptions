using System.Collections.Generic;
using HarmonyLib;
using static KK_HAutoSets.HAutoSets;
using static KK_HAutoSets.Hooks;

namespace KK_HAutoSets
{
	public static class Hooks_Darkness
	{
		//When changing between service modes, if the male gauge is above the orgasm threshold then after the transition the animation will be forced to OLoop with the menu disabled.
		//These hooks bypass that behavior when DisableAutoPrecum is set to true.
		[HarmonyPrefix]
		[HarmonyPatch(typeof(H3PDarkHoushi), nameof(H3PDarkHoushi.MotionChange))]
		public static void HoushiDarkMotionChangePre(ref int _motion)
		{
			//Change parameter from 2 (OLoop) to 1 (WLoop)
			if (_motion == 2 && DisableAutoPrecum.Value)
				_motion = 1;
		}

		////////////////////////////////////////////////////////////////////////////////
		/// Keep the in-game menu accessible in forced OLoop
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.Sonyu3PDarkProc))]
		public static IEnumerable<CodeInstruction> HSpriteDarkSonyuProcTpl(IEnumerable<CodeInstruction> instructions) => HSpriteSonyuProcTpl(instructions);


		////////////////////////////////////////////////////////////////////////////////
		/// See section "Disable AutoFinish in Service Modes" under KKHautoSets.Hooks
		/// 
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkHoushi), "LoopProc")]
		public static IEnumerable<CodeInstruction> Houshi3PDisableAutoFinishTpl(IEnumerable<CodeInstruction> instructions) => HoushiDisableAutoFinish(instructions, HFlag.EMode.houshi3PMMF);



		#region Override game behavior to extend or exit OLoop based on plugin status

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkSonyu), nameof(H3PDarkSonyu.Proc))]
		public static IEnumerable<CodeInstruction> DarkOLoopExtendTpl(IEnumerable<CodeInstruction> instructions) => OLoopExtendTpl(instructions);


		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkHoushi), nameof(H3PDarkHoushi.Proc))]
		public static IEnumerable<CodeInstruction> DarkHoushiOLoopExtendTpl(IEnumerable<CodeInstruction> instructions) => H3POLoopExtendTpl(instructions);

		#endregion
	}
}
