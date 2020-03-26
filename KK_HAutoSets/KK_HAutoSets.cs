using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
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

		internal static HFlag hflag;

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

		private void Start()
		{
			LockFemaleGauge = new ConfigWrapper<bool>("lockFemaleGauge", this, true);
			LockMaleGauge = new ConfigWrapper<bool>("lockMaleGauge", this, true);
			FemaleGaugeMin = new ConfigWrapper<int>("femaleGaugeMin", this, 0);
			FemaleGaugeMax = new ConfigWrapper<int>("femaleGaugeMax", this, 100);
			MaleGaugeMin = new ConfigWrapper<int>("maleGaugeMin", this, 0);
			MaleGaugeMax = new ConfigWrapper<int>("maleGaugeMax", this, 100);
			SubAccessories = new ConfigWrapper<bool>("subAccessories", this, false);
			HideMaleShadow = new ConfigWrapper<bool>("hideMaleShadow", this, false);
			HideFemaleShadow = new ConfigWrapper<bool>("hideFemaleShadow", this, false);
			DisableHideBody = new ConfigWrapper<bool>("disableHideBody", this, false);

			//Harmony patching
			HarmonyInstance harmony = HarmonyInstance.Create(GUID);
			harmony.PatchAll(typeof(OtherPatches));
			harmony.PatchAll(Assembly.GetExecutingAssembly());
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
						foreach (SkinnedMeshRenderer mesh in male.objRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
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
							if (child.name != "p_cf_body_bone")
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
				hflag.gaugeFemale = Mathf.Clamp(hflag.gaugeFemale, FemaleGaugeMin.Value, FemaleGaugeMax.Value);
			if (MaleGaugeMax.Value >= MaleGaugeMin.Value)
				hflag.gaugeMale = Mathf.Clamp(hflag.gaugeMale, FemaleGaugeMin.Value, MaleGaugeMax.Value);
		}
	}
}
