using System.Linq;
using Verse;
using RimWorld;

namespace Replace_Stuff.CoolersOverWalls
{
	// Designator_Build and Designator_Dropdown both use the same dropdown UI, so only one can show.
	// For stuffable buildings that is normally the material picker, which hid grouping dropdowns.
	// Material Sub-Menu already solves that, and unpacking every stuffable group broke mods like Basic Dropdowns.
	// Keep the unpack only for this mod's over-wall cooler/vent groups when material sub-menus are unavailable.
	public static class DesignatorBuildDropdownStuffFix
	{
		public static void SanityCheck()
		{
			if (ModLister.GetActiveModWithIdentifier("cedaro.material.submenu", true) != null)
				return;

			foreach (var catDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
				for (int i = 0; i < catDef.AllResolvedDesignators.Count; i++)
					if (catDef.AllResolvedDesignators[i] is Designator_Dropdown des
						&& des.Elements.All(IsOverWallBuildDesignator)
						&& des.Elements.Any(d => d is Designator_Build db && db.PlacingDef.MadeFromStuff))
					{
						catDef.AllResolvedDesignators.RemoveAt(i);
						foreach (var dropDes in des.Elements)
							catDef.AllResolvedDesignators.Insert(i, dropDes);
					}
		}

		private static bool IsOverWallBuildDesignator(Designator designator)
		{
			return designator is Designator_Build build && OverWallDef.IsOverWall(build.PlacingDef);
		}
	}
}
