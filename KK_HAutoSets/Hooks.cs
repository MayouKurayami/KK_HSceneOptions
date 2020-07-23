using System.Collections.Generic;
using Harmony;
using UnityEngine;
using Manager;
using System;
using static KK_HAutoSets.HAutoSets;
using static KK_HAutoSets.Utility;

namespace KK_HAutoSets
{
	public static class Hooks
	{
		internal static float maleGaugeOld = -1;
		internal static bool houshiRestoreGauge;

		//This should hook to a method that loads as late as possible in the loading phase
		//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
		//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
		public static void HSceneProcLoadPostfix(HSceneProc __instance)
		{
			var females = (List<ChaControl>)Traverse.Create(__instance).Field("lstFemale").GetValue();
			sprites.Clear();
			sprites.Add(__instance.sprite);
			List<ChaControl> males = new List<ChaControl>
			{
				(ChaControl)Traverse.Create(__instance).Field("male").GetValue(),
				(ChaControl)Traverse.Create(__instance).Field("male1").GetValue()
			};

			lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			flags = __instance.flags;
			lstFemale = females;
			voice = __instance.voice;
			hands[0] = __instance.hand;
			hands[1] = __instance.hand1;

			EquipAllAccessories(females);
			foreach (HSprite sprite in sprites)
				LockGaugesAction(sprite);

			HideShadow(males, females);

			if (AutoVoice.Value == SpeechMode.Timer)
				SetVoiceTimer(2f);

			animationToggle = __instance.gameObject.AddComponent<AnimationToggle>();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "LateUpdate")]
		public static void HSceneLateUpdatePostfix()
		{
			GaugeLimiter();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HActionBase), "IsBodyTouch")]
		public static bool IsBodyTouchPre(bool __result)
		{
			if (DisableHideBody.Value)
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
			if (animationToggle.forceOLoop && flags.nowAnimStateName.Contains("OLoop"))
				_ai = AnimationToggle.sLoopInfo;
		}

		/// <summary>
		/// When the stop voice flag is set, this patch makes calls to check currently playing speech to return false,
		/// effectively allowing the game to interrupt the current playing speech.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Voice), "IsVoiceCheck", new Type[] { typeof(Transform), typeof(bool) })]
		public static bool IsVoiceCheckPre(ref bool __result)
		{
			if (animationToggle.forceStopVoice)
			{
				__result = false;
				return false;
			}
			else if (PrecumExtend.Value && animationToggle.orgasmTimer > 0)
			{
				__result = true;
				return false;
			}
			else
			{
				return true;
			}			
		}

		/// <summary>
		/// Resets OLoop flag when switching animation, to account for leaving OLoop.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HActionBase), "SetPlay")]
		public static void SetPlayPost()
		{
			if (animationToggle?.forceOLoop ?? false)			
				animationToggle.forceOLoop = false;
		}

		//In intercourse modes, force the game to play idle voice line while forceIdleVoice is true
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), "IsSonyuIdleTime")]
		public static bool IsSonyuIdleTimePre(ref bool __result)
		{
			if (forceIdleVoice)
			{
				__result = true;
				return false;
			}
			//If speech control is not disabled and timer has a positive value, 
			//make this method return false so that default idle speech will not trigger during countdown or mute idle mode.
			//The timer would have a positive value if it's currently counting down in timer mode, or at its default positive value if in mute idle mode.
			else if (voiceTimer > 0 && AutoVoice.Value != SpeechMode.Disabled)
			{
				__result = false;
				return false;
			}

			return true;
		}

		//In service modes, force the game to play idle voice line while forceIdleVoice is true
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), "IsHoushiIdleTime")]
		public static bool IsHoushiIdleTimePre(ref bool __result)
		{
			if (forceIdleVoice)
			{
				__result = true;
				return false;
			}
			//If speech control is not disabled and timer has a positive value, 
			//make this method return false so that default idle speech will not trigger during countdown or mute idle mode.
			//The timer would have a positive value if it's currently counting down in timer mode, or at its default positive value if in mute idle mode.
			else if (voiceTimer > 0 && AutoVoice.Value != SpeechMode.Disabled)
			{
				__result = false;
				return false;
			}

			return true;
		}

		//In caress modes, force the game to play idle voice line while forceIdleVoice is true
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), "IsAibuIdleTime")]
		public static bool IsAibuIdleTimePre(ref bool __result)
		{
			if (forceIdleVoice)
			{
				__result = true;
				return false;
			}
			//If speech control is not disabled and timer has a positive value, 
			//make this method return false so that default idle speech will not trigger during countdown or mute idle mode.
			//The timer would have a positive value if it's currently counting down in timer mode, or at its default positive value if in mute idle mode.
			else if (voiceTimer > 0 && AutoVoice.Value != SpeechMode.Disabled)
			{
				__result = false;
				return false;
			}

			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HVoiceCtrl), "VoiceProc")]
		public static bool VoiceProcPre(ref bool __result)
		{
			if (AutoVoice.Value == SpeechMode.MuteAll && !forceIdleVoice)
			{
				__result = false;
				return false;
			}
			else
				return true;
		}


		[HarmonyPostfix]
		[HarmonyPatch(typeof(HVoiceCtrl), "VoiceProc")]
		public static void VoiceProcPost(bool __result)
		{
			if (__result && AutoVoice.Value == SpeechMode.Timer)
				SetVoiceTimer(2f);
		}


		////////////////////////////////////////////////////////////////////////////////
		/// If precum countdown timer is set, manually proc orgasm when pressing the corresponding menu buttons to forcibly start orgasm immediately
		/// to prevent issues with the timer
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), "OnInsideClick")]
		public static void OnInsideClickPost()
		{
			if (PrecumTimer.Value > 0)
				animationToggle.ManualOrgasm(inside: true);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), "OnOutsideClick")]
		public static void OnOutsideClickPost()
		{
			if (PrecumTimer.Value > 0)
				animationToggle.ManualOrgasm(inside: false);
		}
		////////////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////


		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), "OnSpeedUpClick")]
		public static void OnSpeedUpClickPost()
		{
			switch (flags.click)
			{
				// If toggling through OLoop (PrecumToggle) is enabled, right clicking the control pad while in strong motion (SLoop) should transition to OLoop.
				// This detects if the user clicked to change motion while in SLoop, cancel the change, and manually proc OLoop
				case HFlag.ClickKind.motionchange:
					if (PrecumToggle.Value && flags.nowAnimStateName.Contains("SLoop"))
					{
						flags.click = HFlag.ClickKind.none;
						animationToggle.ManualOLoop();
					}
					break;

				// Disable middle click and left click actions while in forced OLoop because they don't do anything while in OLoop
				case HFlag.ClickKind.modeChange:
				case HFlag.ClickKind.speedup:
					if (animationToggle.forceOLoop && flags.nowAnimStateName.Contains("OLoop"))
						flags.click = HFlag.ClickKind.none;
					break;

				//If the user clicked the control pad yet flags.click is not assigned anything, then the click has to be from a VR controller.
				//This makes sure clicking the controller while in forced OLoop will result in leaving OLoop
				case HFlag.ClickKind.none:
					if (animationToggle.forceOLoop && flags.nowAnimStateName.Contains("OLoop"))
						flags.click = HFlag.ClickKind.motionchange;
					break;
			}
		}

		/// <summary>
		/// Disable speeding up piston gauge by scrolling while in forced OLoop, as the speed is fixed.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HFlag), "SpeedUpClick")]
		public static bool SpeedUpClickPre()
		{
			if (animationToggle.forceOLoop)
				return false;
			else
				return true;
		}


		////////////////////////////////////////////////////////////////////////////////
		/// Keep the in-game menu accessible in forced OLoop by skipping the sonyu methods that disable them if in OLoop
		/// Then activate the orgasm buttons if male excitement gauge is above 70
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSprite), "SonyuProc")]
		public static bool HSpriteSonyuPre(HSprite __instance)
		{
			if (animationToggle.forceOLoop)
			{
				int index = ((flags.selectAnimationListInfo != null) ? (flags.selectAnimationListInfo.isFemaleInitiative ? 1 : 0) : (flags.nowAnimationInfo.isFemaleInitiative ? 1 : 0)) * 7;
				HSceneSpriteCategorySetActive(__instance.sonyu.categoryActionButton.lstButton, __instance.sonyu.tglAutoFinish.isOn, 4 + index);

				return false;
			}	
			else
			{
				return true;
			}				
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSprite), "Sonyu3PProc")]
		public static bool HSpriteSonyu3PProcPre(HSprite __instance)
		{
			if (animationToggle.forceOLoop)
			{
				int index = ((flags.selectAnimationListInfo != null) ? (flags.selectAnimationListInfo.isFemaleInitiative ? 1 : 0) : (flags.nowAnimationInfo.isFemaleInitiative ? 1 : 0)) * 7;
				HSceneSpriteCategorySetActive(__instance.sonyu3P.categoryActionButton.lstButton, __instance.sonyu3P.tglAutoFinish.isOn, 4 + index);

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
		///                    Disable AutoFinish in Service Modes
		////////////////////////////////////////////////////////////////////////////////
		/// To prevent the game from automatically going into precum animation in service modes, 
		/// we need to prevent execution of a block of vanilla code (inside LoopProc() method of the current mode), by not satisfying its condition: 
		/// if (flags.click == HFlag.ClickKind.OLoop || flags.gaugeMale >= 70f)
		/// The first condition will never be met under normal gameplay, so we only need to make sure the second condition is false during the execution of said method.
		/// 
		/// We begin by keeping track when LoopProc() has begun to run with a flag (houshiRestoreGauge). 
		/// Then while houshiRestoreGauge is true, we modify gaugeMale to a value below 70f and store the vanilla value in a variable after it is updated by MaleGaugeUp().
		/// This means gaugeMale would only be modified inside LoopProc(), so that other methods that depend on gaugeMale wouldn't be affected.
		/// At the end of LoopProc() we then restore gaugeMale to the vanilla value and turn houshiRestoreGauge back to false to signal the end of LoopProc().
		/// 
		/// At the beginning of LoopProc, only make houshiRestoreGauge true if the funactionality is enabled in config.
		/// This acts as a master switch since the proceeding patches all depend on houshiRestoreGauge to be true.
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HFlag), "MaleGaugeUp")]
		public static void HoushiOLoopGaugePre()
		{
			if (houshiRestoreGauge && flags.gaugeMale >= 70f)
			{
				maleGaugeOld = flags.gaugeMale;
				flags.gaugeMale = 65f; //This can be any number below 70
			}
			else
				houshiRestoreGauge = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HHoushi), "LoopProc")]
		public static void HoushiOLoopInit()
		{
			if (DisableAutoPrecum.Value)
				houshiRestoreGauge = true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HHoushi), "LoopProc")]
		public static void HoushiOLoopGaugePost()
		{
			if (houshiRestoreGauge)
			{
				flags.gaugeMale = maleGaugeOld;
				maleGaugeOld = -1;

				foreach (HSprite sprite in sprites)
					sprite.SetHoushiAutoFinish(_force: true);

				houshiRestoreGauge = false;
			}		
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(H3PHoushi), "LoopProc")]
		public static void Houshi3POLoopInit()
		{
			if (DisableAutoPrecum.Value)
				houshiRestoreGauge = true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(H3PHoushi), "LoopProc")]
		public static void Houshi3POLoopGaugePost()
		{
			if (houshiRestoreGauge)
			{
				flags.gaugeMale = maleGaugeOld;
				maleGaugeOld = -1;

				foreach (HSprite sprite in sprites)
					sprite.SetHoushi3PAutoFinish(_force: true);

				houshiRestoreGauge = false;
			}
		}
		////////////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////
	}
}
