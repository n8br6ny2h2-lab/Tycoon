using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    /// <summary>Colour palette and element factories for the code-built UI Toolkit interface.</summary>
    public static class UiKit
    {
        public static readonly Color Bg = Hex("#151A22F2");
        public static readonly Color Card = Hex("#212834");
        public static readonly Color CardHi = Hex("#2B3443");
        public static readonly Color Text = Hex("#F4F6FA");
        public static readonly Color Muted = Hex("#9AA6B8");
        public static readonly Color Accent = Hex("#FFA62B");
        public static readonly Color AccentDark = Hex("#C97A10");
        public static Color Good = Hex("#3DDC97");
        public static Color Warn = Hex("#FFC531");
        public static Color Bad = Hex("#FF5A4E");

        /// <summary>Colour-blind friendly palette (blue / orange / magenta instead of green / yellow / red).</summary>
        public static void SetColorblind(bool on)
        {
            Good = on ? Hex("#3A86FF") : Hex("#3DDC97");
            Warn = on ? Hex("#FFBE0B") : Hex("#FFC531");
            Bad = on ? Hex("#FB5607") : Hex("#FF5A4E");
        }
        public static readonly Color Diamond = Hex("#7FD8FF");
        public static readonly Color Disabled = Hex("#4A5262");
        public static readonly Color Overlay = Hex("#05080DB0");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static Color RarityColor(int rarity) =>
            rarity switch { 1 => Hex("#4FA3FF"), 2 => Hex("#B36BFF"), 3 => Hex("#FFC83D"), _ => Hex("#A9B4C2") };

        public static void Radius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        public static void Pad(VisualElement e, float v, float h)
        {
            e.style.paddingTop = v; e.style.paddingBottom = v; e.style.paddingLeft = h; e.style.paddingRight = h;
        }

        public static void Margin(VisualElement e, float v, float h)
        {
            e.style.marginTop = v; e.style.marginBottom = v; e.style.marginLeft = h; e.style.marginRight = h;
        }

        public static VisualElement Box(Color bg, float radius = 18, float padV = 18, float padH = 22)
        {
            var e = new VisualElement();
            e.style.backgroundColor = bg;
            Radius(e, radius);
            Pad(e, padV, padH);
            return e;
        }

        public static VisualElement Row(float gap = 0, Justify justify = Justify.FlexStart)
        {
            var e = new VisualElement();
            e.style.flexDirection = FlexDirection.Row;
            e.style.alignItems = Align.Center;
            e.style.justifyContent = justify;
            return e;
        }

        public static Label Label(string text, float size = 34, bool bold = false, Color? color = null, bool wrap = false)
        {
            var l = new Label(text);
            l.style.fontSize = size;
            l.style.color = color ?? Text;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.whiteSpace = wrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            l.style.marginTop = 2; l.style.marginBottom = 2;
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        /// <summary>Large touch-friendly button (min 96 px tall at the 1080x1920 reference = ~48 pt).</summary>
        public static Button Button(string text, Action onClick, Color? bg = null, float size = 34, float height = 104)
        {
            var b = new Button(() => onClick?.Invoke()) { text = text };
            var color = bg ?? Accent;
            b.style.backgroundColor = color;
            b.style.color = color == Accent || color == Good || color == Warn ? Hex("#1A1206") : Text;
            b.style.fontSize = size;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.minHeight = height;
            b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0; b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0;
            b.style.whiteSpace = WhiteSpace.Normal;
            Radius(b, 20);
            Margin(b, 6, 6);
            Pad(b, 8, 18);
            b.userData = color;
            return b;
        }

        public static void SetEnabled(Button b, bool enabled)
        {
            if (b.enabledSelf == enabled) return;
            b.SetEnabled(enabled);
            var color = b.userData is Color c ? c : Accent;
            b.style.backgroundColor = enabled ? color : Disabled;
            b.style.opacity = enabled ? 1f : 0.8f;
        }

        public static VisualElement ProgressBar(out VisualElement fill, Color? color = null, float height = 18)
        {
            var bar = new VisualElement();
            bar.style.height = height;
            bar.style.backgroundColor = Hex("#0E1218");
            Radius(bar, height / 2);
            bar.style.overflow = Overflow.Hidden;
            bar.style.marginTop = 8; bar.style.marginBottom = 8;
            fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = color ?? Accent;
            Radius(fill, height / 2);
            bar.Add(fill);
            return bar;
        }

        public static void SetProgress(VisualElement fill, double value01) =>
            fill.style.width = Length.Percent((float)(Math.Max(0, Math.Min(1, value01)) * 100.0));

        public static VisualElement Spacer(float h = 16)
        {
            var e = new VisualElement();
            e.style.height = h;
            e.style.flexShrink = 0;
            return e;
        }

        public static VisualElement Grow()
        {
            var e = new VisualElement();
            e.style.flexGrow = 1;
            return e;
        }
    }

    /// <summary>Holds per-screen live bindings refreshed a few times per second (no per-frame allocations).</summary>
    public sealed class Bindings
    {
        private readonly List<Action> _updates = new List<Action>();

        public void Clear() => _updates.Clear();
        public void Refresh() { for (int i = 0; i < _updates.Count; i++) _updates[i](); }

        public Label Text(Label label, Func<string> text)
        {
            string last = null;
            _updates.Add(() =>
            {
                string t = text();
                if (t != last) { last = t; label.text = t; }
            });
            return label;
        }

        public Button Button(Button button, Func<string> text, Func<bool> enabled)
        {
            string last = null;
            _updates.Add(() =>
            {
                if (text != null)
                {
                    string t = text();
                    if (t != last) { last = t; button.text = t; }
                }
                if (enabled != null) UiKit.SetEnabled(button, enabled());
            });
            return button;
        }

        public void Progress(VisualElement fill, Func<double> value) => _updates.Add(() => UiKit.SetProgress(fill, value()));

        public void Visible(VisualElement e, Func<bool> visible) =>
            _updates.Add(() => e.style.display = visible() ? DisplayStyle.Flex : DisplayStyle.None);

        public void Custom(Action a) => _updates.Add(a);
    }
}
