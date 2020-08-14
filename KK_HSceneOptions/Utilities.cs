using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using System;

namespace KK_HSceneOptions
{
	internal static class Utilities
	{
		public class InstructionNotFoundException : Exception
		{
			public InstructionNotFoundException()
			{
			}

			public InstructionNotFoundException(string message)
				 : base(message)
			{
			}
		}


		/// <summary>
		/// Injects code instructions into block(s) of codes that begins with a check for OLoop
		/// </summary>
		/// <param name="instructions">The list of instructions to modify. This list will be directly modified.</param>
		/// <param name="targetOperand">Inject after the instruction that contains this operand</param>
		/// <param name="injection">The code instructions to inject</param>
		/// <param name="targetNextOpCode">If specified, the OpCode of the instruction immediately after the targetOperand instruction must be targetOpCode before injection can proceed</param>
		/// <param name="targetNextOperand">If specified, the operand of the instruction immediately after the targetOperand instruction must be targetNextOperand before injection can proceed</param>
		/// <param name="rangeStart">The index of the list of instructions where the start of the search space for targetOperand is. By default the search begins at the beginning of the list</param>
		/// <param name="rangeEnd">The index of the list of instructions where the end of the search space for targetOperand is. Default value of 0 results in searching til the end of the list.</param>
		/// <param name="insertAfter">Inject after this many elements in the instruction list</param>
		/// <exception cref="IndexOutOfRangeException">Thrown if rangeStart or rangeEnd are negative or out of bounds</exception>
		/// <exception cref="InstructionNotFoundException">Thrown if injection target is not found</exception>
		/// <returns>Returns the modified list of instructions.</returns>
		internal static List<CodeInstruction> InjectInstruction(
			List<CodeInstruction> instructions,
			object targetOperand,
			CodeInstruction[] injection,
			object targetNextOpCode = null,
			object targetNextOperand = null,
			int rangeStart = 0,
			int rangeEnd = 0,
			int insertAfter = 1)
		{
			var inserted = false;

			if (rangeEnd == 0)
				rangeEnd = instructions.Count;
			else if (rangeEnd < 0)
				throw new IndexOutOfRangeException("Search range end is out of bounds");

			for (var i = rangeStart; i < rangeEnd; i++)
			{
				if (instructions[i].operand != targetOperand)
					continue;
				else if (targetNextOpCode != null && instructions[i + 1].opcode != (OpCode)targetNextOpCode)
					continue;
				else if (targetNextOperand != null && instructions[i + 1].operand != targetNextOperand)
					continue;

				instructions.InsertRange(i + insertAfter, injection);
				inserted = true;
#if DEBUG
				UnityEngine.Debug.LogWarning(new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name + " injected instructions after " + targetOperand.ToString() + " at index " + i);
#endif
			}

			if (!inserted)
				throw new InstructionNotFoundException("Target instruction not found. Nothing Injected");

			return instructions;
		}


		internal static object GetValueWeakDictionary(object dic, object key)
		{
			var tryMethod = AccessTools.Method(dic.GetType(), "TryGetValue");
			if (tryMethod == null)
				throw new NotImplementedException(dic.GetType().ToString() + " has not implemented TryGetValue");

			var parameters = new object[] { key, null };

			tryMethod.Invoke(dic, parameters);
			return parameters[1];
		}
	}
}
