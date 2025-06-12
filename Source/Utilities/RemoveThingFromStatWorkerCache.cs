using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Replace_Stuff.Utilities
{
	public static class RemoveThingFromStatWorkerCache
	{
		//class StatDef {
		//private StatWorker workerInt;
		public static AccessTools.FieldRef<StatDef, StatWorker> workerInt = AccessTools.FieldRefAccess<StatDef, StatWorker>("workerInt");

		//class StatWorker {
		//private Dictionary<Thing, StatCacheEntry> temporaryStatCache;
		//private Dictionary<Thing, float> immutableStatCache;
		public static AccessTools.FieldRef<StatWorker, Dictionary<Thing, StatCacheEntry>> temporaryStatCache = AccessTools.FieldRefAccess<StatWorker, Dictionary<Thing, StatCacheEntry>>("temporaryStatCache");
		public static AccessTools.FieldRef<StatWorker, ConcurrentDictionary<Thing, float>> immutableStatCache = AccessTools.FieldRefAccess<StatWorker, ConcurrentDictionary<Thing, float>>("immutableStatCache");

		public static void RemoveFromStatWorkerCaches(this Thing thing)
		{
			foreach (StatDef statDef in DefDatabase<StatDef>.AllDefsListForReading)
			{
				var worker = statDef.workerInt;
				if (worker != null)
				{
					worker.temporaryStatCache?.Remove(thing);
					worker.immutableStatCache?.TryRemove(thing, out _);
				}
			}
		}
	}
}
