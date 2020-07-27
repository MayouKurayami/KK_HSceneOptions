using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static ChaFileDefine;
using Manager;

namespace KK_HAutoSets
{
	[BepInPlugin(GUID, PluginName, Version)]
	[BepInProcess("Koikatu")]
	[BepInProcess("KoikatuVR")]
	[BepInProcess("Koikatsu Party")]
	[BepInProcess("Koikatsu Party VR")]
	public class HAutoSets : BaseUnityPlugin
	{
		public const string GUID = "MK.KK_HAutoSets";
		public const string PluginName = "HAutoSets";
		public const string AssembName = "KK_HAutoSets";
		public const string Version = "2.0.5";
		internal const float voiceMinInterval = 3f;
		internal const float voiceMaxInterval = 60f;

		internal static bool isVR;

		internal static HFlag flags;
		internal static List<HActionBase> lstProc;
		internal static List<ChaControl> lstFemale;
		internal static List<HSprite> sprites = new List<HSprite>();
		internal static HVoiceCtrl voice;
		internal static object[] hands = new object[2];
		internal static AnimationToggle animationToggle;
		internal static HCategory hCategory;

		internal static bool forceIdleVoice;
		internal static float voiceTimer = voiceMinInterval;


		public static ConfigEntry<bool> SubAccessories { get; private set; }
		public static ConfigEntry<bool> HideMaleShadow { get; private set; }
		public static ConfigEntry<bool> HideFemaleShadow { get; private set; }
		public static ConfigEntry<bool> DisableHideBody { get; private set; }

		public static ConfigEntry<bool> LockFemaleGauge { get; private set; }
		public static ConfigEntry<bool> LockMaleGauge { get; private set; }
		public static ConfigEntry<int> FemaleGaugeMin { get; private set; }
		public static ConfigEntry<int> FemaleGaugeMax { get; private set; }
		public static ConfigEntry<int> MaleGaugeMin { get; private set; }
		public static ConfigEntry<int> MaleGaugeMax { get; private set; }

		public static ConfigEntry<SpeechMode> AutoVoice { get; private set; }
		public static ConfigEntry<float> AutoVoiceTime { get; private set; }

		public static ConfigEntry<bool> VRResetCamera { get; private set; }

		public static ConfigEntry<bool> DisableAutoPrecum { get; private set; }
		public static ConfigEntry<bool> PrecumToggle { get; private set; }
		public static ConfigEntry<float> PrecumTimer { get; private set; }

		public static ConfigEntry<KeyboardShortcut> InsertWaitKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> InsertNowKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> OrgasmInsideKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> OrgasmOutsideKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> OLoopKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> SpitKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> SwallowKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> BottomClothesToggleKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> PantsuStripKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> SubAccToggleKey { get; private set; }
		public static ConfigEntry<KeyboardShortcut> TopClothesToggleKey { get; private set; }		
		public static ConfigEntry<KeyboardShortcut> TriggerVoiceKey { get; private set; }


		private void Start()
		{
			/// 
			/////////////////// Miscellaneous //////////////////////////
			/// 

			SubAccessories = Config.Bind(
				section: "", 
				key: "Auto Equip Sub-Accessories", 
				defaultValue: false,
				"Auto equip sub-accessories at H start");

			DisableHideBody = Config.Bind(
				section: "",
				key: "Disable Hiding of Male Body When Groping",
				defaultValue: false,
				"If enabled, the male body will not be hidden when touching the girl during sex or service");
			
			HideFemaleShadow = Config.Bind(
				section: "",
				key: "Hide Shadow Casted by Female Limbs and Accessories",
				defaultValue: false,
				"Hide shadow casted by female limbs and accessories. This does not affect shadows casted by the head or hair");

			HideMaleShadow = Config.Bind(
				section: "",
				key: "Hide Shadow Casted by Male Body",
				defaultValue: false);		

			/// 
			/////////////////// Excitement Gauge //////////////////////////
			/// 
			LockFemaleGauge = Config.Bind(
				section: "Excitement Gauge",
				key: "Auto Lock Female Gauge",
				defaultValue: false,
				"Auto lock female gauge at H start");

			LockMaleGauge = Config.Bind(
				section: "Excitement Gauge",
				key: "Auto Lock Male Gauge",
				defaultValue: false,
				"Auto lock male gauge at H start");

			FemaleGaugeMin = Config.Bind(
				section: "Excitement Gauge",
				key: "Female Excitement Gauge Minimum Value",
				defaultValue: 0, 
				new ConfigDescription("Female exceitement gauge will not fall below this value",
					new AcceptableValueRange<int>(0, 100)));

			FemaleGaugeMax = Config.Bind(
				section: "Excitement Gauge",
				key: "Female Excitement Gauge Maximum Value",
				defaultValue: 100,
				new ConfigDescription("Female excitement gauge will not go above this value",
					new AcceptableValueRange<int>(0, 100)));

			MaleGaugeMin = Config.Bind(
				section: "Excitement Gauge",
				key: "Male Excitement Gauge Minimum Value",
				defaultValue: 0,
				new ConfigDescription("Male exceitement gauge will not fall below this value",
					new AcceptableValueRange<int>(0, 100)));

			MaleGaugeMax = Config.Bind(
				section: "Excitement Gauge",
				key: "Male Excitement Gauge Maximum Value",
				defaultValue: 100,
				new ConfigDescription("Male excitement gauge will not go above this value",
					new AcceptableValueRange<int>(0, 100)));

			/// 
			/////////////////// Female Speech //////////////////////////
			/// 

			AutoVoice = Config.Bind(
				section: "Female Speech",
				key: "Speech Control",
				defaultValue: SpeechMode.Disabled,
				"Default Behavior: Disable this feature and return to vanilla behavior" +
					"\n\nBased on Timer: Automatically trigger speech at set interval" +
					"\n\nMute Idle Speech: Prevent the girl from speaking at all during idle (she would still speak during events such as insertion)" +
					"\n\nMute All Spoken Lines: Mute all speech other than moans"); 

			AutoVoiceTime = Config.Bind(
				section: "Female Speech",
				key: "Speech Timer  (Effective only if Speech Control is set to Based on Timer)",
				defaultValue: 20f,
				new ConfigDescription("Sets the time interval at which the girl will randomly speak. In seconds.",
					new AcceptableValueRange<float>(voiceMinInterval, voiceMaxInterval)));

			AutoVoiceTime.SettingChanged += (sender, args) => { SetVoiceTimer(2f); };

			/// 
			/////////////////// VR //////////////////////////
			/// 

			VRResetCamera = Config.Bind(
				section: "Official VR",
				key: "Reset Camera At Position Change",
				defaultValue: true,
				"Resets the camera back to the male's head when switching to a different position in official VR.");

			/// 
			/////////////////// Precum Related //////////////////////////
			/// 

			DisableAutoPrecum = Config.Bind(
				section: "Force Precum",
				key: "Disable Auto Finish in Service Mode",
				defaultValue: false,
				"If enabled, animation will not automatically enter the fast precum animation when male's excitement gauge is past the 70% threshold");

			PrecumTimer = Config.Bind(
				section: "Force Precum",
				key: "Precum Timer",
				defaultValue: 0f,
				new ConfigDescription("When orgasm is initiated via the keyboard shortcuts or in-game menu, animation will forcibly exit precum and enter orgasm after this many seconds. " +
						"\n\nSet to 0 to disable this.",
					new AcceptableValueRange<float>(0, 13f)));

			PrecumToggle = Config.Bind(
				section: "Force Precum",
				key: "Precum Toggle",
				defaultValue: false,
				"Allow toggling throhgh precum loop when right clicking the speed control pad." +
					"\n\nToggle order: weak motion > strong motion > precum > back to weak motion");

			/// 
			/////////////////// Keyboard Shortcuts //////////////////////////
			/// 

			TopClothesToggleKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Toggle Top Clothes",
				defaultValue: KeyboardShortcut.Empty,
				new ConfigDescription("Toggle through states of the top clothes of the main female, including top cloth and bra at the same time.",
					acceptableValues: null, 
					new ConfigurationManagerAttributes { Order = 3 }));

			BottomClothesToggleKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Toggle Bottom Clothes",
				defaultValue: KeyboardShortcut.Empty,
				new ConfigDescription("Toggle through states of the bottom cloth (skirt, pants...etc) of the main female.",
					acceptableValues: null, 
					new ConfigurationManagerAttributes { Order = 2 }));

			PantsuStripKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Toggle Pantsu Stripped/Half Stripped",
				defaultValue: KeyboardShortcut.Empty,
				new ConfigDescription("Toggle between a fully stripped and a partially stripped pantsu." +
						"\n(You would not be able to fully dress the pantsu with this shortcut)",
					acceptableValues: null, 
					new ConfigurationManagerAttributes { Order = 1 }));

			InsertWaitKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Insert After Asking Female",
				defaultValue: KeyboardShortcut.Empty,
				"Insert male genital after female speech");

			InsertNowKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Insert Without Asking",
				defaultValue: KeyboardShortcut.Empty,
				"Insert male genital without asking for permission");

			OrgasmInsideKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Orgasm Inside",
				defaultValue: KeyboardShortcut.Empty,
				"Press this key to manually cum inside mouth or vagina");

			OrgasmOutsideKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Orgasm Outside",
				defaultValue: KeyboardShortcut.Empty,
				"Press this key to manually cum outside of mouth or vagina");

			OLoopKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Precum Loop Toggle",
				defaultValue: KeyboardShortcut.Empty,
				"Press this key to enter/exit precum animation");

			SpitKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Spit Out",
				defaultValue: KeyboardShortcut.Empty,
				"Press this key to make female spit out after blowjob");

			SwallowKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Swallow",
				defaultValue: KeyboardShortcut.Empty,
				"Press this key to make female swallow after blowjob");

			SubAccToggleKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Toggle Sub-Accessories",
				defaultValue: KeyboardShortcut.Empty,
				"Toggle the display of sub-accessories");

			TriggerVoiceKey = Config.Bind(
				section: "Keyboard Shortcut",
				key: "Trigger Speech",
				defaultValue: KeyboardShortcut.Empty,
				"Trigger a voice line based on the current context");



			//Harmony patching
			HarmonyWrapper.PatchAll(typeof(Hooks));

			if (isVR = Application.dataPath.EndsWith("KoikatuVR_Data"))
				HarmonyWrapper.PatchAll(typeof(Hooks_VR));

			if (Type.GetType("H3PDarkSonyu, Assembly-CSharp") != null)
				HarmonyWrapper.PatchAll(typeof(Hooks_Darkness));
		}		

		private void Update()
		{
			if (!flags)
				return;

			if (Input.GetKeyDown(InsertWaitKey.Value.MainKey) && InsertWaitKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertClick();
			else if (Input.GetKeyDown(InsertNowKey.Value.MainKey) && InsertNowKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertNoVoiceClick();
			else if (Input.GetKeyDown(SwallowKey.Value.MainKey) && SwallowKey.Value.Modifiers.All(x => Input.GetKey(x)))
				flags.click = HFlag.ClickKind.drink;
			else if (Input.GetKeyDown(SpitKey.Value.MainKey) && SpitKey.Value.Modifiers.All(x => Input.GetKey(x)))
				flags.click = HFlag.ClickKind.vomit;
			
			if (Input.GetKeyDown(SubAccToggleKey.Value.MainKey) && SubAccToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				ToggleMainGirlAccessories(category: 1);
			if (Input.GetKeyDown(TriggerVoiceKey.Value.MainKey) && TriggerVoiceKey.Value.Modifiers.All(x => Input.GetKey(x)))
				PlayVoice();
			else if (AutoVoice.Value == SpeechMode.Timer)
			{
				voiceTimer -= Time.deltaTime;

				if (voiceTimer <= 0 && voice.nowVoices[0].state < HVoiceCtrl.VoiceKind.startvoice && voice.nowVoices[1]?.state < HVoiceCtrl.VoiceKind.startvoice)
				{
					PlayVoice();
					SetVoiceTimer(2f);
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

			if (Input.GetKeyDown(PantsuStripKey.Value.MainKey) && PantsuStripKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.shorts }, true);
			if (Input.GetKeyDown(TopClothesToggleKey.Value.MainKey) && TopClothesToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.top, ClothesKind.bra });
			if (Input.GetKeyDown(BottomClothesToggleKey.Value.MainKey) && BottomClothesToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.bot });
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
		/// Toggle a boolean flag to true for one frame then toggle it back to false
		/// </summary>
		/// <param name="toggleFlag">The action used to assign the target of the toggle</param>
		internal static IEnumerator ToggleFlagSingleFrame(Action<bool> toggleFlag)
		{
			toggleFlag(true);
			yield return null;
			toggleFlag(false);
		}

		/// <summary>
		/// Modify a flag to the targetValue using the supplied delegate. Wait for one frame then restore back to its original value. Can only be used with value types.
		/// </summary>
		/// <param name="toggleFlagFunc">Delegate for toggling the flag and returning its original value</param>
		/// <param name="targetValue">The value for the flag to be toggled to</param>
		/// <returns></returns>
		internal static IEnumerator ToggleFlagSingleFrame<T>(Func<T, T> toggleFlagFunc, T targetValue) where T : struct
		{
			T originalValue = toggleFlagFunc(targetValue);
			yield return null;
			toggleFlagFunc(originalValue);
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

		private void OnInsertNoVoiceClick()
		{
			int num = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? (flags.nowAnimationInfo.id % 2) : 0;
			if (flags.mode != HFlag.EMode.sonyu3PMMF)
			{
				if (flags.isInsertOK[num] || flags.isDebug)
				{
					flags.click = HFlag.ClickKind.insert_voice;
					return;
				}
				if (flags.isCondom)
				{
					flags.click = HFlag.ClickKind.insert_voice;
					return;
				}
				flags.AddNotCondomPlay();
				int num2 = ((flags.mode == HFlag.EMode.sonyu3P) ? ((!flags.nowAnimationInfo.isFemaleInitiative) ? 500 : 538) : ((Game.isAdd20 && flags.nowAnimationInfo.isFemaleInitiative) ? 38 : 0));
				flags.voice.playVoices[num] = 302 + num2;
				flags.voice.SetSonyuIdleTime();
				flags.isDenialvoiceWait = true;
				if (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P)
				{
					int num3 = num ^ 1;
					if (voice.nowVoices[num3].state == HVoiceCtrl.VoiceKind.voice && Singleton<Voice>.Instance.IsVoiceCheck(flags.transVoiceMouth[num3]))
					{
						Singleton<Voice>.Instance.Stop(flags.transVoiceMouth[num3]);
					}
				}
			}
			else
			{
				flags.click = HFlag.ClickKind.insert_voice;
			}
		}

		private void OnInsertClick()
		{
			int num2 = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? (flags.nowAnimationInfo.id % 2) : 0;
			int num = ((flags.mode == HFlag.EMode.sonyu3P) ? ((!flags.nowAnimationInfo.isFemaleInitiative) ? 500 : 538) : ((Game.isAdd20 && flags.nowAnimationInfo.isFemaleInitiative) ? 38 : 0));
			if (flags.mode != HFlag.EMode.sonyu3PMMF)
			{
				if (flags.isInsertOK[num2] || flags.isDebug)
				{
					flags.click = HFlag.ClickKind.insert;
					flags.voice.playVoices[num2] = 301 + num;
				}
				else if (flags.isCondom)
				{
					flags.click = HFlag.ClickKind.insert;
					flags.voice.playVoices[num2] = 301 + num;
				}
				else
				{
					flags.AddNotCondomPlay();
					flags.voice.playVoices[num2] = 302 + num;
					flags.voice.SetSonyuIdleTime();
					flags.isDenialvoiceWait = true;
				}
			}
			else
			{
				flags.click = HFlag.ClickKind.insert;
				flags.voice.playVoices[num2] = 1001;
			}
			if (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P)
			{
				int num3 = num2 ^ 1;
				if (voice.nowVoices[num3].state == HVoiceCtrl.VoiceKind.voice && Singleton<Voice>.Instance.IsVoiceCheck(flags.transVoiceMouth[num3]))
				{
					Singleton<Voice>.Instance.Stop(flags.transVoiceMouth[num3]);
				}
			}
		}

		private void ToggleMainGirlAccessories(int category)
		{
			//In modes with two females, use flags.nowAnimationInfo.id to determine which girl's accessories should be affected.
			ChaControl mainFemale = lstFemale[(flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? flags.nowAnimationInfo.id % 2 : 0];
			bool currentStatus = false;

			for (int i = 0; i < mainFemale.nowCoordinate.accessory.parts.Length; i++)
			{
				if (mainFemale.nowCoordinate.accessory.parts[i].hideCategory == category)
				{
					currentStatus = mainFemale.fileStatus.showAccessory[i];
					break;
				}		
			}

			mainFemale.SetAccessoryStateCategory(category, !currentStatus);
		}

		/// <summary>
		/// Trigger a voice line based on the current context
		/// </summary>
		private void PlayVoice()
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
							var idObj = Traverse.Create(useItem).Field("idObj").GetValue<int>();
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
				int[] voicePattern = { 400, 402, 403, 404, 405};

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

		/// <summary>
		/// Toggle through the states of the kinds of clothes provided in the parameter while keeping their states synchronized, using the state of the first cloth in the parameter as basis.
		/// </summary>
		/// <param name="clotheSelection">The list of clothes that should be affected.</param>
		/// <param name="partialOnly">Whether to toggle through fully dressed and fully stripped states</param>
		private void SetClothesStateRange(ClothesKind[] clotheSelection, bool partialOnly = false)
		{
			//In modes with two females, use flags.nowAnimationInfo.id to determine which girl's clothes should be affected.
			int femaleIndex = (flags.mode == HFlag.EMode.houshi3P || flags.mode == HFlag.EMode.sonyu3P) ? flags.nowAnimationInfo.id % 2 : 0;

			//Trigger the next state of the first cloth provided by the parameters, then use its state to synchronize other clothes in the parameter.
			//  If partialOnly is true, only toggle between the two paritally stripped states
			byte state = lstFemale[femaleIndex].fileStatus.clothesState[(int)clotheSelection[0]];
			if (partialOnly)
				lstFemale[femaleIndex].SetClothesState((int)clotheSelection[0], (byte)((state % 2) + 1), false);
			else
				lstFemale[femaleIndex].SetClothesStateNext((int)clotheSelection[0]);

			state = lstFemale[femaleIndex].fileStatus.clothesState[(int)clotheSelection[0]];

			for (int i = 1; i < clotheSelection.Length; i++)
				lstFemale[femaleIndex].SetClothesState((int)clotheSelection[i], state, next: false);
		}


		internal static void SetVoiceTimer(float deviation)
		{
			voiceTimer = Math.Max(voiceMinInterval, UnityEngine.Random.Range(AutoVoiceTime.Value - deviation, AutoVoiceTime.Value + deviation));
		}

		private enum ClothesState
		{
			Full,
			Open1,
			Open2,
			Nude
		}

		public enum SpeechMode
		{
			[Description("Default Behavior")]
			Disabled,
			[Description("Based on Timer")]
			Timer,
			[Description("Mute Idle Speech")]
			MuteIdle,
			[Description("Mute All Spoken Lines")]
			MuteAll
		}

		internal enum HCategory
		{
			service,
			intercourse,
			maleNotVisible
		}
	}
}
