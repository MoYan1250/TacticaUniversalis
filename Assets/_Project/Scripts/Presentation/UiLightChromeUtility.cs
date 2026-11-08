using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Career chrome helpers. Fills follow the print system in CareerEditorialChrome.
    /// </summary>
    public static class UiLightChromeUtility
    {
        public const float PillAspectMin = 0.75f;
        public const float PillAspectMax = 1.35f;
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public static readonly Color Border = new Color(1f, 1f, 1f, 0.28f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.20f);

        /// <summary>Paper fill. Career no longer uses navy glass.</summary>
        public static readonly Color GlassFace = CareerEditorialChrome.Paper;
        /// <summary>
        /// Multiply tint for blurred RawImage — cool navy, not pure white, so frost stays readable.
        /// Alpha stays 1 so the slice does not punch through the mask.
        /// </summary>
        public static readonly Color FrostBlurTint = new Color(0.96f, 0.95f, 0.92f, 1f);
        /// <summary>Paper film. Career blur slices are not used for new surfaces.</summary>
        public static readonly Color FrostBlurVeil = new Color(0.965f, 0.953f, 0.925f, 0.40f);
        /// <summary>Nested paper on a career surface.</summary>
        public static readonly Color SoftNavyFace = CareerEditorialChrome.Paper;
        /// <summary>Explicit clear inset when a caller needs a hole; CreateInsetFace uses SoftNavyFace for readability.</summary>
        public static readonly Color GlassInset = Color.clear;

        public static readonly Color CapsuleRim = new Color(0.08f, 0.08f, 0.07f, 0.45f);
        public static readonly Color CapsuleFace = CareerEditorialChrome.Paper;
        public static readonly Color CapsuleFaceSelected = CareerEditorialChrome.Paper;
        public static readonly Color CapsuleRimPrimary = CareerEditorialChrome.Mustard;
        public static readonly Color CapsuleFacePrimary = CareerEditorialChrome.Paper;
        public static readonly Color CapsuleRimDanger = CareerEditorialChrome.Red;
        public static readonly Color CapsuleFaceDanger = Color.white;
        /// <summary>Pure black label/icon on white CTAs.</summary>
        public static readonly Color CapsuleLabel = Color.black;
        public static readonly Color CapsuleHoverFace = Hex(0x1E1E22);
        public static readonly Color CapsuleHoverLabel = Color.white;

        /// <summary>Hairline on paper.</summary>
        public static readonly Color FrostedRow = CareerEditorialChrome.Rule;

        /// <summary>Warm paper plane.</summary>
        public static readonly Color FlatFace = CareerEditorialChrome.Paper;
        /// <summary>Ink hairline.</summary>
        public static readonly Color FlatBorder = new Color(0.08f, 0.08f, 0.07f, 0.22f);
        /// <summary>Nested paper.</summary>
        public static readonly Color FlatRow = CareerEditorialChrome.Paper;

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        public static Color ToGlassFill(Color _) => GlassFace;

        public static Color ToGlassInset(Color _) => GlassInset;

        public static Sprite ShapeSpriteForAnchors(Vector2 anchorMin, Vector2 anchorMax)
        {
            float w = Mathf.Abs(anchorMax.x - anchorMin.x) * ReferenceWidth;
            float h = Mathf.Abs(anchorMax.y - anchorMin.y) * ReferenceHeight;
            float aspect = w / Mathf.Max(1f, h);
            if (aspect >= PillAspectMin && aspect <= PillAspectMax)
                return UiRoundedRectUtility.GetRoundedRectSprite();
            return UiRoundedRectUtility.GetPillSprite();
        }

        /// <summary>Flat near-black plane. Career shells no longer capture blur.</summary>
        public static Image ApplyRoundedGlassPanel(Image panel)
        {
            if (panel == null)
                return null;
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = FlatFace;
            return panel;
        }

        /// <summary>Nested plane on a flat card. Almost clear so type, not a navy fill, marks the region.</summary>
        public static Image ApplySoftNavyPanel(Image panel)
        {
            if (panel == null)
                return null;
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = FlatRow;
            return panel;
        }

        /// <summary>List row on a flat card. Almost clear; callers distinguish rows by type weight.</summary>
        public static void ApplyFrostedListRow(Image row)
        {
            if (row == null)
                return;
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            row.sprite = rounded;
            row.type = Image.Type.Sliced;
            row.color = FlatRow;
        }

        public static Image CreateCard(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color fill,
            bool addClickBlocker = true,
            float faceInset = 2.5f,
            float shadowOffset = 3.5f)
        {
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();

            GameObject cardGo = new GameObject(name, typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            SetRect(cardRect, anchorMin, anchorMax);

            Image frame = CreateImage("Frame", cardGo.transform);
            Stretch(frame.rectTransform);
            frame.sprite = rounded;
            frame.type = Image.Type.Sliced;
            frame.color = FlatBorder;
            frame.raycastTarget = false;

            Image face = CreateImage("Face", frame.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(faceInset, faceInset);
            face.rectTransform.offsetMax = new Vector2(-faceInset, -faceInset);
            face.sprite = rounded;
            face.type = Image.Type.Sliced;
            face.color = FlatFace;
            face.raycastTarget = true;

            if (addClickBlocker)
            {
                Button blocker = face.gameObject.AddComponent<Button>();
                blocker.targetGraphic = face;
                blocker.transition = Selectable.Transition.None;
            }

            return face;
        }

        public static Image CreateInsetFace(Transform chromeFace, Color fill, float inset = 2f)
        {
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Image inner = CreateImage("Inner", chromeFace);
            Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(inset, inset);
            inner.rectTransform.offsetMax = new Vector2(-inset, -inset);
            inner.sprite = rounded;
            inner.type = Image.Type.Sliced;
            inner.color = FlatRow;
            inner.raycastTarget = true;
            return inner;
        }

        public static Button CreateGlassCapsuleButton(
            string name,
            string label,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityAction action,
            Font font = null,
            int fontSize = 16,
            bool fillLabel = true,
            bool primary = false,
            bool danger = false,
            float faceInset = 1.5f,
            bool forceSquare = false)
        {
            Color rim = new Color(1f, 1f, 1f, 0.45f);
            Color faceColor = FlatFace;
            if (danger)
                rim = CapsuleRimDanger;
            else if (primary)
                rim = CapsuleRimPrimary;

            Sprite shape = forceSquare
                ? UiRoundedRectUtility.GetSoftSquareSprite()
                : ShapeSpriteForAnchors(anchorMin, anchorMax);

            Image edge = CreateImage(name, parent);
            SetRect(edge.rectTransform, anchorMin, anchorMax);
            edge.sprite = shape;
            edge.type = Image.Type.Sliced;
            edge.color = rim;
            edge.raycastTarget = true;

            Image face = CreateImage("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(faceInset, faceInset);
            face.rectTransform.offsetMax = new Vector2(-faceInset, -faceInset);
            face.sprite = shape;
            face.type = Image.Type.Sliced;
            face.color = faceColor;
            face.raycastTarget = true;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            // Face/label invert is driven by EventTrigger; keep ColorTint off the face wash.
            button.transition = Selectable.Transition.None;
            if (action != null)
                button.onClick.AddListener(action);

            Text text = CreateLabel("Text", face.transform, fontSize, font);
            text.text = label ?? string.Empty;
            text.color = CareerEditorialChrome.Ink;
            if (fillLabel)
                ApplyFilledButtonLabel(text, Mathf.Max(36, fontSize + 12));
            else
            {
                Stretch(text.rectTransform);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = fontSize;
            }

            WireWhiteButtonHoverInvert(button, face, text);
            return button;
        }

        public static Button CreateSquareCloseButton(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityAction action,
            Font font = null,
            int fontSize = 22)
        {
            // Slot keeps the caller's layout region; the button itself is forced square inside it.
            GameObject slotGo = new GameObject(name + "_Slot", typeof(RectTransform));
            slotGo.transform.SetParent(parent, false);
            SetRect(slotGo.GetComponent<RectTransform>(), anchorMin, anchorMax);

            Button button = CreateGlassCapsuleButton(
                name, "x", slotGo.transform, Vector2.zero, Vector2.one, action,
                font, fontSize, fillLabel: true, primary: false, danger: false,
                faceInset: 1.5f, forceSquare: true);

            RectTransform buttonRt = button.transform as RectTransform;
            buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = Vector2.zero;

            AspectRatioFitter fitter = button.gameObject.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = button.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            // Square frame = 90% of parent slot short side (FitInParent is 100%).
            buttonRt.localScale = new Vector3(0.9f, 0.9f, 1f);

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                ApplyFilledButtonLabel(label, maxSize: 128, heightFill: 0.9f);

            return button;
        }

        public static Button CreateGlassCapsuleButton(
            string name,
            string label,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color edgeColor,
            Color faceColor,
            UnityAction action,
            Font font = null,
            int fontSize = 16,
            bool fillLabel = true)
        {
            bool danger = LooksDanger(edgeColor) || LooksDanger(faceColor);
            bool primary = !danger && (LooksAccent(edgeColor) || LooksAccent(faceColor));
            bool forceSquare = IsCloseGlyph(label);
            return CreateGlassCapsuleButton(
                name, label, parent, anchorMin, anchorMax, action,
                font, fontSize, fillLabel, primary, danger, 1.5f, forceSquare);
        }

        /// <summary>
        /// Dark hairline CTA: white text at rest. Hover and press invert to a white face and black text.
        /// A light rest face still inverts to the dark face and white text.
        /// </summary>
        public static void WireWhiteButtonHoverInvert(Button button, Image face, Text label)
        {
            if (button == null || face == null)
                return;

            Color restFace = face.color.a > 0.5f ? face.color : FlatFace;
            Color restLabel = label != null ? label.color : Color.white;
            bool hovering = false;
            bool pressing = false;
            UiPressScale press = button.GetComponent<UiPressScale>();
            if (press == null)
                press = button.gameObject.AddComponent<UiPressScale>();

            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            bool RestIsLight(Color color) =>
                color.a > 0.75f && (color.r + color.g + color.b) / 3f > 0.75f;

            void ApplyContentColors(Color content)
            {
                if (label != null)
                    label.color = content;

                Image[] icons = face.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] == null || icons[i] == face)
                        continue;
                    if (!string.Equals(icons[i].name, "Icon", StringComparison.Ordinal))
                        continue;
                    icons[i].color = content;
                }
            }

            void Sync()
            {
                if (!button.interactable)
                {
                    face.color = new Color(0.12f, 0.12f, 0.14f, 0.55f);
                    if (label != null)
                        label.color = new Color(0.55f, 0.57f, 0.60f, 1f);
                    return;
                }

                bool inverted = hovering || pressing;
                bool light = RestIsLight(restFace);
                if (inverted)
                {
                    face.color = light ? CapsuleHoverFace : Color.white;
                    ApplyContentColors(light ? Color.white : Color.black);
                }
                else
                {
                    face.color = restFace;
                    ApplyContentColors(light ? Color.black : restLabel);
                }
            }

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                if (!hovering && !pressing)
                {
                    restFace = face.color.a > 0.5f ? face.color : FlatFace;
                    if (label != null)
                        restLabel = label.color;
                }
                hovering = true;
                Sync();
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
            {
                hovering = false;
                bool wasPressing = pressing;
                pressing = false;
                Sync();
                if (wasPressing && press != null)
                    press.SetPressed(false);
            });
            AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
            {
                pressing = true;
                Sync();
                if (press != null)
                    press.SetPressed(true);
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, _ =>
            {
                pressing = false;
                Sync();
                if (press != null)
                    press.SetPressed(false);
            });
            Sync();
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> cb)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(cb);
            trigger.triggers.Add(entry);
        }

        public static bool IsCloseGlyph(string label)
        {
            if (string.IsNullOrEmpty(label))
                return false;
            string t = label.Trim();
            return t == "×" || t == "X" || t == "x";
        }

        public static bool LooksAccent(Color c) =>
            c.r > 0.45f && c.g > 0.25f && c.b < 0.45f && c.r > c.b + 0.15f;

        public static bool LooksDanger(Color c) =>
            c.r > 0.55f && c.g < 0.55f && c.b < 0.55f && c.r > c.g + 0.2f && c.r > c.b + 0.2f;

        private static void ApplyFilledButtonLabel(Text text, int maxSize = 48, float heightFill = 0.9f)
        {
            if (text == null)
                return;
            float yInset = Mathf.Clamp01((1f - Mathf.Clamp(heightFill, 0.2f, 1f)) * 0.5f);
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, new Vector2(0.05f, yInset), new Vector2(0.95f, 1f - yInset));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = Mathf.Max(maxSize, text.fontSize);
        }

        private static Text CreateLabel(string name, Transform parent, int size, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
