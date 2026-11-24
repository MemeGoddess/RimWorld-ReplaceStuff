using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using DevTools.Testing.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace Replace_Stuff.Tests;

/// <summary>
/// When a bed is replaced under a sleeper, their job fails after the old bed is gone and they lie in the new one.
/// KickOutOfBed then gets the despawned old bed and should kick them out of the bed they are in now, without
/// logging "Tried to kick ... out of a bed they're not currently in". Vanilla clears the bed posture either way.
/// </summary>
[TestFixture(TestType.Playing)]
public class KickOutOfReplacedBedTest
{
	private const string NotInBedError = "out of a bed they're not currently in";
	private const string NotInBedWhenStandingError = "out of bed when they weren't in bed";

	private readonly List<Thing> spawned = [];
	private Pawn? sleeper;

	[Test]
	public IEnumerator KickWithDespawnedBedKicksOutOfCurrentBed()
	{
		var map = Find.CurrentMap;
		var newBed = SpawnBed(map);
		var oldBed = SpawnBed(map);
		oldBed.Destroy(DestroyMode.Vanish);

		yield return LieDown(newBed);

		// LogWatcher fails the test on "Tried to kick ... out of a bed they're not currently in"
		RestUtility.KickOutOfBed(sleeper!, oldBed);

		Expect.IsNull(sleeper!.CurrentBed(), "The pawn should be out of the bed they were lying in");
	}

	[Test]
	[ErrorsAllowed(NotInBedError)]
	public IEnumerator KickWithSpawnedOtherBedIsLeftToVanilla()
	{
		var map = Find.CurrentMap;
		var newBed = SpawnBed(map);
		var otherBed = SpawnBed(map);

		yield return LieDown(newBed);

		// The passed bed is still spawned, so the prefix must not swap it and vanilla still reports the mismatch
		using var errors = new LogCapture(NotInBedError);
		RestUtility.KickOutOfBed(sleeper!, otherBed);

		Expect.IsTrue(errors.Seen, "Vanilla should still log the mismatch for a spawned bed the pawn isn't in");
	}

	[Test]
	[ErrorsAllowed(NotInBedWhenStandingError)]
	public void KickWithDespawnedBedAndNoCurrentBedIsLeftToVanilla()
	{
		var map = Find.CurrentMap;
		var oldBed = SpawnBed(map);
		oldBed.Destroy(DestroyMode.Vanish);
		var pos = TestCells.FreeRect(map, new IntVec2(1, 1));
		sleeper = TestPawns.Blank.WithName("Standing").WithLocation(pos).WithMap(map).Spawn();

		Expect.IsNull(sleeper.CurrentBed(), "The pawn should not be in a bed");
		// Vanilla logs "weren't in bed" here; the prefix has no bed to swap in and must not throw or add errors
		Expect.DoesNotThrow(() => RestUtility.KickOutOfBed(sleeper, oldBed), "Kicking a pawn who isn't in any bed should not throw");
	}

	private Building_Bed SpawnBed(Map map)
	{
		var pos = TestCells.FreeRect(map, ThingDefOf.Bed.Size);
		var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
		bed.SetFactionDirect(Faction.OfPlayer);
		GenSpawn.Spawn(bed, pos, map, Rot4.North);
		spawned.Add(bed);
		return bed;
	}

	private WaitUntilTimeout LieDown(Building_Bed bed)
	{
		sleeper = TestPawns.Blank
			.WithName("Sleeper")
			.WithLocation(bed.GetSleepingSlotPos(0))
			.WithMap(bed.Map)
			.Spawn();
		sleeper.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
		return new WaitUntilTimeout(() => sleeper.CurrentBed() == bed, GenTicks.TicksPerRealSecond);
	}

	[TearDown]
	public void TearDown()
	{
		if (sleeper is { Destroyed: false })
			sleeper.Destroy();
		sleeper = null;
		foreach (var thing in spawned.Where(thing => !thing.Destroyed))
			thing.Destroy();
		spawned.Clear();
	}
}
