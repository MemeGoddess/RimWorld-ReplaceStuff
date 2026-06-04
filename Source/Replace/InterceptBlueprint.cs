using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.DesignateSingleCell))]
	class InterceptDesignator_Build
	{
		//public override void DesignateSingleCell(IntVec3 c)
		public static bool Prefix(Designator_Build __instance, IntVec3 c, BuildableDef ___entDef, Rot4 ___placingRot)
		{
			//Replace the entire Designator_Build.DesignateSingleCell to behave differently if there is a replacable thing
			// Technically this is bypassing godmode, tutorial, PlayerKnowledgeDatabase, PlaceWorkers, but none of that should matter.

			ThingDef thingDef = ___entDef as ThingDef;

			if (thingDef == null)//Terrain?
				return true;

			if (DebugSettings.godMode || ___entDef.GetStatValueAbstract(StatDefOf.WorkToBuild, __instance.StuffDef) == 0f)
				return true;


			//Fix for door rotation so we find any rotation of doors
			if (typeof(Building_Door).IsAssignableFrom(thingDef.thingClass))
				___placingRot = DoorUtility.DoorRotationAt(c, __instance.Map, thingDef.building.preferConnectingToFences);


			//Using simple for loop instead of FindAll for better performance.
			List<Thing> replaceables = c.GetThingList(__instance.Map);

			if (replaceables.Count == 0)
				return true;

			Thing firstReplaceable = null;
			Thing firstBlueprintOrFrame = null;

			for (int i = 0; i < replaceables.Count; i++)
			{
				Thing replaceable = replaceables[i];

				if (replaceable.Position != c) continue;
				if (replaceable.Rotation != ___placingRot) continue;
				if (!Designator_ReplaceStuff.CanReplaceStuffFor(__instance.StuffDef, replaceable, thingDef)) continue;

				firstReplaceable ??= replaceable;

				if (replaceable is Blueprint_Build || replaceable is Frame)
				{
					firstBlueprintOrFrame = replaceable; // If we found a blueprint we replace this instead.
					break;
				}
			}

			Thing thingToReplace = firstBlueprintOrFrame ?? firstReplaceable;

			if (thingToReplace == null)
				return true;

			Designator_ReplaceStuff.DoReplace(thingToReplace, __instance.stuffDef);

			return false;
		}
	}
}

