using System.Linq;
using Verse;

namespace Replace_Stuff.Tests;

internal static class TestCells
{
	/// <summary>
	/// The minimum corner of an empty, standable, unfogged rect of the given size, away from the map edge.
	/// Rot4.North things of this size spawned at the returned cell occupy exactly that rect.
	/// </summary>
	public static IntVec3 FreeRect(Map map, IntVec2 size)
	{
		return map.AllCells.First(cell =>
		{
			var rect = new CellRect(cell.x, cell.z, size.x, size.z);
			return rect.ExpandedBy(1).InBounds(map) && !rect.ExpandedBy(1).Cells.Any(c => c.InNoBuildEdgeArea(map)) &&
			       rect.Cells.All(c => c.Standable(map) && !c.Fogged(map) && c.GetThingList(map).Count == 0);
		});
	}
}
