using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbilityLoadouts
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Verse;
    using Verse.Sound;

    public class Command_LoadoutManager : Command_Action
    {
        public CompAbilityLoadout comp;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("Open Loadout Editor...", () =>
                    Find.WindowStack.Add(new Window_ManageLoadouts(comp)));

                for (int i = 0; i < comp.loadouts.Count; i++)
                {
                    int index = i;
                    string label = comp.loadouts[i].name;

                    if (comp.activeLoadoutIndex == i) label += " (Active)";

                    yield return new FloatMenuOption(label, () =>
                    {
                        comp.activeLoadoutIndex = index;
                    });
                }
            }
        }
    }
    public class Window_ManageLoadouts : Window
    {
        private CompAbilityLoadout comp;
        private Loadout selectedLoadout;

        //handles dragselect
        private bool isDragging = false;
        private bool dragTargetState = false; //whether we are toggling on or off
        private HashSet<Ability> abilitiesModifiedThisDrag = new();

        //handles quickly moving to the renaming bar
        private bool focusRenameField = false;

        //scroll states
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        private string filterText = "";
        public override Vector2 InitialSize => new Vector2(650f, 500f);

        public Window_ManageLoadouts(CompAbilityLoadout comp)
        {
            this.comp = comp;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            
            if (comp.loadouts.Count > 0) selectedLoadout = comp.loadouts[comp.activeLoadoutIndex];
        }
        public override void PostClose()
        {
            base.PostClose();
            //don't highlight new abilities again
            foreach (Ability ability in ((Pawn)comp.parent).abilities.abilities)
                comp.seenAbilities.Add(ability);
        }

        public override void DoWindowContents(Rect inRect)
        {
            //header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "Manage Loadouts: " + comp.parent.LabelShort);
            Text.Font = GameFont.Small;

            //panels
            Rect mainRect = new(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            Rect leftPane = mainRect.LeftHalf().ContractedBy(5f);
            Rect rightPane = mainRect.RightHalf().ContractedBy(5f);
            Widgets.DrawMenuSection(leftPane);
            Widgets.DrawMenuSection(rightPane);

            DrawLoadoutList(leftPane.ContractedBy(10f));
            DrawAbilitySelection(rightPane.ContractedBy(10f));
        }

        private void DrawLoadoutList(Rect rect)
        {
            //add loadout button
            Rect addBtnRect = new(rect.x, rect.y, rect.width, 30f);
            if (Widgets.ButtonText(addBtnRect, "+ New Loadout"))
            {
                Loadout newLoadout = new Loadout { name = "Loadout " + (comp.loadouts.Count + 1), abilities = new List<Ability>() };
                comp.loadouts.Add(newLoadout);
                selectedLoadout = newLoadout;
                //move to the loadout name bar
                focusRenameField = true;
            }

            //list of loadouts on the left
            Rect outRect = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, comp.loadouts.Count * 35f);

            Widgets.BeginScrollView(outRect, ref leftScrollPos, viewRect);
            
            float curY = 0f;
            for (int i = 0; i < comp.loadouts.Count; i++)
            {
                Loadout loadout = comp.loadouts[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, 30f);

                if (selectedLoadout == loadout)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                //delete button
                Rect deleteRect = new Rect(rowRect.xMax - 30f, rowRect.y + 2f, 26f, 26f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    comp.loadouts.Remove(loadout);
                    if (selectedLoadout == loadout) selectedLoadout = null;
                    break;
                }

                //makes the row into a button
                if (Widgets.ButtonInvisible(rowRect)) selectedLoadout = loadout;

                //adds text on the row
                Rect labelRect = new Rect(rowRect.x + 5f, rowRect.y, rowRect.width - 35f, rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, loadout.name + (comp.activeLoadoutIndex == i ? " (Active)" : ""));
                Text.Anchor = TextAnchor.UpperLeft;

                curY += 35f;
            }
            Widgets.EndScrollView();
        }

        private void DrawAbilitySelection(Rect rect)
        {
            if (selectedLoadout == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "Select or create a loadout.");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            //jump to the loadout namer on creating a loadout
            GUI.SetNextControlName("LoadoutRenameField");
            Rect renameRect = new Rect(rect.x, rect.y, rect.width, 30f);

            string textBefore = selectedLoadout.name;
            selectedLoadout.name = Widgets.TextField(renameRect, selectedLoadout.name);

            if (focusRenameField)
            {
                GUI.FocusControl("LoadoutRenameField");
                TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (editor != null)
                {
                    editor.text = selectedLoadout.name;
                    editor.SelectAll();
                }

                //not sure how necessary this is
                if (GUI.GetNameOfFocusedControl() == "LoadoutRenameField") focusRenameField = false;
            }

            //make active button - we can also do this from the float menu but redundancy is good
            Rect setActiveRect = new Rect(rect.x, rect.y + 35f, rect.width, 30f);
            if (Widgets.ButtonText(setActiveRect, "Set as Active Loadout"))
            {
                comp.activeLoadoutIndex = comp.loadouts.IndexOf(selectedLoadout);
            }

            //filter box
            Rect filterRect = new Rect(rect.x, rect.y + 110f, rect.width, 24f);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(filterRect.x, filterRect.y - 15f, filterRect.width, 15f), "Filter by name:");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            filterText = Widgets.TextField(new Rect(filterRect.x, filterRect.y, filterRect.width - 25f, filterRect.height), filterText);

            //button to clear filter
            if (!filterText.NullOrEmpty())
            {
                if (Widgets.ButtonImage(new Rect(filterRect.xMax - 18f, filterRect.y + 3f, 18f, 18f), Widgets.CheckboxOffTex))
                {
                    filterText = "";
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
            }

            //filtered ability list
            List<Ability> filteredAbilities = ((Pawn)comp.parent).abilities.abilities
            .Where(a => filterText.NullOrEmpty() || a.def.label.ToLower().Contains(filterText.ToLower()))
            .ToList();

            Rect bulkBtnRect = new Rect(rect.x, rect.y + 65f, rect.width, 30f);

            //select/clear buttons
            Rect leftBtn = bulkBtnRect.LeftPart(0.485f);
            Rect rightBtn = bulkBtnRect.RightPart(0.485f);

            if (Widgets.ButtonText(leftBtn, "Select All"))
            {
                foreach (Ability ability in filteredAbilities)
                    if (!selectedLoadout.abilities.Contains(ability)) selectedLoadout.abilities.Add(ability);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }

            if (Widgets.ButtonText(rightBtn, "Clear All"))
            {
                foreach (Ability ability in filteredAbilities)
                    if (selectedLoadout.abilities.Contains(ability)) selectedLoadout.abilities.Remove(ability);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }

            Rect outRect = new Rect(rect.x, rect.y + 140f, rect.width, rect.height - 140f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredAbilities.Count * 32f);

            //reset dragselect
            if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
                abilitiesModifiedThisDrag.Clear();
            }

            Widgets.BeginScrollView(outRect, ref rightScrollPos, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            foreach (Ability ability in filteredAbilities)
            {
                AbilityDef def = ability.def;
                bool currentlyIncluded = selectedLoadout.abilities.Contains(ability);
                Rect rowRect = listing.GetRect(30f);

                //dragselect
                if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);

                    //start dragselect
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        isDragging = true;
                        dragTargetState = !currentlyIncluded; //whether to toggle on or off
                        abilitiesModifiedThisDrag.Clear();
                    }

                    //do dragselect
                    if (isDragging && !abilitiesModifiedThisDrag.Contains(ability))
                    {
                        if (dragTargetState)
                        {
                            if (!currentlyIncluded) selectedLoadout.abilities.Add(ability);
                        }
                        else
                        {
                            if (currentlyIncluded) selectedLoadout.abilities.Remove(ability);
                        }
                        abilitiesModifiedThisDrag.Add(ability);
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                }

                //ability checkbox (the entire row is a button, checkbox not a button)
                float chkSize = 24f;
                Rect chkRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - chkSize) / 2f, chkSize, chkSize);
                bool drawState = selectedLoadout.abilities.Contains(ability);
                Widgets.Checkbox(chkRect.position, ref drawState, chkSize);

                //ability icon
                Rect iconRect = new Rect(chkRect.xMax + 4f, rowRect.y + 1f, 28f, 28f);
                Widgets.DrawTextureFitted(iconRect, def.uiIcon, 1f);
                GUI.color = Color.white;

                //ability name
                Rect labelRect = new Rect(iconRect.xMax + 6f, rowRect.y + 3, rowRect.width - (iconRect.xMax + 11f), rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                if (comp.seenAbilities.Any() && !comp.seenAbilities.Contains(ability)) Widgets.DrawHighlightSelected(rowRect);

                Widgets.Label(labelRect, def.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;

                TooltipHandler.TipRegion(rowRect, new TipSignal(ability.def.description, ability.def.GetHashCode()));
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}