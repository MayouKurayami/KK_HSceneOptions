using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

namespace KK_HAutoSets
{
	[HarmonyPatch]
	internal static class VRHScene_Load_Patch
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
			return Type.GetType("VRHScene,Assembly-CSharp.dll").GetMethod("MapSameObjectDisable", AccessTools.all);
		}

		private static void Postfix()
		{
			var sceneObject = UnityEngine.Object.FindObjectOfType(Type.GetType("VRHScene,Assembly-CSharp.dll"));
			var females = (List<ChaControl>) Traverse.Create(sceneObject).Field("lstFemale").GetValue();
			var hSprites = (HSprite[]) Traverse.Create(sceneObject).Field("sprites").GetValue();
			List<ChaControl> males = new List<ChaControl>
			{
				(ChaControl)Traverse.Create(sceneObject).Field("male").GetValue(),
				(ChaControl)Traverse.Create(sceneObject).Field("male1").GetValue()
			};

			KK_HAutoSets.hflag = (HFlag)Traverse.Create(sceneObject).Field("flags").GetValue();
			KK_HAutoSets.EquipAllAccessories(females);

			foreach (HSprite sprite in hSprites)
			{
				KK_HAutoSets.LockGaugesAction(sprite);
			}
				
			KK_HAutoSets.HideMaleShadowAction(males);
		}
	}
}
