using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Manager;
using System;
using static KK_HAutoSets.HAutoSets;
using static KK_HAutoSets.SpeechControl;
using static KK_HAutoSets.Utility;

namespace KK_HAutoSets
{
	public class InstructionNotFoundException : Exception
	{
		public InstructionNotFoundException()
		{
		}

		public InstructionNotFoundException(string message)
			 : base(message)
		{
		}
	}

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
			speechControl = __instance.gameObject.AddComponent<SpeechControl>();
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
			if (hCategory != HCategory.service && (animationToggle.forceOLoop || animationToggle.orgasmTimer > 0) && flags.nowAnimStateName.Contains("OLoop"))
				_ai = AnimationToggle.sLoopInfo;		
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

		#region Override game behavior to extend or exit OLoop based on plugin status

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HSonyu), nameof(HSonyu.Proc))]
		[HarmonyPatch(typeof(HMasturbation), nameof(HMasturbation.Proc))]
		[HarmonyPatch(typeof(HLesbian), nameof(HLesbian.Proc))]
		public static IEnumerable<CodeInstruction> OLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			var voiceCheck = AccessTools.Method(typeof(Voice), nameof(Voice.IsVoiceCheck), new Type[] { typeof(Transform), typeof(bool) }) 
				?? throw new ArgumentNullException("Voice.IsVoiceCheck not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of 1, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Call, injectMethod) };

			return InjectInOLoop(new List<CodeInstruction>(instructions), voiceCheck, injection, OpCodes.Brtrue);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HHoushi), nameof(HHoushi.Proc))]
		public static IEnumerable<CodeInstruction> HoushiOLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			var voiceCheck = AccessTools.Field(typeof(HVoiceCtrl.Voice), nameof(HVoiceCtrl.Voice.state)) ?? throw new ArgumentNullException("HVoiceCtrl.Voice.state not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of 1, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Call, injectMethod) };

			//In Houshi mode's vanilla code there are two conditions that both have to be met for OLoop to be continued.
			//This injects an override for the first condition
			List<CodeInstruction> instructionList = InjectInOLoop(new List<CodeInstruction>(instructions), voiceCheck, injection, OpCodes.Brfalse);

			//Overrides the second condition and return the modified instructions
			var voiceCheck2 = AccessTools.Method(typeof(Voice), nameof(Voice.IsVoiceCheck), new Type[] { typeof(Transform), typeof(bool) }) 
				?? throw new ArgumentNullException("Voice.IsVoiceCheck not found");	
			return InjectInOLoop(instructionList, voiceCheck2, injection, OpCodes.Brtrue);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PHoushi), nameof(H3PHoushi.Proc))]
		[HarmonyPatch(typeof(H3PSonyu), nameof(H3PSonyu.Proc))]
		public static IEnumerable<CodeInstruction> H3POLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

			var voiceCheck = AccessTools.Method(typeof(HActionBase), "IsCheckVoicePlay") ?? throw new ArgumentNullException("HActionBase.IsCheckVoicePlay not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of 0, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Call, injectMethod) };

			return InjectInOLoop(new List<CodeInstruction>(instructions), voiceCheck, injection, OpCodes.Brfalse);
		}

		/// <summary>
		/// Determines if OLoop should be forced to continue, forced to stop, or left alone.
		/// Designed to be injected to override an existing value on the stack after a second value is pushed onto the stack to be loaded as the second parameter.
		/// </summary>
		/// <param name="vanillaValue">The existing value that was pushed to the stack</param>
		/// <param name="targetValue">Designed as a boolean where positive numbers are true and everything else is false. Return this value if OLoop should be forced to continue, or return the logical NOT if OLoop should be forced to stop </param>
		public static int OLoopStackOverride(int vanillaValue, int targetValue)
		{
			//forceStopVoice is set to true when forcing to enter orgasm, thus OLoop should be forcibly stopped. So we would return the inverse of the targetValue
			//Otherwise if orgasmTimer is greater 0, that means currently the orgasm timer is still counting down and OLoop should be continued, so we pass the targetValue
			//If neither of those two conditions are met, return the original value that was on the stack
			if (animationToggle.forceStopVoice)
				return targetValue > 0 ? 0 : 1;
			else if (animationToggle.orgasmTimer > 0)
				return targetValue;
			else
				return vanillaValue;
		}

		/// <summary>
		/// Injects code instructions into block(s) of codes that begins with a check for OLoop
		/// </summary>
		/// <param name="instructions">The list of instructions to work with</param>
		/// <param name="targetOperand">Inject after the instruction that contains this operand</param>
		/// <param name="injection">The code instructions to inject</param>
		/// <param name="targetOpCode">If specified, the OpCode of the instruction immediately after the targetOperand instruction must be targetOpCode before injection can proceed</param>
		/// <returns></returns>
		internal static List<CodeInstruction> InjectInOLoop(List<CodeInstruction> instructions, object targetOperand, CodeInstruction[] injection, object targetOpCode = null)
		{
			FindOLoopInstructionRange(instructions, out int rangeStart, out int rangeEnd);

			for (var i = rangeStart; i < rangeEnd; i++)
			{
				if (instructions[i].operand == targetOperand && (targetOpCode != null ? (instructions[i + 1].opcode == (OpCode)targetOpCode) : true))
				{
					instructions.InsertRange(i + 1, injection);
#if DEBUG
					UnityEngine.Debug.LogWarning(new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name + " injected instructions after " + targetOperand.ToString() + " at index " + i);
#endif               
				}
			}

			return instructions;
		}

		/// <summary>
		/// Find a range in the given instructions that begins with a call of UnityEngine.AnimatorStateInfo.IsName on either "OLoop" or "A_OLoop",
		/// and ends with another call of UnityEngine.AnimatorStateInfo.IsName on some other states of animation.
		/// </summary>
		/// <param name="instructions">The list of instructions to search through</param>
		/// <param name="oLoopStart">Outputs the start index of the range of OLoop</param>
		/// <param name="oLoopEnd">Outputs the end index of the range of OLoop</param>
		private static void FindOLoopInstructionRange(List<CodeInstruction> instructions, out int oLoopStart, out int oLoopEnd)
		{
			oLoopStart = -1;
			oLoopEnd = instructions.Count;
			string[] oLoopStrings = new string[2] { "OLoop", "A_OLoop" };

			var animatorStateInfoMethod = AccessTools.Method(typeof(AnimatorStateInfo), nameof(AnimatorStateInfo.IsName)) 
				?? throw new ArgumentNullException("UnityEngine.AnimatorStateInfo.IsName not found");

			for (var i = 0; i < instructions.Count; i++)
			{
				if (oLoopStrings.Contains(instructions[i].operand as string) && instructions[i + 1].operand == animatorStateInfoMethod)
				{
					oLoopStart = i + 2;
					break;
				}
			}

			if (oLoopStart < 0)
				throw new InstructionNotFoundException("OLoop");

			for (var i = oLoopStart + 1; i < instructions.Count; i++)
			{
				if (instructions[i].operand == animatorStateInfoMethod && !oLoopStrings.Contains(instructions[i - 1].operand as string))
				{
					oLoopEnd = i;
					break;
				}
			}
		}

		#endregion
	}
}
