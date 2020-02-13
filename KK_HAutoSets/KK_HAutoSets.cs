using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx;
using Harmony;

namespace KK_HAutoSets
{
	[BepInPlugin(GUID, "HAutoSets", Version)]
	public class KK_HAutoSets : BaseUnityPlugin
	{
		public const string GUID = "MK.HAutoSets";
		internal const string Version = "1.0";

		[DisplayName("Auto lock female gauge")]
		[Description("Auto lock female gauge at H start")]
		public static ConfigWrapper<bool> lockFemaleGauge { get; private set; }

		[DisplayName("Auto lock male gauge")]
		[Description("Auto lock male gauge at H start")]
		public static ConfigWrapper<bool> lockMaleGauge { get; private set; }

		[DisplayName("Auto equip sub-accessories")]
		[Description("Auto equip sub-accessories at H start")]
		public static ConfigWrapper<bool> subAccessories { get; private set; }

		private void Start()
		{
			//Terminate if running Studio
			if (Application.productName == "CharaStudio")
			{
				BepInEx.Bootstrap.Chainloader.Plugins.Remove(this);
				Destroy(this);
				return;
			}

			lockFemaleGauge = new ConfigWrapper<bool>("lockFemaleGauge", this, true);
			lockMaleGauge = new ConfigWrapper<bool>("lockMaleGauge", this, true);
			subAccessories = new ConfigWrapper<bool>("subAccessories", this, true);

			//Harmony patching
			HarmonyInstance.Create(GUID).PatchAll(Assembly.GetExecutingAssembly());
		}

		/// <summary>
		/// Function to equip all accessories
		/// </summary>
		internal static void EquipAllAccessories(List<ChaControl> females)
		{
			if (subAccessories.Value)
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
			if (lockFemaleGauge.Value)
			{
				hSprite.OnFemaleGaugeLockOnGauge();
				hSprite.flags.lockGugeFemale = true;
			}

			if (lockMaleGauge.Value)
			{
				hSprite.OnMaleGaugeLockOnGauge();
				hSprite.flags.lockGugeMale = true;
			}
		}
	}
}
