using System;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Tier chips: rounded fill + brighter rounded border, Black font.
    /// Placement chips: square fill, no border, Black font.
    /// </summary>
    public sealed partial class CareerDemoController
    {
        private Font _uiFontBlack;
        private Sprite _roundedRectSprite;

        private void EnsureCompetitionBadgeResources()
        {
            if (_uiFontBlack == null)
                _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont700 ?? _uiFont;
            if (_roundedRectSprite == null)
                _roundedRectSprite = UiRoundedRectUtility.GetRoundedRectSprite(64, 14);
        }

        private static Color BrightenBadgeColor(Color fill, float amount = 0.22f) =>
            new Color(
                Mathf.Min(1f, fill.r + amount),
                Mathf.Min(1f, fill.g + amount),
                Mathf.Min(1f, fill.b + amount),
                fill.a);

        private static bool TryGetTierBadgeStyle(CompetitionTier tier, out Color fill, out string label)
        {
            switch (tier)
            {
                case CompetitionTier.C:
                    fill = CareerEditorialChrome.Mustard;
                    label = "C-Tier";
                    return true;
                case CompetitionTier.B:
                    fill = CareerEditorialChrome.Ink;
                    label = "B-Tier";
                    return true;
                case CompetitionTier.A:
                    fill = CareerEditorialChrome.Cyan;
                    label = "A-Tier";
                    return true;
                case CompetitionTier.S:
                    fill = CareerEditorialChrome.Mustard;
                    label = "S-Tier";
                    return true;
                case CompetitionTier.Elite:
                    fill = CareerEditorialChrome.Cyan;
                    label = "Elite-Tier";
                    return true;
                case CompetitionTier.SuperElite:
                    fill = CareerEditorialChrome.Red;
                    label = "Super Elite-Tier";
                    return true;
                case CompetitionTier.Major:
                    fill = CareerEditorialChrome.Red;
                    label = "Major-Tier";
                    return true;
                case CompetitionTier.D:
                    fill = CareerEditorialChrome.Ink;
                    label = "D-Tier";
                    return true;
                case CompetitionTier.Exhibition:
                    fill = CareerEditorialChrome.Ink;
                    label = "Exhibition";
                    return true;
                default:
                    fill = CareerEditorialChrome.Ink;
                    label = tier + "-Tier";
                    return true;
            }
        }

        private static bool TryGetPlacementBadgeStyle(int placement, out Color fill, out string label)
        {
            if (placement <= 0)
            {
                fill = default;
                label = null;
                return false;
            }

            if (placement == 1)
            {
                fill = CareerEditorialChrome.Mustard;
                label = "1st";
                return true;
            }
            if (placement == 2)
            {
                fill = CareerEditorialChrome.Cyan;
                label = "2nd";
                return true;
            }
            // Knockout has no bronze match — both semi losers share 3rd–4th.
            if (placement <= 4)
            {
                fill = CareerEditorialChrome.Red;
                label = "3rd-4th";
                return true;
            }
            if (placement <= 8)
            {
                fill = CareerEditorialChrome.Cyan;
                label = "5th-8th";
                return true;
            }
            if (placement <= 16)
            {
                fill = CareerEditorialChrome.Ink;
                label = "9th-16th";
                return true;
            }

            fill = CareerEditorialChrome.Ink;
            label = placement + "th";
            return true;
        }

        private RectTransform AddTierBadge(
            Transform parent, CompetitionTier tier, float preferredHeight = 22f, int fontSize = 12)
        {
            EnsureCompetitionBadgeResources();
            if (!TryGetTierBadgeStyle(tier, out Color fill, out string label))
                return null;

            Color border = BrightenBadgeColor(fill);
            float width = EstimateBadgeTextWidth(label, fontSize) + 22f;

            Image edge = CreateUiObject<Image>("TierBadge", parent);
            edge.sprite = _roundedRectSprite;
            edge.type = Image.Type.Sliced;
            edge.color = border;
            edge.raycastTarget = false;
            LayoutElement edgeLe = edge.gameObject.AddComponent<LayoutElement>();
            edgeLe.preferredHeight = preferredHeight;
            edgeLe.minHeight = preferredHeight;
            edgeLe.preferredWidth = width;
            edgeLe.minWidth = width;
            edgeLe.flexibleWidth = 0f;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            face.sprite = _roundedRectSprite;
            face.type = Image.Type.Sliced;
            face.color = fill;
            face.raycastTarget = false;

            Text text = CreateText("Label", face.transform, fontSize, TextAnchor.MiddleCenter, Color.white, _uiFontBlack);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyle.Normal;
            text.text = label;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return edge.rectTransform;
        }

        private RectTransform AddPlacementBadge(
            Transform parent, int placement, float preferredHeight = 22f, int fontSize = 12)
        {
            EnsureCompetitionBadgeResources();
            if (!TryGetPlacementBadgeStyle(placement, out Color fill, out string label))
                return null;

            float width = EstimateBadgeTextWidth(label, fontSize) + 16f;
            Image face = CreateUiObject<Image>("PlacementBadge", parent);
            face.color = fill;
            face.raycastTarget = false;
            LayoutElement faceLe = face.gameObject.AddComponent<LayoutElement>();
            faceLe.preferredHeight = preferredHeight;
            faceLe.minHeight = preferredHeight;
            faceLe.preferredWidth = width;
            faceLe.minWidth = width;
            faceLe.flexibleWidth = 0f;

            Text text = CreateText("Label", face.transform, fontSize, TextAnchor.MiddleCenter, Color.white, _uiFontBlack);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyle.Normal;
            text.text = label;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return face.rectTransform;
        }

        private static float EstimateBadgeTextWidth(string label, int fontSize)
        {
            if (string.IsNullOrEmpty(label))
                return fontSize * 2f;
            // Black Latin glyphs are wide; keep chip readable without relying on preferred-size text.
            return Mathf.Max(28f, label.Length * fontSize * 0.62f + 4f);
        }
    }
}
