using System;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private static readonly Color EventDimTint = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color EventDimTintLight = new Color(0f, 0f, 0f, 0.40f);
        private static readonly Color EventFallbackBackground = CareerEditorialChrome.Stage;
        private static readonly Color EventGlowElectric = new Color(0.55f, 0.35f, 0.06f, 0.28f);
        private static readonly Color EventGlowElectricSoft = new Color(0.10f, 0.36f, 0.39f, 0.16f);
        private static readonly Color EventGlassFill = CareerEditorialChrome.Paper;
        private static readonly Color EventGlassFillInner = CareerEditorialChrome.Paper;
        private static readonly Color EventPanelShadow = new Color(0f, 0f, 0f, 0.45f);

        private GameObject _scriptedEventRoot;
        private Image _scriptedEventRootHit;
        private Image _scriptedEventDim;
        private RawImage _scriptedEventBlurBackground;
        private Image _scriptedEventFallbackBackground;
        private Image _scriptedEventShadow;
        private RectTransform _scriptedEventCardRect;
        private Image _scriptedEventPanel;
        private Image _scriptedEventFace;
        private Image _scriptedEventOuterFrame;
        private Image _scriptedEventAccent;
        private RectTransform _scriptedEventLogoRow;
        private readonly Image[] _scriptedEventRelatedLogos = new Image[3];
        private Text _scriptedEventCaption;
        private Text _scriptedEventTitle;
        private Text _scriptedEventBody;
        private UiWindowDrag[] _scriptedEventDrags;
        private readonly Button[] _scriptedEventOptionButtons = new Button[3];
        private readonly Text[] _scriptedEventOptionLabels = new Text[3];
        private UiBackdropBlurRunner _scriptedEventBlurRunner;
        private Texture2D _scriptedEventBlurTexture;
        private bool _scriptedEventOpening;
        private bool _scriptedEventDeferPause;
        private string _scriptedEventDeferPauseReason;
        private ScriptedEventTier _scriptedEventShowTier;

        private GameObject _eventToastRoot;
        private Image _eventToastFace;
        private Text _eventToastTitle;
        private Text _eventToastBody;
        private float _eventToastHideAt;

        private GameObject _newsDetailRoot;
        private RawImage _newsDetailBlurBackground;
        private Image _newsDetailFallbackBackground;
        private Image _newsDetailDim;
        private Text _newsDetailCaption;
        private Text _newsDetailTitle;
        private Text _newsDetailBody;
        private UiBackdropBlurRunner _newsDetailBlurRunner;
        private Texture2D _newsDetailBlurTexture;
        private bool _newsDetailOpening;

        private void BuildScriptedEventOverlay(Transform canvas)
        {
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Sprite glow = UiRoundedRectUtility.GetRadialGlowSprite();

            Image rootHit = CreateUiObject<Image>("ScriptedEventOverlay", canvas);
            Stretch(rootHit.rectTransform);
            rootHit.color = Color.clear;
            rootHit.raycastTarget = true;
            _scriptedEventRootHit = rootHit;
            _scriptedEventRoot = rootHit.gameObject;
            _scriptedEventRoot.SetActive(false);

            _scriptedEventBlurBackground = CreateUiObject<RawImage>("BlurBg", _scriptedEventRoot.transform);
            Stretch(_scriptedEventBlurBackground.rectTransform);
            _scriptedEventBlurBackground.raycastTarget = false;
            _scriptedEventBlurBackground.color = UiLightChromeUtility.FrostBlurTint;
            _scriptedEventBlurBackground.texture = null;
            _scriptedEventBlurBackground.gameObject.SetActive(false);

            _scriptedEventFallbackBackground = CreateUiObject<Image>("FallbackBg", _scriptedEventRoot.transform);
            Stretch(_scriptedEventFallbackBackground.rectTransform);
            _scriptedEventFallbackBackground.color = EventFallbackBackground;
            _scriptedEventFallbackBackground.raycastTarget = false;
            _scriptedEventFallbackBackground.gameObject.SetActive(false);

            _scriptedEventDim = CreateUiObject<Image>("DimTint", _scriptedEventRoot.transform);
            Stretch(_scriptedEventDim.rectTransform);
            _scriptedEventDim.color = EventDimTint;
            _scriptedEventDim.raycastTarget = false;

            AddScriptedEventGlowLayer(_scriptedEventRoot.transform, "GlowBottom",
                new Vector2(-0.08f, -0.22f), new Vector2(1.08f, 0.48f), EventGlowElectric, glow);
            AddScriptedEventGlowLayer(_scriptedEventRoot.transform, "GlowTop",
                new Vector2(0.52f, 0.48f), new Vector2(1.18f, 1.10f), EventGlowElectricSoft, glow);

            GameObject cardGo = new GameObject("EventCard", typeof(RectTransform));
            cardGo.transform.SetParent(_scriptedEventRoot.transform, false);
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            SetRect(cardRect, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
            _scriptedEventCardRect = cardRect;
            UiChromeMotion.Bind(_scriptedEventRoot, _scriptedEventCardRect);

            _scriptedEventShadow = CreateUiObject<Image>("CardShadow", cardGo.transform);
            Stretch(_scriptedEventShadow.rectTransform);
            _scriptedEventShadow.rectTransform.offsetMin = new Vector2(6f, -6f);
            _scriptedEventShadow.rectTransform.offsetMax = new Vector2(6f, -6f);
            _scriptedEventShadow.sprite = rounded;
            _scriptedEventShadow.type = Image.Type.Sliced;
            _scriptedEventShadow.color = EventPanelShadow;
            _scriptedEventShadow.raycastTarget = false;

            _scriptedEventOuterFrame = CreateUiObject<Image>("OuterFrame", cardGo.transform);
            Stretch(_scriptedEventOuterFrame.rectTransform);
            _scriptedEventOuterFrame.sprite = rounded;
            _scriptedEventOuterFrame.type = Image.Type.Sliced;
            _scriptedEventOuterFrame.color = Accent;
            _scriptedEventOuterFrame.raycastTarget = false;

            _scriptedEventPanel = CreateUiObject<Image>("EventPanel", _scriptedEventOuterFrame.transform);
            Stretch(_scriptedEventPanel.rectTransform);
            _scriptedEventPanel.rectTransform.offsetMin = new Vector2(3f, 3f);
            _scriptedEventPanel.rectTransform.offsetMax = new Vector2(-3f, -3f);
            _scriptedEventPanel.sprite = rounded;
            _scriptedEventPanel.type = Image.Type.Sliced;
            _scriptedEventPanel.color = EventGlassFill;
            _scriptedEventPanel.raycastTarget = true;

            _scriptedEventFace = CreateUiObject<Image>("Face", _scriptedEventPanel.transform);
            Stretch(_scriptedEventFace.rectTransform);
            _scriptedEventFace.rectTransform.offsetMin = new Vector2(2f, 2f);
            _scriptedEventFace.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _scriptedEventFace.sprite = rounded;
            _scriptedEventFace.type = Image.Type.Sliced;
            _scriptedEventFace.color = EventGlassFillInner;
            var faceDrag = _scriptedEventFace.gameObject.AddComponent<UiWindowDrag>();
            faceDrag.Target = _scriptedEventCardRect;
            faceDrag.CanDrag = false;
            var panelDrag = _scriptedEventPanel.gameObject.AddComponent<UiWindowDrag>();
            panelDrag.Target = _scriptedEventCardRect;
            panelDrag.CanDrag = false;
            _scriptedEventDrags = new[] { faceDrag, panelDrag };

            _scriptedEventAccent = CreateUiObject<Image>("Accent", _scriptedEventFace.transform);
            SetRect(_scriptedEventAccent.rectTransform, new Vector2(0.02f, 0.975f), new Vector2(0.98f, 1f));
            _scriptedEventAccent.sprite = rounded;
            _scriptedEventAccent.type = Image.Type.Sliced;
            _scriptedEventAccent.color = Accent;
            _scriptedEventAccent.raycastTarget = false;

            Image logoRowHost = CreateUiObject<Image>("RelatedClubLogos", _scriptedEventFace.transform);
            SetRect(logoRowHost.rectTransform, new Vector2(0.72f, 0.78f), new Vector2(0.96f, 0.94f));
            logoRowHost.color = Color.clear;
            logoRowHost.raycastTarget = false;
            _scriptedEventLogoRow = logoRowHost.rectTransform;
            _scriptedEventLogoRow.gameObject.SetActive(false);
            for (int i = 0; i < _scriptedEventRelatedLogos.Length; i++)
            {
                Image logo = CreateUiObject<Image>("Logo_" + i, _scriptedEventLogoRow);
                float x0 = i / (float)_scriptedEventRelatedLogos.Length;
                float x1 = (i + 1) / (float)_scriptedEventRelatedLogos.Length;
                SetRect(logo.rectTransform, new Vector2(x0 + 0.02f, 0.05f), new Vector2(x1 - 0.02f, 0.95f));
                logo.preserveAspect = true;
                logo.color = Color.white;
                logo.raycastTarget = false;
                logo.gameObject.SetActive(false);
                _scriptedEventRelatedLogos[i] = logo;
            }

            _scriptedEventCaption = CreateText("Caption", _scriptedEventFace.transform, 16, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(_scriptedEventCaption.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.70f, 0.96f));
            _scriptedEventCaption.fontStyle = FontStyle.Normal;
            _scriptedEventCaption.text = "隊伍事件";

            _scriptedEventTitle = CreateText("Title", _scriptedEventFace.transform, 26, TextAnchor.MiddleLeft, Accent, _uiFontBlack);
            SetRect(_scriptedEventTitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.70f, 0.90f));
            _scriptedEventTitle.fontStyle = FontStyle.Normal;

            _scriptedEventBody = CreateText("Body", _scriptedEventFace.transform, 22, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_scriptedEventBody.rectTransform, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.76f));
            _scriptedEventBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _scriptedEventBody.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_scriptedEventBody);

            for (int i = 0; i < _scriptedEventOptionButtons.Length; i++)
            {
                float y1 = 0.28f - i * 0.10f;
                float y0 = y1 - 0.08f;
                int captured = i;
                Button btn = CreateGlassCapsuleButton(
                    "EventOpt_" + i, "選項", _scriptedEventFace.transform,
                    new Vector2(0.12f, y0), new Vector2(0.88f, y1),
                    ButtonEdge, ButtonFace, () => OnScriptedEventOption(captured), _uiFont700);
                _scriptedEventOptionButtons[i] = btn;
                _scriptedEventOptionLabels[i] = btn.GetComponentInChildren<Text>();
                StyleScriptedEventOption(btn, _scriptedEventOptionLabels[i]);
            }

            BuildEventToast(canvas);
            BuildNewsDetailOverlay(canvas);
        }

        private void AddScriptedEventGlowLayer(
            Transform parent, string name, Vector2 min, Vector2 max, Color color, Sprite glowSprite)
        {
            Image glow = CreateUiObject<Image>(name, parent);
            SetRect(glow.rectTransform, min, max);
            glow.sprite = glowSprite;
            glow.type = Image.Type.Simple;
            glow.color = color;
            glow.raycastTarget = false;
        }

        private void BuildNewsDetailOverlay(Transform canvas)
        {
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Sprite glow = UiRoundedRectUtility.GetRadialGlowSprite();
            Color newsFrame = new Color(0.45f, 0.52f, 0.68f, 1f);

            Image rootHit = CreateUiObject<Image>("NewsDetailOverlay", canvas);
            Stretch(rootHit.rectTransform);
            rootHit.color = Color.clear;
            rootHit.raycastTarget = true;
            _newsDetailRoot = rootHit.gameObject;
            _newsDetailRoot.SetActive(false);

            _newsDetailBlurBackground = CreateUiObject<RawImage>("BlurBg", _newsDetailRoot.transform);
            Stretch(_newsDetailBlurBackground.rectTransform);
            _newsDetailBlurBackground.raycastTarget = false;
            _newsDetailBlurBackground.color = UiLightChromeUtility.FrostBlurTint;

            _newsDetailFallbackBackground = CreateUiObject<Image>("FallbackBg", _newsDetailRoot.transform);
            Stretch(_newsDetailFallbackBackground.rectTransform);
            _newsDetailFallbackBackground.color = EventFallbackBackground;
            _newsDetailFallbackBackground.raycastTarget = false;
            _newsDetailFallbackBackground.gameObject.SetActive(false);

            _newsDetailDim = CreateUiObject<Image>("DimTint", _newsDetailRoot.transform);
            Stretch(_newsDetailDim.rectTransform);
            _newsDetailDim.color = EventDimTint;
            _newsDetailDim.raycastTarget = false;

            AddScriptedEventGlowLayer(_newsDetailRoot.transform, "GlowBottom",
                new Vector2(-0.08f, -0.22f), new Vector2(1.08f, 0.48f), EventGlowElectric, glow);
            AddScriptedEventGlowLayer(_newsDetailRoot.transform, "GlowTop",
                new Vector2(0.52f, 0.48f), new Vector2(1.18f, 1.10f), EventGlowElectricSoft, glow);

            GameObject cardGo = new GameObject("NewsCard", typeof(RectTransform));
            cardGo.transform.SetParent(_newsDetailRoot.transform, false);
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            SetRect(cardRect, new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.80f));
            UiChromeMotion.Bind(_newsDetailRoot, cardRect);

            Image shadow = CreateUiObject<Image>("CardShadow", cardGo.transform);
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(6f, -6f);
            shadow.rectTransform.offsetMax = new Vector2(6f, -6f);
            shadow.sprite = rounded;
            shadow.type = Image.Type.Sliced;
            shadow.color = EventPanelShadow;
            shadow.raycastTarget = false;

            Image outerFrame = CreateUiObject<Image>("OuterFrame", cardGo.transform);
            Stretch(outerFrame.rectTransform);
            outerFrame.sprite = rounded;
            outerFrame.type = Image.Type.Sliced;
            outerFrame.color = newsFrame;
            outerFrame.raycastTarget = false;

            Image panel = CreateUiObject<Image>("Panel", outerFrame.transform);
            Stretch(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(3f, 3f);
            panel.rectTransform.offsetMax = new Vector2(-3f, -3f);
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = EventGlassFill;
            panel.raycastTarget = true;

            Image face = CreateUiObject<Image>("Face", panel.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            face.sprite = rounded;
            face.type = Image.Type.Sliced;
            face.color = EventGlassFillInner;

            Image accent = CreateUiObject<Image>("Accent", face.transform);
            SetRect(accent.rectTransform, new Vector2(0.02f, 0.975f), new Vector2(0.98f, 1f));
            accent.sprite = rounded;
            accent.type = Image.Type.Sliced;
            accent.color = Accent;
            accent.raycastTarget = false;

            _newsDetailCaption = CreateText("Caption", face.transform, 13, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(_newsDetailCaption.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.96f));

            _newsDetailTitle = CreateText("Title", face.transform, 22, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_newsDetailTitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.90f));

            _newsDetailBody = CreateText("Body", face.transform, 15, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_newsDetailBody.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.76f));
            _newsDetailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _newsDetailBody.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_newsDetailBody);

            CreateGlassCapsuleButton(
                "NewsDetailClose", "關閉", face.transform,
                new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.14f),
                ButtonEdge, ButtonFace, HideNewsDetail, _uiFont700);
        }

        private void ShowNewsDetail(CareerEvent evt)
        {
            if (_newsDetailRoot == null || evt == null)
                return;
            if (_newsDetailCaption != null && _content?.Balance != null)
                _newsDetailCaption.text = CareerCalendar.FormatShort(_content.Balance, evt.clockHour);
            else if (_newsDetailCaption != null)
                _newsDetailCaption.text = "新聞";
            if (_newsDetailTitle != null)
                _newsDetailTitle.text = ResolveNewsHeadline(evt);
            if (_newsDetailBody != null)
                NarrativeUiText.Set(_newsDetailBody, ResolveNewsBody(evt));

            if (_newsDetailRoot.activeSelf)
            {
                _newsDetailRoot.transform.SetAsLastSibling();
                return;
            }

            BeginShowNewsDetailWithBlur();
        }

        private void BeginShowNewsDetailWithBlur()
        {
            if (_newsDetailOpening)
                _newsDetailOpening = false;

            _newsDetailOpening = true;
            if (_newsDetailRoot != null)
                _newsDetailRoot.SetActive(false);
            EnsureNewsDetailBlurRunner();
            _newsDetailBlurRunner.CaptureBlurred(OnNewsDetailBlurReady);
        }

        private void EnsureNewsDetailBlurRunner()
        {
            if (_newsDetailBlurRunner != null)
                return;

            Transform parent = _newsDetailRoot != null ? _newsDetailRoot.transform.parent : transform;
            Transform host = parent != null ? parent.Find("NewsDetailBlurHost") : null;
            if (host == null && parent != null)
            {
                var go = new GameObject("NewsDetailBlurHost", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                host = go.transform;
            }

            if (host == null)
                host = parent;

            _newsDetailBlurRunner = host.GetComponent<UiBackdropBlurRunner>();
            if (_newsDetailBlurRunner == null)
                _newsDetailBlurRunner = host.gameObject.AddComponent<UiBackdropBlurRunner>();
        }

        private void OnNewsDetailBlurReady(Texture2D blurred)
        {
            _newsDetailOpening = false;
            ReleaseNewsDetailBlurTexture();
            if (blurred != null)
            {
                _newsDetailBlurTexture = blurred;
                if (_newsDetailBlurBackground != null)
                {
                    _newsDetailBlurBackground.texture = blurred;
                    _newsDetailBlurBackground.color = UiLightChromeUtility.FrostBlurTint;
                    _newsDetailBlurBackground.gameObject.SetActive(true);
                }
                if (_newsDetailFallbackBackground != null)
                    _newsDetailFallbackBackground.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CareerScriptedEventPresentation] News detail blur unavailable; using fallback background.");
                if (_newsDetailBlurBackground != null)
                    _newsDetailBlurBackground.gameObject.SetActive(false);
                if (_newsDetailFallbackBackground != null)
                {
                    _newsDetailFallbackBackground.gameObject.SetActive(true);
                    _newsDetailFallbackBackground.color = EventFallbackBackground;
                }
            }

            if (_newsDetailRoot == null)
                return;

            UiChromeMotion.Show(_newsDetailRoot);
            _newsDetailRoot.transform.SetAsLastSibling();
        }

        private void ReleaseNewsDetailBlurTexture()
        {
            if (_newsDetailBlurTexture == null)
                return;
            UiBackdropBlurCapture.Release(_newsDetailBlurTexture);
            _newsDetailBlurTexture = null;
            if (_newsDetailBlurBackground != null)
                _newsDetailBlurBackground.texture = null;
        }

        private void HideNewsDetail()
        {
            _newsDetailOpening = false;
            UiChromeMotion.Hide(_newsDetailRoot, ReleaseNewsDetailBlurTexture);
        }

        private void BuildEventToast(Transform canvas)
        {
            Image toast = CreateUiObject<Image>("EventToast", canvas);
            SetRect(toast.rectTransform, new Vector2(0.62f, 0.78f), new Vector2(0.985f, 0.96f));
            toast.color = new Color(0.08f, 0.09f, 0.16f, 0.94f);
            toast.raycastTarget = false;
            _eventToastRoot = toast.gameObject;
            _eventToastRoot.SetActive(false);

            Image edge = CreateUiObject<Image>("Edge", toast.transform);
            Stretch(edge.rectTransform);
            edge.color = new Color(0.55f, 0.58f, 0.72f, 1f);
            edge.raycastTarget = false;

            _eventToastFace = CreateUiObject<Image>("Face", edge.transform);
            Stretch(_eventToastFace.rectTransform);
            _eventToastFace.rectTransform.offsetMin = new Vector2(2f, 2f);
            _eventToastFace.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _eventToastFace.color = new Color(0.12f, 0.13f, 0.24f, 1f);
            _eventToastFace.raycastTarget = false;

            _eventToastTitle = CreateText("Title", _eventToastFace.transform, 14, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_eventToastTitle.rectTransform, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.92f));

            _eventToastBody = CreateText("Body", _eventToastFace.transform, 12, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_eventToastBody.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.56f));
            _eventToastBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _eventToastBody.verticalOverflow = VerticalWrapMode.Truncate;
            NarrativeUiText.ApplyBodyStyle(_eventToastBody);
        }

        private void WireScriptedEventPausePreference()
        {
            if (_session == null)
                return;
            _session.ShouldPauseOnEventTier = tier =>
            {
                GameSettingsState settings = GameSettingsRuntime.Current;
                return ScriptedEventTierRules.ShouldPause(
                    tier,
                    settings.pauseOnEventS,
                    settings.pauseOnEventA,
                    settings.pauseOnEventB,
                    settings.pauseOnEventC);
            };
        }

        private void TryShowPendingScriptedEvent()
        {
            if (_session == null || _scriptedEventRoot == null)
                return;
            CareerScriptedEventDef def = _session.GetPendingEventDefinition();
            if (def == null)
            {
                HideScriptedEventOverlay();
                return;
            }

            ScriptedEventTier tier = ScriptedEventTierRules.Resolve(def);
            if (ScriptedEventTierRules.UsesNotificationOnly(def))
            {
                HideScriptedEventOverlay();
                return;
            }

            ApplyScriptedEventChrome(tier);
            ApplyScriptedEventDragMode(tier);
            _scriptedEventCaption.text =
                $"{ScriptedEventTierRules.TierCode(tier)} · {ScriptedEventTierRules.TierLabel(tier)}";
            _scriptedEventTitle.text = def.title ?? "事件";
            NarrativeUiText.Set(
                _scriptedEventBody,
                _session.ResolveScriptedEventBody(def));

            RefreshScriptedEventRelatedLogos(def, tier);

            CareerEventOptionDef[] options = _session.ResolveScriptedEventOptions(def);
            for (int i = 0; i < _scriptedEventOptionButtons.Length; i++)
            {
                bool show = i < options.Length;
                _scriptedEventOptionButtons[i].gameObject.SetActive(show);
                if (!show)
                    continue;
                string label = options[i].label ?? ("選項 " + (i + 1));
                if (!string.IsNullOrWhiteSpace(options[i].tooltip))
                    label += "  ·  " + options[i].tooltip;
                if (_scriptedEventOptionLabels[i] != null)
                {
                    StyleScriptedEventOption(_scriptedEventOptionButtons[i], _scriptedEventOptionLabels[i]);
                    _scriptedEventOptionLabels[i].text = label;
                    _scriptedEventOptionLabels[i].color = CareerEditorialChrome.Ink;
                }
            }

            _scriptedEventShowTier = tier;
            _scriptedEventDeferPause = _session.EvaluatePauseForTier(tier);
            _scriptedEventDeferPauseReason = $"{ScriptedEventTierRules.TierLabel(tier)} · 時間已暫停";

            if (_scriptedEventRoot.activeSelf)
            {
                _scriptedEventRoot.transform.SetAsLastSibling();
                if (_scriptedEventDeferPause)
                    PauseTime(_scriptedEventDeferPauseReason);
                return;
            }

            if (TierUsesBackdropBlur(tier))
                BeginShowScriptedEventWithBlur();
            else
                FinishShowScriptedEventWithoutBlur();
        }

        private static bool TierUsesBackdropBlur(ScriptedEventTier tier) =>
            tier == ScriptedEventTier.S || tier == ScriptedEventTier.SPlus;

        private void BeginShowScriptedEventWithBlur()
        {
            // Allow retry if a prior blur capture was cancelled without OnScriptedEventBlurReady.
            if (_scriptedEventOpening)
                _scriptedEventOpening = false;

            _scriptedEventOpening = true;
            if (_scriptedEventRoot != null)
                _scriptedEventRoot.SetActive(false);
            EnsureScriptedEventBlurRunner();
            _scriptedEventBlurRunner.CaptureBlurred(OnScriptedEventBlurReady);
        }

        private void FinishShowScriptedEventWithoutBlur()
        {
            _scriptedEventOpening = false;
            ReleaseScriptedEventBlurTexture();
            if (_scriptedEventBlurBackground != null)
                _scriptedEventBlurBackground.gameObject.SetActive(false);
            if (_scriptedEventFallbackBackground != null)
            {
                // Soft veil only — A/B/C must not run backdrop blur capture.
                _scriptedEventFallbackBackground.gameObject.SetActive(true);
                _scriptedEventFallbackBackground.color = new Color(
                    EventFallbackBackground.r,
                    EventFallbackBackground.g,
                    EventFallbackBackground.b,
                    0.32f);
            }

            if (_scriptedEventRoot == null)
                return;

            UiChromeMotion.Show(_scriptedEventRoot);
            _scriptedEventRoot.transform.SetAsLastSibling();
            if (_scriptedEventDeferPause)
                PauseTime(_scriptedEventDeferPauseReason);
        }

        private void EnsureScriptedEventBlurRunner()
        {
            if (_scriptedEventBlurRunner != null)
                return;

            Transform parent = _scriptedEventRoot != null ? _scriptedEventRoot.transform.parent : transform;
            Transform host = parent != null ? parent.Find("ScriptedEventBlurHost") : null;
            if (host == null && parent != null)
            {
                var go = new GameObject("ScriptedEventBlurHost", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                host = go.transform;
            }

            if (host == null)
                host = parent;

            _scriptedEventBlurRunner = host.GetComponent<UiBackdropBlurRunner>();
            if (_scriptedEventBlurRunner == null)
                _scriptedEventBlurRunner = host.gameObject.AddComponent<UiBackdropBlurRunner>();
        }

        private void OnScriptedEventBlurReady(Texture2D blurred)
        {
            _scriptedEventOpening = false;
            ReleaseScriptedEventBlurTexture();
            if (blurred != null)
            {
                _scriptedEventBlurTexture = blurred;
                if (_scriptedEventBlurBackground != null)
                {
                    _scriptedEventBlurBackground.texture = blurred;
                    _scriptedEventBlurBackground.color = UiLightChromeUtility.FrostBlurTint;
                    _scriptedEventBlurBackground.gameObject.SetActive(true);
                }
                if (_scriptedEventFallbackBackground != null)
                    _scriptedEventFallbackBackground.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CareerScriptedEventPresentation] Blur capture unavailable; using fallback background.");
                if (_scriptedEventBlurBackground != null)
                    _scriptedEventBlurBackground.gameObject.SetActive(false);
                if (_scriptedEventFallbackBackground != null)
                {
                    _scriptedEventFallbackBackground.gameObject.SetActive(true);
                    _scriptedEventFallbackBackground.color = EventFallbackBackground;
                }
            }

            if (_scriptedEventRoot == null)
                return;

            UiChromeMotion.Show(_scriptedEventRoot);
            _scriptedEventRoot.transform.SetAsLastSibling();
            if (_scriptedEventDeferPause)
                PauseTime(_scriptedEventDeferPauseReason);
        }

        private void ReleaseScriptedEventBlurTexture()
        {
            if (_scriptedEventBlurTexture == null)
                return;
            UiBackdropBlurCapture.Release(_scriptedEventBlurTexture);
            _scriptedEventBlurTexture = null;
            if (_scriptedEventBlurBackground != null)
                _scriptedEventBlurBackground.texture = null;
        }

        private void HideScriptedEventOverlay()
        {
            _scriptedEventOpening = false;
            _scriptedEventDeferPause = false;
            UiChromeMotion.Hide(_scriptedEventRoot, ReleaseScriptedEventBlurTexture);
        }

        private void RefreshScriptedEventRelatedLogos(CareerScriptedEventDef def, ScriptedEventTier tier)
        {
            if (_scriptedEventLogoRow == null || _scriptedEventRelatedLogos == null)
                return;

            for (int i = 0; i < _scriptedEventRelatedLogos.Length; i++)
            {
                if (_scriptedEventRelatedLogos[i] != null)
                    _scriptedEventRelatedLogos[i].gameObject.SetActive(false);
            }

            // Only A-tier history events feature related club logos in the chrome.
            if (tier != ScriptedEventTier.A || def == null)
            {
                _scriptedEventLogoRow.gameObject.SetActive(false);
                ExpandScriptedEventTitleForLogos(false);
                return;
            }

            string[] clubIds = CollectRelatedClubIds(def);
            int shown = 0;
            for (int i = 0; i < clubIds.Length && shown < _scriptedEventRelatedLogos.Length; i++)
            {
                Sprite sprite = LoadClubLogoOrNull(clubIds[i]);
                if (sprite == null)
                    continue;
                Image logo = _scriptedEventRelatedLogos[shown];
                if (logo == null)
                    continue;
                logo.sprite = sprite;
                logo.color = Color.white;
                logo.gameObject.SetActive(true);
                shown++;
            }

            _scriptedEventLogoRow.gameObject.SetActive(shown > 0);
            if (shown > 0)
            {
                // Pack active logos to the right of the title band.
                float slot = 1f / shown;
                for (int i = 0; i < shown; i++)
                {
                    Image logo = _scriptedEventRelatedLogos[i];
                    float x0 = i * slot;
                    float x1 = (i + 1) * slot;
                    SetRect(logo.rectTransform, new Vector2(x0 + 0.02f, 0.05f), new Vector2(x1 - 0.02f, 0.95f));
                }
            }

            ExpandScriptedEventTitleForLogos(shown > 0);
        }

        private void ExpandScriptedEventTitleForLogos(bool hasLogos)
        {
            float titleRight = hasLogos ? 0.70f : 0.95f;
            if (_scriptedEventCaption != null)
                SetRect(_scriptedEventCaption.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(titleRight, 0.96f));
            if (_scriptedEventTitle != null)
                SetRect(_scriptedEventTitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(titleRight, 0.90f));
        }

        private static string[] CollectRelatedClubIds(CareerScriptedEventDef def)
        {
            if (def == null)
                return Array.Empty<string>();

            var ids = new string[1 + (def.relatedClubIds?.Length ?? 0)];
            int n = 0;
            void TryAdd(string id)
            {
                if (string.IsNullOrWhiteSpace(id))
                    return;
                for (int i = 0; i < n; i++)
                {
                    if (string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                        return;
                }
                if (n >= ids.Length)
                    return;
                ids[n++] = id;
            }

            TryAdd(def.subjectClubId);
            if (def.relatedClubIds != null)
            {
                for (int i = 0; i < def.relatedClubIds.Length; i++)
                    TryAdd(def.relatedClubIds[i]);
            }

            if (n == ids.Length)
                return ids;
            var trimmed = new string[n];
            Array.Copy(ids, trimmed, n);
            return trimmed;
        }

        private void ApplyScriptedEventDragMode(ScriptedEventTier tier)
        {
            bool allowDrag = ScriptedEventTierRules.AllowsWindowDrag(tier);
            if (_scriptedEventDrags != null)
            {
                for (int i = 0; i < _scriptedEventDrags.Length; i++)
                {
                    if (_scriptedEventDrags[i] != null)
                        _scriptedEventDrags[i].CanDrag = allowDrag;
                }
            }

            // S+ stays fully modal. Other tiers use a light, click-through dim so the
            // window can be dragged aside and the career UI remains usable.
            if (_scriptedEventRootHit != null)
                _scriptedEventRootHit.raycastTarget = !allowDrag;
            if (_scriptedEventDim != null)
                _scriptedEventDim.color = allowDrag ? EventDimTintLight : EventDimTint;
        }

        private static void StyleScriptedEventOption(Button button, Text label)
        {
            if (button != null)
            {
                Image rim = button.GetComponent<Image>();
                if (rim != null)
                    rim.color = new Color(
                        CareerEditorialChrome.Ink.r,
                        CareerEditorialChrome.Ink.g,
                        CareerEditorialChrome.Ink.b,
                        0.78f);
                RectTransform face = button.transform.Find("Face") as RectTransform;
                if (face != null)
                {
                    face.offsetMin = new Vector2(4.5f, 4.5f);
                    face.offsetMax = new Vector2(-4.5f, -4.5f);
                }
            }

            if (label == null)
                return;
            label.fontStyle = FontStyle.Normal;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = false;
            SetRect(label.rectTransform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.84f));
        }

        private void SetScriptedEventFrame(float thickness)
        {
            if (_scriptedEventPanel == null)
                return;
            var inset = new Vector2(thickness, thickness);
            _scriptedEventPanel.rectTransform.offsetMin = inset;
            _scriptedEventPanel.rectTransform.offsetMax = -inset;
        }

        private void SyncScriptedEventCardRect(Vector2 min, Vector2 max)
        {
            if (_scriptedEventCardRect != null)
                SetRect(_scriptedEventCardRect, min, max);
            if (_scriptedEventShadow != null)
            {
                Stretch(_scriptedEventShadow.rectTransform);
                _scriptedEventShadow.rectTransform.offsetMin = new Vector2(6f, -6f);
                _scriptedEventShadow.rectTransform.offsetMax = new Vector2(6f, -6f);
            }
        }

        private void ApplyScriptedEventChrome(ScriptedEventTier tier)
        {
            Color gold = new Color(0.86f, 0.70f, 0.28f, 1f);
            Color blackGoldEdge = new Color(0.72f, 0.58f, 0.22f, 1f);
            Color steel = new Color(0.55f, 0.62f, 0.78f, 1f);

            switch (tier)
            {
                case ScriptedEventTier.SPlus:
                    SyncScriptedEventCardRect(new Vector2(0.14f, 0.08f), new Vector2(0.86f, 0.92f));
                    SetScriptedEventFrame(10f);
                    _scriptedEventOuterFrame.color = steel;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = CareerEditorialChrome.Cyan;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 32;
                    break;
                case ScriptedEventTier.S:
                    SyncScriptedEventCardRect(new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.90f));
                    SetScriptedEventFrame(9f);
                    _scriptedEventOuterFrame.color = blackGoldEdge;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = CareerEditorialChrome.Mustard;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 30;
                    break;
                case ScriptedEventTier.A:
                    SyncScriptedEventCardRect(new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));
                    SetScriptedEventFrame(8f);
                    _scriptedEventOuterFrame.color = gold;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = CareerEditorialChrome.Mustard;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 28;
                    break;
                case ScriptedEventTier.B:
                {
                    Color silver = new Color(0.72f, 0.74f, 0.78f, 1f);
                    SyncScriptedEventCardRect(new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f));
                    SetScriptedEventFrame(8f);
                    _scriptedEventOuterFrame.color = silver;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = silver;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 26;
                    break;
                }
                case ScriptedEventTier.C:
                    SyncScriptedEventCardRect(new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.88f));
                    SetScriptedEventFrame(8f);
                    _scriptedEventOuterFrame.color = CareerEditorialChrome.Cyan;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = CareerEditorialChrome.Cyan;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 26;
                    break;
                default:
                    SyncScriptedEventCardRect(new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f));
                    SetScriptedEventFrame(8f);
                    _scriptedEventOuterFrame.color = AccentDim;
                    _scriptedEventPanel.color = EventGlassFill;
                    _scriptedEventFace.color = EventGlassFillInner;
                    _scriptedEventAccent.color = Accent;
                    _scriptedEventTitle.color = CareerEditorialChrome.Ink;
                    _scriptedEventTitle.fontSize = 26;
                    break;
            }

            if (_scriptedEventTitle != null)
                _scriptedEventTitle.fontStyle = FontStyle.Normal;
            if (_scriptedEventBody != null)
            {
                _scriptedEventBody.color = CareerEditorialChrome.Ink;
                _scriptedEventBody.fontSize = 22;
                NarrativeUiText.ApplyBodyStyle(_scriptedEventBody);
            }
            if (_scriptedEventCaption != null)
            {
                _scriptedEventCaption.color = CareerEditorialChrome.InkMuted;
                _scriptedEventCaption.fontStyle = FontStyle.Normal;
            }
        }

        private void ShowEventToast(string title, string body, ScriptedEventTier tier, bool pause)
        {
            if (_eventToastRoot == null)
                return;
            if (_eventToastTitle != null)
                _eventToastTitle.text = title ?? string.Empty;
            if (_eventToastBody != null)
                NarrativeUiText.Set(_eventToastBody, body ?? string.Empty);
            if (_eventToastFace != null)
            {
                _eventToastFace.color = tier == ScriptedEventTier.C
                    ? new Color(0.14f, 0.16f, 0.28f, 1f)
                    : new Color(0.12f, 0.13f, 0.22f, 1f);
            }

            _eventToastRoot.SetActive(true);
            _eventToastRoot.transform.SetAsLastSibling();
            _eventToastHideAt = Time.unscaledTime + 8f;
            if (pause)
                PauseTime($"{ScriptedEventTierRules.TierLabel(tier)} · 時間已暫停");
        }

        private void TickEventToast()
        {
            if (_eventToastRoot == null || !_eventToastRoot.activeSelf)
                return;
            if (Time.unscaledTime >= _eventToastHideAt)
                _eventToastRoot.SetActive(false);
        }

        private void HandleAdvanceEventFeedback(CareerAdvanceResult result)
        {
            if (result == null)
                return;
            if (result.eventToastPending)
                ShowEventToast(
                    result.eventToastTitle,
                    result.eventToastBody,
                    result.eventToastTier,
                    result.eventToastShouldPause);
            if (_session != null && _session.GetPendingEventDefinition() != null)
                TryShowPendingScriptedEvent();
        }

        private void OnScriptedEventOption(int index)
        {
            if (_session == null)
                return;
            CareerScriptedEventDef def = _session.GetPendingEventDefinition();
            CareerEventOptionDef[] options = _session.ResolveScriptedEventOptions(def);
            if (index < 0 || index >= options.Length)
                return;

            string optionId = options[index].id;
            if (!_session.TryResolvePendingEvent(optionId, out string failure))
            {
                _status = failure ?? "無法處理事件";
                RefreshAll();
                return;
            }

            if (_scriptedEventRoot != null)
                HideScriptedEventOverlay();
            RefreshAll();
            TryShowPendingScriptedEvent();
        }
    }
}
