using System;
using System.Collections.Generic;
using Harmony;
using ToolBox.Extensions;
using UnityEngine;

namespace KK_HAutoSets
{
	[HarmonyPatch(typeof(HSceneProc), "Start", null, null)]
	internal static class HSceneProc_Start_Patches
	{
		private static void Postfix()
		{
			var sceneObject = UnityEngine.Object.FindObjectOfType(Type.GetType("HSceneProc,Assembly-CSharp.dll"));
			var female = (List<ChaControl>)sceneObject.GetPrivate("lstFemale");
			var hSprite = (HSprite)sceneObject.GetPrivate("sprites");
			KK_HAutoSets.Features(female, hSprite);
		}
	}
}
