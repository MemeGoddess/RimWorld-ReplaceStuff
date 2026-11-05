using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Replace_Stuff.Replace
{
	/// <summary>
	/// Toils_Construct.MakeSolidThingFromBlueprintIfNecessary reserves the frame it just created and ignores the
	/// result, since nobody else could hold a new frame. ReserveSharing redirects that reservation to the thing being
	/// replaced, which another pawn may hold, and then the call logs "Could not reserve".
	/// The job carries on either way, so this only turns off the error for that one call.
	/// </summary>
	[HarmonyPatch]
	static class FrameReservationNoError
	{
		static MethodBase TargetMethod() => typeof(Toils_Construct).GetNestedTypes(AccessTools.all)
			.SelectMany(type => AccessTools.GetDeclaredMethods(type))
			.FirstOrDefault(method => method.Name.Contains(nameof(Toils_Construct.MakeSolidThingFromBlueprintIfNecessary)));

		// The target is compiler-generated. If it's ever renamed, skip this patch rather than fail the mod's PatchAll
		static bool Prepare()
		{
			if (TargetMethod() != null) return true;

			Verse.Log.Warning("[Replace Stuff] Couldn't find the frame creation toil, so skipping its reservation patch");
			return false;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo reserve = AccessTools.Method(typeof(ReservationUtility), nameof(ReservationUtility.Reserve));
			MethodInfo reserveNoError = AccessTools.Method(typeof(FrameReservationNoError), nameof(ReserveNoError));

			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.Calls(reserve))
					instruction.operand = reserveNoError;
				yield return instruction;
			}
		}

		public static bool ReserveNoError(Pawn pawn, LocalTargetInfo target, Job job, int maxPawns, int stackCount, ReservationLayerDef layer, bool errorOnFailed, bool ignoreOtherReservations)
		{
			return pawn.Reserve(target, job, maxPawns, stackCount, layer, false, ignoreOtherReservations);
		}
	}
}
