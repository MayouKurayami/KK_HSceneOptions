using System;
using System.Collections.Generic;
using HarmonyLib;
using static KK_HSceneOptions.HSceneOptions;
using static KK_HSceneOptions.SpeechControl;

namespace KK_HSceneOptions
{
	public static class Hooks_VR
	{
		//This should hook to a method that loads as late as possible in the loading phase
		//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
		//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
		public static void VRHSceneLoadPostFix(VRHScene __instance)
		{
			var females = (List<ChaControl>) Traverse.Create(__instance).Field("lstFemale").GetValue();

			sprites.Clear();
			foreach (HSprite sprite in __instance.sprites)
			{
				sprites.Add(sprite);
				LockGaugesAction(sprite);
			}

			lstmMale = new List<ChaControl> { Traverse.Create(__instance).Field("male").GetValue<ChaControl>() };
			if (isDarkness)
				lstmMale.Add(Traverse.Create(__instance).Field("male1").GetValue<ChaControl>());
			lstmMale = lstmMale.FindAll(male => male != null);

			lstFemale = females;
			lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			flags = __instance.flags;			
			voice = __instance.voice;

			EquipAllAccessories(females);

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
		[HarmonyPatch(typeof(VRHScene), "LateUpdate")]
		public static void VRHsceneLateUpdatePostfix()
		{
			GaugeLimiter();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(VRHScene), "ChangeAnimator")]
		public static void ChangeAnimatorPrefix(ref bool _isForceCameraReset)
		{
			if (VRResetCamera.Value)
				_isForceCameraReset = true;
		}
	}
}
