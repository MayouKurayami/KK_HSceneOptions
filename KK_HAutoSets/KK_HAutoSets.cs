using BepInEx;
using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace KK_HAutoSets
{
	[BepInPlugin(GUID, PluginName, Version)]
	[BepInProcess("Koikatu")]
	[BepInProcess("KoikatuVR")]
	[BepInProcess("Koikatsu Party")]
	[BepInProcess("Koikatsu Party VR")]
	public class KK_HAutoSets : BaseUnityPlugin
	{
		public const string GUID = "MK.KK_HAutoSets";
		public const string PluginName = "HAutoSets";
		public const string AssembName = "KK_HAutoSets";
		public const string Version = "1.3.0";

		internal static HFlag flags;
		internal static List<HActionBase> lstProc;
		internal static HActionBase proc;
		internal static ChaControl female;
		private static string animationName = "";

		internal static bool forceOLoop;
		internal static AnimatorStateInfo sLoopInfo;
		private static bool malePresent;
		internal static bool forceStopVoice;
		private float orgasmTimer;

		private delegate bool LoopProc(bool _loop);
		private static LoopProc loopProcDelegate;


		/// 
		/////////////////// Excitement Gauge //////////////////////////
		/// 
		[Category("Excitement Gauge")]
		[DisplayName("Auto Lock Female Gauge")]
		[Description("Auto lock female gauge at H start")]
		public static ConfigWrapper<bool> LockFemaleGauge { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Auto Lock Male Gauge")]
		[Description("Auto lock male gauge at H start")]
		public static ConfigWrapper<bool> LockMaleGauge { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Female Excitement Gauge Minimum Value")]
		[Description("Female exceitement gauge will not fall below this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<int> FemaleGaugeMin { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Female Excitement Gauge Maximum Value")]
		[Description("Female excitement gauge will not go above this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<int> FemaleGaugeMax { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Male Excitement Gauge Minimum Value")]
		[Description("Male exceitement gauge will not fall below this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<int> MaleGaugeMin { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Male Excitement Gauge Maximum Value")]
		[Description("Male exceitement gauge will not go above this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<int> MaleGaugeMax { get; private set; }

		/// 
		/////////////////// Keyboard Shortcuts //////////////////////////
		/// 
		[DisplayName("Precum Loop Toggle")]
		[Description("Press this key to enter/exit precum animation")]
		public static SavedKeyboardShortcut OLoopKey { get; private set; }

		[DisplayName("Orgasm Inside")]
		[Description("Press this key to manually cum inside with the specified amount of time in precum")]
		public static SavedKeyboardShortcut OrgasmInsideKey { get; private set; }

		[DisplayName("Orgasm Outside")]
		[Description("Press this key to manually cum outside with the specified amount of time in precum")]
		public static SavedKeyboardShortcut OrgasmOutsideKey { get; private set; }


		/// 
		/////////////////// Others //////////////////////////
		/// 
		[DisplayName("Auto Equip Sub-Accessories")]
		[Description("Auto equip sub-accessories at H start")]
		public static ConfigWrapper<bool> SubAccessories { get; private set; }

		[DisplayName("Hide Shadow Casted by Male Body")]
		[Description("Hide shadow casted by male body")]
		public static ConfigWrapper<bool> HideMaleShadow { get; private set; }

		[DisplayName("Hide Shadow Casted by Female Limbs and Accessories")]
		[Description("Hide shadow casted by female limbs and accessories. This does not affect shadows casted by the head or hair")]
		public static ConfigWrapper<bool> HideFemaleShadow { get; private set; }

		[DisplayName("Disable Hiding of Male Body When Groping")]
		[Description("If enabled, the male body will not be hidden when touching the girl during sex or service")]
		public static ConfigWrapper<bool> DisableHideBody { get; private set; }

		[DisplayName("Precum Timer")]
		[Description("When orgasm is triggered via the keyboard shortcut, animation will forcibly exit precum and enter orgasm after this many seconds. \nSet to 0 to disable this.")]
		public static ConfigWrapper<float> PrecumTimer { get; private set; }

		private void Start()
		{
			LockFemaleGauge = new ConfigWrapper<bool>(nameof(LockFemaleGauge), this, false);
			LockMaleGauge = new ConfigWrapper<bool>(nameof(LockMaleGauge), this, false);
			FemaleGaugeMin = new ConfigWrapper<int>(nameof(FemaleGaugeMin), this, 0);
			FemaleGaugeMax = new ConfigWrapper<int>(nameof(FemaleGaugeMax), this, 100);
			MaleGaugeMin = new ConfigWrapper<int>(nameof(MaleGaugeMin), this, 0);
			MaleGaugeMax = new ConfigWrapper<int>(nameof(MaleGaugeMax), this, 100);
			SubAccessories = new ConfigWrapper<bool>(nameof(SubAccessories), this, false);
			HideMaleShadow = new ConfigWrapper<bool>(nameof(HideMaleShadow), this, false);
			HideFemaleShadow = new ConfigWrapper<bool>(nameof(HideFemaleShadow), this, false);
			DisableHideBody = new ConfigWrapper<bool>(nameof(DisableHideBody), this, false);
			PrecumTimer = new ConfigWrapper<float>(nameof(PrecumTimer), this, 0);

			OLoopKey = new SavedKeyboardShortcut(nameof(OLoopKey), this, new KeyboardShortcut(KeyCode.None));
			OrgasmInsideKey = new SavedKeyboardShortcut(nameof(OrgasmInsideKey), this, new KeyboardShortcut(KeyCode.None));
			OrgasmOutsideKey = new SavedKeyboardShortcut(nameof(OrgasmOutsideKey), this, new KeyboardShortcut(KeyCode.None));

			sLoopInfo = new AnimatorStateInfo();
			object dummyInfo = sLoopInfo;
			Traverse.Create(dummyInfo).Field("m_Name").SetValue(-1715982390);
			Traverse.Create(dummyInfo).Field("m_SpeedMultiplier").SetValue(3f);
			Traverse.Create(dummyInfo).Field("m_Speed").SetValue(1f);
			Traverse.Create(dummyInfo).Field("m_Loop").SetValue(1);
			Traverse.Create(dummyInfo).Field("m_NormalizedTime").SetValue(59.73729f);
			Traverse.Create(dummyInfo).Field("m_Length").SetValue(0.4444448f);
			sLoopInfo = (AnimatorStateInfo)dummyInfo;

			//Harmony patching
			HarmonyInstance harmony = HarmonyInstance.Create(GUID);
			harmony.PatchAll(typeof(Hooks));

			if (Application.dataPath.EndsWith("KoikatuVR_Data"))
				harmony.PatchAll(typeof(VRHooks));
		}

		private void Update()
		{
			if (!flags)
				return;

			if (animationName != flags.nowAnimationInfo.nameAnimation)
				UpdateProc();

			if (Input.GetKeyDown(OLoopKey.Value.MainKey) && OLoopKey.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				//Only allow forced entering of precum loop if currently in piston loop, to prevent issues with unintended orgasm caused by entering precum loop elsewhere
				if (!forceOLoop && (flags.nowAnimStateName.Contains("SLoop") || flags.nowAnimStateName.Contains("WLoop") || flags.nowAnimStateName.Contains("MLoop")))
				{
					flags.speedCalc = 1f;
					proc.SetPlay(flags.isAnalPlay ? "A_OLoop" : "OLoop", true);
					forceOLoop = true;
				}
				else if (flags.nowAnimStateName.Contains("OLoop"))
				{
					proc.SetPlay(flags.isAnalPlay ? "A_SLoop" : "SLoop", true);
					forceOLoop = false;
				}
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

				if (malePresent)
					loopProcDelegate?.Invoke(true);			
			}

			//if orgasm countdown timer is initialized and the time since its intialization has past the configured amount, 
			//stop the currently playing speech to allow entering orgasm, then reset counter to 0.
			//As precaution against extravagant timer values and consequently stopping voice after orgasm has already finished, only run this if still in precum loop.
			if (orgasmTimer > 0 && (Time.time - orgasmTimer) > PrecumTimer.Value)
			{
				if (flags.nowAnimStateName.Contains("OLoop"))
					StartCoroutine(VoiceStop());

				orgasmTimer = 0;
			}
		}

		/// <summary>
		/// Update proc field to reflect the current active H mode, and point loopProcDelegate to the correct LoopProc method in modes where it exists
		/// </summary>
		private static void UpdateProc()
		{
			MethodInfo loopProcInfo;
			Type procType = typeof(HSonyu);

			switch (flags.mode)
			{
				case (HFlag.EMode.sonyu):
					proc = lstProc.OfType<HSonyu>().FirstOrDefault();
					procType = typeof(HSonyu);
					break;
				case (HFlag.EMode.houshi):
					proc = lstProc.OfType<HHoushi>().FirstOrDefault();
					procType = typeof(HHoushi);
					break;
				case (HFlag.EMode.houshi3P):
					proc = lstProc.OfType<H3PHoushi>().FirstOrDefault();
					procType = typeof(H3PHoushi);
					break;
				case (HFlag.EMode.houshi3PMMF):
					proc = lstProc.OfType<H3PDarkHoushi>().FirstOrDefault();
					procType = typeof(H3PDarkHoushi);
					break;
				case (HFlag.EMode.aibu):
					proc = lstProc.OfType<HAibu>().FirstOrDefault();
					break;
				case (HFlag.EMode.lesbian):
					proc = lstProc.OfType<HLesbian>().FirstOrDefault();
					break;
				case (HFlag.EMode.masturbation):
					proc = lstProc.OfType<HMasturbation>().FirstOrDefault();
					break;
				case (HFlag.EMode.sonyu3P):
					proc = lstProc.OfType<H3PSonyu>().FirstOrDefault();
					procType = typeof(H3PSonyu);
					break;
				case (HFlag.EMode.sonyu3PMMF):
					proc = lstProc.OfType<H3PDarkSonyu>().FirstOrDefault();
					procType = typeof(H3PDarkSonyu);
					break;
				default:
					proc = lstProc.OfType<HSonyu>().FirstOrDefault();
					break;
			}

			if (flags.mode != HFlag.EMode.aibu && flags.mode != HFlag.EMode.lesbian && flags.mode != HFlag.EMode.masturbation)
			{
				loopProcInfo = AccessTools.Method(procType, "LoopProc", new Type[] { typeof(bool) });
				loopProcDelegate = (LoopProc)Delegate.CreateDelegate(typeof(LoopProc), proc, loopProcInfo);

				malePresent = true;
			}
			else
				malePresent = false;
		}

		/// <summary>
		/// Manually start orgasm accordindg to current the condition and initialize cum countdown timer 
		/// </summary>
		/// <param name="inside">Whether the to cum inside or not</param>
		private void ManualOrgasm(bool inside)
		{
			//In piston (intercourse) modes, set the cum click value to be processed by the game, 
			//then set the voice state of all females to breath to allow the game to interrupt currently playing speech
			//Then run the LoopProc method twice. The first call allows the game to set the correct HFlag.finish value based on the click value we sent, 
			//and the second call allows the game to enter OLoop and play back the corresponding speech based on the HFlag.finish value set in the previous call
			if (flags.mode == HFlag.EMode.sonyu || flags.mode == HFlag.EMode.sonyu3P || flags.mode == HFlag.EMode.sonyu3PMMF)
			{
				flags.click = inside ? HFlag.ClickKind.inside : HFlag.ClickKind.outside;

				HVoiceCtrl voiceCtrl = Traverse.Create(proc).Field("voice").GetValue<HVoiceCtrl>();
				foreach (HVoiceCtrl.Voice voice in voiceCtrl.nowVoices)
					voice.state = HVoiceCtrl.VoiceKind.breath;

				for (int i = 0; i < 2; i++)
					loopProcDelegate?.Invoke(true);
			}
			//Outside of intercourse, if in service or female only modes, manually enter precum loop (OLoop) to allow the game to start looking at parameters related to entering orgasm.
			//Then use a coroutine to set the cum click value after animation crossfade into OLoop is over, since during which the click value would be ignored and lost.
			else if (flags.mode > HFlag.EMode.aibu)
			{
				proc.SetPlay("OLoop", false);
				StartCoroutine(DelayOrgasmClick(inside));
			}
			//In aibu mode, simply set the cum click value to enter orgasm immediately.
			else
			{
				flags.click = HFlag.ClickKind.orgW;
			}

			//Initiate timer if value is greater than 0 and male is present.
			//No point in delaying orgasm when there is no male to sychronize to.
			if (PrecumTimer.Value > 0 && malePresent)
				orgasmTimer = Time.time;
		}

		/// <summary>
		/// Wait for current animation transition to finish, then trigger orgasm according to the current condition
		/// </summary>
		/// <param name="inside">Whether to cum inside or not</param>
		/// <returns></returns>
		private IEnumerator DelayOrgasmClick(bool inside)
		{
			yield return new WaitUntil(() => female?.animBody.GetCurrentAnimatorStateInfo(0).IsName(flags.nowAnimStateName) ?? true);

			//* In modes where male is not present (masturbation and lesbian), the condition to trigger orgasm is for the current speech to finish.
			//  Stop the voice immediately to trigger orgasm immediately as the timer wouldn't be initialized in those modes.
			//* In other modes, simply set the cum click flag to trigger orgasm
			if (!malePresent)
				StartCoroutine(VoiceStop());
			else
				flags.click = inside ? HFlag.ClickKind.inside : HFlag.ClickKind.outside;
		}

		/// <summary>
		/// Interrupt current playing speech by forcing the game to behave as if no speech is playing for exactly one frame. Depends on the IsVoiceCheckPre patch to work.
		/// </summary>
		/// <returns></returns>
		private IEnumerator VoiceStop()
		{
			forceStopVoice = true;
			yield return null;
			forceStopVoice = false;
		}

		/// <summary>
		/// Function to equip all accessories
		/// </summary>
		internal static void EquipAllAccessories(List<ChaControl> females)
		{
			if (SubAccessories.Value)
			{
				foreach (ChaControl chaCtrl in females)
					chaCtrl.SetAccessoryStateAll(true);
			}
		}

		/// <summary>
		///Function to lock female/male gauge depending on config
		/// </summary>
		internal static void LockGaugesAction(HSprite hSprite)
		{
			if (LockFemaleGauge.Value)
			{
				hSprite.OnFemaleGaugeLockOnGauge();
				hSprite.flags.lockGugeFemale = true;
			}

			if (LockMaleGauge.Value)
			{
				hSprite.OnMaleGaugeLockOnGauge();
				hSprite.flags.lockGugeMale = true;
			}
		}

		/// <summary>
		///Function to disable shadow from male body
		/// </summary>
		internal static void HideShadow(List<ChaControl> males, List<ChaControl> females = null)
		{
			if (HideMaleShadow.Value)
			{
				foreach (ChaControl male in males)
				{
					if (male)
					{
						foreach (Renderer mesh in male.objRoot.GetComponentsInChildren<Renderer>(true))
						{
							if (mesh.name != "o_shadowcaster_cm")
								mesh.shadowCastingMode = 0;
						}		
					}				
				}	
				
				if(females != null && HideFemaleShadow.Value)
				{
					foreach (ChaControl female in females)
					{
						foreach (Transform child in female.objTop.transform)
						{
							if (child.name == "p_cf_body_bone")
							{
								foreach (MeshRenderer mesh in child.GetComponentsInChildren<MeshRenderer>(true))
										mesh.shadowCastingMode = 0;
							}
							else
							{
								foreach (SkinnedMeshRenderer mesh in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
								{
									if (mesh.name != "o_shadowcaster")
										mesh.shadowCastingMode = 0;
								}
							}
						}
					}
				}
			}

		}

		/// <summary>
		/// Function to limit excitement gauges based on configured values
		/// </summary>
		internal static void GaugeLimiter()
		{
			if (FemaleGaugeMax.Value >= FemaleGaugeMin.Value)
				flags.gaugeFemale = Mathf.Clamp(flags.gaugeFemale, FemaleGaugeMin.Value, FemaleGaugeMax.Value);
			if (MaleGaugeMax.Value >= MaleGaugeMin.Value)
				flags.gaugeMale = Mathf.Clamp(flags.gaugeMale, FemaleGaugeMin.Value, MaleGaugeMax.Value);
		}
	}
}
