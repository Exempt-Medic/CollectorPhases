using Modding;
using System;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using Satchel;
using Satchel.BetterMenus;

namespace CollectorPhases
{
    #region Menu
    public static class ModMenu
    {
        private static Menu? MenuRef;
        public static MenuScreen CreateModMenu(MenuScreen modlistmenu)
        {
            MenuRef ??= new Menu("Collector Phase Options", new Element[]
            {
                new CustomSlider
                (
                    "Infinite Phase",
                    f =>
                    {
                        CollectorPhasesMod.LS.infinitePhase = (int)f;
                        CollectorPhasesMod.LS.HoG = (int)f != 1 && CollectorPhasesMod.LS.HoG;
                        CollectorPhasesMod.LS.HoG2 = (int)f != 1 && CollectorPhasesMod.LS.HoG2;
                        CollectorPhasesMod.LS.Pantheons = (int)f != 1 && CollectorPhasesMod.LS.Pantheons;
                        CollectorPhasesMod.LS.ToL = (int)f != 1 && CollectorPhasesMod.LS.ToL;
                        MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.infinitePhase,
                    1f,
                    3f,
                    true
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Ignore Initial Jar Limits",
                    "Ignore the 1 jar at at a time limitation for the first 2 jars",
                    (b) =>
                    {
                    CollectorPhasesMod.LS.IgnoreInitialJarLimit = b;
                    MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.IgnoreInitialJarLimit
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Tower of Love",
                    "Should the second Phase start immediately?",
                    (b) =>
                    {
                    CollectorPhasesMod.LS.ToL = CollectorPhasesMod.LS.infinitePhase != 1 && b;
                    MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.ToL
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Hall of Gods Attuned",
                    "Should the second Phase start immediately?",
                    (b) =>
                    {
                    CollectorPhasesMod.LS.HoG = CollectorPhasesMod.LS.infinitePhase != 1 && b;
                    MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.HoG
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Hall of Gods Ascended/Radiant",
                    "Should the second Phase start immediately?",
                    (b) =>
                    {
                    CollectorPhasesMod.LS.HoG2 = CollectorPhasesMod.LS.infinitePhase != 1 && b;
                    MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.HoG2
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Pantheons",
                    "Should the second Phase start immediately?",
                    (b) =>
                    {
                    CollectorPhasesMod.LS.Pantheons = CollectorPhasesMod.LS.infinitePhase != 1 && b;
                    MenuRef?.Update();
                    },
                    () => CollectorPhasesMod.LS.Pantheons
                ),

                new MenuButton
                (
                    "Reset Defaults",
                    "",
                    a =>
                    {
                        CollectorPhasesMod.LS.ToL = false;
                        CollectorPhasesMod.LS.Pantheons = false;
                        CollectorPhasesMod.LS.HoG = false;
                        CollectorPhasesMod.LS.HoG2 = false;
                        CollectorPhasesMod.LS.infinitePhase = 3;
                        CollectorPhasesMod.LS.IgnoreInitialJarLimit = false;
                        MenuRef?.Update();
                    }
                )
            });
            return MenuRef.GetMenuScreen(modlistmenu);
        }
    }
    #endregion

    public class CollectorPhasesMod : Mod, ICustomMenuMod, ILocalSettings<LocalSettings>
    {
        #region Boilerplate
        private static CollectorPhasesMod? _instance;
        internal static CollectorPhasesMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(CollectorPhasesMod)} was never constructed");
                }
                return _instance;
            }
        }
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) => ModMenu.CreateModMenu(modListMenu);
        public bool ToggleButtonInsideMenu => false;
        public static LocalSettings LS { get; private set; } = new();
        public void OnLoadLocal(LocalSettings s) => LS = s;
        public LocalSettings OnSaveLocal() => LS;
        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();
        public CollectorPhasesMod() : base("CollectorPhases")
        {
            _instance = this;
        }
        #endregion

        #region Init
        public override void Initialize()
        {
            Log("Initializing");

            On.PlayMakerFSM.OnEnable += FsmChanges;

            Log("Initialized");
        }
        #endregion

        #region Changes
        private void FsmChanges(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.gameObject.name == "Jar Collector" && self.FsmName == "Phase Control")
            {
                // Infinite Phase 1
                if (LS.infinitePhase == 1)
                {
                    self.RemoveFsmTransition("Init", "FINISHED");
                }

                // Grub Tower Collector
                else if (self.gameObject.scene.name.StartsWith("Ruins2_11"))
                {
                    self.ChangeFsmTransition("Init", "FINISHED", LS.ToL ? "Phase 2" : "Set");
                }

                // Attuned / Pantheon Collector
                else if (self.gameObject.scene.name == "GG_Collector")
                {
                    if (BossSceneController.IsBossScene && BossSceneController.Instance.BossLevel == 0)
                    {
                        self.ChangeFsmTransition("Init", "FINISHED", LS.HoG ? "Phase 2" : "Set");
                    }
                    else
                    {
                        self.ChangeFsmTransition("Init", "FINISHED", LS.Pantheons ? "Phase 2" : "Set");
                    }
                }

                // Ascended / Radiant Collector
                else if (self.gameObject.scene.name == "GG_Collector_V")
                {
                    self.ChangeFsmTransition("Init", "FINISHED", LS.HoG2 ? "Phase 2" : "Set");
                }
            }

            else if (self.gameObject.name == "Jar Collector" && self.FsmName == "Control")
            {
                // Jar Limits
                self.GetFirstActionOfType<IntCompare>("Resummon?").integer2.Value = LS.IgnoreInitialJarLimit ? 0 : 3;

                // Infinite Phase 2
                if (LS.infinitePhase != 3)
                {
                    self.RemoveFsmGlobalTransition("ZERO HP");
                }
            }
        }
        #endregion
    }
    #region Settings
    public class LocalSettings
    {
        public int infinitePhase = 3;
        public bool IgnoreInitialJarLimit = false;
        public bool ToL = false; // Tower of Love
        public bool Pantheons = false; // Pantheon of Hallownest
        public bool HoG = false; // Hall of Gods
        public bool HoG2 = false; // Hall of Gods Ascended / Radiant
    }
    #endregion
}
