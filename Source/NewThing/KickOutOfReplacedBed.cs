using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.NewThing
{
	/// <summary>
	/// A bed replaced under a sleeper is gone by the time their job fails, and they are lying in the new bed by then.
	/// KickOutOfBed would log "Tried to kick pawn ... out of a bed they're not currently in", so this gives it the bed
	/// they are in now.
	/// </summary>
	[HarmonyPatch(typeof(RestUtility), nameof(RestUtility.KickOutOfBed))]
	static class KickOutOfReplacedBed
	{
		//public static void KickOutOfBed(Pawn pawn, Building_Bed bed)
		public static void Prefix(Pawn pawn, ref Building_Bed bed)
		{
			if (bed is { Spawned: false } && pawn?.CurrentBed() is { } currentBed)
				bed = currentBed;
		}
	}
}
