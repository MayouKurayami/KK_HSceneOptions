using System;
using System.Reflection;
using Harmony;

namespace KK_HAutoSets
{
	[HarmonyPatch]
	internal static class VRHScene_Update_Patch
	{
		private static bool Prepare()
		{
			//skip patching if VRHscene class is not found in assembly
			return Type.GetType("VRHScene,Assembly-CSharp.dll") != null;
		}

		private static MethodInfo TargetMethod()
		{
			//This should hook to a method that loads as late as possible in the loading phase
			//Hooking method "MapSameObjectDisable" because: "Something that happens at the end of H scene loading, good enough place to hook" - DeathWeasel1337/Anon11
			//https://github.com/DeathWeasel1337/KK_Plugins/blob/master/KK_EyeShaking/KK.EyeShaking.Hooks.cs#L20
			return Type.GetType("VRHScene,Assembly-CSharp.dll").GetMethod("LateUpdate", AccessTools.all);
		}

		private static void Postfix()
		{
			KK_HAutoSets.GaugeLimiter();
		}
	}
}
