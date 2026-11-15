using System.Collections;
using DevTools.Testing;
using DevTools.Testing.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace Replace_Stuff.Tests;

[TestFixture(TestType.Playing)]
public class CornerTouchTest
{
	IntVec3 topLeft;
	[SetUp]
	public void Setup()
	{
		var map = Find.CurrentMap;
		topLeft = map.Center - new IntVec3(5, 0, -5);
		var def = ThingDefOf.MineableSteel;
		for (var i = 0; i < 10; i++)
		{
			GenSpawn.Spawn(def, topLeft + new IntVec3(i, 0, 0), map);
			GenSpawn.Spawn(def, topLeft + new IntVec3(i, 0, -10), map);
			GenSpawn.Spawn(def, topLeft + new IntVec3(0, 0, -i), map);
			GenSpawn.Spawn(def, topLeft + new IntVec3(10, 0, -i), map);
		}
	}

	[Test]
	public IEnumerator TestCornerTouch()
	{
		var pawn = TestPawns.Worker
			.WithWorkMaxed(WorkTypeDefOf.Mining)
			.WithStatMultiplier(10f, StatDefOf.WorkSpeedGlobal)
			.WithName("Miner")
			.Spawn();
		
		var thing = topLeft.GetFirstThing<Mineable>(Find.CurrentMap);
		Find.CurrentMap.designationManager.AddDesignation(new Designation(thing, DesignationDefOf.Mine));
		yield return new WaitUntil(() => thing.Destroyed);
		var spot = topLeft + new IntVec3(1, 0, -1);
		yield return new WaitUntilTimeout(() => spot.GetItems(Find.CurrentMap).Any(x => x.def == ThingDefOf.Steel), GenDate.TicksPerHour);
	}

	[TearDown]
	public void TearDown()
	{
		foreach (var mineable in Find.CurrentMap.listerThings.AllThings.ToList()) 
			mineable.Destroy();
		
	}
}