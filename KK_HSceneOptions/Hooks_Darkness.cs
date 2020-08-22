using System.Collections.Generic;
using HarmonyLib;
using static KK_HSceneOptions.HSceneOptions;
using static KK_HSceneOptions.Hooks;

namespace KK_HSceneOptions
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
		public static IEnumerable<CodeInstruction> Houshi3PDisableAutoFinishTpl(IEnumerable<CodeInstruction> instructions) => HoushiDisableAutoFinish(
			instructions, AccessTools.Method(typeof(Hooks_Darkness), nameof(Houshi3PDarkMaleGaugeOverride)));

		/// <summary>
		/// Designed to modify the stack and override the orgasm threshold from 70 to 110 if DisableAutoPrecum is enabled, effectively making it impossible to pass the threshold.
		/// Also, manually activate the orgasm menu buttons if male gauge is past 70. 
		/// </summary>
		/// <returns>The value to replace the vanilla threshold value</returns>
		private static float Houshi3PDarkMaleGaugeOverride(float vanillaThreshold)
		{
			if (DisableAutoPrecum.Value && flags.gaugeMale >= vanillaThreshold)
			{
				foreach (HSprite sprite in sprites)
					sprite.SetHoushi3PDarkAutoFinish(_force: true);

				return 110;  //this can be any number greater than 100, the maximum possible gauge value.
			}
			else
				return vanillaThreshold;
		}

		#region Override game behavior to extend or exit OLoop based on plugin status

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkSonyu), nameof(H3PDarkSonyu.Proc))]
		public static IEnumerable<CodeInstruction> DarkOLoopExtendTpl(IEnumerable<CodeInstruction> instructions) => OLoopExtendTpl(instructions);


		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkHoushi), nameof(H3PDarkHoushi.Proc))]
		public static IEnumerable<CodeInstruction> DarkHoushiOLoopExtendTpl(IEnumerable<CodeInstruction> instructions) => H3POLoopExtendTpl(instructions);

		#endregion

		#region Quick Position Change

		/// <summary>
		/// If maintaining motion when changing positions, prevent the game from resetting H related parameters (e.g., speed gauge) after the new position is loaded. 
		/// Additionally, set the animation of the new position to the animation before the position change instead of idle.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(H3PDarkSonyu), nameof(H3PDarkSonyu.MotionChange))]
		[HarmonyPatch(typeof(H3PDarkHoushi), nameof(H3PDarkHoushi.MotionChange))]
		public static bool MotionChangeOverrideDark(HActionBase __instance, ref bool __result)
			=> MotionChangeOverride(__instance, ref __result);

		/// <summary>
		/// If maintaining motion when changing positions, make sure the game does not redraw the buttons for insertion
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetHoushi3PDarkStart))]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetSonyu3PDarkStart))]
		public static bool HSpriteInitOverrideDark(ref bool __result)
			=> HSpriteInitOverride(ref __result);

		#endregion
	}
}
