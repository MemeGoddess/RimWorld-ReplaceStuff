using System.Linq;
using DevTools.Testing;
using RimWorld;
using Verse;

namespace Replace_Stuff.Tests;

[TestFixture(TestType.Playing)]
public class ReplacedWallAttachmentsTest
{
	private Thing? wall;
	private Thing? wallLamp;
	private Thing? replacement;
	private MinifiedThing? minifiedWallLamp;

	[Test]
	public void ReplacingWallWithDoorMinifiesWallAttachments()
	{
		var map = Find.CurrentMap;
		var wallPos = map.Center + new IntVec3(5, 0, 5);
		var wallDef = ThingDef.Named("Wall");
		var wallLampDef = ThingDef.Named("WallLamp");
		var doorDef = ThingDef.Named("Door");
		var steelDef = ThingDef.Named("Steel");
		var attachmentPos = wallPos + GenAdj.CardinalDirections[2];

		var spawnedWall = GenSpawn.Spawn(ThingMaker.MakeThing(wallDef, steelDef), wallPos, map);
		wall = spawnedWall;
		var spawnedWallLamp = GenSpawn.Spawn(ThingMaker.MakeThing(wallLampDef), attachmentPos, map, Rot4.North);
		wallLamp = spawnedWallLamp;

		Expect.IsTrue(spawnedWallLamp.def.building.isAttachment, "The test lamp should be a wall attachment");
		Expect.IsTrue(spawnedWallLamp.def.Minifiable, "The test lamp should be minifiable");
		Expect.ReferencesAreEqual(spawnedWall, GenConstruct.GetWallAttachedTo(spawnedWallLamp), "The lamp should be attached to the wall before replacement");

		var spawnedReplacement = GenSpawn.Spawn(ThingMaker.MakeThing(doorDef, steelDef), wallPos, map, Rot4.North, WipeMode.FullRefund);
		replacement = spawnedReplacement;

		minifiedWallLamp = attachmentPos.GetThingList(map)
			.OfType<MinifiedThing>()
			.FirstOrDefault(thing => thing.InnerThing.def == wallLampDef);

		Expect.IsNotNull(replacement, "The replacement door should spawn");
		Expect.IsNotNull(minifiedWallLamp, "The wall lamp should be present as a minified thing after replacement");
		Expect.IsFalse(spawnedWallLamp.Spawned, "The original wall lamp should be inside the minified thing, not spawned separately");
		if (minifiedWallLamp != null)
			Expect.ReferencesAreEqual(spawnedWallLamp, minifiedWallLamp.InnerThing, "Minification should preserve the original wall lamp");
	}

	[TearDown]
	public void TearDown()
	{
		if (replacement is { Destroyed: false })
			replacement.Destroy();
		if (minifiedWallLamp is { Destroyed: false })
			minifiedWallLamp.Destroy();
		if (wallLamp is { Destroyed: false, Spawned: true })
			wallLamp.Destroy();
		if (wall is { Destroyed: false })
			wall.Destroy();
	}
}
