using System.Collections.Generic;
using Harmony;
using UnityEngine;
using Manager;
using System;
using System.Linq;

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

			HAutoSets.lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			HAutoSets.flags = __instance.flags;
			HAutoSets.female = females.FirstOrDefault<ChaControl>();

			HAutoSets.EquipAllAccessories(females);
			HAutoSets.LockGaugesAction(hSprite);
			HAutoSets.HideShadow(males, females);

			__instance.gameObject.AddComponent<AnimationToggle>();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "LateUpdate")]
		public static void HSceneLateUpdatePostfix()
		{
			HAutoSets.GaugeLimiter();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HActionBase), "IsBodyTouch")]
		public static bool IsBodyTouchPre(bool __result)
		{
			if (HAutoSets.DisableHideBody.Value)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// The vanilla game does not have any moan or breath sounds available for the precum (OLoop) animation.
		/// This patch makes the game play sound effects as if it's in strong loop when the game is in fact playing OLoop without entering cum,
		/// such as when forced by this plugin.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HVoiceCtrl), "BreathProc")]
		public static void BreathProcPre(ref AnimatorStateInfo _ai)
		{
			if (AnimationToggle.forceOLoop && HAutoSets.flags.nowAnimStateName.Contains("OLoop"))
				_ai = AnimationToggle.sLoopInfo;
		}

		/// <summary>
		/// When the stop voice flag is set, this patch makes calls to check currently playing speech to return false,
		/// effectively allowing the game to interrupt the current playing speech.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Voice), "IsVoiceCheck", new Type[] { typeof(Transform), typeof(bool) })]
		public static bool IsVoiceCheckPre(ref bool __result, Transform voiceTrans)
		{
			if (AnimationToggle.forceStopVoice && (voiceTrans == HAutoSets.flags.transVoiceMouth[0] || voiceTrans == HAutoSets.flags.transVoiceMouth[1]))
			{
				__result = false;
				return false;
			}
			else
				return true;
		}

		/// <summary>
		/// Resets OLoop flag when switching animation, to account for leaving OLoop.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HActionBase), "SetPlay")]
		public static void SetPlayPost()
		{
			AnimationToggle.forceOLoop = false;
		}


	}
}
