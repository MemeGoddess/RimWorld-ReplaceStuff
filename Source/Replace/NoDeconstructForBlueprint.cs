using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Replace_Stuff.Replace;

// 1.6 made a new job to specifically deconstruct for blueprints. Noooo
// WorkGiver_ConstructDeliverResourcesToBlueprints will do it anyway! The new job, what, only is higher priority?
[HarmonyPatch(typeof(WorkGiver_DeconstructForBlueprint), nameof(WorkGiver_DeconstructForBlueprint.PotentialWorkThingsGlobal))]
public static class NoDeconstructForBlueprint
{
	// public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	public static bool Prefix(ref IEnumerable<Thing> __result)
	{
		__result = Enumerable.Empty<Thing>();
		return false;
	}
}