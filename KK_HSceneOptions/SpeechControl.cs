using System;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using static KK_HSceneOptions.HSceneOptions;

namespace KK_HSceneOptions
{
	internal class SpeechControl : MonoBehaviour
	{
		internal const float voiceMinInterval = 0.1f;
		internal const float voiceMaxInterval = 60f;

		internal static bool forceIdleVoice;
		internal static float voiceTimer = Time.time;
		/// <summary>
		/// Whether a voice line is currently being played. This should only be used when speech control is set to timer mode.
		/// </summary>
		internal static bool voicePlaying;


		private void Update()
		{
			if (Input.GetKeyDown(TriggerVoiceKey.Value.MainKey) && TriggerVoiceKey.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				PlayVoice();
			}
			else if (SpeechControlMode.Value == SpeechMode.Timer)
			{
				if (voiceTimer < Time.time)
				{
					//When timer is up and no voice is currently playing as indicated by the voicePlaying flag, manually play a voice.
					//We don't set the timer yet because we want it to starting counting only after the voice is done playing.
					//But we set voicePlaying to true so that the plugin will not keep trying to call PlayVoice() before the timer is re-initialized.				
					if (!voicePlaying)
					{
						PlayVoice();
						voicePlaying = true;
					}
					//If voicePlaying is already true when the timer is up, then we wait for the voice to finish playing before resetting the timer and the voicePlaying flag.
					//This effectively makes the timer interval set by the user to start at the end of a voice line, 
					//while also having the effect of resetting the timer and voicePlaying flag in case the voice was never played.
					else if (voice.nowVoices[0].state == HVoiceCtrl.VoiceKind.breath && voice.nowVoices[1].state == HVoiceCtrl.VoiceKind.breath)
					{
						SetVoiceTimer();
						voicePlaying = false;
					}
				}


				//In masturbation and lesbian modes, post orgasm cooldown only starts counting if no girl is speaking.
				//When timer is set to low values, this behavior causes it to take forever for action to restart after orgasm.
				//The below block circumvents this behavior by forcing the cooldown timer to keep increasing during post orgasm animation.
				if (flags.nowAnimStateName == "Orgasm_B")
				{
					if (flags.mode == HFlag.EMode.masturbation)
						flags.timeMasturbation.timeIdleCalc += Time.deltaTime;
					else if (flags.mode == HFlag.EMode.lesbian)
						flags.timeLesbian.timeIdleCalc += Time.deltaTime;
				}
			}
			else if (voicePlaying)
			{
				voicePlaying = false;
			}				
		}

		/// <summary>
		/// Trigger a voice line based on the current context
		/// </summary>
		public void PlayVoice()
		{
			//Set the flag used by hooks to force idle voice line playback
			StartCoroutine(ToggleFlagSingleFrame(x => forceIdleVoice = x));

			//Take care of singular/edge cases
			if (hCategory == HCategory.intercourse)
			{
				//Take care of edge cases where there would be no idle voice lines by satifying the conditions for them to be played,
				//by flipping some flags for one frame and returning them to their original values afterward
				string[] afterOrg = new string[] { "IN_A", "A_IN_A", "OUT_A", "A_OUT_A" };
				if (afterOrg.Any(x => flags.nowAnimStateName == x))
				{
					StartCoroutine(ToggleFlagSingleFrame(targetValue => {
						bool oldValue = flags.voice.isAfterVoicePlay;
						flags.voice.isAfterVoicePlay = targetValue;
						return oldValue;
					}, false));

					//Takes care of situations when guy pulls out after ejaculating inside
					if (flags.finish != HFlag.FinishKind.outside && flags.nowAnimStateName.Contains("OUT"))
					{
						int femaleLeadIndex = flags.nowAnimationInfo.isFemaleInitiative ? 38 : 0;
						int condomIndex = flags.isCondom ? 0 : 36;
						switch (flags.mode)
						{
							case HFlag.EMode.sonyu:
								flags.voice.playVoices[0] = 300 + condomIndex + femaleLeadIndex;
								break;
							case HFlag.EMode.sonyu3P:
								flags.voice.playVoices[UnityEngine.Random.Range(0, 2)] = 800 + condomIndex + femaleLeadIndex;
								break;
							case HFlag.EMode.sonyu3PMMF:
								flags.voice.playVoices[0] = 1000 + condomIndex;
								break;
						}
					}
				}
				else
				{
					//Set 70PercentageVoicePlay flag to false to allow idle voice to proc when excitement gauge is over 70.
					//If both male and female gauges are over 70, randomly allow either one to play
					if (flags.gaugeMale >= 70f && flags.gaugeFemale >= 70f)
					{
						int maleORFemale = UnityEngine.Random.Range(0, 2);
						if (maleORFemale == 0)
							flags.voice.isFemale70PercentageVoicePlay = false;
						else
							flags.voice.isMale70PercentageVoicePlay = false;
					}
					else
					{
						flags.voice.isFemale70PercentageVoicePlay = false;
						flags.voice.isMale70PercentageVoicePlay = false;
					}
				}
			}
			else if (hCategory == HCategory.service)
			{
				//Prepare variables to store the indexer values used to manually trigger voice with flags.voice.playVoices.
				//houshi3P is the only mode with two females, and we see whether the flags.nowAnimationInfo.id is even or odd to determine which girl's voice should be triggered
				int voiceFlagIndex = flags.mode == HFlag.EMode.houshi3P ? UnityEngine.Random.Range(0, 2) : 0;
				int voiceIdBase = 198;
				switch (flags.mode)
				{
					case HFlag.EMode.houshi:
						voiceIdBase = 198;
						break;
					case HFlag.EMode.houshi3P:
						voiceIdBase = 700;
						break;
					case HFlag.EMode.houshi3PMMF:
						voiceIdBase = 900;
						break;
				}

				if (flags.nowAnimStateName.Contains("OLoop"))
				{
					if (flags.click == HFlag.ClickKind.inside)
						flags.voice.playVoices[voiceFlagIndex] = voiceIdBase + 7;
					else
						flags.voice.playVoices[voiceFlagIndex] = voiceIdBase + 6;
				}
				else if (flags.nowAnimStateName == "OUT_A")
					flags.voice.playVoices[voiceFlagIndex] = voiceIdBase + 9;
				else if (flags.nowAnimStateName == "Drink_A")
					flags.voice.playVoices[voiceFlagIndex] = voiceIdBase + 10;
				else if (flags.nowAnimStateName == "Vomit_A")
					flags.voice.playVoices[voiceFlagIndex] = voiceIdBase + 11;
			}
			else if (flags.mode == HFlag.EMode.aibu)
			{
				//If currently groping, as indicated by the animation state name ending in "_Idle", 
				// find the first hand that's currently using an item/hand as indicated by an non-empty useItem field,
				// then retrive the kind of object being used as well as the body part being touched, then use those values to determine which voice line to proc.
				if (flags.nowAnimStateName.Contains("_Idle"))
				{
					foreach (object hand in hands)
					{
						object useItem;
						if (isVR)
							useItem = Traverse.Create(hand).Field("useItem").GetValue<object>();
						else
							useItem = Traverse.Create(hand).Field("useItems").GetValue<object[]>().FirstOrDefault(x => x != null);

						if (useItem != null)
						{
							var idObj = Traverse.Create(useItem).Property("idObj").GetValue<int>();
							var kindTouch = Traverse.Create(useItem).Field("kindTouch").GetValue<int>();
							int[,] voicePattern = new int[6, 5]
							{
								{ -1, 112, 114, 116, 118 },
								{ -1, 124, 120, 122, -1 },
								{ -1, 132, 126, 128, 130 },
								{ -1, 138, 134, -1, 136 },
								{ -1, -1, 140, -1, -1 },
								{ -1, -1, -1, -1, -1 }
							};
							int[] touchArea = new int[7] { 0, 1, 1, 2, 3, 4, 4 };

							//Clamp the indexer variables to avoid index out of range exception
							flags.voice.playVoices[0] = voicePattern[Mathf.Clamp(idObj, 0, 5), touchArea[Mathf.Clamp(kindTouch - 1, 0, 6)]];
							break;
						}
					}
				}
				//Proc a fixed line if after orgasm, or use the idle line for all other situations.
				else if (flags.nowAnimStateName == "Orgasm_A")
					flags.voice.playVoices[0] = 143;
			}
			else if (flags.mode == HFlag.EMode.lesbian || flags.mode == HFlag.EMode.masturbation)
			{
				//Based on the current mode, prepare variables to store the indexer values used to manually trigger voice with flags.voice.playVoices.
				//Randomly choose one of the two females to speak if in lesbian mode.
				//Note that UnityEngine.Random.Range's upper bound is EXCLUSIVE instead of inclusive like the lower bound, for fuck's sake.
				int voiceFlagIndex = 0;
				int[] voicePattern = { 400, 402, 403, 404, 405 };

				if (flags.mode == HFlag.EMode.lesbian)
				{
					voiceFlagIndex = UnityEngine.Random.Range(0, 2);
					voicePattern = new int[5] { 600, 601, 602, 603, 606 };
				}

				if (flags.nowAnimStateName == "WLoop" || flags.nowAnimStateName == "MLoop")
					flags.voice.playVoices[voiceFlagIndex] = voicePattern[UnityEngine.Random.Range(0, 2)];
				else if (flags.nowAnimStateName == "SLoop" || flags.nowAnimStateName == "OLoop")
					flags.voice.playVoices[voiceFlagIndex] = voicePattern[UnityEngine.Random.Range(2, 4)];
				// After orgasm, there is only one female, then proc line 405. 
				// If there are two females, then proc either 606 for the first female, or 605 for the second female. (There are no line 605 for the first female, or 606 for the second)
				else if (flags.nowAnimStateName.Contains("Orgasm_"))
					flags.voice.playVoices[voiceFlagIndex] = voicePattern[4] - voiceFlagIndex;
			}
		}

		internal static void SetVoiceTimer(float deviation = 1.5f)
		{
			voiceTimer = Time.time + Math.Max(voiceMinInterval, UnityEngine.Random.Range(SpeechTimer.Value - deviation, SpeechTimer.Value + deviation));
		}
	}
}
