using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Manager;
using System;
using static KK_HAutoSets.HAutoSets;
using static KK_HAutoSets.Hooks;
using static KK_HAutoSets.Utility;

namespace KK_HAutoSets
{
	public static class Hooks_Darkness
	{
		////////////////////////////////////////////////////////////////////////////////
		/// Keep the in-game menu accessible in forced OLoop by skipping the sonyu methods that disable them if in OLoop
		/// Then activate the orgasm buttons if male excitement gauge is above 70
		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSprite), "Sonyu3PDarkProc")]
		public static bool HSpriteSonyu3PDarkProcPre(HSprite __instance)
		{
			if (animationToggle.forceOLoop)
			{
				HSceneSpriteCategorySetActive(__instance.sonyu3PDark.categoryActionButton.lstButton, __instance.sonyu3PDark.tglAutoFinish.isOn, 18);

				return false;
			}
			else
			{
				return true;
			}
		}
		////////////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////


		////////////////////////////////////////////////////////////////////////////////
		/// See comment section "Disable AutoFinish in Service Modes" under KKHautoSets.Hooks
		/// 
		[HarmonyPrefix]
		[HarmonyPatch(typeof(H3PDarkHoushi), "LoopProc")]
		public static void Houshi3PDarkOLoopInit()
		{
			if (DisableAutoPrecum.Value)
				houshiRestoreGauge = true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(H3PDarkHoushi), "LoopProc")]
		public static void Houshi3PDarkOLoopGaugePost()
		{
			if (houshiRestoreGauge)
			{
				flags.gaugeMale = maleGaugeOld;
				maleGaugeOld = -1;

				foreach (HSprite sprite in sprites)
					sprite.SetHoushi3PDarkAutoFinish(_force: true);

				houshiRestoreGauge = false;
			}
		}
		////////////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////

		#region Override game behavior to extend or exit OLoop based on plugin status

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkSonyu), nameof(H3PDarkSonyu.Proc))]
		public static IEnumerable<CodeInstruction> DarkOLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			var voiceCheck = AccessTools.Method(typeof(Voice), nameof(Voice.IsVoiceCheck), new Type[] { typeof(Transform), typeof(bool) }) 
				?? throw new ArgumentNullException("Voice.IsVoiceCheck not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of 1, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Call, injectMethod) };

			return InjectInOLoop(new List<CodeInstruction>(instructions), voiceCheck, injection, OpCodes.Brtrue);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PDarkHoushi), nameof(H3PDarkHoushi.Proc))]
		public static IEnumerable<CodeInstruction> DarkHoushiOLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

			var voiceCheck = AccessTools.Method(typeof(HActionBase), "IsCheckVoicePlay") ?? throw new ArgumentNullException("HActionBase.IsCheckVoicePlay not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of 0, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Call, injectMethod) };

			return InjectInOLoop(new List<CodeInstruction>(instructions), voiceCheck, injection, OpCodes.Brfalse);
		}

		#endregion
	}
}
