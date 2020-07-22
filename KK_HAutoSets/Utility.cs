using System.Collections.Generic;
using static KK_HAutoSets.HAutoSets;

namespace KK_HAutoSets
{
	internal static class Utility
	{
		/// <summary>
		/// Detect if male excitement gauge is above 70 and activate the specified buttons
		/// </summary>
		internal static void HSceneSpriteCategorySetActive(List<UnityEngine.UI.Button> lstButton, bool autoFinish, int array)
		{
			bool active = flags.gaugeMale >= 70f && !autoFinish;
			if (lstButton.Count > array && (lstButton[array].isActiveAndEnabled != active))
				lstButton[array].gameObject.SetActive(active);

			array++;
			active = flags.gaugeMale >= 70f && autoFinish;
			if (lstButton.Count > array && (lstButton[array].isActiveAndEnabled != active))
				lstButton[array].gameObject.SetActive(active);
		}
	}
}
