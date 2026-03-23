using RimWorld;
using System.Collections.Generic;
using System.Xml.Linq;
using Verse;

namespace AbilityLoadouts
{
	public class Loadout: IExposable
	{
		public string name;
		public List<Ability> abilities = new();
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "New Loadout");
            Scribe_Collections.Look(ref abilities, "abilities", LookMode.Reference);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit) abilities.RemoveAll(x => x == null);
        }
    }
	[StaticConstructorOnStartup]
	public class CompAbilityLoadout : ThingComp
	{
		public List<Loadout> loadouts = new();
		public int activeLoadoutIndex;
        public List<Ability> seenAbilities = new();


        public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref activeLoadoutIndex, "activeLoadoutIndex", 0);
			Scribe_Collections.Look(ref loadouts, "loadouts", LookMode.Deep);
            Scribe_Collections.Look(ref seenAbilities, "seenAbilities", LookMode.Reference);
        }
	}
}