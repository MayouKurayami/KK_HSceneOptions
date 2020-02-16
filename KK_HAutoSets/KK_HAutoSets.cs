using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace KK_HAutoSets
{
	[BepInPlugin(GUID, "HAutoSets", Version)]
	[BepInProcess("Koikatu")]
	[BepInProcess("KoikatuVR")]
	[BepInProcess("Koikatsu Party")]
	[BepInProcess("Koikatsu Party VR")]
	public class KK_HAutoSets : BaseUnityPlugin
	{
		public const string GUID = "MK.HAutoSets";
		internal const string Version = "1.0";

		[Category("Excitement Gauge")]
		[DisplayName("Auto lock female gauge")]
		[Description("Auto lock female gauge at H start")]
		public static ConfigWrapper<bool> LockFemaleGauge { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Auto lock male gauge")]
		[Description("Auto lock male gauge at H start")]
		public static ConfigWrapper<bool> LockMaleGauge { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Female Excitement Gauge Minimum Value")]
		[Description("Female exceitement gauge will not fall below this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<float> FemaleGaugeMin { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Female Excitement Gauge Maximum Value")]
		[Description("Female exceitement gauge will not go above this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<float> FemaleGaugeMax { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Male Excitement Gauge Minimum Value")]
		[Description("Male exceitement gauge will not fall below this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<float> MaleGaugeMin { get; private set; }

		[Category("Excitement Gauge")]
		[DisplayName("Male Excitement Gauge Maximum Value")]
		[Description("Male exceitement gauge will not go above this value")]
		[AcceptableValueRange(0f, 100f, false)]
		public static ConfigWrapper<float> MaleGaugeMax { get; private set; }

		[DisplayName("Auto equip sub-accessories")]
		[Description("Auto equip sub-accessories at H start")]
		public static ConfigWrapper<bool> SubAccessories { get; private set; }

		[DisplayName("Hide shadow casted by male body")]
		[Description("Hide shadow casted by male body")]
		public static ConfigWrapper<bool> MaleShadow { get; private set; }

		private void Start()
		{
			LockFemaleGauge = new ConfigWrapper<bool>("lockFemaleGauge", this, true);
			LockMaleGauge = new ConfigWrapper<bool>("lockMaleGauge", this, true);
			FemaleGaugeMin = new ConfigWrapper<float>("femaleGaugeMin", this, 0f);
			FemaleGaugeMax = new ConfigWrapper<float>("femaleGaugeMax", this, 100f);
			MaleGaugeMin = new ConfigWrapper<float>("maleGaugeMin", this, 0f);
			MaleGaugeMax = new ConfigWrapper<float>("maleGaugeMax", this, 100f);
			SubAccessories = new ConfigWrapper<bool>("subAccessories", this, true);
			MaleShadow = new ConfigWrapper<bool>("maleShadow", this, true);

			//Harmony patching
			HarmonyInstance harmony = HarmonyInstance.Create(GUID);
			harmony.PatchAll(typeof(HSceneProc_Patches));
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
		internal static void LockGauges(HSprite hSprite)
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
		internal static void HideMaleShadow()
		{
			if (MaleShadow.Value)
			{
				GameObject.Find("chaM_001/BodyTop/p_cm_body_00/cf_o_root/n_cm_body/o_body_a").GetComponent<SkinnedMeshRenderer>().shadowCastingMode = 0;
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

		internal static HFlag hflag;
	}
}
