using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using UnityEngine;
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
		/// Find a range in the given instructions that begins with a call of UnityEngine.AnimatorStateInfo.IsName with the parameter being any strings from clipNames,
		/// and ends with another call of UnityEngine.AnimatorStateInfo.IsName with the parameter being some other states of animation.
		/// </summary>
		/// <param name="instructions">The list of instructions to search through</param>
		/// <param name="rangeStart">Outputs the start index of the range</param>
		/// <param name="rangeEnd">Outputs the end index of the range</param>
		/// <exception cref="InstructionNotFoundException">Thrown if no calls of UnityEngine.AnimatorStateInfo.IsName with parameter matching clipNames is found</exception>
		internal static void FindClipInstructionRange(List<CodeInstruction> instructions, string[] clipNames, out int rangeStart, out int rangeEnd)
		{
			rangeStart = -1;
			rangeEnd = instructions.Count;

			var animatorStateInfoMethod = AccessTools.Method(typeof(AnimatorStateInfo), nameof(AnimatorStateInfo.IsName))
				?? throw new ArgumentNullException("UnityEngine.AnimatorStateInfo.IsName not found");

			for (var i = 0; i < instructions.Count; i++)
			{
				if (clipNames.Contains(instructions[i].operand as string) && instructions[i + 1].operand == animatorStateInfoMethod)
				{
					rangeStart = i + 2;
					break;
				}
			}

			if (rangeStart < 0)
				throw new InstructionNotFoundException("Instructions not found that begin with AnimatorStateInfo.IsName(" + clipNames[0] + ")");

			for (var i = rangeStart + 1; i < instructions.Count; i++)
			{
				if (instructions[i].operand == animatorStateInfoMethod && !clipNames.Contains(instructions[i - 1].operand as string))
				{
					rangeEnd = i;
					break;
				}
			}
		}


		/// <summary>
		/// Injects code instruction(s) after the code instruction that meets the specified criteria
		/// </summary>
		/// <param name="instructions">The list of instructions to modify. This list will be directly modified.</param>
		/// <param name="targetOperand">Inject after the instruction that contains this operand</param>
		/// <param name="injection">The code instructions to inject</param>
		/// <param name="targetNextOpCode">If specified, the OpCode of the instruction immediately after the targetOperand instruction must be targetOpCode before injection can proceed</param>
		/// <param name="targetNextOperand">If specified, the operand of the instruction immediately after the targetOperand instruction must be targetNextOperand before injection can proceed</param>
		/// <param name="rangeStart">The index of the list of instructions where the start of the search space for targetOperand is. By default the search begins at the beginning of the list</param>
		/// <param name="rangeEnd">The index  where the end of the search space for targetOperand is. Default value of 0 results in searching til the end of the list.</param>
		/// <param name="insertAt">Index position within the instructions list to inject, relative to the instruction containing targetOperand. A value of 0 means to inject at where the targetOperand instruction is at, while shifting the targetOperand instruction and its subsequent instructions towards the end of the list.</param>
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
			int insertAt = 1)
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

				instructions.InsertRange(i + insertAt, injection);
				inserted = true;
#if DEBUG
				UnityEngine.Debug.LogWarning("HSceneOptions: " + new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name + " injected instructions after " + targetOperand.ToString() + " at index " + i);
#endif
			}

			if (!inserted)
				throw new InstructionNotFoundException("Target instruction not found. Nothing Injected");

			return instructions;
		}


		internal static object GetValueWeakDict(object dic, object key)
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
