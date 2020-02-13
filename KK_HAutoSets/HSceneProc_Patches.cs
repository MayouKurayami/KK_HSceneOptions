using System;
using System.Collections.Generic;
using Harmony;
using ToolBox.Extensions;

namespace KK_HAutoSets
{
	//this is should hook to a method that loads as late as possible in the loading phase
	//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
	//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
	[HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable", null, null)]
	internal static class HSceneProc_Patches
	{
		private static void Postfix(HSceneProc __instance)
		{
			var females = (List<ChaControl>)__instance.GetPrivate("lstFemale");
			var hSprite = __instance.sprite;
			KK_HAutoSets.EquipAllAccessories(females);
			KK_HAutoSets.LockGauges(hSprite);
		}
	}
}
