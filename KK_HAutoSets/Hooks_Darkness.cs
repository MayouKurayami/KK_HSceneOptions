using System.Collections.Generic;
using Harmony;
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
	}
}
