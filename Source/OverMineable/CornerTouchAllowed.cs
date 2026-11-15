using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;

namespace Replace_Stuff.OverMineable
{
	//Include blueprints and frames in IsCornerTouchAllowed
	//(Frames were included, but ReplaceStuff removes their 'edifice' status so they need to be re-included)
	[HarmonyPatch(typeof(TouchPathEndModeUtility), nameof(TouchPathEndModeUtility.IsCornerTouchAllowed_NewTemp))]
	public static class IsCornerTouchAllowed
	{
		//public static bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z, PathingContext pc)
		public static void Postfix(ref bool __result, IntVec3 adjCardinalX,
			IntVec3 adjCardinalZ,
			PathingContext pc,
			Thing target)
		{
			if (__result)
				return;

			if (target == null)
				return;

			if (pc.map.thingGrid.ThingsListAtFast(target.Position).Any(thing => thing is Blueprint || thing is Frame && TouchPathEndModeUtility.MakesOccupiedCellsAlwaysReachableDiagonally(thing.def)))
			{
				__result = true;
				return;
			}
		}
	}

	//remove rock from rejection of CanInteractThroughCorners
	[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.CanInteractThroughCorners), MethodType.Getter)]
	public static class CanInteractThroughCorners
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo buildingInfo = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.building));

			List<CodeInstruction> instList = instructions.ToList();
			for(int i=0;i<instList.Count();i++)
			{
				CodeInstruction inst = instList[i];

				if(inst.LoadsField(buildingInfo))
				{
					//IL_0015: ldarg.0      // this
					//IL_0016: ldfld        class RimWorld.BuildingProperties Verse.ThingDef::building

					//replace the this.building code with the end return true:

					instList[i - 1].opcode = OpCodes.Ldc_I4_1;//preserve label here
					instList[i] = new CodeInstruction(OpCodes.Ret);

					//chop off rest of the code that checks rock and smooth ezpz, we can corner touch rocks now!
					return instList.Take(i + 1);
				}
			}
			Verse.Log.Warning("Replace Stuff failed to patch CanInteractThroughCorners");
			return instructions;
		}
	}


	//Make sure mineable drop is in miner's region so it's not blocked off 
	[HarmonyPatch(typeof(Mineable), "TrySpawnYield", [typeof(Map), typeof(bool), typeof(Pawn)])]
	public static class DropOnPawn
	{
		//private void TrySpawnYield(Map map, float yieldChance, bool moteOnWaste, Pawn pawn)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo TryPlaceThingInfo = typeof(GenPlace).GetMethods(AccessTools.all)
				.Where(mi => mi.Name == "TryPlaceThing")
				.MinBy(mi => mi.GetParameters().Length);
			MethodInfo TryPlaceThingInSameRoomValidator = AccessTools.Method(typeof(DropOnPawn), nameof(DropOnPawn.TryPlaceThingInSameRoomValidator));
			
			var matcher = new CodeMatcher(instructions);
			matcher.End();
			matcher.MatchEndBackwards([
					new CodeMatch(OpCodes.Ldftn),
					new CodeMatch(OpCodes.Newobj),
					new CodeMatch(OpCodes.Ldnull)
				])
				.ThrowIfInvalid("Unable to find validator signature");

			matcher.RemoveInstruction();
			matcher.Insert([
				new CodeInstruction(OpCodes.Ldarg_S, 3), // Pawn
				new CodeInstruction(OpCodes.Call, TryPlaceThingInSameRoomValidator)
			])
			.ThrowIfInvalid("Invalid after replacing null validator");

			return matcher.Instructions();
		}

		//public static bool TryPlaceThing(Thing thing, IntVec3 center, Map map, ThingPlaceMode mode, Action<Thing, int> placedAction = null, Predicate<IntVec3> extraValidator = null, Rot4? rot = null, int squareRadius = 1)
		public static Predicate<IntVec3> TryPlaceThingInSameRoomValidator(Pawn miner = null)
		{
			//For godmode mining there is no pawn
			if (miner != null)
			{
				return (IntVec3 pos) => pos.GetRoom(miner.Map) == miner.GetRoom();
				//(Good luck setting this up in ILCode so I'll do it here)
				// Done ^.^
			}

			return null;
		}
	}
}
