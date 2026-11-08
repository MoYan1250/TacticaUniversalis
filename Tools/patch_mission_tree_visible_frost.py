# -*- coding: utf-8 -*-
"""Mission tree: show immediately, CreateCard frost, SoftNavy nests."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "Assets/_Project/Scripts/Presentation/CareerMissionTreePresentation.cs"
text = PATH.read_text(encoding="utf-8")

old_open = '''        public void Open(Transform canvas)
        {
            EnsureUi(canvas);
            BringToFront();
            if (_root.activeSelf)
            {
                RebuildNodes();
                return;
            }

            // Allow retry if a prior blur capture was cancelled without OnBlurReady.
            if (_opening)
                _opening = false;

            _opening = true;
            _root.SetActive(false);
            EnsureBlurRunner(canvas);
            _blurRunner.CaptureBlurred(OnBlurReady);
        }'''

new_open = '''        public void Open(Transform canvas)
        {
            EnsureUi(canvas);

            // Show readable SoftNavy / card UI immediately — never hide while waiting for blur.
            if (_fallbackBackground != null && _blurTexture == null)
            {
                _fallbackBackground.gameObject.SetActive(true);
                _fallbackBackground.color = FallbackBackground;
            }
            if (_blurBackground != null && _blurTexture == null)
                _blurBackground.gameObject.SetActive(false);

            bool alreadyOpen = _root.activeSelf;
            _root.SetActive(true);
            BringToFront();
            RebuildNodes();
            RefreshFrostHost(canvas);

            if (alreadyOpen)
                return;

            _opening = true;
            EnsureBlurRunner(canvas);
            _blurRunner.CaptureBlurred(OnBlurReady);
        }'''

if old_open not in text:
    raise SystemExit("Open block not found")
text = text.replace(old_open, new_open, 1)

old_blur_ready = '''        private void OnBlurReady(Texture2D blurred)
        {
            _opening = false;
            ReleaseBlurTexture();
            if (blurred != null)
            {
                _blurTexture = blurred;
                if (_blurBackground != null)
                {
                    _blurBackground.texture = blurred;
                    _blurBackground.color = UiLightChromeUtility.FrostBlurTint;
                    _blurBackground.gameObject.SetActive(true);
                }
                if (_fallbackBackground != null)
                    _fallbackBackground.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CareerMissionTreePresentation] Blur capture unavailable; using fallback background.");
                if (_blurBackground != null)
                    _blurBackground.gameObject.SetActive(false);
                if (_fallbackBackground != null)
                {
                    _fallbackBackground.gameObject.SetActive(true);
                    _fallbackBackground.color = FallbackBackground;
                }
            }

            if (_root != null)
            {
                _root.SetActive(true);
                BringToFront();
                RebuildNodes();
            }
        }'''

new_blur_ready = '''        private void OnBlurReady(Texture2D blurred)
        {
            _opening = false;
            ReleaseBlurTexture();
            if (blurred != null)
            {
                _blurTexture = blurred;
                if (_blurBackground != null)
                {
                    _blurBackground.texture = blurred;
                    _blurBackground.color = UiLightChromeUtility.FrostBlurTint;
                    _blurBackground.gameObject.SetActive(true);
                }
                if (_fallbackBackground != null)
                    _fallbackBackground.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CareerMissionTreePresentation] Blur capture unavailable; using fallback background.");
                if (_blurBackground != null)
                    _blurBackground.gameObject.SetActive(false);
                if (_fallbackBackground != null)
                {
                    _fallbackBackground.gameObject.SetActive(true);
                    _fallbackBackground.color = FallbackBackground;
                }
            }

            if (_root != null && _root.activeSelf)
            {
                BringToFront();
                RefreshFrostHost(_root.transform.parent);
            }
        }

        private static void RefreshFrostHost(Transform canvasOrRoot)
        {
            UiFrostedGlassHost host = UiFrostedGlassHost.FindFor(canvasOrRoot);
            host?.RefreshBlur();
        }'''

if old_blur_ready not in text:
    raise SystemExit("OnBlurReady block not found")
text = text.replace(old_blur_ready, new_blur_ready, 1)

old_ensure = '''        private void EnsureUi(Transform canvas)
        {
            if (_root != null)
                return;

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Sprite pill = UiRoundedRectUtility.GetPillSprite();
            Sprite glow = UiRoundedRectUtility.GetRadialGlowSprite();

            Image rootHit = CreateUiObject<Image>("MissionTreeRoot", canvas);
            Stretch(rootHit.rectTransform);
            rootHit.color = Color.clear;
            rootHit.raycastTarget = true;
            _root = rootHit.gameObject;
            _root.SetActive(false);

            _blurBackground = CreateUiObject<RawImage>("BlurBg", _root.transform);
            Stretch(_blurBackground.rectTransform);
            _blurBackground.raycastTarget = false;
            _blurBackground.color = UiLightChromeUtility.FrostBlurTint;

            _fallbackBackground = CreateUiObject<Image>("FallbackBg", _root.transform);
            Stretch(_fallbackBackground.rectTransform);
            _fallbackBackground.color = FallbackBackground;
            _fallbackBackground.raycastTarget = false;
            _fallbackBackground.gameObject.SetActive(false);

            Image dimTint = CreateUiObject<Image>("DimTint", _root.transform);
            Stretch(dimTint.rectTransform);
            dimTint.color = DimTint;
            dimTint.raycastTarget = false;

            AddGlowLayer(_root.transform, "GlowBottom",
                new Vector2(-0.08f, -0.22f), new Vector2(1.08f, 0.48f), GlowElectric, glow);
            AddGlowLayer(_root.transform, "GlowTop",
                new Vector2(0.52f, 0.48f), new Vector2(1.18f, 1.10f), GlowElectricSoft, glow);

            Image panel = CreateFramedGlassPanel("Panel", _root.transform,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), GlassFill, rounded, 6f);

            Image header = CreateFramedGlassPanel("Header", panel.transform,
                new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.98f), GlassFillInner, rounded, 4f);

            Text title = CreateText("Title", header.transform, 26, TextAnchor.MiddleLeft, AccentIce);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.72f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.text = "任務樹";

            Image scrollHost = CreateFramedGlassPanel("ScrollHost", panel.transform,
                new Vector2(0.03f, 0.04f), new Vector2(0.49f, 0.88f), GlassFillInner, rounded, 4f);

            _scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.sprite = rounded;
            viewport.type = Image.Type.Sliced;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _scroll.viewport = viewport.rectTransform;

            _scrollContent = CreateUiObject<RectTransform>("Content", viewport.transform);
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, 800f);
            _scroll.content = _scrollContent;

            VerticalLayoutGroup layout = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildTooltip(panel.transform, rounded);
            CreateCloseButton(panel.transform);
        }'''

new_ensure = '''        private void EnsureUi(Transform canvas)
        {
            if (_root != null)
                return;

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Sprite glow = UiRoundedRectUtility.GetRadialGlowSprite();

            Image rootHit = CreateUiObject<Image>("MissionTreeRoot", canvas);
            Stretch(rootHit.rectTransform);
            rootHit.color = Color.clear;
            rootHit.raycastTarget = true;
            _root = rootHit.gameObject;
            _root.SetActive(false);

            _blurBackground = CreateUiObject<RawImage>("BlurBg", _root.transform);
            Stretch(_blurBackground.rectTransform);
            _blurBackground.raycastTarget = false;
            _blurBackground.color = UiLightChromeUtility.FrostBlurTint;
            _blurBackground.gameObject.SetActive(false);

            _fallbackBackground = CreateUiObject<Image>("FallbackBg", _root.transform);
            Stretch(_fallbackBackground.rectTransform);
            _fallbackBackground.color = FallbackBackground;
            _fallbackBackground.raycastTarget = false;
            _fallbackBackground.gameObject.SetActive(true);

            Image dimTint = CreateUiObject<Image>("DimTint", _root.transform);
            Stretch(dimTint.rectTransform);
            dimTint.color = DimTint;
            dimTint.raycastTarget = false;

            AddGlowLayer(_root.transform, "GlowBottom",
                new Vector2(-0.08f, -0.22f), new Vector2(1.08f, 0.48f), GlowElectric, glow);
            AddGlowLayer(_root.transform, "GlowTop",
                new Vector2(0.52f, 0.48f), new Vector2(1.18f, 1.10f), GlowElectricSoft, glow);

            Image panel = UiLightChromeUtility.CreateCard(
                "Panel", _root.transform,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
                UiLightChromeUtility.SoftNavyFace);

            Image header = CreateUiObject<Image>("Header", panel.transform);
            SetRect(header.rectTransform, new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.98f));
            UiLightChromeUtility.ApplySoftNavyPanel(header);

            Text title = CreateText("Title", header.transform, 26, TextAnchor.MiddleLeft, AccentIce);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.72f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.text = "任務樹";

            Image scrollHost = CreateUiObject<Image>("ScrollHost", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.49f, 0.88f));
            UiLightChromeUtility.ApplySoftNavyPanel(scrollHost);

            _scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.sprite = rounded;
            viewport.type = Image.Type.Sliced;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _scroll.viewport = viewport.rectTransform;

            _scrollContent = CreateUiObject<RectTransform>("Content", viewport.transform);
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(0f, 800f);
            _scroll.content = _scrollContent;

            VerticalLayoutGroup layout = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildTooltip(panel.transform);
            CreateCloseButton(panel.transform);
        }'''

if old_ensure not in text:
    raise SystemExit("EnsureUi block not found")
text = text.replace(old_ensure, new_ensure, 1)

old_tooltip_sig = '''        private void BuildTooltip(Transform panel, Sprite rounded)
        {
            Image tooltipPanel = CreateFramedGlassPanel("Tooltip", panel.transform,
                new Vector2(0.52f, 0.04f), new Vector2(0.97f, 0.88f), GlassFill, rounded, 4f);
            _tooltipPanel = tooltipPanel;
            tooltipPanel.raycastTarget = false;'''

new_tooltip_sig = '''        private void BuildTooltip(Transform panel)
        {
            Image tooltipPanel = CreateUiObject<Image>("Tooltip", panel.transform);
            SetRect(tooltipPanel.rectTransform, new Vector2(0.52f, 0.04f), new Vector2(0.97f, 0.88f));
            UiLightChromeUtility.ApplySoftNavyPanel(tooltipPanel);
            _tooltipPanel = tooltipPanel;
            tooltipPanel.raycastTarget = false;'''

if old_tooltip_sig not in text:
    raise SystemExit("BuildTooltip block not found")
text = text.replace(old_tooltip_sig, new_tooltip_sig, 1)

old_node = '''        private void CreateNodeRow(CareerMissionNodeDef node)
        {
            CareerMissionNodeStatus status = _session.GetMissionNodeStatus(node.id);
            TrackDefaultTooltip(node, status);

            Sprite pill = UiRoundedRectUtility.GetPillSprite();
            Color faceColor = status switch
            {
                CareerMissionNodeStatus.Completable => CompleteTint,
                CareerMissionNodeStatus.Completed => CompletedTint,
                CareerMissionNodeStatus.Available => AvailableTint,
                _ => LockedTint
            };

            Image row = CreateUiObject<Image>("Node_" + node.id, _scrollContent);
            row.sprite = pill;
            row.type = Image.Type.Sliced;
            row.color = GlassBorder;
            row.raycastTarget = true;
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = status == CareerMissionNodeStatus.Completable ? 88f : 64f;
            le.flexibleWidth = 1f;

            Image face = CreateUiObject<Image>("Face", row.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            face.sprite = pill;
            face.type = Image.Type.Sliced;
            face.color = faceColor;
            face.raycastTarget = true;

            Color titleColor = TextPrimary;
            Color metaColor = TextMuted;

            Text title = CreateText("Title", face.transform, 16, TextAnchor.MiddleLeft, titleColor);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.68f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.text = node.title ?? node.id;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text meta = CreateText("Meta", face.transform, 11, TextAnchor.MiddleRight, metaColor);
            SetRect(meta.rectTransform, new Vector2(0.68f, 0.52f), new Vector2(0.96f, 0.92f));
            meta.alignment = TextAnchor.MiddleRight;
            meta.text = CareerMissionRules.StatusLabel(status);

            if (status == CareerMissionNodeStatus.Completable)
            {
                string capturedId = node.id;
                CreateActionButton(
                    "Claim_" + node.id, "完成", face.transform,
                    new Vector2(0.58f, 0.08f), new Vector2(0.96f, 0.46f),
                    () => OnClaimClicked(capturedId));
            }

            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => ShowTooltip(node, status));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit,
                _ => ShowDefaultTooltip(_defaultTooltipNode, _defaultTooltipStatus));
        }'''

new_node = '''        private void CreateNodeRow(CareerMissionNodeDef node)
        {
            CareerMissionNodeStatus status = _session.GetMissionNodeStatus(node.id);
            TrackDefaultTooltip(node, status);

            Color faceColor = status switch
            {
                CareerMissionNodeStatus.Completable => CompleteTint,
                CareerMissionNodeStatus.Completed => CompletedTint,
                CareerMissionNodeStatus.Available => AvailableTint,
                _ => LockedTint
            };

            Image row = CreateUiObject<Image>("Node_" + node.id, _scrollContent);
            UiLightChromeUtility.ApplyFrostedListRow(row);
            row.raycastTarget = true;
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = status == CareerMissionNodeStatus.Completable ? 88f : 64f;
            le.flexibleWidth = 1f;

            Image face = CreateUiObject<Image>("Face", row.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            UiLightChromeUtility.ApplySoftNavyPanel(face);
            face.color = faceColor;
            face.raycastTarget = true;

            Text title = CreateText("Title", face.transform, 16, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.68f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.text = node.title ?? node.id;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text meta = CreateText("Meta", face.transform, 11, TextAnchor.MiddleRight, AccentIce);
            SetRect(meta.rectTransform, new Vector2(0.68f, 0.52f), new Vector2(0.96f, 0.92f));
            meta.alignment = TextAnchor.MiddleRight;
            meta.text = CareerMissionRules.StatusLabel(status);

            if (status == CareerMissionNodeStatus.Completable)
            {
                string capturedId = node.id;
                CreateActionButton(
                    "Claim_" + node.id, "完成", face.transform,
                    new Vector2(0.58f, 0.08f), new Vector2(0.96f, 0.46f),
                    () => OnClaimClicked(capturedId));
            }

            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => ShowTooltip(node, status));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit,
                _ => ShowDefaultTooltip(_defaultTooltipNode, _defaultTooltipStatus));
        }'''

if old_node not in text:
    raise SystemExit("CreateNodeRow block not found")
text = text.replace(old_node, new_node, 1)

old_framed = '''        private Image CreateFramedGlassPanel(
            string name, Transform parent, Vector2 min, Vector2 max, Color fill, Sprite rounded,
            float borderInset = 2f, float shadowOffset = 6f)
        {
            Image shadow = CreateUiObject<Image>(name + "Shadow", parent);
            SetRect(shadow.rectTransform, min, max);
            shadow.rectTransform.offsetMin = new Vector2(shadowOffset, -shadowOffset);
            shadow.rectTransform.offsetMax = new Vector2(shadowOffset, -shadowOffset);
            shadow.sprite = rounded;
            shadow.type = Image.Type.Sliced;
            shadow.color = PanelShadow;
            shadow.raycastTarget = false;

            Image frame = CreateUiObject<Image>(name + "Frame", parent);
            SetRect(frame.rectTransform, min, max);
            frame.sprite = rounded;
            frame.type = Image.Type.Sliced;
            frame.color = GlassBorder;

            Image panel = CreateUiObject<Image>(name, frame.transform);
            Stretch(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(borderInset, borderInset);
            panel.rectTransform.offsetMax = new Vector2(-borderInset, -borderInset);
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = fill;
            return panel;
        }
'''

if old_framed not in text:
    raise SystemExit("CreateFramedGlassPanel not found")
text = text.replace(old_framed, "", 1)

# Drop unused color fields that no longer appear
for unused in (
    "        private static readonly Color GlassFill = new Color(39f / 255f, 40f / 255f, 92f / 255f, 0.62f);\n",
    "        private static readonly Color GlassFillInner = new Color(39f / 255f, 40f / 255f, 92f / 255f, 0.70f);\n",
    "        private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.28f);\n",
    "        private static readonly Color PanelShadow = new Color(0f, 0f, 0f, 0.45f);\n",
):
    text = text.replace(unused, "")

PATH.write_text(text, encoding="utf-8")
print("patched", PATH)
# sanity
assert "CreateCard" in text
assert "_root.SetActive(false);" not in text.split("public void Open")[1].split("public void Close")[0]
assert "任務樹" in text
assert "完成" in text
assert "CreateCard" in text
print("ok")
