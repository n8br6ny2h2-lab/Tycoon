using System;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    /// <summary>
    /// Phone-first entry flow: title screen over the live 3D factory (no account needed — progress is tied to the
    /// device, iCloud and Game Center), first-launch setup (language + company name), pause menu and help.
    /// Primary action ("Play") sits low on the screen within thumb reach; secondary actions below it.
    /// </summary>
    public static class TitleScreens
    {
        public static void ShowTitle(this UiManager ui, Action play, Action<bool> attract)
        {
            var C = ui.Context;
            var s = C.Session;
            attract?.Invoke(true);
            ui.ShowFullscreen((root, close) =>
            {
                root.style.justifyContent = Justify.SpaceBetween;

                var top = new VisualElement();
                top.style.alignItems = Align.Center;
                top.Add(UiKit.Label("♛", 150, true, UiKit.Accent));
                var title = UiKit.Label("FACTORY KING", 92, true);
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                top.Add(title);
                top.Add(UiKit.Label(C.T("game.title"), 40, false, UiKit.Muted));
                root.Add(top);

                var panel = UiKit.Box(new Color(0.08f, 0.10f, 0.14f, 0.9f), 34, 30, 30);
                if (!string.IsNullOrEmpty(s.State.CompanyName))
                    panel.Add(UiKit.Label(s.State.CompanyName, 46, true, UiKit.Text, true));
                panel.Add(UiKit.Label(C.TF("hud.season_left", s.Seasons.Number, NumberFormatter.FormatDuration(s.Seasons.SecondsLeft)), 30, false, UiKit.Warn));
                panel.Add(UiKit.Label(C.TF("season.factories", s.Seasons.OwnedFactories, s.Content.Factories.Count) + "  ·  " +
                                      C.TF("ui.company_level", s.CompanyLevel), 28, false, UiKit.Muted, true));
                panel.Add(UiKit.Spacer(12));

                bool fresh = s.State.LifetimeEarnings.IsZero;
                var playBtn = UiKit.Button(fresh ? C.T("menu.play") : C.T("menu.continue"), () =>
                {
                    C.Audio?.Play(Sfx.Click);
                    close();
                    attract?.Invoke(false);
                    play?.Invoke();
                }, UiKit.Accent, 52, 150);
                panel.Add(playBtn);

                var row = UiKit.Row();
                void Small(string key, Action a)
                {
                    var b = UiKit.Button(C.T(key), a, UiKit.CardHi, 28, 100);
                    b.style.flexGrow = 1;
                    b.style.flexBasis = 0;
                    row.Add(b);
                }
                Small("screen.season", () => ui.Open("season"));
                Small("screen.cosmetics", () => ui.Open("cosmetics"));
                Small("menu.help", () => ui.Open("help"));
                panel.Add(row);
                var row2 = UiKit.Row();
                row = row2;
                Small("screen.settings", () => ui.Open("settings"));
                if (C.GameCenter != null) Small("gc.leaderboards", () =>
                {
                    if (C.GameCenter.Authenticated) C.GameCenter.ShowLeaderboards(); else C.GameCenter.SignIn();
                });
                Small(C.Loc.Current == Language.Turkish ? "menu.lang_en" : "menu.lang_tr", () =>
                {
                    var next = C.Loc.Current == Language.Turkish ? Language.English : Language.Turkish;
                    s.State.Settings.Language = (int)next;
                    close();
                    C.Loc.SetLanguage(next);
                    ui.ShowTitle(play, attract);
                });
                panel.Add(row2);
                panel.Add(UiKit.Label(C.T("menu.cloud_note"), 22, false, UiKit.Muted, true));
                root.Add(panel);
            });
        }

        /// <summary>First launch only: language → company name → play. Short, skippable name (default offered).</summary>
        public static void ShowOnboarding(this UiManager ui, Action done)
        {
            var C = ui.Context;
            var s = C.Session;
            ui.ShowFullscreen((root, close) =>
            {
                root.style.justifyContent = Justify.Center;
                var panel = UiKit.Box(new Color(0.08f, 0.10f, 0.14f, 0.94f), 34, 34, 34);
                panel.Add(UiKit.Label(C.T("onboard.welcome"), 46, true, UiKit.Accent, true));
                panel.Add(UiKit.Label(C.T("onboard.pitch"), 30, false, UiKit.Text, true));
                panel.Add(UiKit.Spacer(14));

                panel.Add(UiKit.Label(C.T("settings.language"), 30, true));
                var langs = UiKit.Row();
                var en = UiKit.Button("English", null, C.Loc.Current == Language.English ? UiKit.Accent : UiKit.CardHi, 30, 96);
                var tr = UiKit.Button("Türkçe", null, C.Loc.Current == Language.Turkish ? UiKit.Accent : UiKit.CardHi, 30, 96);
                en.style.flexGrow = 1; tr.style.flexGrow = 1;
                en.clicked += () => { s.State.Settings.Language = (int)Language.English; close(); C.Loc.SetLanguage(Language.English); ui.ShowOnboarding(done); };
                tr.clicked += () => { s.State.Settings.Language = (int)Language.Turkish; close(); C.Loc.SetLanguage(Language.Turkish); ui.ShowOnboarding(done); };
                langs.Add(en); langs.Add(tr);
                panel.Add(langs);
                panel.Add(UiKit.Spacer(14));

                panel.Add(UiKit.Label(C.T("onboard.name"), 30, true));
                var field = new TextField { maxLength = CompanyNameRules.MaxLength, value = C.T("onboard.default_name") };
                field.style.fontSize = 38;
                field.style.minHeight = 100;
                field.style.marginTop = 8;
                field.style.marginBottom = 4;
                panel.Add(field);
                var hint = UiKit.Label(C.T("onboard.name_hint"), 24, false, UiKit.Muted, true);
                panel.Add(hint);
                panel.Add(UiKit.Spacer(14));

                panel.Add(UiKit.Button(C.T("onboard.start"), () =>
                {
                    string name = CompanyNameRules.Sanitize(field.value);
                    if (name == null)
                    {
                        hint.text = C.T("onboard.name_invalid");
                        hint.style.color = UiKit.Bad;
                        C.Audio?.Play(Sfx.Error);
                        return;
                    }
                    s.State.CompanyName = name;
                    s.State.Settings.OnboardingDone = true;
                    C.Audio?.Play(Sfx.Success);
                    close();
                    done?.Invoke();
                }, UiKit.Accent, 46, 140));
                panel.Add(UiKit.Label(C.T("menu.cloud_note"), 22, false, UiKit.Muted, true));
                root.Add(panel);
            });
        }

        public static void ShowPauseMenu(this UiManager ui, Action toTitle, Action resetView)
        {
            var C = ui.Context;
            ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(C.T("pause.title"), 46, true));
                if (!string.IsNullOrEmpty(C.Session.State.CompanyName))
                    box.Add(UiKit.Label(C.Session.State.CompanyName, 32, false, UiKit.Muted));
                box.Add(UiKit.Button(C.T("pause.resume"), close, UiKit.Accent, 40, 120));
                box.Add(UiKit.Button(C.T("pause.home_view"), () => { close(); resetView?.Invoke(); }, UiKit.CardHi));
                box.Add(UiKit.Button(C.T("menu.help"), () => { close(); ui.Open("help"); }, UiKit.CardHi));
                box.Add(UiKit.Button(C.T("screen.settings"), () => { close(); ui.Open("settings"); }, UiKit.CardHi));
                box.Add(UiKit.Button(C.T("pause.main_menu"), () => { close(); ui.Close(); toTitle?.Invoke(); }, UiKit.CardHi));
            });
        }
    }

    /// <summary>"How to play": five short cards, readable in under a minute.</summary>
    public sealed class HelpScreen : UiScreen
    {
        public override string Id => "help";
        public override string TitleKey => "menu.help";

        protected override void Build(VisualElement root)
        {
            for (int i = 1; i <= 6; i++)
            {
                var card = Card(root);
                card.Add(UiKit.Label(T("help.t" + i), 36, true, UiKit.Accent, true));
                card.Add(UiKit.Label(T("help.b" + i), 28, false, UiKit.Text, true));
            }
        }
    }
}
