using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using Replace_Stuff.NewThing;
using RimWorld;
using Verse;

namespace Replace_Stuff.Tests;

/// <summary>
/// Frames copy their building's replaceTags, so a double bed frame matches itself in CanReplace.
/// IsNewThingReplacement must skip it and find the bed it actually replaces.
/// </summary>
[TestFixture(TestType.Playing)]
public class FrameReplacementScanTest
{
	private readonly List<Thing> spawned = [];

	[TestCase(0)]
	[TestCase(1)]
	public void DoubleBedFrameFindsOldBedNotItself(int oldBedColumn)
	{
		var map = Find.CurrentMap;
		var doubleBed = ThingDef.Named("DoubleBed");
		var pos = TestCells.FreeRect(map, doubleBed.Size);

		// Old bed first, so in the left column it comes before the frame in the cell's thing list.
		// In the right column the frame's first cell holds only the frame, which matched itself before the fix.
		var oldBed = Spawn(ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog), pos + new IntVec3(oldBedColumn, 0, 0), map);
		var frame = Spawn(ThingMaker.MakeThing(doubleBed.frameDef, ThingDefOf.WoodLog), pos, map);

		Expect.IsTrue(doubleBed.CanReplace(frame.def), "The frame should share the double bed's replaceTags, or this test proves nothing");
		Expect.IsTrue(doubleBed.CanReplace(oldBed.def), "A double bed should be able to replace a single bed");

		var isReplacement = doubleBed.IsNewThingReplacement(frame.Position, frame.Rotation, map, out var oldThing);

		Expect.IsTrue(isReplacement, "The double bed frame should count as a replacement");
		Expect.ReferencesAreEqual(oldBed, oldThing, $"The old bed (column {oldBedColumn}) should be found, not {oldThing}");
	}

	[Test]
	public void FrameAloneIsNotAReplacement()
	{
		var map = Find.CurrentMap;
		var doubleBed = ThingDef.Named("DoubleBed");
		var pos = TestCells.FreeRect(map, doubleBed.Size);
		var frame = Spawn(ThingMaker.MakeThing(doubleBed.frameDef, ThingDefOf.WoodLog), pos, map);

		var isReplacement = doubleBed.IsNewThingReplacement(frame.Position, frame.Rotation, map, out var oldThing);

		Expect.IsFalse(isReplacement, $"A frame on empty ground should not replace anything, found {oldThing}");
		Expect.IsNull(oldThing, "No old thing should be returned");
	}

	private Thing Spawn(Thing thing, IntVec3 pos, Map map)
	{
		thing.SetFactionDirect(Faction.OfPlayer);
		var result = GenSpawn.Spawn(thing, pos, map, Rot4.North);
		spawned.Add(result);
		return result;
	}

	[TearDown]
	public void TearDown()
	{
		foreach (var thing in spawned.Where(thing => !thing.Destroyed))
			thing.Destroy();
		spawned.Clear();
	}
}
