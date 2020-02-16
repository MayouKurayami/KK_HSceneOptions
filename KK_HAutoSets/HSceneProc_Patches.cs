using System;
using System.Collections.Generic;
using Harmony;

namespace KK_HAutoSets
{
	public static class HSceneProc_Patches
	{
		//This should hook to a method that loads as late as possible in the loading phase
		//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
		//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
		public static void HSceneProcPostfix(HSceneProc __instance)
		{
			var females = (List<ChaControl>)Traverse.Create(__instance).Field("lstFemale").GetValue();
			var hSprite = __instance.sprite;
			KK_HAutoSets.hflag = __instance.flags;
			KK_HAutoSets.EquipAllAccessories(females);
			KK_HAutoSets.LockGauges(hSprite);
			KK_HAutoSets.HideMaleShadow();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HSceneProc), "LateUpdate")]
		public static void HSceneLateUpdatePostfix()
		{
			KK_HAutoSets.GaugeLimiter();
		}
	}
}
