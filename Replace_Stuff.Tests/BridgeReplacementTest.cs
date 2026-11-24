using System.Collections;
using System.Linq;
using DevTools.Testing;
using Replace_Stuff.PlaceBridges;
using RimWorld;
using Verse;

namespace Replace_Stuff.Tests;

[TestFixture(TestType.Playing)]
public class BridgeReplacementTest
{
	private Map map = null!;
	private IntVec3 cell;
	private TerrainDef? originalTopTerrain;
	private TerrainDef? originalUnderTerrain;
	private TerrainDef? originalFoundation;
	private bool originalGodMode;

	[SetUp]
	public void Setup()
	{
		map = Find.CurrentMap;
		cell = map.AllCells.First(c =>
			c.GetThingList(map).Count == 0 &&
			!c.Fogged(map) &&
			map.terrainGrid.FoundationAt(c) == null &&
			map.terrainGrid.TempTerrainAt(c) == null);
		originalTopTerrain = map.terrainGrid.TopTerrainAt(cell);
		originalUnderTerrain = map.terrainGrid.UnderTerrainAt(cell);
		originalFoundation = map.terrainGrid.FoundationAt(cell);
		originalGodMode = DebugSettings.godMode;
		DebugSettings.godMode = false;
	}

	[Test]
	public void AnyBridgeCanReplaceAnyOtherBridge()
	{
		var bridges = BridgelikeTerrain.allBridgeTerrains
			.Where(bridge => bridge.bridge && bridge.blueprintDef != null && bridge.GetStatValueAbstract(StatDefOf.WorkToBuild) > 0f)
			.Distinct()
			.ToList();
		Expect.IsTrue(bridges.Count > 1, "At least two buildable bridge terrain defs are required for this test");

		foreach (var existingBridge in bridges)
		{
			foreach (var replacementBridge in bridges.Where(bridge => bridge != existingBridge))
			{
				SetTerrainForTest(existingBridge);
				var report = GenConstruct.CanPlaceBlueprintAt(replacementBridge, cell, Rot4.North, map, godMode: true);
				Expect.IsTrue(report.Accepted,
					$"{replacementBridge} should be placeable over {existingBridge}: {report.Reason}");

				GenConstruct.PlaceBlueprintForBuild(replacementBridge, cell, map, Rot4.North, Faction.OfPlayer, null);
				var blueprint = cell.GetThingList(map)
					.OfType<Blueprint_Build>()
					.FirstOrDefault(thing => thing.def.entityDefToBuild == replacementBridge);
				Expect.IsNotNull(blueprint, $"A blueprint for {replacementBridge} should be placed over {existingBridge}");
				Expect.AreEqual(existingBridge, map.terrainGrid.FoundationAt(cell),
					"The existing bridge should remain until the replacement finishes construction");

				blueprint?.Destroy(DestroyMode.Cancel);
				map.terrainGrid.RemoveFoundation(cell, doLeavings: false);
			}
		}
	}

	[Test]
	public void CannotRemoveRequiredBridge()
	{
		SetTerrainForTest(TerrainDefOf.HeavyBridge);
		var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.BlocksGranite);
		GenSpawn.Spawn(wall, cell, map);
		var report = GenConstruct.CanPlaceBlueprintAt(TerrainDefOf.Bridge, cell, Rot4.North, map, godMode: true);
		Expect.IsFalse(report.Accepted, "Should not be able to replace bridge when higher affordance is required");
	}

	[Test]
	public void CannotReplaceBridgeWithSameBridge()
	{
		SetTerrainForTest(TerrainDefOf.HeavyBridge);
		var report = GenConstruct.CanPlaceBlueprintAt(TerrainDefOf.HeavyBridge, cell, Rot4.North, map, true);
		Expect.IsFalse(report.Accepted, "Should not be able to replace bridge with same bridge");
	}

	private void SetTerrainForTest(TerrainDef terrain)
	{
		if (map.terrainGrid.FoundationAt(cell) != null)
			map.terrainGrid.RemoveFoundation(cell, doLeavings: false);

		map.terrainGrid.SetTerrain(cell, TerrainDefOf.WaterShallow);
		map.terrainGrid.SetFoundation(cell, terrain);
	}

	[TearDown]
	public void TearDown()
	{
		cell.GetThingList(map).ToList().ForEach(x => x.Destroy());
		
		if (map.terrainGrid.FoundationAt(cell) != null)
			map.terrainGrid.RemoveFoundation(cell, doLeavings: false);

		if (originalTopTerrain != null)
			map.terrainGrid.SetTerrain(cell, originalTopTerrain);
		if (originalUnderTerrain != null)
			map.terrainGrid.SetUnderTerrain(cell, originalUnderTerrain);
		if (originalFoundation != null)
			map.terrainGrid.SetFoundation(cell, originalFoundation);

		DebugSettings.godMode = originalGodMode;
	}
}
