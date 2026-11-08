using System;
using System.Collections.Generic;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Pre-match BO3 map ban/pick overlay for career official matches.</summary>
    public sealed class MapBanPickPresentation
    {
        private static readonly Color Panel = CareerEditorialChrome.Paper;
        private static readonly Color PanelInner = CareerEditorialChrome.Paper;
        private static readonly Color TextPrimary = CareerEditorialChrome.Ink;
        private static readonly Color TextMuted = CareerEditorialChrome.InkMuted;
        private static readonly Color BanTint = CareerEditorialChrome.Red;
        private static readonly Color BanWash = new Color(0.86f, 0.48f, 0.44f, 1f);
        private static readonly Color PickTint = new Color(0.24f, 0.48f, 0.30f, 1f);
        private static readonly Color PickWash = new Color(0.58f, 0.86f, 0.62f, 1f);

        private readonly CareerSession _session;
        private readonly Font _font;
        private readonly Font _fontBlack;
        private readonly Action _onVetoComplete;
        private readonly Action<string> _onStatus;

        private GameObject _root;
        private Text _stepText;
        private Text _summaryText;
        private Transform _panelFace;
        private bool _builtForTrainRemoved;
        private readonly Dictionary<string, Button> _mapButtons =
            new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Image> _mapThumbs =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Text> _mapRanks =
            new Dictionary<string, Text>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Text> _mapLabels =
            new Dictionary<string, Text>(StringComparer.OrdinalIgnoreCase);
        private Button _startMatchButton;

        public MapBanPickPresentation(
            CareerSession session,
            Font font,
            Action onVetoComplete,
            Action<string> onStatus,
            Font fontBlack = null,
            Font fontAnton = null)
        {
            _session = session;
            _font = font;
            _fontBlack = fontBlack != null ? fontBlack : font;
            _ = fontAnton;
            _onVetoComplete = onVetoComplete;
            _onStatus = onStatus;
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(Transform canvas)
        {
            EnsureUi(canvas);
            RebuildMapButtons();
            if (_session.GetOfficialMapSeries() == null ||
                _session.GetOfficialMapSeries().vetoStep == 0 &&
                !_session.GetOfficialMapSeries().vetoComplete &&
                (_session.GetOfficialMapSeries().poolRemaining == null ||
                 _session.GetOfficialMapSeries().poolRemaining.Count == 0))
                _session.BeginOfficialMapVeto();

            CareerMapSeriesState series = _session.GetOfficialMapSeries();
            if (series != null && !series.vetoComplete &&
                (series.poolRemaining == null || series.poolRemaining.Count == 0))
                _session.BeginOfficialMapVeto();

            UiChromeMotion.Show(_root);
            Refresh();
            TryAutoAdvanceAi();
        }

        public void Close()
        {
            UiChromeMotion.Hide(_root);
        }

        private void EnsureUi(Transform canvas)
        {
            if (_root != null)
                return;

            Image dim = CreateUiObject<Image>("MapBanPickRoot", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
            _root = dim.gameObject;

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur. Neutral Panel fills kept.
            Image panel = UiLightChromeUtility.CreateCard(
                "Panel", dim.transform,
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f), Panel);
            Image face = UiLightChromeUtility.CreateInsetFace(panel.transform, PanelInner, 4f);
            _panelFace = face.transform;
            UiChromeMotion.BindFace(_root, panel);

            Text title = CreateText("Title", face.transform, 30, TextAnchor.MiddleCenter, TextPrimary, _font);
            SetRect(title.rectTransform, new Vector2(0.12f, 0.90f), new Vector2(0.88f, 0.985f));
            title.text = "地圖禁用與選擇";
            title.fontStyle = FontStyle.Normal;

            UiLightChromeUtility.CreateSquareCloseButton(
                "Close", face.transform,
                new Vector2(0.925f, 0.915f), new Vector2(0.985f, 0.985f),
                Close, _font, 26);

            _stepText = CreateText("Step", face.transform, 22, TextAnchor.MiddleCenter, BanTint, _font);
            SetRect(_stepText.rectTransform, new Vector2(0.06f, 0.835f), new Vector2(0.94f, 0.90f));
            _stepText.fontStyle = FontStyle.Normal;

            _summaryText = CreateText("Summary", face.transform, 20, TextAnchor.MiddleCenter, TextPrimary, _font);
            SetRect(_summaryText.rectTransform, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.835f));
            _summaryText.fontStyle = FontStyle.Normal;

            RebuildMapButtons();

            _startMatchButton = UiLightChromeUtility.CreateGlassCapsuleButton(
                "StartMaps", "開始比賽", face.transform,
                new Vector2(0.42f, 0.035f), new Vector2(0.58f, 0.105f),
                () =>
                {
                    Close();
                    _onVetoComplete?.Invoke();
                },
                _fontBlack, 18, fillLabel: true, primary: true);
            _startMatchButton.gameObject.SetActive(false);
            Text startLabel = _startMatchButton.GetComponentInChildren<Text>();
            if (startLabel != null)
            {
                startLabel.font = _fontBlack;
                ApplyFilledButtonLabel(startLabel, 48, 0.9f);
            }
        }

        private void RebuildMapButtons()
        {
            if (_panelFace == null)
                return;

            bool trainRemoved = _session.IsTrainPoolRemoved();
            if (_mapButtons.Count > 0 && _builtForTrainRemoved == trainRemoved)
                return;

            foreach (KeyValuePair<string, Button> pair in _mapButtons)
            {
                if (pair.Value == null)
                    continue;
                Transform column = pair.Value.transform.parent;
                if (column != null)
                    UnityEngine.Object.Destroy(column.gameObject);
            }
            _mapButtons.Clear();
            _mapThumbs.Clear();
            _mapRanks.Clear();
            _mapLabels.Clear();

            MapPoolEntry[] pool = _session.ResolveActiveOfficialMapPool(MapPoolCatalog.GetEntries());
            int count = pool.Length;
            if (count <= 0)
                return;

            const float rowMin = 0.02f;
            const float rowMax = 0.98f;
            const float gap = 0.010f;
            float slot = (rowMax - rowMin - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                MapPoolEntry entry = pool[i];
                float x0 = rowMin + i * (slot + gap);
                float x1 = x0 + slot;
                CreateMapColumn(entry, i + 1, x0, x1);
            }

            _builtForTrainRemoved = trainRemoved;
        }

        private void CreateMapColumn(MapPoolEntry entry, int rank, float x0, float x1)
        {
            Image column = CreateUiObject<Image>("Map_" + entry.id, _panelFace);
            SetRect(column.rectTransform, new Vector2(x0, 0.13f), new Vector2(x1, 0.75f));
            column.color = new Color(0f, 0f, 0f, 0f);
            column.raycastTarget = false;

            Image edge = CreateUiObject<Image>("Card", column.transform);
            SetRect(edge.rectTransform, new Vector2(0.03f, 0.22f), new Vector2(0.97f, 0.92f));
            edge.color = CareerEditorialChrome.Ink;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            face.color = CareerEditorialChrome.Ink;
            face.gameObject.AddComponent<RectMask2D>();

            Image thumb = CreateUiObject<Image>("Thumb", face.transform);
            Stretch(thumb.rectTransform);
            thumb.raycastTarget = false;
            thumb.color = Color.white;
            Sprite bg = LoadMapBackgroundSprite(entry.id);
            if (bg == null && !string.IsNullOrEmpty(entry.overviewResource))
                bg = Resources.Load<Sprite>(entry.overviewResource);
            ApplyCoverCropThumb(thumb, bg);

            _mapThumbs[entry.id] = thumb;

            Text label = CreateText("Label", column.transform, 20, TextAnchor.MiddleCenter, TextPrimary, _font);
            SetRect(label.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.20f));
            label.text = entry.displayName;
            label.fontStyle = FontStyle.Normal;
            _mapLabels[entry.id] = label;

            Button btn = edge.gameObject.AddComponent<Button>();
            btn.targetGraphic = thumb.sprite != null ? thumb : face;
            string mapId = entry.id;
            btn.onClick.AddListener(() => OnMapClicked(mapId));
            _mapButtons[entry.id] = btn;
        }

        private static Sprite LoadMapBackgroundSprite(string mapId)
        {
            string id = string.IsNullOrEmpty(mapId) ? "dust2" : mapId;
            string prefix = "UI/Backgrounds/" + id + "_bg_";
            for (int i = 0; i < 4; i++)
            {
                Sprite sprite = Resources.Load<Sprite>(prefix + i);
                if (sprite != null)
                    return sprite;
                Sprite[] all = Resources.LoadAll<Sprite>(prefix + i);
                if (all != null && all.Length > 0)
                    return all[0];
            }

            if (!string.Equals(id, "dust2", StringComparison.OrdinalIgnoreCase))
                return LoadMapBackgroundSprite("dust2");
            return null;
        }

        private static void ApplyCoverCropThumb(Image thumb, Sprite sprite)
        {
            if (thumb == null)
                return;

            thumb.sprite = sprite;
            thumb.preserveAspect = false;
            Stretch(thumb.rectTransform);

            AspectRatioFitter fitter = thumb.GetComponent<AspectRatioFitter>();
            if (sprite == null)
            {
                if (fitter != null)
                    UnityEngine.Object.Destroy(fitter);
                return;
            }

            if (fitter == null)
                fitter = thumb.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            float h = sprite.rect.height;
            fitter.aspectRatio = h > 0.01f ? sprite.rect.width / h : 1f;
        }

        private void OnMapClicked(string mapId)
        {
            if (!_session.TryApplyOfficialMapVetoChoice(mapId, out string error))
            {
                _onStatus?.Invoke(error ?? "無法選擇");
                return;
            }

            Refresh();
            TryAutoAdvanceAi();
        }

        private void TryAutoAdvanceAi()
        {
            CareerMapSeriesState series = _session.GetOfficialMapSeries();
            if (series == null)
                return;

            int guard = 0;
            while (!series.vetoComplete && guard++ < 12)
            {
                if (series.vetoStep == (int)MapVetoStepKind.Decider)
                {
                    _session.TryAdvanceOfficialMapVetoAi(out _, out _);
                    Refresh();
                    continue;
                }

                if (MapVetoRules.IsPlayerTurn(series.vetoStep, series.playerIsVetoA))
                    break;

                if (!_session.TryAdvanceOfficialMapVetoAi(out _, out _))
                    break;
                Refresh();
            }

            Refresh();
        }

        public void Refresh()
        {
            CareerMapSeriesState series = _session.GetOfficialMapSeries();
            if (series == null || _stepText == null)
                return;

            MapVetoRules.EnsureLists(series);
            if (!series.vetoComplete && series.vetoStep < MapVetoRules.StepCount)
            {
                MapVetoStepKind kind = MapVetoRules.GetStepKind(
                    Math.Min(series.vetoStep, MapVetoRules.StepCount - 1));
                _stepText.text = kind == MapVetoStepKind.Decider
                    ? "決勝圖"
                    : MapVetoRules.StepLabelZh(kind, series.playerIsVetoA);
                _stepText.color = kind == MapVetoStepKind.Decider || MapVetoRules.IsPickStep(kind)
                    ? PickTint
                    : BanTint;
            }
            else
            {
                _stepText.text = "地圖選定完成";
                _stepText.color = PickTint;
            }

            _summaryText.text =
                FormatSeriesSlot(series, 0) + "        " +
                FormatSeriesSlot(series, 1) + "        " +
                FormatSeriesSlot(series, 2);

            foreach (KeyValuePair<string, Button> kv in _mapButtons)
            {
                string id = kv.Key;
                Button btn = kv.Value;
                bool remaining = series.poolRemaining.Exists(x =>
                    string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
                bool banned = series.bannedIds.Exists(x =>
                    string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
                int pickSlot = -1;
                if (series.seriesMapIds != null)
                {
                    for (int i = 0; i < series.seriesMapIds.Length; i++)
                    {
                        if (string.Equals(series.seriesMapIds[i], id, StringComparison.OrdinalIgnoreCase))
                            pickSlot = i;
                    }
                }

                bool canClick = !series.vetoComplete &&
                                remaining &&
                                MapVetoRules.IsPlayerTurn(series.vetoStep, series.playerIsVetoA) &&
                                series.vetoStep < (int)MapVetoStepKind.Decider;
                btn.interactable = canClick;

                Color wash = Color.white;
                Color labelColor = TextPrimary;
                if (banned)
                {
                    wash = BanWash;
                    labelColor = BanTint;
                }
                else if (pickSlot >= 0)
                {
                    wash = PickWash;
                    labelColor = PickTint;
                }

                if (_mapThumbs.TryGetValue(id, out Image thumb) && thumb != null)
                    thumb.color = wash;
                if (_mapLabels.TryGetValue(id, out Text label) && label != null)
                {
                    label.color = labelColor;
                    label.fontStyle = FontStyle.Normal;
                    label.text = MapPoolCatalog.DisplayName(id);
                }
            }

            if (_startMatchButton != null)
                _startMatchButton.gameObject.SetActive(series.vetoComplete);
        }

        private static string FormatSeriesSlot(CareerMapSeriesState series, int index)
        {
            string prefix = index == 0 ? "圖一" : index == 1 ? "圖二" : "圖三";
            if (series.seriesMapIds == null || series.seriesMapIds.Length <= index)
                return ColorTag(TextMuted, prefix);
            string id = series.seriesMapIds[index];
            if (string.IsNullOrEmpty(id))
                return ColorTag(TextMuted, prefix);
            return ColorTag(PickTint, prefix + " " + MapPoolCatalog.DisplayName(id));
        }

        private static string ColorTag(Color color, string text)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        private static void ApplyFilledButtonLabel(Text text, int maxSize = 48, float heightFill = 0.9f)
        {
            if (text == null)
                return;
            float yInset = Mathf.Clamp01((1f - Mathf.Clamp(heightFill, 0.2f, 1f)) * 0.5f);
            SetRect(text.rectTransform, new Vector2(0.05f, yInset), new Vector2(0.95f, 1f - yInset));
            EnableBestFit(text, maxSize);
        }

        private static void EnableBestFit(Text text, int maxSize)
        {
            if (text == null)
                return;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = Mathf.Max(maxSize, text.fontSize);
        }

        private T CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private Text CreateText(
            string name, Transform parent, int size, TextAnchor anchor, Color color, Font font = null)
        {
            Text text = CreateUiObject<Text>(name, parent);
            Font resolved = font != null ? font : _font;
            text.font = resolved != null ? resolved : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
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
