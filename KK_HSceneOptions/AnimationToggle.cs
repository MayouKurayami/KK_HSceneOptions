using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using static KK_HSceneOptions.HSceneOptions;

namespace KK_HSceneOptions
{
	internal class AnimationToggle : MonoBehaviour
	{
		private HActionBase proc;

		internal bool forceOLoop;
		internal bool forceStopVoice;
		
		internal float orgasmTimer;

		private string animationName = "";
		internal static AnimatorStateInfo sLoopInfo;

		private delegate bool LoopProc(bool _loop);
		private LoopProc loopProcDelegate;

		private void Start()
		{
			sLoopInfo = new AnimatorStateInfo();
			object dummyInfo = sLoopInfo;
			Traverse.Create(dummyInfo).Field("m_Name").SetValue(-1715982390);
			Traverse.Create(dummyInfo).Field("m_SpeedMultiplier").SetValue(3f);
			Traverse.Create(dummyInfo).Field("m_Speed").SetValue(1f);
			Traverse.Create(dummyInfo).Field("m_Loop").SetValue(1);
			Traverse.Create(dummyInfo).Field("m_NormalizedTime").SetValue(59.73729f);
			Traverse.Create(dummyInfo).Field("m_Length").SetValue(0.4444448f);
			sLoopInfo = (AnimatorStateInfo)dummyInfo;
		}

		private void Update()
		{
			if (!flags)
				return;

			if (animationName != flags.nowAnimationInfo.nameAnimation)
				UpdateProc();

			if (Input.GetKeyDown(OLoopKey.Value.MainKey) && OLoopKey.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				ManualOLoop();
			}
			else if (Input.GetKeyDown(OrgasmInsideKey.Value.MainKey) && OrgasmInsideKey.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				ManualOrgasm(inside: true);
			}
			else if (Input.GetKeyDown(OrgasmOutsideKey.Value.MainKey) && OrgasmOutsideKey.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				ManualOrgasm(inside: false);
			}

			//If currently in forced precum loop and male (penis) is involved in the action, manually proc LoopProc which otherwise wouldn't run in OLoop.
			//LoopProc is an imporant method which takes care of essential functions such as speech and entering orgasm during regular piston loops.
			//This method wouldn't exist in scenes where there is no male involvement
			if (forceOLoop)
			{
				flags.speedCalc = 1f;

				if (hCategory != HCategory.maleNotVisible)
					loopProcDelegate?.Invoke(true);
			}

			//if orgasm countdown timer is initialized and the time since its intialization has past the configured amount, 
			//stop the currently playing speech to allow entering orgasm, then reset counter to 0.
			//As precaution against extravagant timer values and consequently stopping voice after orgasm has already finished, only run this if still in precum loop.
			if (orgasmTimer > 0)
			{			
				if (flags.nowAnimStateName.Contains("OLoop"))
				{
					if ((Time.time - orgasmTimer) > PrecumTimer.Value)
					{
						StartCoroutine(ToggleFlagSingleFrame(x => forceStopVoice = x));

						orgasmTimer = 0;
					}	
				}
				//Reset the timer back to 0 once time is reached regardless of current animation state
				else if ((Time.time - orgasmTimer) > PrecumTimer.Value)
				{
					orgasmTimer = 0;
				}

			}
		}

		private Type FindProc()
		{
			switch (flags.mode)
			{
				case (HFlag.EMode.sonyu):
					proc = lstProc.OfType<HSonyu>().FirstOrDefault();
					hCategory = HCategory.intercourse;
					return typeof(HSonyu);
				case (HFlag.EMode.houshi):
					proc = lstProc.OfType<HHoushi>().FirstOrDefault();
					hCategory = HCategory.service;
					return typeof(HHoushi);
				case (HFlag.EMode.houshi3P):
					proc = lstProc.OfType<H3PHoushi>().FirstOrDefault();
					hCategory = HCategory.service;
					return typeof(H3PHoushi);
				case (HFlag.EMode.aibu):
					proc = lstProc.OfType<HAibu>().FirstOrDefault();
					hCategory = HCategory.maleNotVisible;
					return typeof(HAibu);
				case (HFlag.EMode.lesbian):
					proc = lstProc.OfType<HLesbian>().FirstOrDefault();
					hCategory = HCategory.maleNotVisible;
					return typeof(HLesbian);
				case (HFlag.EMode.masturbation):
					proc = lstProc.OfType<HMasturbation>().FirstOrDefault();
					hCategory = HCategory.maleNotVisible;
					return typeof(HMasturbation);
				case (HFlag.EMode.sonyu3P):
					proc = lstProc.OfType<H3PSonyu>().FirstOrDefault();
					hCategory = HCategory.intercourse;
					return typeof(H3PSonyu);
				default:
					return null;
			}
		}

		private Type FindProcDarkness()
		{
			switch (flags.mode)
			{
				case (HFlag.EMode.houshi3PMMF):
					proc = lstProc.OfType<H3PDarkHoushi>().FirstOrDefault();
					hCategory = HCategory.service;
					return typeof(H3PDarkHoushi);
				case (HFlag.EMode.sonyu3PMMF):
					proc = lstProc.OfType<H3PDarkSonyu>().FirstOrDefault();
					hCategory = HCategory.intercourse;
					return typeof(H3PDarkSonyu);
				default:
					return null;
			}
		}

		/// <summary>
		/// Update proc field to reflect the current active H mode, and point loopProcDelegate to the correct LoopProc method in modes where it exists
		/// </summary>
		private void UpdateProc()
		{
			MethodInfo loopProcInfo;
			Type procType;

			procType = FindProc();

			if (procType == null && isDarkness)
			{
				procType = FindProcDarkness();
			}

			if (procType == null)
			{
				Destroy(this);
				return;
			}

			if (hCategory != HCategory.maleNotVisible)
			{
				loopProcInfo = AccessTools.Method(procType, "LoopProc", new Type[] { typeof(bool) });
				loopProcDelegate = (LoopProc)Delegate.CreateDelegate(typeof(LoopProc), proc, loopProcInfo);
			}

			animationName = flags.nowAnimationInfo.nameAnimation;
		}

		internal void ManualOLoop()
		{
			//Only allow forced entering of precum loop if currently in piston loop, to prevent issues with unintended orgasm caused by entering precum loop elsewhere
			if (!forceOLoop && (flags.nowAnimStateName.Contains("SLoop") || flags.nowAnimStateName.Contains("WLoop") || flags.nowAnimStateName.Contains("MLoop")))
			{
				proc.SetPlay(flags.isAnalPlay ? "A_OLoop" : "OLoop", true);
				forceOLoop = true;
			}
			//Only allow exiting OLoop via keyboard shortcut if orgasm has not been initiated, 
			//by checking flags.finish in intercourse modes, or the "rePlay" field in service modes which would be at a non-zero value if orgasm sequence has been initiated.
			else if (flags.nowAnimStateName.Contains("OLoop"))
			{
				bool notOrgasm = true;

				if (hCategory == HCategory.intercourse)
					notOrgasm = flags.finish == HFlag.FinishKind.none;
				else if (hCategory == HCategory.service)
					notOrgasm = (Traverse.Create(proc).Field("rePlay")?.GetValue<int>() ?? 0) == 0;

				if (notOrgasm)
				{
					proc.SetPlay(flags.isAnalPlay ? "A_SLoop" : "SLoop", true);
					forceOLoop = false;
				}
			}
		}


		/// <summary>
		/// Manually start orgasm accordindg to current the condition and initialize cum countdown timer 
		/// </summary>
		/// <param name="inside">Whether the to cum inside or not</param>
		internal void ManualOrgasm(bool inside)
		{
			//In piston (intercourse) modes, set the cum click value to be processed by the game, 
			//then set the voice state of all females to breath to allow the game to interrupt currently playing speech
			//Then run the LoopProc method twice. The first call allows the game to set the correct HFlag.finish value based on the click value we sent, 
			//and the second call allows the game to enter OLoop and play back the corresponding speech based on the HFlag.finish value set in the previous call
			if (hCategory == HCategory.intercourse)
			{
				flags.click = inside ? HFlag.ClickKind.inside : HFlag.ClickKind.outside;
			
				foreach (HVoiceCtrl.Voice v in voice.nowVoices)
					v.state = HVoiceCtrl.VoiceKind.breath;

				for (int i = 0; i < 2; i++)
					loopProcDelegate?.Invoke(true);
			}
			//Outside of intercourse, if the mode is not aibu then it must be either service or female only (masturbation and lesbian) modes,
			//For those modes we manually enter precum (OLoop) to allow the game to start looking at parameters related to entering orgasm, then use a coroutine to wait for the transition to OLoop.
			//After that, either play a voice line in females only modes, or set the cum click flag to trigger orgasm in service modes
			else if (flags.mode > HFlag.EMode.aibu)
			{
				proc.SetPlay("OLoop", false);

				if (hCategory == HCategory.maleNotVisible && SpeechControlMode.Value != SpeechMode.MuteAll)
					StartCoroutine(RunAfterTransition(() => speechControl.PlayVoice()));
				else
					StartCoroutine(RunAfterTransition(() => flags.click = inside ? HFlag.ClickKind.inside : HFlag.ClickKind.outside));
			}
			//In aibu mode, simply set the cum click value to enter orgasm immediately.
			else
			{
				flags.click = HFlag.ClickKind.orgW;
			}

			if (PrecumTimer.Value > 0)
				orgasmTimer = Time.time;
		}	
	}
}
