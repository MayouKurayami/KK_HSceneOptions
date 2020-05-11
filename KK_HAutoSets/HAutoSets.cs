using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Manager;
using System.Linq;

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
		public const string Version = "1.3.0";

		internal static HFlag flags;
		internal static List<HActionBase> lstProc;
		internal static HActionBase proc;
		internal static List<ChaControl> lstFemale;
		internal static HVoiceCtrl voice;

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


		[DisplayName("Insert Without Asking")]
		[Description("Insert male genital without asking for permission")]
		public static SavedKeyboardShortcut InsertNowKey { get; private set; }

		[DisplayName("Insert After Asking Female")]
		[Description("Insert male genital after female speech")]
		public static SavedKeyboardShortcut InsertWaitKey { get; private set; }


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
			InsertNowKey = new SavedKeyboardShortcut(nameof(InsertNowKey), this, new KeyboardShortcut(KeyCode.None));
			InsertWaitKey = new SavedKeyboardShortcut(nameof(InsertWaitKey), this, new KeyboardShortcut(KeyCode.None));

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

			if (Input.GetKeyDown(InsertWaitKey.Value.MainKey) && InsertWaitKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertClick();
			else if (Input.GetKeyDown(InsertNowKey.Value.MainKey) && InsertNowKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertNoVoiceClick();
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
	}
}
