using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AbilityLoadouts
{
    [StaticConstructorOnStartup]
    public static class Class1
    {
        static Class1()
        {
            new Harmony("AbilityLoadouts").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_AbilityTracker), "GetGizmos")]
    public static class CompAbilities_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn_AbilityTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            CompAbilityLoadout loadoutsComp = pawn.TryGetComp<CompAbilityLoadout>();
            bool multiSelect = Find.Selector.SelectedObjects.Count > 1;

            if (pawn.abilities.abilities.Count > 1 && loadoutsComp != null)
            {
                yield return new Command_LoadoutManager
                {
                    comp = loadoutsComp, //pass the comp so the gizmo can read the loadouts
                    //groupKey = pawn.thingIDNumber,  //on multiselect, show one gizmo per pawn?
                    //we don't actually want that because annoying in combat
                    defaultLabel = multiSelect ? "Loadouts (" + pawn.LabelShort + ")" : "Loadouts",
                    defaultDesc = "Left-click: Open Editor\nRight-click: Quick Swap",
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Config"),
                    action = () => Find.WindowStack.Add(new Window_ManageLoadouts(loadoutsComp))
                };
            }

            if (loadoutsComp == null || loadoutsComp.activeLoadoutIndex < 0 || loadoutsComp.activeLoadoutIndex >= loadoutsComp.loadouts.Count)
            {
                foreach (Gizmo g in gizmos) yield return g;
                yield break;
            }

            Loadout loadout = loadoutsComp.loadouts[loadoutsComp.activeLoadoutIndex];
            if (loadout.abilities.Any())
                foreach (Ability ability in loadout.abilities)
                {
                    if (!pawn.abilities.abilities.Contains(ability)) continue;
                    IEnumerable<Command> abilityGizmos = ability.GetGizmos();
                    foreach(Command gizmo in abilityGizmos)
                        if (gizmo is Command_Ability) yield return gizmo;
                }
        }
    }
}