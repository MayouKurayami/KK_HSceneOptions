using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;

namespace KK_HAutoSets
{
	public static class VRHooks
	{
		//This should hook to a method that loads as late as possible in the loading phase
		//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
		//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
		public static void VRHSceneLoadPostFix(VRHScene __instance)
		{
			var females = (List<ChaControl>) Traverse.Create(__instance).Field("lstFemale").GetValue();
			var hSprites = __instance.sprites;
			List<ChaControl> males = new List<ChaControl>
			{
				(ChaControl)Traverse.Create(__instance).Field("male").GetValue(),
				(ChaControl)Traverse.Create(__instance).Field("male1").GetValue()
			};

			HAutoSets.lstProc = (List<HActionBase>)Traverse.Create(__instance).Field("lstProc").GetValue();
			HAutoSets.flags = __instance.flags;
			HAutoSets.lstFemale = females;

			HAutoSets.EquipAllAccessories(females);
			foreach (HSprite sprite in hSprites)
				HAutoSets.LockGaugesAction(sprite);
		
			HAutoSets.HideShadow(males, females);

			__instance.gameObject.AddComponent<AnimationToggle>();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(VRHScene), "LateUpdate")]
		public static void VRHsceneLateUpdatePostfix()
		{
			HAutoSets.GaugeLimiter();
		}
	}
}
