using System.Collections.Generic;
using Harmony;
using UnityEngine;

namespace KK_HAutoSets
{
	public static class Hooks
	{
		//This should hook to a method that loads as late as possible in the loading phase
		//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
		//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
		public static void HSceneProcLoadPostfix(HSceneProc __instance)
		{
			var females = (List<ChaControl>)Traverse.Create(__instance).Field("lstFemale").GetValue();
			var hSprite = __instance.sprite;
			List<ChaControl> males = new List<ChaControl>
			{
				(ChaControl)Traverse.Create(__instance).Field("male").GetValue(),
				(ChaControl)Traverse.Create(__instance).Field("male1").GetValue()
			};		

			KK_HAutoSets.lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			KK_HAutoSets.flags = __instance.flags;
			
			KK_HAutoSets.EquipAllAccessories(females);
			KK_HAutoSets.LockGaugesAction(hSprite);
			KK_HAutoSets.HideShadow(males, females);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "LateUpdate")]
		public static void HSceneLateUpdatePostfix()
		{
			KK_HAutoSets.GaugeLimiter();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HActionBase), "IsBodyTouch")]
		public static bool IsBodyTouchPre(bool __result)
		{
			if (KK_HAutoSets.DisableHideBody.Value)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// The vanilla game does not have any moan or breath sounds available for the precum (OLoop) animation.
		/// This patch makes the game play sound effects as if it's in strong loop when the game is in fact playing OLoop without entering cum,
		/// such as when forced by this plugin or when finish flag is none.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HVoiceCtrl), "BreathProc")]
		public static void BreathProcPre(ref AnimatorStateInfo _ai)
		{
			if ((KK_HAutoSets.forceOLoop || KK_HAutoSets.flags.finish == HFlag.FinishKind.none) && KK_HAutoSets.flags.nowAnimStateName.Contains("OLoop"))
				_ai = KK_HAutoSets.sLoopInfo;
		}
	}
}
