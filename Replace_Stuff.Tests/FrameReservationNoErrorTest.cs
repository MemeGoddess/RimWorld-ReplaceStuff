using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using DevTools.Testing.Utils;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Replace_Stuff.Tests;

/// <summary>
/// Toils_Construct.MakeSolidThingFromBlueprintIfNecessary reserves the frame it creates. ReserveSharing redirects that
/// to the thing being replaced, which a sleeper may hold, and the toil must not log "Could not reserve" for it.
/// </summary>
[TestFixture(TestType.Playing)]
public class FrameReservationNoErrorTest
{
	private const string HarmonyId = "Uuugggg.rimworld.Replace_Stuff.main";

	private readonly List<Thing> spawned = [];

	[Test]
	public void FrameCreationToilIsPatched()
	{
		// Same lookup as the patch: the toil's initAction is a compiler-generated method
		var target = typeof(Toils_Construct).GetNestedTypes(AccessTools.all)
			.SelectMany(type => AccessTools.GetDeclaredMethods(type))
			.FirstOrDefault(method => method.Name.Contains(nameof(Toils_Construct.MakeSolidThingFromBlueprintIfNecessary)));
		Expect.IsNotNull(target, "The frame creation toil's initAction should exist, or the reservation patch is skipped");

		var transpilers = target == null ? [] : Harmony.GetPatchInfo(target)?.Transpilers ?? [];
		Expect.Any(transpilers, patch => patch.owner == HarmonyId && patch.PatchMethod.DeclaringType?.Name == "FrameReservationNoError",
			"Replace Stuff's transpiler should be applied to the frame creation toil");
	}

	[Test]
	public IEnumerator CreatingFrameOverReservedBedLogsNoError()
	{
		var map = Find.CurrentMap;
		var doubleBed = ThingDef.Named("DoubleBed");
		var pos = TestCells.FreeRect(map, doubleBed.Size);

		var oldBed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
		oldBed.SetFactionDirect(Faction.OfPlayer);
		spawned.Add(GenSpawn.Spawn(oldBed, pos, map, Rot4.North));

		var sleeper = Track(TestPawns.Blank.WithName("Sleeper").WithLocation(oldBed.GetSleepingSlotPos(0)).WithMap(map).Spawn());
		sleeper.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, oldBed), JobCondition.InterruptForced);
		yield return new WaitUntilTimeout(() => sleeper.CurrentBed() == oldBed, GenTicks.TicksPerRealSecond);
		Expect.IsTrue(map.reservationManager.ReservedBy(oldBed, sleeper), "The sleeper should hold the old bed");

		var blueprint = Track(GenConstruct.PlaceBlueprintForBuild(doubleBed, pos, map, Rot4.North, Faction.OfPlayer, ThingDefOf.WoodLog));
		var builderPos = TestCells.FreeRect(map, new IntVec2(1, 1));
		var builder = Track(TestPawns.Worker.WithName("Builder").WithLocation(builderPos).WithMap(map).Spawn());

		// Run the vanilla toil the way a construction job would, without the rest of the job
		var job = JobMaker.MakeJob(JobDefOf.PlaceNoCostFrame, blueprint);
		var toil = Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(TargetIndex.A);
		toil.actor = builder;
		builder.jobs.curJob = job;
		try
		{
			toil.initAction();
		}
		finally
		{
			builder.jobs.curJob = null;
		}

		var frame = job.targetA.Thing as Frame;
		Expect.IsNotNull(frame, "The toil should have replaced the blueprint with a frame");
		if (frame != null)
			spawned.Add(frame);
		Expect.IsFalse(map.reservationManager.ReservedBy(oldBed, builder), "The builder should not take the sleeper's bed");
		// LogWatcher fails the test on "Could not reserve"
	}

	private T Track<T>(T thing) where T : Thing
	{
		spawned.Add(thing);
		return thing;
	}

	[TearDown]
	public void TearDown()
	{
		foreach (var thing in Enumerable.Reverse(spawned).Where(thing => !thing.Destroyed))
			thing.Destroy();
		spawned.Clear();
	}
}
