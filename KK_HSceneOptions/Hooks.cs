using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Manager;
using System;
using static KK_HSceneOptions.HSceneOptions;
using static KK_HSceneOptions.SpeechControl;
using static KK_HSceneOptions.Utilities;

namespace KK_HSceneOptions
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
			sprites.Clear();
			sprites.Add(__instance.sprite);

			lstmMale = new List<ChaControl> { Traverse.Create(__instance).Field("male").GetValue<ChaControl>() };
			if (isDarkness)
				lstmMale.Add(Traverse.Create(__instance).Field("male1").GetValue<ChaControl>());
			lstmMale = lstmMale.FindAll(male => male != null);

			lstFemale = females;
			lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			flags = __instance.flags;		
			voice = __instance.voice;
			hands[0] = __instance.hand;
			hands[1] = __instance.hand1;

			EquipAllAccessories(females);
			foreach (HSprite sprite in sprites)
				LockGaugesAction(sprite);

			if (HideMaleShadow.Value)
				MaleShadow();

			if (HideFemaleShadow.Value)
				FemaleShadow();

			if (SpeechControlMode.Value == SpeechMode.Timer)
				SetVoiceTimer();

			animationToggle = __instance.gameObject.AddComponent<AnimationToggle>();
			speechControl = __instance.gameObject.AddComponent<SpeechControl>();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "LateUpdate")]
		public static void HSceneLateUpdatePostfix() => GaugeLimiter();
		

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

		
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), nameof(HFlag.VoiceFlag.IsSonyuIdleTime))]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), nameof(HFlag.VoiceFlag.IsHoushiIdleTime))]
		[HarmonyPatch(typeof(HFlag.VoiceFlag), nameof(HFlag.VoiceFlag.IsAibuIdleTime))]
		public static bool IsIdleTimePre(ref bool __result)
		{
			//Force the game to play idle voice line while forceIdleVoice is true
			if (forceIdleVoice)
			{
				__result = true;
				return false;
			}
			//If speech control is not disabled then idle voice is either muted or triggered manually according to the timer.
			//In those situations we don't want the game to trigger idle voice lines anyway, 
			//so we make this method return false to prevent idle speech from triggering.
			else if (SpeechControlMode.Value != SpeechMode.Disabled)
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
			if (SpeechControlMode.Value == SpeechMode.MuteAll && !forceIdleVoice)
			{
				__result = false;
				return false;
			}
			else
				return true;
		}

		//After SpeechControl.PlayVoice() is called, VoiceProc isn't called immediately or sometimes at all. 
		//This causes SpeechControl to reset the timer and set voicePlaying to false before the voice is done playing (if it has been queued to play).
		//Therefore, we re-initialize the timer and the voicePlaying flag again here in case the voice was indeed played as indicated by __result being true,
		//allowing SpeechControl to set the timer again at the end of voice playback.
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HVoiceCtrl), "VoiceProc")]
		public static void VoiceProcPost(bool __result)
		{
			if (__result)
			{
				voiceTimer = Time.time;
				voicePlaying = true;
			}			
		}


		#region Force start orgasm when pressing menu buttons
		/// If precum countdown timer is set, forcibly start orgasm immediately when pressing the corresponding menu buttons so that the start of orgasm is synchronized with the start of the countdown.
		/// Also forcibly start orgasm during service modes if auto finish is disabled, as the buttons wouldn't do anything when the animation is not in OLoop

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), "OnInsideClick")]
		public static void OnInsideClickPost()
		{
			if (PrecumTimer.Value > 0 || (DisableAutoPrecum.Value && hCategory == HCategory.service))
				animationToggle.ManualOrgasm(inside: true);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), "OnOutsideClick")]
		public static void OnOutsideClickPost()
		{
			if (PrecumTimer.Value > 0 || (DisableAutoPrecum.Value && hCategory == HCategory.service))
				animationToggle.ManualOrgasm(inside: false);
		}

		#endregion


		#region Disable non-functional controlpad input during forced OLoop

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

		#endregion


		//When changing between service modes, if the male gauge is above the orgasm threshold then after the transition the animation will be forced to OLoop with the menu disabled.
		//These hooks bypass that behavior when DisableAutoPrecum is set to true.
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HHoushi), nameof(HHoushi.MotionChange))]
		[HarmonyPatch(typeof(H3PHoushi), nameof(H3PHoushi.MotionChange))]
		public static void HoushiMotionChangePre(ref int _motion)
		{
			//Change parameter from 2 (OLoop) to 1 (WLoop)
			if (_motion == 2 && DisableAutoPrecum.Value)
				_motion = 1;
		}


		//If service mode is prevented from automatically entering OLoop due to DisableAutoPrecum, the speed gauge would not be reset after orgasm.
		//As a result, if service is restarted afterward it may start at a really high speed which is a bit unnatural.
		//This resets the speed guage when cumming in service modes.
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiInside))]
		[HarmonyPatch(typeof(HFlag), nameof(HFlag.AddHoushiOutside))]
		public static void HoushiSpeedReset()
		{
			if (DisableAutoPrecum.Value && flags)
				flags.speedCalc = 0f;
		}


		#region Keep in-game menu accessible when in forced OLoop

		/// <summary>
		/// Make the game treats forced OLoop the same as SLoop, thus preventing the game menu from deactivating during forced OLoop
		/// </summary>
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SonyuProc))]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.Sonyu3PProc))]
		public static IEnumerable<CodeInstruction> HSpriteSonyuProcTpl(IEnumerable<CodeInstruction> instructions)
		{
			var animatorStateInfoMethod = AccessTools.Method(typeof(AnimatorStateInfo), nameof(AnimatorStateInfo.IsName))
				?? throw new ArgumentNullException("UnityEngine.AnimatorStateInfo.IsName not found");
			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(HSpriteProcStackOverride)) ?? throw new ArgumentNullException("Hooks.HSpriteProcStackOverride not found");

			//Look for the check for SLoop then replace it with a check for (SLoop || forceOLoop)
			return InjectInstruction(
				instructions: new List<CodeInstruction>(instructions), 
				targetOperand: "SLoop",
				targetNextOperand: animatorStateInfoMethod,
				injection: new CodeInstruction[] { new CodeInstruction(OpCodes.Call, injectMethod) }, 			
				insertAt: 2);
		}

		/// <summary>
		/// Replace a boolean value on the stack to true if OLoop is being enforced by this plugin. Otherwise, return the original value on the stack.
		/// </summary>
		/// <returns>The value to be pushed back onto the stack to replace what was on it.</returns>
		internal static bool HSpriteProcStackOverride(bool valueOnStack) => animationToggle.forceOLoop ? true : valueOnStack;		

		#endregion


		#region Disable AutoFinish in Service Modes

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HHoushi), "LoopProc")]
		public static IEnumerable<CodeInstruction> HoushiDisableAutoFinishTpl(IEnumerable<CodeInstruction> instructions) => HoushiDisableAutoFinish(
			instructions, AccessTools.Method(typeof(Hooks), nameof(HoushiMaleGaugeOverride)));

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PHoushi), "LoopProc")]
		public static IEnumerable<CodeInstruction> Houshi3PDisableAutoFinishTpl(IEnumerable<CodeInstruction> instructions) => HoushiDisableAutoFinish(
			instructions, AccessTools.Method(typeof(Hooks), nameof(Houshi3PMaleGaugeOverride)));

		/// <summary>
		/// Injects HoushiMaleGaugeOverride to where the vanilla code checks for the male gauge, allowing the check to be bypassed based on config.
		/// </summary>
		/// <param name="instructions">The instructions to be processed</param>
		/// <param name="menuMethod">The reflection info for the HSprite method used to activate the orgasm menu buttons</param>
		/// <returns>Returns the processed instructions</returns>
		public static IEnumerable<CodeInstruction> HoushiDisableAutoFinish(IEnumerable<CodeInstruction> instructions, MethodInfo menuMethod)
		{
			var gaugeCheck = AccessTools.Field(typeof(HFlag), nameof(HFlag.gaugeMale)) ?? throw new ArgumentNullException("HFlag.gaugeMale not found");

			//Push the specified service mode as int onto the stack, then use it as a parameter to call HoushiMaleGaugeOverride
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Call, menuMethod) };

			return InjectInstruction(
				instructions: new List<CodeInstruction>(instructions),
				targetOperand: gaugeCheck,
				targetNextOpCode: OpCodes.Ldc_R4,
				injection: injection, 			
				insertAt: 2);
		}

		/// <summary>
		/// Designed to modify the stack and override the orgasm threshold from 70 to 110 if DisableAutoPrecum is enabled, effectively making it impossible to pass the threshold.
		/// Also, manually activate the orgasm menu buttons if male gauge is past 70. 
		/// </summary>
		/// <returns>The value to replace the vanilla threshold value</returns>
		private static float HoushiMaleGaugeOverride(float vanillaThreshold)
		{
			if (DisableAutoPrecum.Value && flags.gaugeMale >= vanillaThreshold)
			{
				foreach (HSprite sprite in sprites)
					sprite.SetHoushiAutoFinish(_force: true);
				
				return 110;  //this can be any number greater than 100, the maximum possible gauge value.
			}
			else
				return vanillaThreshold;		
		}

		/// <summary>
		/// Designed to modify the stack and override the orgasm threshold from 70 to 110 if DisableAutoPrecum is enabled, effectively making it impossible to pass the threshold.
		/// Also, manually activate the orgasm menu buttons if male gauge is past 70. 
		/// </summary>
		/// <returns>The value to replace the vanilla threshold value</returns>
		private static float Houshi3PMaleGaugeOverride(float vanillaThreshold)
		{
			if (DisableAutoPrecum.Value && flags.gaugeMale >= vanillaThreshold)
			{
				foreach (HSprite sprite in sprites)
					sprite.SetHoushi3PAutoFinish(_force: true);

				return 110;  //this can be any number greater than 100, the maximum possible gauge value.
			}
			else
				return vanillaThreshold;
		}

		#endregion


		#region Override game behavior to extend or exit OLoop based on plugin status
		//In the game code for the various sex modes, the logic for when the precum animation (OLoop) should end involves checking whether the girl is done speaking.
		//Therefore, to extend OLoop we'd need to find the check for speech then override its returned value.

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HSonyu), nameof(HSonyu.Proc))]
		[HarmonyPatch(typeof(HMasturbation), nameof(HMasturbation.Proc))]
		[HarmonyPatch(typeof(HLesbian), nameof(HLesbian.Proc))]
		public static IEnumerable<CodeInstruction> OLoopExtendTpl(IEnumerable<CodeInstruction> instructions) 
			=> OLoopExtendInstructions(
				instructions,
				targetOperand: AccessTools.Method(typeof(Voice), nameof(Voice.IsVoiceCheck), new Type[] { typeof(Transform), typeof(bool) }), 
				overrideValue: 1);

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(H3PHoushi), nameof(H3PHoushi.Proc))]
		[HarmonyPatch(typeof(H3PSonyu), nameof(H3PSonyu.Proc))]
		public static IEnumerable<CodeInstruction> H3POLoopExtendTpl(IEnumerable<CodeInstruction> instructions) 
			=> OLoopExtendInstructions(
				instructions,
				targetOperand: AccessTools.Method(typeof(HActionBase), "IsCheckVoicePlay"),
				targetNextOpCode: OpCodes.Brfalse,
				overrideValue: 0);

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(HHoushi), nameof(HHoushi.Proc))]
		public static IEnumerable<CodeInstruction> HoushiOLoopExtendTpl(IEnumerable<CodeInstruction> instructions)
		{
			//In Houshi mode's vanilla code there are two conditions that both have to be met for OLoop to be continued.
			//This injects an override for the first condition
			List<CodeInstruction> instructionList = (List<CodeInstruction>)OLoopExtendTpl(instructions);

			//Overrides the second condition and return the modified instructions
			var secondVoiceCheck = AccessTools.Field(typeof(HVoiceCtrl.Voice), nameof(HVoiceCtrl.Voice.state));		
			return OLoopExtendInstructions(instructionList, secondVoiceCheck, overrideValue: 1, targetNextOpCode: OpCodes.Brfalse);
		}


		/// <summary>
		/// Within the given set of instructions, find the instruction that matches the specified operand/opcode and insert a call to OLoopStackOverride that allows extending/exiting OLoop based on the status of the plugin
		/// </summary>
		/// <param name="instructions">Instructions to be processed</param>
		/// <param name="targetOperand">Reflection info of the operand of the instruction after which new instructions will be injected</param>
		/// <param name="overrideValue">Passed as targetValue for OLoopStackOverride to push onto the stack</param>
		/// <param name="targetNextOpCode">If specified, OpCode of the instruction after the target instruction must match this for injection to proceed</param>
		public static IEnumerable<CodeInstruction> OLoopExtendInstructions(IEnumerable<CodeInstruction> instructions, object targetOperand, int overrideValue, object targetNextOpCode = null)
		{
			if (targetOperand == null)
				throw new ArgumentNullException("Operand of target instruction not found");

			if (targetNextOpCode == null)
				targetNextOpCode = OpCodes.Brtrue;

			var instructionList = new List<CodeInstruction>(instructions);

			var injectMethod = AccessTools.Method(typeof(Hooks), nameof(OLoopStackOverride)) ?? throw new ArgumentNullException("Hooks.OLoopExtendOverride not found");

			// Instructions that inject OLoopStackOverride and pass it the value of overrideValue, which is the value required to be on the stack to satisfy the condition to let OLoop continue
			var injection = new CodeInstruction[] { new CodeInstruction(OpCodes.Ldc_I4, overrideValue), new CodeInstruction(OpCodes.Call, injectMethod) };
			
			FindOLoopInstructionRange(instructionList, out int rangeStart, out int rangeEnd);
			return InjectInstruction(instructionList, targetOperand, injection, targetNextOpCode, rangeStart: rangeStart, rangeEnd: rangeEnd);
		}

		/// <summary>
		/// Determines if OLoop should be forced to continue, forced to stop, or left alone. Designed to be injected in IL instructions.
		/// This method expects two values on the stack. The second-from-the-top value will become vanillaValue and be replaced by the returned value, while the top value on the stack becomes targetValue and gets consumed.
		/// </summary>
		/// <param name="vanillaValue">The existing value that was pushed to the stack</param>
		/// <param name="targetValue">Treated as a boolean where positive numbers are true and everything else is false. Return this value if OLoop should be forced to continue, or return the logical NOT if OLoop should be forced to stop </param>
		/// <returns>The value to be pushed onto the stack to replace vanillaValue</returns>
		public static int OLoopStackOverride(int vanillaValue, int targetValue)
		{
			//If forceStopVoice is set to true when forcing to enter orgasm, OLoop should be forcibly stopped. In that case we would return the inverse of the targetValue.
			//Otherwise, if orgasmTimer is greater 0, that means currently the orgasm timer is still counting down and OLoop should be continued, so we return the targetValue.
			//If neither of those two conditions are met, return the original value that was on the stack
			if (animationToggle.forceStopVoice)
				return targetValue > 0 ? 0 : 1;
			else if (animationToggle.orgasmTimer > 0)
				return targetValue;
			else
				return vanillaValue;
		}

		/// <summary>
		/// Find a range in the given instructions that begins with a call of UnityEngine.AnimatorStateInfo.IsName with the parameter being either "OLoop" or "A_OLoop",
		/// and ends with another call of UnityEngine.AnimatorStateInfo.IsName with the parameter being some other states of animation.
		/// </summary>
		/// <param name="instructions">The list of instructions to search through</param>
		/// <param name="oLoopStart">Outputs the start index of the range of OLoop</param>
		/// <param name="oLoopEnd">Outputs the end index of the range of OLoop</param>
		/// <exception cref="InstructionNotFoundException">Thrown if UnityEngine.AnimatorStateInfo.IsName with the parameter "OLoop" or "A_OLoop" is not found</exception>
		internal static void FindOLoopInstructionRange(List<CodeInstruction> instructions, out int oLoopStart, out int oLoopEnd)
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


		#region Quick Position Change


		internal static string motionChangeOld;
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.OnChangePlaySelect))]
		public static void OnChangePositionButtonPost()
		{
			if (flags.selectAnimationListInfo == null)
				return;

			string[] loopAnims = new string[] { "WLoop", "SLoop", "OLoop" };
			bool inPistonSameMode = flags.selectAnimationListInfo.mode == flags.mode && loopAnims.Any(str => flags.nowAnimStateName.Contains(str));

			if (QuickPositionChange.Value == PositionSkipMode.Always || (QuickPositionChange.Value == PositionSkipMode.Auto && inPistonSameMode))
			{
				//Reset voiceWait to false so that HSceneProc.ChangeAnimator will run immediately
				flags.voiceWait = false;

				for (int i = 0; i < 2; i++)
				{
					flags.voice.playVoices[i] = -1;

					if (voice.nowVoices[i].state == HVoiceCtrl.VoiceKind.voice)
						Singleton<Voice>.Instance.Stop(flags.transVoiceMouth[i]);
				}
				//Reset flags.click to bypass the vanilla behavior of switching back to idle animation before changing position
				flags.click = HFlag.ClickKind.none;

				if (inPistonSameMode)
					motionChangeOld = flags.nowAnimStateName;
			}			
		}

		/// <summary>
		/// If maintaining motion when changing positions, prevent the game from resetting H related parameters (e.g., speed gauge) after the new position is loaded. 
		/// Additionally, set the animation of the new position to the animation before the position change instead of idle.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSonyu), nameof(HSonyu.MotionChange))]
		[HarmonyPatch(typeof(H3PSonyu), nameof(H3PSonyu.MotionChange))]
		[HarmonyPatch(typeof(HHoushi), nameof(HHoushi.MotionChange))]
		[HarmonyPatch(typeof(H3PHoushi), nameof(H3PHoushi.MotionChange))]

		public static bool MotionChangeOverride(HActionBase __instance, ref bool __result)
		{
			if (motionChangeOld != null)
			{
				//If the animation was OLoop before the position change, set the current animation to SLoop, since beginning a new position in OLoop seems unnatural and requires a lot more work.
				__instance.SetPlay(motionChangeOld.Contains("OLoop") ? "SLoop" : motionChangeOld);

				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// If maintaining motion when changing positions, make sure the game does not redraw the buttons for insertion
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetHoushiStart))]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetHoushi3PStart))]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetSonyuStart))]
		[HarmonyPatch(typeof(HSprite), nameof(HSprite.SetSonyu3PStart))]
		public static bool HSpriteInitOverride(ref bool __result)
		{
			if (motionChangeOld != null)
			{
				__result = false;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "ChangeAnimator")]
		public static void ChangeAnimatorPost() => motionChangeOld = null;


		#endregion
	}
}
