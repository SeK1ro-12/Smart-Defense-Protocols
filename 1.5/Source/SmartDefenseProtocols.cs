using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;

namespace SmartDefenseProtocols
{
    /// <summary>
    /// Расширение для CompFlickable для безопасного доступа к приватным полям через Reflection.
    /// </summary>
    public static class CompFlickableExtensions
    {
        private static readonly FieldInfo wantSwitchOnField = typeof(CompFlickable).GetField("wantSwitchOn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static bool GetWantSwitchOn(this CompFlickable flick)
        {
            if (flick == null) return false;
            if (wantSwitchOnField != null)
            {
                return (bool)wantSwitchOnField.GetValue(flick);
            }
            return flick.SwitchIsOn;
        }

        public static void SetWantSwitchOn(this CompFlickable flick, bool value)
        {
            if (flick == null) return;
            if (wantSwitchOnField != null)
            {
                wantSwitchOnField.SetValue(flick, value);
            }
        }
    }

    /// <summary>
    /// Initializes Harmony patches on startup to hook into RimWorld methods.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class SmartDefensePatches
    {
        static SmartDefensePatches()
        {
            var harmony = new Harmony("com.smartdefense.protocols");
            harmony.PatchAll();
        }
    }

    /// <summary>
    /// Harmony patch on CompFlickable.DoFlick to ensure that when a colonist finishes flicking a turret,
    /// physical power state and visual graphics are forcefully synchronized.
    /// </summary>
    [HarmonyPatch(typeof(CompFlickable), nameof(CompFlickable.DoFlick))]
    public static class CompFlickable_DoFlick_Patch
    {
        public static void Postfix(CompFlickable __instance)
        {
            if (__instance?.parent == null) return;
            Building b = __instance.parent as Building;
            if (b == null) return;

            if (DefenseManager.IsTurret(b))
            {
                bool targetState = __instance.SwitchIsOn;
                DefenseManager.SyncTurretPowerAndVisuals(b, targetState);
            }
        }
    }

    public enum AlertLevel { Green, Yellow, Red }
    public enum PawnRole { Auto, Colonist, Combatant }

    /// <summary>
    /// Global mod settings saved across sessions.
    /// </summary>
    public class DefenseSettings : ModSettings
    {
        public static bool AutoDraftCombatants = true;
        public static bool AutoToggleTurrets = true;
        public static bool InstantTurretToggle = false;
        public static bool AutoRedOnRaid = true;
        public static bool AutoYellowOnRaidEnd = true;
        public static bool ManualModeOnly = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref AutoDraftCombatants, "AutoDraftCombatants", true);
            Scribe_Values.Look(ref AutoToggleTurrets, "AutoToggleTurrets", true);
            Scribe_Values.Look(ref InstantTurretToggle, "InstantTurretToggle", false);
            Scribe_Values.Look(ref AutoRedOnRaid, "AutoRedOnRaid", true);
            Scribe_Values.Look(ref AutoYellowOnRaidEnd, "AutoYellowOnRaidEnd", true);
            Scribe_Values.Look(ref ManualModeOnly, "ManualModeOnly", false);
        }
    }

    /// <summary>
    /// Game component storing save-game state for defense protocols, area assignments, and pawn roles.
    /// </summary>
    public class GameComponent_DefenseProtocols : GameComponent
    {
        public AlertLevel currentAlert = AlertLevel.Green;

        public string yellowDefaultCivilian = "";
        public string yellowDefaultCombatant = "";
        public string yellowDefaultAnimals = "";
        public string yellowDefaultMechs = "";

        public string redDefaultCivilian = "";
        public string redDefaultCombatant = "";
        public string redDefaultAnimals = "";
        public string redDefaultMechs = "";

        public Dictionary<string, PawnRole> pawnRoles = new Dictionary<string, PawnRole>();
        public Dictionary<string, string> pawnYellowAreas = new Dictionary<string, string>();
        public Dictionary<string, string> pawnRedAreas = new Dictionary<string, string>();

        public Dictionary<string, string> animalYellowAreas = new Dictionary<string, string>();
        public Dictionary<string, string> animalRedAreas = new Dictionary<string, string>();

        public Dictionary<string, string> mechYellowAreas = new Dictionary<string, string>();
        public Dictionary<string, string> mechRedAreas = new Dictionary<string, string>();

        public Dictionary<string, string> originalAreas = new Dictionary<string, string>();

        public GameComponent_DefenseProtocols(Game game) : base() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentAlert, "currentAlert", AlertLevel.Green);

            Scribe_Values.Look(ref yellowDefaultCivilian, "yellowDefaultCivilian", "");
            Scribe_Values.Look(ref yellowDefaultCombatant, "yellowDefaultCombatant", "");
            Scribe_Values.Look(ref yellowDefaultAnimals, "yellowDefaultAnimals", "");
            Scribe_Values.Look(ref yellowDefaultMechs, "yellowDefaultMechs", "");

            Scribe_Values.Look(ref redDefaultCivilian, "redDefaultCivilian", "");
            Scribe_Values.Look(ref redDefaultCombatant, "redDefaultCombatant", "");
            Scribe_Values.Look(ref redDefaultAnimals, "redDefaultAnimals", "");
            Scribe_Values.Look(ref redDefaultMechs, "redDefaultMechs", "");

            Scribe_Collections.Look(ref pawnRoles, "pawnRoles", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnYellowAreas, "pawnYellowAreas", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pawnRedAreas, "pawnRedAreas", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref animalYellowAreas, "animalYellowAreas", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref animalRedAreas, "animalRedAreas", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref mechYellowAreas, "mechYellowAreas", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref mechRedAreas, "mechRedAreas", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref originalAreas, "originalAreas", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                yellowDefaultCivilian = yellowDefaultCivilian ?? "";
                yellowDefaultCombatant = yellowDefaultCombatant ?? "";
                yellowDefaultAnimals = yellowDefaultAnimals ?? "";
                yellowDefaultMechs = yellowDefaultMechs ?? "";

                redDefaultCivilian = redDefaultCivilian ?? "";
                redDefaultCombatant = redDefaultCombatant ?? "";
                redDefaultAnimals = redDefaultAnimals ?? "";
                redDefaultMechs = redDefaultMechs ?? "";

                pawnRoles = pawnRoles ?? new Dictionary<string, PawnRole>();
                pawnYellowAreas = pawnYellowAreas ?? new Dictionary<string, string>();
                pawnRedAreas = pawnRedAreas ?? new Dictionary<string, string>();
                animalYellowAreas = animalYellowAreas ?? new Dictionary<string, string>();
                animalRedAreas = animalRedAreas ?? new Dictionary<string, string>();
                mechYellowAreas = mechYellowAreas ?? new Dictionary<string, string>();
                mechRedAreas = mechRedAreas ?? new Dictionary<string, string>();
                originalAreas = originalAreas ?? new Dictionary<string, string>();
            }
        }

        public static GameComponent_DefenseProtocols Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<GameComponent_DefenseProtocols>();
            }
        }
    }

    /// <summary>
    /// Core manager responsible for executing alert changes, turret toggle commands, and pawn area reassignments.
    /// </summary>
    public static class DefenseManager
    {
        public static AlertLevel CurrentAlert
        {
            get => GameComponent_DefenseProtocols.Instance?.currentAlert ?? AlertLevel.Green;
            set
            {
                var comp = GameComponent_DefenseProtocols.Instance;
                if (comp != null) comp.currentAlert = value;
            }
        }

        public static void SetAlertLevel(AlertLevel newLevel, Map map = null)
        {
            if (map == null) map = Find.CurrentMap;
            AlertLevel previousLevel = CurrentAlert;
            CurrentAlert = newLevel;

            if (newLevel != AlertLevel.Green && previousLevel == AlertLevel.Green)
            {
                SaveOriginalAreas(map);
            }

            ApplyAlertLevel(newLevel, map);

            if (newLevel == AlertLevel.Green)
            {
                RestoreOriginalAreas(map);
                UndraftAllColonists(map);
            }

            PlayAlertSound(newLevel);

            string msg = (newLevel == AlertLevel.Green)
                ? "SmartDefense.Msg.GreenSwitched".Translate()
                : "SmartDefense.Msg.LevelSwitched".Translate(GetAlertLabel(newLevel));

            Messages.Message(msg, MessageTypeDefOf.CautionInput, false);
        }

        public static void UndraftAllColonists(Map map)
        {
            if (map == null) return;
            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                if (p.Faction == Faction.OfPlayer && p.drafter != null && p.Drafted)
                {
                    p.drafter.Drafted = false;
                }
            }
        }

        private static string GetAlertLabel(AlertLevel level)
        {
            switch (level)
            {
                case AlertLevel.Green: return "SmartDefense.DEFCON.Green".Translate();
                case AlertLevel.Yellow: return "SmartDefense.DEFCON.Yellow".Translate();
                case AlertLevel.Red: return "SmartDefense.DEFCON.Red".Translate();
                default: return "SmartDefense.DEFCON.Unknown".Translate();
            }
        }

        public static void SaveOriginalAreas(Map map)
        {
            if (map == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                if (p.Faction != Faction.OfPlayer) continue;
                string id = p.ThingID;
                if (p.playerSettings != null)
                {
                    Area area = p.playerSettings.AreaRestrictionInPawnCurrentMap;
                    comp.originalAreas[id] = area != null ? area.Label : "";
                }
            }
        }

        public static void RestoreOriginalAreas(Map map)
        {
            if (map == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                if (p.Faction != Faction.OfPlayer) continue;
                string id = p.ThingID;
                if (p.playerSettings != null)
                {
                    if (comp.originalAreas.TryGetValue(id, out string areaLabel) && !string.IsNullOrEmpty(areaLabel))
                    {
                        p.playerSettings.AreaRestrictionInPawnCurrentMap = FindAreaByName(map, areaLabel);
                    }
                    else
                    {
                        p.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                    }
                }
            }
        }

        public static void ApplyAlertLevel(AlertLevel level, Map map)
        {
            if (map == null) return;
            foreach (Pawn p in map.mapPawns.AllPawns)
            {
                if (p.Faction != Faction.OfPlayer) continue;

                if (p.RaceProps != null && p.RaceProps.Humanlike)
                {
                    ApplyHumanlikeArea(p, level, map);
                }
                else if (p.RaceProps != null && p.RaceProps.Animal)
                {
                    ApplyAnimalArea(p, level, map);
                }
                else if (IsMechOrDrone(p))
                {
                    ApplyMechArea(p, level, map);
                }
            }

            if (DefenseSettings.AutoToggleTurrets)
            {
                ToggleTurrets(map, level != AlertLevel.Green);
            }
        }

        public static bool IsMechOrDrone(Pawn p)
        {
            if (p == null || p.RaceProps == null) return false;
            return p.RaceProps.IsMechanoid;
        }

        private static void ApplyHumanlikeArea(Pawn p, AlertLevel level, Map map)
        {
            if (p.playerSettings == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            string id = p.ThingID;
            bool combatant = IsCombatant(p);

            if (level == AlertLevel.Green) return;

            string targetAreaLabel = "";
            if (level == AlertLevel.Yellow)
            {
                if (!comp.pawnYellowAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                {
                    targetAreaLabel = combatant ? comp.yellowDefaultCombatant : comp.yellowDefaultCivilian;
                }
            }
            else if (level == AlertLevel.Red)
            {
                if (!comp.pawnRedAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                {
                    targetAreaLabel = combatant ? comp.redDefaultCombatant : comp.redDefaultCivilian;
                }

                if (combatant && DefenseSettings.AutoDraftCombatants && p.drafter != null && !p.Downed && !p.InMentalState)
                {
                    p.drafter.Drafted = true;
                }
            }

            p.playerSettings.AreaRestrictionInPawnCurrentMap = FindAreaByName(map, targetAreaLabel);
        }

        private static void ApplyAnimalArea(Pawn p, AlertLevel level, Map map)
        {
            if (p.playerSettings == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            string id = p.ThingID;
            if (level == AlertLevel.Green) return;

            string targetAreaLabel = "";
            if (level == AlertLevel.Yellow)
            {
                if (!comp.animalYellowAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                    targetAreaLabel = comp.yellowDefaultAnimals;
            }
            else if (level == AlertLevel.Red)
            {
                if (!comp.animalRedAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                    targetAreaLabel = comp.redDefaultAnimals;
            }

            p.playerSettings.AreaRestrictionInPawnCurrentMap = FindAreaByName(map, targetAreaLabel);
        }

        private static void ApplyMechArea(Pawn p, AlertLevel level, Map map)
        {
            if (p.playerSettings == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            string id = p.ThingID;
            if (level == AlertLevel.Green) return;

            string targetAreaLabel = "";
            if (level == AlertLevel.Yellow)
            {
                if (!comp.mechYellowAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                    targetAreaLabel = comp.yellowDefaultMechs;
            }
            else if (level == AlertLevel.Red)
            {
                if (!comp.mechRedAreas.TryGetValue(id, out targetAreaLabel) || string.IsNullOrEmpty(targetAreaLabel))
                    targetAreaLabel = comp.redDefaultMechs;
            }

            p.playerSettings.AreaRestrictionInPawnCurrentMap = FindAreaByName(map, targetAreaLabel);
        }

        public static bool IsCombatant(Pawn p)
        {
            if (p == null) return false;
            var comp = GameComponent_DefenseProtocols.Instance;
            string id = p.ThingID;
            if (comp != null && comp.pawnRoles.TryGetValue(id, out PawnRole role))
            {
                if (role == PawnRole.Combatant) return true;
                if (role == PawnRole.Colonist) return false;
            }
            if (p.story != null && p.WorkTagIsDisabled(WorkTags.Violent)) return false;
            if (p.equipment != null && p.equipment.Primary != null && p.equipment.Primary.def.IsWeapon) return true;
            if (p.skills != null && (p.skills.GetSkill(SkillDefOf.Shooting).Level >= 6 || p.skills.GetSkill(SkillDefOf.Melee).Level >= 6)) return true;
            return false;
        }

        public static Area FindAreaByName(Map map, string name)
        {
            if (map == null || string.IsNullOrWhiteSpace(name)) return null;
            return map.areaManager.AllAreas.FirstOrDefault(a => a.Label.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Controls turret state switching, setting `wantSwitchOn` on CompFlickable so colonists
        /// receive a job to turn turrets on/off or instantly toggled if configured.
        /// </summary>
        private static void ToggleTurrets(Map map, bool enable)
        {
            if (map == null) return;

            List<Building> colonistBuildings = map.listerBuildings.allBuildingsColonist;
            if (colonistBuildings == null) return;

            for (int i = 0; i < colonistBuildings.Count; i++)
            {
                Building b = colonistBuildings[i];
                if (!IsTurret(b)) continue;

                CompFlickable flick = b.GetComp<CompFlickable>();

                if (flick != null)
                {
                    if (DefenseSettings.InstantTurretToggle)
                    {
                        // Instant switch mode
                        flick.SetWantSwitchOn(enable);

                        Designation existing = map.designationManager.DesignationOn(b, DesignationDefOf.Flick);
                        if (existing != null)
                        {
                            map.designationManager.RemoveDesignation(existing);
                        }

                        SyncTurretPowerAndVisuals(b, enable);
                    }
                    else
                    {
                        // Standard flick job mode (colonist comes and switches the toggle)
                        if (flick.GetWantSwitchOn() != enable)
                        {
                            flick.SetWantSwitchOn(enable);

                            Designation existing = map.designationManager.DesignationOn(b, DesignationDefOf.Flick);

                            if (flick.WantsFlick())
                            {
                                if (existing == null)
                                {
                                    map.designationManager.AddDesignation(new Designation(b, DesignationDefOf.Flick));
                                }
                            }
                            else
                            {
                                if (existing != null)
                                {
                                    map.designationManager.RemoveDesignation(existing);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Turrets without a switch component get power toggled directly
                    SyncTurretPowerAndVisuals(b, enable);
                }
            }
        }

        public static void SyncTurretPowerAndVisuals(Building b, bool enable)
        {
            if (b == null) return;

            CompPowerTrader power = b.GetComp<CompPowerTrader>();
            if (power != null)
            {
                power.PowerOn = enable;
            }

            if (b.AllComps != null)
            {
                foreach (var comp in b.AllComps)
                {
                    if (comp == null) continue;
                    Type t = comp.GetType();
                    string tName = t.Name;

                    if (tName.Contains("Power") || tName.Contains("Flick") || tName.Contains("Turret"))
                    {
                        var propPowerOn = t.GetProperty("PowerOn", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (propPowerOn != null && propPowerOn.CanWrite)
                        {
                            try { propPowerOn.SetValue(comp, enable, null); } catch { }
                        }

                        var propDesire = t.GetProperty("DesirePowerOn", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (propDesire != null && propDesire.CanWrite)
                        {
                            try { propDesire.SetValue(comp, enable, null); } catch { }
                        }
                    }
                }
            }

            if (b.Spawned && b.Map != null)
            {
                b.Map.mapDrawer.MapMeshDirty(b.Position, MapMeshFlagDefOf.Things);
                b.Map.overlayDrawer.DisposeHandle(b);
            }
        }

        public static bool IsTurret(Building b)
        {
            if (b == null) return false;

            if (b is Building_Turret) return true;

            if (b.def != null)
            {
                if (b.def.building != null && b.def.building.turretGunDef != null) return true;

                if (b.def.designationCategory != null &&
                    b.def.designationCategory.defName.Equals("Security", StringComparison.OrdinalIgnoreCase))
                {
                    if (b.GetComp<CompFlickable>() != null || b.GetComp<CompPowerTrader>() != null)
                        return true;
                }

                string defName = b.def.defName.ToLower();
                if (defName.Contains("turret") || defName.Contains("sentry") || defName.Contains("mortar") || defName.Contains("defense"))
                {
                    if (b.GetComp<CompFlickable>() != null || b.GetComp<CompPowerTrader>() != null)
                        return true;
                }
            }

            return false;
        }

        private static void PlayAlertSound(AlertLevel level)
        {
            try
            {
                SoundDef sd;
                switch (level)
                {
                    case AlertLevel.Green: sd = SoundDefOf.Click; break;
                    case AlertLevel.Yellow: sd = SoundDefOf.Designate_PlanAdd; break;
                    case AlertLevel.Red: sd = SoundDefOf.Designate_PlanAdd; break;
                    default: sd = SoundDefOf.Click; break;
                }
                sd?.PlayOneShotOnCamera();
            }
            catch { }
        }
    }

    public static class UIHelper
    {
        public static void DrawSolidColor(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = Color.white;
        }

        public static void DrawOutlinedRect(Rect rect, Color color, int thickness = 1)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), BaseContent.WhiteTex);
            GUI.color = Color.white;
        }

        public static bool DrawStyledButton(Rect rect, string label, Color baseBg, Color hoverBg, Color border, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            bool hovering = Mouse.IsOver(rect);

            DrawSolidColor(rect, hovering ? hoverBg : baseBg);
            DrawOutlinedRect(rect, hovering ? Color.Lerp(border, Color.white, 0.35f) : border);

            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), label);
            Text.Anchor = prevAnchor;

            return Widgets.ButtonInvisible(rect);
        }
    }

    /// <summary>
    /// Bottom bar main button rendering current DEFCON alert status.
    /// </summary>
    public class MainButtonWorker_Defense : MainButtonWorker
    {
        private static Texture2D cachedIcon;

        private Texture2D RealIcon
        {
            get
            {
                if (cachedIcon == null)
                {
                    cachedIcon = ContentFinder<Texture2D>.Get(def.iconPath, true);
                }

                return cachedIcon;
            }
        }

        public override void DoButton(Rect rect)
        {
            if (!Visible) return;

            // RimWorld сам рисует стандартный синий фон, рамку и белую иконку.
            base.DoButton(rect);

            // Поверх стандартной иконки рисуем тот же щит, но уже цветом DEFCON.
            Color statusColor;

            switch (DefenseManager.CurrentAlert)
            {
                case AlertLevel.Green:
                    statusColor = new Color(0.2f, 0.85f, 0.3f, 1f);
                    break;

                case AlertLevel.Yellow:
                    statusColor = new Color(1f, 0.85f, 0.2f, 1f);
                    break;

                case AlertLevel.Red:
                    statusColor = new Color(1f, 0.25f, 0.25f, 1f);
                    break;

                default:
                    statusColor = Color.white;
                    break;
            }

            // Квадратная область кнопки, чтобы щит не растягивался.
            float size = rect.height;
            Rect square = new Rect(
                rect.center.x - size / 2f,
                rect.y,
                size,
                size
            );

            Rect iconRect = square.ContractedBy(size * 0.22f);

            Color previousColor = GUI.color;
            GUI.color = statusColor;
            Widgets.DrawTextureFitted(iconRect, RealIcon, 1f);
            GUI.color = previousColor;

            // Подсказка с текущим режимом.
            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(
                    rect,
                    $"SmartDefense.Tip.Protocol".Translate(
                        GetAlertStatusText(DefenseManager.CurrentAlert)
                    )
                );
            }
        }

        public override void Activate()
        {
            Dialog_DefenseSettings window =
                Find.WindowStack.WindowOfType<Dialog_DefenseSettings>();

            if (window != null)
            {
                Find.WindowStack.TryRemove(window);
            }
            else
            {
                Find.WindowStack.Add(new Dialog_DefenseSettings());
            }
        }

        private string GetAlertStatusText(AlertLevel level)
        {
            switch (level)
            {
                case AlertLevel.Green:
                    return "SmartDefense.DEFCON.Green".Translate();

                case AlertLevel.Yellow:
                    return "SmartDefense.DEFCON.Yellow".Translate();

                case AlertLevel.Red:
                    return "SmartDefense.DEFCON.Red".Translate();

                default:
                    return "SmartDefense.DEFCON.Unknown".Translate();
            }
        }
    }

    public class Dialog_DefenseSettings : Window
    {
        private int currentTab = 0;
        private Vector2 scrollPosition = Vector2.zero;

        // Global Drag & Select State
        private static bool isDragging = false;
        private static int activeTab = -1;
        private static int activeColumn = -1;
        private static object activeSourceValue = null;
        private static Vector2 dragStartPos = Vector2.zero;
        private static bool dragMovedFar = false;
        private static int dragStartIndex = -1;

        private bool isDraggingWindow = false;

        public override Vector2 InitialSize => new Vector2(920f, 680f);

        public Dialog_DefenseSettings()
        {
            this.forcePause = false;
            this.preventCameraMotion = false;
            this.absorbInputAroundWindow = false;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.draggable = false;
        }

        private static void ResetDragState()
        {
            isDragging = false;
            activeTab = -1;
            activeColumn = -1;
            activeSourceValue = null;
            dragMovedFar = false;
            dragStartIndex = -1;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Strict Mouse-Up / Release Detection to guarantee ZERO drag stickiness
            if (!Input.GetMouseButton(0))
            {
                ResetDragState();
                isDraggingWindow = false;
            }

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 45f);
            UIHelper.DrawSolidColor(headerRect, new Color(0.1f, 0.13f, 0.18f, 0.95f));
            UIHelper.DrawOutlinedRect(headerRect, new Color(0.25f, 0.35f, 0.5f, 0.8f));
            UIHelper.DrawSolidColor(new Rect(headerRect.x, headerRect.yMax - 2f, headerRect.width, 2f), new Color(0.3f, 0.55f, 0.85f, 0.9f));

            Rect dragHandleRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 45f, headerRect.height);
            HandleWindowDrag(dragHandleRect);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(headerRect.x + 15f, headerRect.y, 350f, headerRect.height), "SmartDefense.Window.Title".Translate());

            Rect statusBadge = new Rect(headerRect.xMax - 320f, headerRect.y + 7f, 300f, 30f);
            DrawAlertStatusBadge(statusBadge);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect tabsRect = new Rect(inRect.x, inRect.y + 55f, inRect.width, 35f);
            DrawTabs(tabsRect);

            Rect contentRect = new Rect(inRect.x, inRect.y + 95f, inRect.width, inRect.height - 150f);
            UIHelper.DrawSolidColor(contentRect, new Color(0.08f, 0.1f, 0.14f, 0.85f));
            UIHelper.DrawOutlinedRect(contentRect, new Color(0.2f, 0.25f, 0.35f, 0.5f));

            Rect innerRect = contentRect.ContractedBy(10f);

            switch (currentTab)
            {
                case 0: DrawMainTab(innerRect); break;
                case 1: DrawPawnsTab(innerRect); break;
                case 2: DrawAnimalsTab(innerRect); break;
                case 3: DrawMechsTab(innerRect); break;
            }
        }

        private void HandleWindowDrag(Rect dragHandleRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && dragHandleRect.Contains(e.mousePosition))
            {
                isDraggingWindow = true;
                e.Use();
            }

            if (isDraggingWindow && e.type == EventType.MouseDrag)
            {
                this.windowRect.x += e.delta.x;
                this.windowRect.y += e.delta.y;
                e.Use();
            }
        }

        private void DrawAlertStatusBadge(Rect rect)
        {
            Color badgeBg;
            string label;
            switch (DefenseManager.CurrentAlert)
            {
                case AlertLevel.Green:
                    badgeBg = new Color(0.12f, 0.4f, 0.18f, 0.9f);
                    label = "SmartDefense.DEFCON.Green".Translate();
                    break;
                case AlertLevel.Yellow:
                    badgeBg = new Color(0.55f, 0.42f, 0.08f, 0.9f);
                    label = "SmartDefense.DEFCON.Yellow".Translate();
                    break;
                case AlertLevel.Red:
                    badgeBg = new Color(0.6f, 0.12f, 0.12f, 0.9f);
                    label = "SmartDefense.DEFCON.Red".Translate();
                    break;
                default:
                    badgeBg = Color.gray;
                    label = "SmartDefense.DEFCON.Unknown".Translate();
                    break;
            }

            UIHelper.DrawSolidColor(rect, badgeBg);
            UIHelper.DrawOutlinedRect(rect, Color.white * 0.7f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTabs(Rect rect)
        {
            float tabWidth = (rect.width - 15f) / 4f;
            string[] tabNames = new string[] { "SmartDefense.Tab.Main".Translate(), "SmartDefense.Tab.Pawns".Translate(), "SmartDefense.Tab.Animals".Translate(), "SmartDefense.Tab.Mechs".Translate() };

            for (int i = 0; i < 4; i++)
            {
                Rect tabRect = new Rect(rect.x + i * (tabWidth + 5f), rect.y, tabWidth, rect.height);
                bool isActive = (currentTab == i);
                bool hovering = tabRect.Contains(Event.current.mousePosition);

                Color tabColor = isActive
                    ? new Color(0.2f, 0.35f, 0.55f, 0.95f)
                    : (hovering ? new Color(0.17f, 0.22f, 0.32f, 0.9f) : new Color(0.12f, 0.15f, 0.22f, 0.75f));

                UIHelper.DrawSolidColor(tabRect, tabColor);
                UIHelper.DrawOutlinedRect(tabRect, isActive ? Color.cyan : new Color(0.3f, 0.4f, 0.5f, 0.4f));

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tabRect, tabNames[i]);
                Text.Anchor = TextAnchor.UpperLeft;

                if (isActive)
                {
                    UIHelper.DrawSolidColor(new Rect(tabRect.x, tabRect.yMax - 3f, tabRect.width, 3f), Color.cyan);
                }

                if (Widgets.ButtonInvisible(tabRect))
                {
                    currentTab = i;
                    scrollPosition = Vector2.zero;
                    ResetDragState();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }
        }

        private void DrawMainTab(Rect rect)
        {
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "SmartDefense.Header.QuickDEFCON".Translate());
            Text.Font = GameFont.Small;

            Rect btnGroup = listing.GetRect(40f);
            float btnW = (btnGroup.width - 20f) / 3f;

            if (UIHelper.DrawStyledButton(new Rect(btnGroup.x, btnGroup.y, btnW, 40f), "SmartDefense.Btn.GreenCode".Translate(),
                new Color(0.12f, 0.3f, 0.15f, 0.85f), new Color(0.18f, 0.42f, 0.22f, 0.95f), new Color(0.3f, 0.7f, 0.35f)))
            {
                DefenseManager.SetAlertLevel(AlertLevel.Green);
            }

            if (UIHelper.DrawStyledButton(new Rect(btnGroup.x + btnW + 10f, btnGroup.y, btnW, 40f), "SmartDefense.Btn.YellowCode".Translate(),
                new Color(0.35f, 0.28f, 0.05f, 0.85f), new Color(0.48f, 0.38f, 0.08f, 0.95f), new Color(0.85f, 0.7f, 0.2f)))
            {
                DefenseManager.SetAlertLevel(AlertLevel.Yellow);
            }

            if (UIHelper.DrawStyledButton(new Rect(btnGroup.x + (btnW + 10f) * 2, btnGroup.y, btnW, 40f), "SmartDefense.Btn.RedCode".Translate(),
                new Color(0.38f, 0.08f, 0.08f, 0.85f), new Color(0.5f, 0.12f, 0.12f, 0.95f), new Color(0.85f, 0.25f, 0.25f)))
            {
                DefenseManager.SetAlertLevel(AlertLevel.Red);
            }

            Rect sepRect1 = listing.GetRect(14f);
            Widgets.DrawLineHorizontal(sepRect1.x, sepRect1.y + 7f, listing.ColumnWidth);

            Rect titleWithResetRect = listing.GetRect(30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(titleWithResetRect.x, titleWithResetRect.y, 450f, 30f), "SmartDefense.Header.CategoryFallback".Translate());
            Text.Font = GameFont.Small;

            Rect resetBtnRect = new Rect(titleWithResetRect.xMax - 210f, titleWithResetRect.y, 210f, 26f);
            if (UIHelper.DrawStyledButton(resetBtnRect, "SmartDefense.Btn.ResetDefaults".Translate(), new Color(0.25f, 0.15f, 0.15f, 0.8f), new Color(0.4f, 0.2f, 0.2f, 0.95f), new Color(0.7f, 0.3f, 0.3f)))
            {
                comp.yellowDefaultCivilian = "";
                comp.yellowDefaultCombatant = "";
                comp.yellowDefaultAnimals = "";
                comp.yellowDefaultMechs = "";
                comp.redDefaultCivilian = "";
                comp.redDefaultCombatant = "";
                comp.redDefaultAnimals = "";
                comp.redDefaultMechs = "";
                Messages.Message("SmartDefense.Msg.ResetDefaultsDone".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }

            Rect headerRow = listing.GetRect(28f);
            Widgets.Label(new Rect(headerRow.x, headerRow.y, 200f, 25f), "SmartDefense.Col.Category".Translate());

            Rect yellowHeader = new Rect(headerRow.x + 220f, headerRow.y, 200f, 25f);
            UIHelper.DrawSolidColor(yellowHeader, new Color(0.45f, 0.35f, 0.05f, 0.8f));
            UIHelper.DrawOutlinedRect(yellowHeader, Color.yellow * 0.8f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(yellowHeader, "SmartDefense.Col.YellowCode".Translate());

            Rect redHeader = new Rect(headerRow.x + 440f, headerRow.y, 200f, 25f);
            UIHelper.DrawSolidColor(redHeader, new Color(0.5f, 0.1f, 0.1f, 0.8f));
            UIHelper.DrawOutlinedRect(redHeader, Color.red * 0.8f);
            Widgets.Label(redHeader, "SmartDefense.Btn.RedCode".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            listing.Gap(5f);

            DrawCategoryDefaultRow(listing.GetRect(32f), "SmartDefense.Cat.Civilians".Translate(), comp.yellowDefaultCivilian, a => comp.yellowDefaultCivilian = a, comp.redDefaultCivilian, a => comp.redDefaultCivilian = a);
            DrawCategoryDefaultRow(listing.GetRect(32f), "SmartDefense.Cat.Combatants".Translate(), comp.yellowDefaultCombatant, a => comp.yellowDefaultCombatant = a, comp.redDefaultCombatant, a => comp.redDefaultCombatant = a);
            DrawCategoryDefaultRow(listing.GetRect(32f), "SmartDefense.Tab.Animals".Translate(), comp.yellowDefaultAnimals, a => comp.yellowDefaultAnimals = a, comp.redDefaultAnimals, a => comp.redDefaultAnimals = a);
            DrawCategoryDefaultRow(listing.GetRect(32f), "SmartDefense.Tab.Mechs".Translate(), comp.yellowDefaultMechs, a => comp.yellowDefaultMechs = a, comp.redDefaultMechs, a => comp.redDefaultMechs = a);

            Rect sepRect2 = listing.GetRect(14f);
            Widgets.DrawLineHorizontal(sepRect2.x, sepRect2.y + 7f, listing.ColumnWidth);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "SmartDefense.Header.AdditionalOptions".Translate());
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled("SmartDefense.Opt.ManualModeOnly".Translate(), ref DefenseSettings.ManualModeOnly);
            listing.CheckboxLabeled("SmartDefense.Opt.AutoDraftCombatants".Translate(), ref DefenseSettings.AutoDraftCombatants);
            listing.CheckboxLabeled("SmartDefense.Opt.AutoToggleTurrets".Translate(), ref DefenseSettings.AutoToggleTurrets);
            listing.CheckboxLabeled("SmartDefense.Opt.InstantTurretToggle".Translate(), ref DefenseSettings.InstantTurretToggle);
            listing.CheckboxLabeled("SmartDefense.Opt.AutoRedOnRaid".Translate(), ref DefenseSettings.AutoRedOnRaid);
            listing.CheckboxLabeled("SmartDefense.Opt.AutoYellowOnRaidEnd".Translate(), ref DefenseSettings.AutoYellowOnRaidEnd);

            listing.End();
        }

        private void DrawCategoryDefaultRow(Rect row, string label, string yellowVal, Action<string> setYellow, string redVal, Action<string> setRed)
        {
            UIHelper.DrawSolidColor(row, new Color(0.12f, 0.15f, 0.2f, 0.6f));
            UIHelper.DrawOutlinedRect(row, new Color(0.2f, 0.25f, 0.3f, 0.3f));

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x + 10f, row.y, 200f, row.height), label);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect yellowBtn = new Rect(row.x + 220f, row.y + 2f, 200f, 28f);
            if (UIHelper.DrawStyledButton(yellowBtn, string.IsNullOrEmpty(yellowVal) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : yellowVal,
                new Color(0.16f, 0.14f, 0.05f, 0.7f), new Color(0.24f, 0.2f, 0.07f, 0.85f), new Color(0.6f, 0.5f, 0.2f, 0.6f)))
            {
                List<FloatMenuOption> opts = GetAreaOptions(setYellow);
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            Rect redBtn = new Rect(row.x + 440f, row.y + 2f, 200f, 28f);
            if (UIHelper.DrawStyledButton(redBtn, string.IsNullOrEmpty(redVal) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : redVal,
                new Color(0.2f, 0.08f, 0.08f, 0.7f), new Color(0.3f, 0.1f, 0.1f, 0.85f), new Color(0.6f, 0.25f, 0.25f, 0.6f)))
            {
                List<FloatMenuOption> opts = GetAreaOptions(setRed);
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private List<FloatMenuOption> GetAreaOptions(Action<string> onSelect)
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>
            {
                new FloatMenuOption("SmartDefense.Area.Unrestricted".Translate(), () => onSelect(""))
            };
            Map map = Find.CurrentMap;
            if (map != null)
            {
                foreach (Area a in map.areaManager.AllAreas)
                {
                    string label = a.Label;
                    opts.Add(new FloatMenuOption(label, () => onSelect(label)));
                }
            }
            return opts;
        }

        private void DrawPawnsTab(Rect rect)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            List<Pawn> pawns = map.mapPawns.FreeColonists.ToList();

            DrawPawnTableHeaders(rect, true);

            Rect listRect = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, pawns.Count * 36f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                Rect row = new Rect(0f, i * 36f, viewRect.width, 32f);
                DrawPawnRow(row, p, i, pawns);
            }

            Widgets.EndScrollView();
        }

        private void DrawPawnTableHeaders(Rect rect, bool hasRoleColumn)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, 30f);
            UIHelper.DrawSolidColor(header, new Color(0.12f, 0.16f, 0.22f, 0.9f));
            UIHelper.DrawOutlinedRect(header, new Color(0.3f, 0.4f, 0.5f, 0.6f));

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(header.x + 10f, header.y, 200f, header.height), "SmartDefense.Col.Name".Translate());

            float curX = header.x + 210f;
            if (hasRoleColumn)
            {
                Widgets.Label(new Rect(curX, header.y, 140f, header.height), "SmartDefense.Col.Role".Translate());
                curX += 150f;
            }

            Rect yellowH = new Rect(curX, header.y + 2f, 210f, 26f);
            UIHelper.DrawSolidColor(yellowH, new Color(0.45f, 0.35f, 0.05f, 0.8f));
            UIHelper.DrawOutlinedRect(yellowH, Color.yellow * 0.8f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(yellowH, "SmartDefense.Col.YellowCode".Translate());

            curX += 220f;
            Rect redH = new Rect(curX, header.y + 2f, 210f, 26f);
            UIHelper.DrawSolidColor(redH, new Color(0.5f, 0.1f, 0.1f, 0.8f));
            UIHelper.DrawOutlinedRect(redH, Color.red * 0.8f);
            Widgets.Label(redH, "SmartDefense.Btn.RedCode".Translate());

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPawnRow(Rect row, Pawn p, int index, List<Pawn> pawns)
        {
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            bool hoveringRow = row.Contains(Event.current.mousePosition);
            Color rowBg = (index % 2 == 0) ? new Color(0.12f, 0.15f, 0.2f, 0.6f) : new Color(0.08f, 0.11f, 0.15f, 0.6f);
            if (hoveringRow) rowBg = Color.Lerp(rowBg, new Color(0.2f, 0.3f, 0.45f, 0.6f), 0.5f);
            UIHelper.DrawSolidColor(row, rowBg);
            UIHelper.DrawOutlinedRect(row, new Color(0.2f, 0.25f, 0.35f, 0.3f));

            string id = p.ThingID;

            Rect nameRect = new Rect(row.x + 10f, row.y, 190f, row.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, p.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            float curX = row.x + 210f;

            PawnRole currentRole = comp.pawnRoles.TryGetValue(id, out var r) ? r : PawnRole.Auto;
            Rect roleRect = new Rect(curX, row.y + 2f, 140f, 28f);
            string roleLabel;
            switch (currentRole)
            {
                case PawnRole.Combatant: roleLabel = "SmartDefense.Role.Combatant".Translate(); break;
                case PawnRole.Colonist: roleLabel = "SmartDefense.Role.Civilian".Translate(); break;
                default: roleLabel = DefenseManager.IsCombatant(p) ? "SmartDefense.Role.AutoCombatant".Translate() : "SmartDefense.Role.AutoCivilian".Translate(); break;
            }

            DrawDraggableCell(roleRect, 1, 0, index, roleLabel, currentRole,
                (startIdx, endIdx, val) => {
                    if (pawns == null) return;
                    for (int k = startIdx; k <= endIdx; k++)
                    {
                        if (k >= 0 && k < pawns.Count)
                            comp.pawnRoles[pawns[k].ThingID] = (PawnRole)val;
                    }
                },
                () => {
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("SmartDefense.Role.Auto".Translate(), () => comp.pawnRoles[id] = PawnRole.Auto),
                        new FloatMenuOption("SmartDefense.Role.CombatantAlways".Translate(), () => comp.pawnRoles[id] = PawnRole.Combatant),
                        new FloatMenuOption("SmartDefense.Role.CivilianAlways".Translate(), () => comp.pawnRoles[id] = PawnRole.Colonist)
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            );

            curX += 150f;

            string yellowArea = comp.pawnYellowAreas.TryGetValue(id, out var ya) ? ya : "";
            Rect yellowRect = new Rect(curX, row.y + 2f, 210f, 28f);
            string yellowLabel = string.IsNullOrEmpty(yellowArea) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : yellowArea;

            DrawDraggableCell(yellowRect, 1, 1, index, yellowLabel, yellowArea,
                (startIdx, endIdx, val) => {
                    if (pawns == null) return;
                    for (int k = startIdx; k <= endIdx; k++)
                    {
                        if (k >= 0 && k < pawns.Count)
                            comp.pawnYellowAreas[pawns[k].ThingID] = (string)val;
                    }
                },
                () => Find.WindowStack.Add(new FloatMenu(GetAreaOptions(a => comp.pawnYellowAreas[id] = a)))
            );

            curX += 220f;

            string redArea = comp.pawnRedAreas.TryGetValue(id, out var ra) ? ra : "";
            Rect redRect = new Rect(curX, row.y + 2f, 210f, 28f);
            string redLabel = string.IsNullOrEmpty(redArea) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : redArea;

            DrawDraggableCell(redRect, 1, 2, index, redLabel, redArea,
                (startIdx, endIdx, val) => {
                    if (pawns == null) return;
                    for (int k = startIdx; k <= endIdx; k++)
                    {
                        if (k >= 0 && k < pawns.Count)
                            comp.pawnRedAreas[pawns[k].ThingID] = (string)val;
                    }
                },
                () => Find.WindowStack.Add(new FloatMenu(GetAreaOptions(a => comp.pawnRedAreas[id] = a)))
            );
        }

        private void DrawAnimalsTab(Rect rect)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            List<Pawn> animals = map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps != null && p.RaceProps.Animal).ToList();

            DrawPawnTableHeaders(rect, false);

            Rect listRect = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, animals.Count * 36f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            for (int i = 0; i < animals.Count; i++)
            {
                Pawn p = animals[i];
                Rect row = new Rect(0f, i * 36f, viewRect.width, 32f);
                DrawSimpleCreatureRow(row, p, i, 2, animals, comp.animalYellowAreas, comp.animalRedAreas);
            }

            Widgets.EndScrollView();
        }

        private void DrawMechsTab(Rect rect)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            var comp = GameComponent_DefenseProtocols.Instance;
            if (comp == null) return;

            List<Pawn> mechs = map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                .Where(p => DefenseManager.IsMechOrDrone(p)).ToList();

            DrawPawnTableHeaders(rect, false);

            Rect listRect = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, mechs.Count * 36f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            for (int i = 0; i < mechs.Count; i++)
            {
                Pawn p = mechs[i];
                Rect row = new Rect(0f, i * 36f, viewRect.width, 32f);
                DrawSimpleCreatureRow(row, p, i, 3, mechs, comp.mechYellowAreas, comp.mechRedAreas);
            }

            Widgets.EndScrollView();
        }

        private void DrawSimpleCreatureRow(Rect row, Pawn p, int index, int tabIndex, List<Pawn> creatures, Dictionary<string, string> yellowDict, Dictionary<string, string> redDict)
        {
            bool hoveringRow = row.Contains(Event.current.mousePosition);
            Color rowBg = (index % 2 == 0) ? new Color(0.12f, 0.15f, 0.2f, 0.6f) : new Color(0.08f, 0.11f, 0.15f, 0.6f);
            if (hoveringRow) rowBg = Color.Lerp(rowBg, new Color(0.2f, 0.3f, 0.45f, 0.6f), 0.5f);
            UIHelper.DrawSolidColor(row, rowBg);
            UIHelper.DrawOutlinedRect(row, new Color(0.2f, 0.25f, 0.35f, 0.3f));

            string id = p.ThingID;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x + 10f, row.y, 190f, row.height), p.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            float curX = row.x + 210f;

            string yellowArea = yellowDict.TryGetValue(id, out var ya) ? ya : "";
            Rect yellowRect = new Rect(curX, row.y + 2f, 210f, 28f);
            string yellowLabel = string.IsNullOrEmpty(yellowArea) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : yellowArea;

            DrawDraggableCell(yellowRect, tabIndex, 1, index, yellowLabel, yellowArea,
                (startIdx, endIdx, val) => {
                    if (creatures == null) return;
                    for (int k = startIdx; k <= endIdx; k++)
                    {
                        if (k >= 0 && k < creatures.Count)
                            yellowDict[creatures[k].ThingID] = (string)val;
                    }
                },
                () => Find.WindowStack.Add(new FloatMenu(GetAreaOptions(a => yellowDict[id] = a)))
            );

            curX += 220f;

            string redArea = redDict.TryGetValue(id, out var ra) ? ra : "";
            Rect redRect = new Rect(curX, row.y + 2f, 210f, 28f);
            string redLabel = string.IsNullOrEmpty(redArea) ? "SmartDefense.Area.Unrestricted".Translate().ToString() : redArea;

            DrawDraggableCell(redRect, tabIndex, 2, index, redLabel, redArea,
                (startIdx, endIdx, val) => {
                    if (creatures == null) return;
                    for (int k = startIdx; k <= endIdx; k++)
                    {
                        if (k >= 0 && k < creatures.Count)
                            redDict[creatures[k].ThingID] = (string)val;
                    }
                },
                () => Find.WindowStack.Add(new FloatMenu(GetAreaOptions(a => redDict[id] = a)))
            );
        }

        /// <summary>
        /// Handles drag-and-select interaction strictly while holding Left Mouse Button.
        /// Releasing mouse instantly releases drag state without sticking.
        /// </summary>
        private void DrawDraggableCell(Rect rect, int tabIndex, int column, int rowIndex, string label, object currentValue, Action<int, int, object> applyRangeAction, Action openMenu)
        {
            Event e = Event.current;

            // Strict LKM release safety check
            if (!Input.GetMouseButton(0))
            {
                if (isDragging && activeTab == tabIndex && activeColumn == column)
                {
                    ResetDragState();
                }
            }

            bool isHovered = rect.Contains(e.mousePosition);

            // 1. Mouse Down -> Begin drag initiation
            if (isHovered && e.type == EventType.MouseDown && e.button == 0)
            {
                isDragging = true;
                activeTab = tabIndex;
                activeColumn = column;
                activeSourceValue = currentValue;
                dragStartIndex = rowIndex;
                dragStartPos = e.mousePosition;
                dragMovedFar = false;
            }

            // 2. Continuous drag application while holding LKM
            if (isDragging && activeTab == tabIndex && activeColumn == column && Input.GetMouseButton(0))
            {
                if (isHovered || Math.Abs(e.mousePosition.y - (rect.y + rect.height / 2f)) < rect.height / 2f)
                {
                    if (Vector2.Distance(dragStartPos, e.mousePosition) > 3f)
                    {
                        dragMovedFar = true;
                    }
                    if (dragMovedFar && isHovered)
                    {
                        int start = Math.Min(dragStartIndex, rowIndex);
                        int end = Math.Max(dragStartIndex, rowIndex);
                        applyRangeAction(start, end, activeSourceValue);
                    }
                }
            }

            // 3. UI Cell Visual Highlight
            bool isSelectedInDrag = isDragging && activeTab == tabIndex && activeColumn == column &&
                rowIndex >= Math.Min(dragStartIndex, rowIndex) && rowIndex <= Math.Max(dragStartIndex, rowIndex) && dragMovedFar;

            Color bg = (isHovered || isSelectedInDrag) ? new Color(0.2f, 0.28f, 0.4f, 0.9f) : new Color(0.13f, 0.16f, 0.22f, 0.75f);
            Color border = (isHovered || isSelectedInDrag) ? Color.cyan : new Color(0.3f, 0.4f, 0.5f, 0.5f);

            UIHelper.DrawSolidColor(rect, bg);
            UIHelper.DrawOutlinedRect(rect, border);

            TextAnchor prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height), label);
            Text.Anchor = prevAnchor;

            // 4. Trigger context menu on single click (not drag)
            if (Widgets.ButtonInvisible(rect))
            {
                if (!dragMovedFar)
                {
                    openMenu();
                }
            }
        }
    }

    /// <summary>
    /// Map component checking periodically for hostile raids and automatically managing DEFCON levels.
    /// </summary>
    public class MapComponent_ThreatMonitor : MapComponent
    {
        private bool wasInRedCode = false;

        public MapComponent_ThreatMonitor(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Если включен ручной режим — авто-переключение полностью отключается
            if (DefenseSettings.ManualModeOnly) return;

            // Проверяем раз в секунду (60 тиков)
            if (Find.TickManager.TicksGame % 60 != 0) return;

            bool hasActiveHostiles = AnyActiveHostiles();

            // 1. Появились ВРАГИ -> Включаем Красный код
            if (DefenseSettings.AutoRedOnRaid && hasActiveHostiles)
            {
                if (DefenseManager.CurrentAlert != AlertLevel.Red)
                {
                    DefenseManager.SetAlertLevel(AlertLevel.Red, map);
                }
                wasInRedCode = true;
            }
            // 2. Врагов НЕТ (все убиты/лежат/сбежали), но мы БЫЛИ в Красном коде -> Понижаем до Жёлтого
            else if (DefenseSettings.AutoYellowOnRaidEnd && !hasActiveHostiles && wasInRedCode && DefenseManager.CurrentAlert == AlertLevel.Red)
            {
                DefenseManager.SetAlertLevel(AlertLevel.Yellow, map);
                wasInRedCode = false;
            }
            else if (!hasActiveHostiles)
            {
                wasInRedCode = false;
            }
        }

        /// <summary>
        /// Проверяет наличие только АКТИВНЫХ (опасных, ходячих) врагов на карте.
        /// Упавшие без сознания (Downed) и мертвые враги игнорируются.
        /// </summary>
        private bool AnyActiveHostiles()
        {
            if (map == null || map.mapPawns == null) return false;

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null) return false;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];

                // Игнорируем мертвых, отсутствующих и сбитых с ног (Downed)
                if (p == null || p.Dead || p.Downed) continue;

                // Проверяем враждебность фракции
                if (GenHostility.HostileTo(p, Faction.OfPlayer))
                {
                    // Если это дикое животное без фракции — считаем его врагом только в состоянии психоза
                    if (p.Faction == null && p.MentalStateDef == null) continue;

                    return true;
                }
            }

            return false;
        }
    }

    public class Building_RallyPoint : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }

    /// <summary>
    /// Main mod class required by RimWorld loader.
    /// </summary>
    public class SmartDefenseProtocolsMod : Mod
    {
        public static DefenseSettings Settings;

        public SmartDefenseProtocolsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DefenseSettings>();
        }

        public override string SettingsCategory() => "SmartDefense.Window.Title".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Dialog_DefenseSettings dialog = new Dialog_DefenseSettings();
            dialog.DoWindowContents(inRect);
        }
    }
}