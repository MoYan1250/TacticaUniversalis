using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private void PlaceStandingIcon(
            Transform parent, string name, string statusRaw,
            float anchorX, float anchorY, float pixelSize,
            string tooltipKind = null)
        {
            string id = PlayerStandingRules.NormalizeId(statusRaw);
            string standingName = PlayerStandingRules.DisplayName(id);
            string body = PlayerStandingRules.Description(id);
            string title = string.IsNullOrWhiteSpace(tooltipKind)
                ? standingName
                : $"{tooltipKind} · {standingName}";

            Image root = CreateUiObject<Image>(name, parent);
            RectTransform rect = root.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(pixelSize, pixelSize);
            root.color = Color.clear;
            root.raycastTarget = true;

            Image icon = CreateUiObject<Image>("Icon", root.transform);
            Stretch(icon.rectTransform);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            string resource = PlayerStandingRules.IconResourcePath(id);
            Sprite sprite = !string.IsNullOrEmpty(resource)
                ? Resources.Load<Sprite>(resource)
                : null;
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                icon.color = new Color(0.25f, 0.26f, 0.32f, 1f);
                Text fallback = CreateText("Fallback", root.transform, 11, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
                Stretch(fallback.rectTransform);
                fallback.text = standingName.Length <= 2 ? standingName : "?";
                fallback.raycastTarget = false;
            }

            EventTrigger trigger = root.gameObject.AddComponent<EventTrigger>();
            string capturedTitle = title;
            string capturedBody = body;
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                CancelTitleHover();
                HideTitleHoverPopup();
                CancelStatTooltip();
                float height = Mathf.Clamp(150f + capturedBody.Length * 0.55f, 160f, 280f);
                ShowPlainTooltip(capturedTitle, capturedBody, new Vector2(360f, height), Input.mousePosition);
            });
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => HideStatTooltip());
        }

        /// <summary>
        /// Compact pair: historical on the left, current on the right.
        /// Used by transfer market and club-select / intel rosters.
        /// </summary>
        private void PlaceStandingPair(
            Transform parent, string historicalRaw, string currentRaw,
            float histAnchorX, float currAnchorX, float pixelSize = 28f)
        {
            PlaceStandingIcon(parent, "HistoricalStanding", historicalRaw,
                histAnchorX, 0.5f, pixelSize, "歷史地位");
            PlaceStandingIcon(parent, "CurrentStanding", currentRaw,
                currAnchorX, 0.5f, pixelSize, "當前地位");
        }

        private void PlaceStandingPairInRow(
            RectTransform row, string historicalRaw, string currentRaw,
            float x0, float x1, float pixelSize = 34f)
        {
            float third = (x1 - x0) / 3f;
            PlaceStandingPair(row, historicalRaw, currentRaw, x0 + third, x1 - third, pixelSize);
        }

        private void AddProfileRatingTableWithStandingIcons(
            int prestige, int fame, int potential, string marketValueText,
            string historicalRaw, string currentRaw)
        {
            string[] headers = { "威望", "名氣", "潛力", "身價", "歷史地位", "當前地位" };
            float[] weights = { 0.85f, 0.85f, 0.85f, 1.35f, 1.1f, 1.1f };
            float[] normalized = NormalizeColumnWeights(weights, headers.Length);
            AddProfileTableHeader(headers, normalized, 30f);

            RectTransform row = CreateProfileRow(42f);
            float x = 0f;
            string[] values =
            {
                prestige.ToString(),
                fame.ToString(),
                potential.ToString(),
                marketValueText
            };
            for (int i = 0; i < values.Length; i++)
            {
                float w = normalized[i];
                PlaceProfileCell(row, $"V{i}", values[i], x, x + w, 16, ProfileValue, TextAnchor.MiddleCenter);
                x += w;
            }

            // Align icons to the centers of the 歷史地位 / 當前地位 header columns.
            float histMid = x + normalized[4] * 0.5f;
            float currMid = x + normalized[4] + normalized[5] * 0.5f;
            PlaceStandingPair(row, historicalRaw, currentRaw, histMid, currMid, 34f);
        }
    }
}
