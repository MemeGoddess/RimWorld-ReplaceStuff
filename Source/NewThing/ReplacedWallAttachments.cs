using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Replace_Stuff.NewThing
{
	// When a new thing replaces a wall (e.g. a door built over a wall), vanilla wipes the wall with
	// DestroyMode.WillReplace (GenSpawn.Refund with willReplace = newDef.IsEdifice()), and Building.DeSpawn
	// skips its wall-attachment cleanup in that mode, assuming the replacement can hold them.
	// If the new thing can't hold wall attachments, they'd be left floating: GenConstruct.GetWallAttachedTo
	// then returns null for them, which throws in the power net code (PowerConnectionMaker.TryConnectToAnyPowerNet)
	// as soon as a powered attachment has to reconnect, and gravships leave them behind.
	// So handle them the way Building.DeSpawn does: minify them, or destroy them if they can't be minified.
	[HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.WipeAndRefundExistingThings))]
	public static class ReplacedWallDropsAttachments
	{
		public static void Prefix(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map)
		{
			if (map == null || thingDef is not ThingDef spawnDef || GenConstruct.BuiltDefOf(spawnDef) is not ThingDef newDef)
				return;

			if (newDef.building?.supportsWallAttachments ?? false)
				return;

			foreach (IntVec3 cell in GenAdj.OccupiedRect(thingPos, thingRot, thingDef.Size))
			{
				foreach (Thing oldThing in cell.GetThingList(map).ToList())
				{
					if (!(oldThing.def.building?.supportsWallAttachments ?? false))
						continue;

					if (!GenSpawn.SpawningWipes(thingDef, oldThing.def) || !newDef.CanReplace(oldThing.def))
						continue;

					foreach (Thing attached in GenConstruct.GetAttachedBuildings(oldThing))
					{
						if (attached.Destroyed)
							continue;

						if (attached.def.Minifiable)
							GenSpawn.Spawn(attached.MakeMinified(), attached.Position, map);
						else
							attached.Destroy(DestroyMode.Deconstruct);
					}
				}
			}
		}
	}
}
