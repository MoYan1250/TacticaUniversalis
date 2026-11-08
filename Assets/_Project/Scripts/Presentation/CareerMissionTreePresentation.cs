using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Fullscreen HOI-style mission tree: pannable nodes, hover tip, click detail popup.</summary>
    public sealed class CareerMissionTreePresentation
    {
        private const float ColWidth = 240f;
        private const float RowHeight = 190f;
        private const float TreePad = 72f;
        private const float IconSize = 78f;
        private const float LabelWidth = 168f;
        private const float LabelHeight = 52f;

        private static readonly Color DimTint = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color FallbackBackground = CareerEditorialChrome.Stage;
        private static readonly Color GlowElectric = new Color(0.55f, 0.35f, 0.06f, 0.28f);
        private static readonly Color GlowElectricSoft = new Color(0.10f, 0.36f, 0.39f, 0.16f);
        private static readonly Color TextPrimary = CareerEditorialChrome.Ink;
        private static readonly Color TextMuted = CareerEditorialChrome.InkMuted;
        private static readonly Color AccentIce = CareerEditorialChrome.Mustard;
        private static readonly Color LineTint = CareerEditorialChrome.Mustard;
        private static readonly Color CompleteTint = CareerEditorialChrome.Mustard;
        private static readonly Color AvailableTint = CareerEditorialChrome.Cyan;
        private static readonly Color CompletedTint = CareerEditorialChrome.Red;
        private static readonly Color LockedTint = CareerEditorialChrome.Ink;
        private static readonly Color IconRim = CareerEditorialChrome.Paper;
        private static readonly Color PopupDim = new Color(0f, 0f, 0f, 0.55f);

        private readonly CareerSession _session;
        private readonly Font _font;
        private readonly Action _onChanged;
        private readonly Action<string> _onStatus;
        private readonly Action _onRequestClose;

        private GameObject _root;
        private RawImage _blurBackground;
        private Image _fallbackBackground;
        private UiBackdropBlurRunner _blurRunner;
        private RectTransform _scrollContent;
        private ScrollRect _scroll;
        private Texture2D _blurTexture;

        private GameObject _hoverTipRoot;
        private Text _hoverTipTitle;
        private Text _hoverTipGoal;
        private string _hoverNodeId;

        private GameObject _detailRoot;
        private Text _headerTitle;
        private Text _detailTitle;
        private Text _detailFlavor;
        private Text _detailGoal;
        private Text _detailStatus;
        private Button _detailClaimButton;
        private string _detailNodeId;

        private readonly Dictionary<string, Vector2> _nodeCenters = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);

        public CareerMissionTreePresentation(
            CareerSession session,
            Font font,
            Action onChanged,
            Action<string> onStatus,
            Action onRequestClose)
        {
            _session = session;
            _font = font;
            _onChanged = onChanged;
            _onStatus = onStatus;
            _onRequestClose = onRequestClose;
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(Transform canvas)
        {
            EnsureUi(canvas);

            if (_fallbackBackground != null && _blurTexture == null)
            {
                _fallbackBackground.gameObject.SetActive(true);
                _fallbackBackground.color = FallbackBackground;
            }
            if (_blurBackground != null && _blurTexture == null)
                _blurBackground.gameObject.SetActive(false);

            bool alreadyOpen = _root.activeSelf;
            UiChromeMotion.Show(_root);
            BringToFront();
            HideDetailPopup();
            HideHoverTip();
            RebuildNodes();
            RefreshFrostHost(canvas);

            if (alreadyOpen)
                return;

            EnsureBlurRunner(canvas);
            _blurRunner.CaptureBlurred(OnBlurReady);
        }

        public void Close()
        {
            HideDetailPopup();
            HideHoverTip();
            UiChromeMotion.Hide(_root, ReleaseBlurTexture);
        }

        public void BringToFront()
        {
            if (_root != null)
                _root.transform.SetAsLastSibling();
        }

        public void Refresh()
        {
            if (!IsOpen)
                return;
            BringToFront();
            string keepDetailId = _detailNodeId;
            RebuildNodes();
            if (!string.IsNullOrWhiteSpace(keepDetailId))
            {
                CareerMissionTreeDef tree = _session?.GetPlayerMissionTree();
                CareerMissionNodeDef node = CareerMissionRules.FindNode(tree, keepDetailId);
                if (node != null)
                    ShowDetailPopup(node, _session.GetMissionNodeStatus(node.id));
                else
                    HideDetailPopup();
            }
        }

        private void EnsureBlurRunner(Transform canvas)
        {
            if (_blurRunner != null)
                return;

            Transform parent = canvas != null ? canvas : _root.transform.parent;
            Transform host = parent != null ? parent.Find("MissionTreeBlurHost") : null;
            if (host == null && parent != null)
            {
                var go = new GameObject("MissionTreeBlurHost", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                host = go.transform;
            }

            if (host == null)
                host = parent;

            _blurRunner = host.GetComponent<UiBackdropBlurRunner>();
            if (_blurRunner == null)
                _blurRunner = host.gameObject.AddComponent<UiBackdropBlurRunner>();
        }

        private void OnBlurReady(Texture2D blurred)
        {
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
        }

        private void ReleaseBlurTexture()
        {
            if (_blurTexture == null)
                return;
            UiBackdropBlurCapture.Release(_blurTexture);
            _blurTexture = null;
            if (_blurBackground != null)
                _blurBackground.texture = null;
        }

        private void EnsureUi(Transform canvas)
        {
            if (_root != null)
                return;

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
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                UiLightChromeUtility.SoftNavyFace);
            UiChromeMotion.BindFace(_root, panel);

            Image header = CreateUiObject<Image>("Header", panel.transform);
            SetRect(header.rectTransform, new Vector2(0.03f, 0.90f), new Vector2(0.97f, 0.98f));
            UiLightChromeUtility.ApplySoftNavyPanel(header);

            _headerTitle = CreateText("Title", header.transform, 32, TextAnchor.MiddleLeft, AccentIce);
            SetRect(_headerTitle.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.78f, 0.92f));
            _headerTitle.fontStyle = FontStyle.Bold;
            _headerTitle.text = "任務樹";
            EnableBestFit(_headerTitle, 36);

            Image scrollHost = CreateUiObject<Image>("ScrollHost", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.88f));
            UiLightChromeUtility.ApplySoftNavyPanel(scrollHost);

            _scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = true;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = true;
            _scroll.decelerationRate = 0.135f;
            _scroll.scrollSensitivity = 28f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            // RectMask2D: avoids nested Mask stencil under CreateCard frost Mask (invisible but clickable).
            viewport.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport = viewport.rectTransform;

            _scrollContent = CreateUiObject<RectTransform>("Content", viewport.transform);
            _scrollContent.anchorMin = new Vector2(0f, 1f);
            _scrollContent.anchorMax = new Vector2(0f, 1f);
            _scrollContent.pivot = new Vector2(0f, 1f);
            _scrollContent.anchoredPosition = Vector2.zero;
            _scrollContent.sizeDelta = new Vector2(1200f, 900f);
            _scroll.content = _scrollContent;

            BuildHoverTip(panel.transform);
            BuildDetailPopup(panel.transform);
            CreateCloseButton(panel.transform);
        }

        private void CreateCloseButton(Transform panel)
        {
            Button btn = UiLightChromeUtility.CreateSquareCloseButton(
                "CloseTree", panel,
                new Vector2(0.935f, 0.935f), new Vector2(0.985f, 0.985f),
                () => _onRequestClose?.Invoke(),
                _font, 28);
            btn.transform.parent.SetAsLastSibling();
        }

        private void BuildHoverTip(Transform panel)
        {
            Image tip = CreateUiObject<Image>("HoverTip", panel.transform);
            tip.rectTransform.anchorMin = new Vector2(0f, 1f);
            tip.rectTransform.anchorMax = new Vector2(0f, 1f);
            tip.rectTransform.pivot = new Vector2(0f, 1f);
            tip.rectTransform.sizeDelta = new Vector2(320f, 140f);
            tip.rectTransform.anchoredPosition = new Vector2(40f, -120f);
            UiLightChromeUtility.ApplySoftNavyPanel(tip);
            tip.raycastTarget = false;
            _hoverTipRoot = tip.gameObject;
            _hoverTipRoot.SetActive(false);

            _hoverTipTitle = CreateText("HoverTitle", tip.transform, 18, TextAnchor.UpperLeft, AccentIce);
            SetRect(_hoverTipTitle.rectTransform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.94f));
            _hoverTipTitle.fontStyle = FontStyle.Bold;
            _hoverTipTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            EnableBestFit(_hoverTipTitle, 22);

            Text goalLabel = CreateText("HoverGoalLabel", tip.transform, 14, TextAnchor.UpperLeft, TextMuted);
            SetRect(goalLabel.rectTransform, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.62f));
            goalLabel.text = "任務條件";
            goalLabel.fontStyle = FontStyle.Bold;

            _hoverTipGoal = CreateText("HoverGoal", tip.transform, 16, TextAnchor.UpperLeft, TextPrimary);
            SetRect(_hoverTipGoal.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.48f));
            _hoverTipGoal.horizontalOverflow = HorizontalWrapMode.Wrap;
            _hoverTipGoal.verticalOverflow = VerticalWrapMode.Overflow;
            EnableBestFit(_hoverTipGoal, 18);
        }

        private void BuildDetailPopup(Transform panel)
        {
            Image dim = CreateUiObject<Image>("DetailDim", panel.transform);
            Stretch(dim.rectTransform);
            dim.color = PopupDim;
            dim.raycastTarget = true;
            _detailRoot = dim.gameObject;
            _detailRoot.SetActive(false);

            EventTrigger dimTrigger = dim.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(dimTrigger, EventTriggerType.PointerClick, _ => HideDetailPopup());

            Image card = CreateUiObject<Image>("DetailCard", dim.transform);
            SetRect(card.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.78f));
            UiLightChromeUtility.ApplySoftNavyPanel(card);
            UiChromeMotion.Bind(_detailRoot, card.rectTransform);
            card.raycastTarget = true;

            _detailTitle = CreateText("DetailTitle", card.transform, 26, TextAnchor.UpperLeft, AccentIce);
            SetRect(_detailTitle.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.82f, 0.96f));
            _detailTitle.fontStyle = FontStyle.Bold;
            _detailTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            EnableBestFit(_detailTitle, 30);

            UiLightChromeUtility.CreateSquareCloseButton(
                "DetailClose", card.transform,
                new Vector2(0.88f, 0.86f), new Vector2(0.96f, 0.96f),
                HideDetailPopup,
                _font, 24);

            Text flavorLabel = CreateText("FlavorLabel", card.transform, 14, TextAnchor.UpperLeft, TextMuted);
            SetRect(flavorLabel.rectTransform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.82f));
            flavorLabel.text = "描述";
            flavorLabel.fontStyle = FontStyle.Bold;

            _detailFlavor = CreateText("DetailFlavor", card.transform, 17, TextAnchor.UpperLeft, TextPrimary);
            SetRect(_detailFlavor.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.74f));
            _detailFlavor.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailFlavor.verticalOverflow = VerticalWrapMode.Overflow;
            EnableBestFit(_detailFlavor, 20);

            Text goalLabel = CreateText("GoalLabel", card.transform, 14, TextAnchor.UpperLeft, TextMuted);
            SetRect(goalLabel.rectTransform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.42f));
            goalLabel.text = "任務條件";
            goalLabel.fontStyle = FontStyle.Bold;

            _detailGoal = CreateText("DetailGoal", card.transform, 17, TextAnchor.UpperLeft, TextPrimary);
            SetRect(_detailGoal.rectTransform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.34f));
            _detailGoal.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailGoal.verticalOverflow = VerticalWrapMode.Overflow;
            EnableBestFit(_detailGoal, 20);

            _detailStatus = CreateText("DetailStatus", card.transform, 15, TextAnchor.MiddleLeft, AccentIce);
            SetRect(_detailStatus.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.14f));
            _detailStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            EnableBestFit(_detailStatus, 18);

            _detailClaimButton = UiLightChromeUtility.CreateGlassCapsuleButton(
                "DetailClaim", "任務完成", card.transform,
                new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.14f),
                OnDetailClaimClicked,
                _font, 18, fillLabel: true, primary: true);
            _detailClaimButton.gameObject.SetActive(false);
        }

        private void RebuildNodes()
        {
            if (_scrollContent == null)
                return;

            _nodeCenters.Clear();
            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_scrollContent.GetChild(i).gameObject);

            CareerMissionTreeDef tree = _session?.GetPlayerMissionTree();
            if (tree?.nodes == null || tree.nodes.Length == 0)
            {
                _scrollContent.sizeDelta = new Vector2(640f, 320f);
                Text empty = CreateText("Empty", _scrollContent, 22, TextAnchor.MiddleCenter, TextMuted);
                empty.rectTransform.anchorMin = new Vector2(0.1f, 0.35f);
                empty.rectTransform.anchorMax = new Vector2(0.9f, 0.65f);
                empty.rectTransform.offsetMin = Vector2.zero;
                empty.rectTransform.offsetMax = Vector2.zero;
                empty.text = "此俱樂部目前沒有可用的任務樹。";
                EnableBestFit(empty, 26);
                return;
            }

            CareerMissionNodeDef[] nodes = tree.nodes
                .Where(n => n != null && !string.IsNullOrWhiteSpace(n.id))
                .ToArray();

            int maxCol = 0;
            int maxRow = 0;
            foreach (CareerMissionNodeDef n in nodes)
            {
                maxCol = Mathf.Max(maxCol, Mathf.Max(0, n.uiColumn));
                maxRow = Mathf.Max(maxRow, Mathf.Max(0, n.uiRow));
            }

            float contentW = TreePad * 2f + (maxCol + 1) * ColWidth;
            float contentH = TreePad * 2f + (maxRow + 1) * RowHeight;
            _scrollContent.sizeDelta = new Vector2(Mathf.Max(contentW, 900f), Mathf.Max(contentH, 700f));

            foreach (CareerMissionNodeDef node in nodes)
            {
                Vector2 center = CellCenter(node.uiColumn, node.uiRow);
                _nodeCenters[node.id] = center;
            }

            Transform linksHost = CreateUiObject<RectTransform>("Links", _scrollContent);
            Stretch(linksHost as RectTransform);
            linksHost.SetAsFirstSibling();

            foreach (CareerMissionNodeDef node in nodes)
            {
                if (node.prerequisites == null)
                    continue;
                foreach (string preId in node.prerequisites)
                {
                    if (string.IsNullOrWhiteSpace(preId))
                        continue;
                    if (!_nodeCenters.TryGetValue(preId, out Vector2 from))
                        continue;
                    if (!_nodeCenters.TryGetValue(node.id, out Vector2 to))
                        continue;
                    CreateEdge(linksHost, from, to, preId, node.id);
                }
            }

            foreach (CareerMissionNodeDef node in nodes
                         .OrderBy(n => n.uiRow)
                         .ThenBy(n => n.uiColumn)
                         .ThenBy(n => n.id, StringComparer.OrdinalIgnoreCase))
            {
                CreateTreeNode(node, _nodeCenters[node.id]);
            }

            Canvas.ForceUpdateCanvases();
            if (_scroll != null)
            {
                _scroll.horizontalNormalizedPosition = 0f;
                _scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static Vector2 CellCenter(int col, int row)
        {
            float x = TreePad + Mathf.Max(0, col) * ColWidth + ColWidth * 0.5f;
            float y = -(TreePad + Mathf.Max(0, row) * RowHeight + RowHeight * 0.5f);
            return new Vector2(x, y);
        }

        private void CreateEdge(Transform parent, Vector2 from, Vector2 to, string fromId, string toId)
        {
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 1f)
                return;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Image line = CreateUiObject<Image>("Link_" + fromId + "_" + toId, parent);
            RectTransform rt = line.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(dist, 4f);
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.localEulerAngles = new Vector3(0f, 0f, angle);
            line.color = LineTint;
            line.raycastTarget = false;
        }

        private void CreateTreeNode(CareerMissionNodeDef node, Vector2 center)
        {
            CareerMissionNodeStatus status = _session.GetMissionNodeStatus(node.id);
            Color faceColor = status switch
            {
                CareerMissionNodeStatus.Completable => CompleteTint,
                CareerMissionNodeStatus.Completed => CompletedTint,
                CareerMissionNodeStatus.Available => AvailableTint,
                _ => LockedTint
            };

            RectTransform host = CreateUiObject<RectTransform>("Node_" + node.id, _scrollContent);
            host.anchorMin = new Vector2(0f, 1f);
            host.anchorMax = new Vector2(0f, 1f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(LabelWidth + 24f, IconSize + LabelHeight + 18f);
            host.anchoredPosition = center;

            Image hit = host.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            Sprite soft = UiRoundedRectUtility.GetSoftSquareSprite(64, 18);
            Image rim = CreateUiObject<Image>("Rim", host);
            rim.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rim.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rim.rectTransform.pivot = new Vector2(0.5f, 1f);
            rim.rectTransform.sizeDelta = new Vector2(IconSize + 8f, IconSize + 8f);
            rim.rectTransform.anchoredPosition = new Vector2(0f, -4f);
            rim.sprite = soft;
            rim.type = Image.Type.Sliced;
            rim.color = IconRim;
            rim.raycastTarget = false;

            Image icon = CreateUiObject<Image>("Icon", host);
            icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            icon.rectTransform.pivot = new Vector2(0.5f, 1f);
            icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
            icon.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            icon.sprite = soft;
            icon.type = Image.Type.Sliced;
            icon.color = faceColor;
            icon.raycastTarget = false;

            Text glyph = CreateText("Glyph", icon.transform, 28, TextAnchor.MiddleCenter, CareerEditorialChrome.Paper);
            Stretch(glyph.rectTransform);
            glyph.text = StatusGlyph(status);
            glyph.fontStyle = FontStyle.Bold;
            EnableBestFit(glyph, 34);

            Image labelBg = CreateUiObject<Image>("LabelBg", host);
            labelBg.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            labelBg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            labelBg.rectTransform.pivot = new Vector2(0.5f, 0f);
            labelBg.rectTransform.sizeDelta = new Vector2(LabelWidth, LabelHeight);
            labelBg.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            UiLightChromeUtility.ApplySoftNavyPanel(labelBg);
            labelBg.raycastTarget = false;

            Text title = CreateText("Title", labelBg.transform, 18, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            title.fontStyle = FontStyle.Bold;
            title.text = node.title ?? node.id;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            EnableBestFit(title, 22);

            CareerMissionNodeDef captured = node;
            CareerMissionNodeStatus capturedStatus = status;
            EventTrigger trigger = host.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => ShowHoverTip(captured, capturedStatus, host));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit,
                _ =>
                {
                    if (_hoverNodeId == captured.id)
                        HideHoverTip();
                });
            AddPointerTrigger(trigger, EventTriggerType.PointerClick,
                _ => ShowDetailPopup(captured, _session.GetMissionNodeStatus(captured.id)));
        }

        private static string StatusGlyph(CareerMissionNodeStatus status)
        {
            return status switch
            {
                CareerMissionNodeStatus.Completed => "✓",
                CareerMissionNodeStatus.Completable => "!",
                CareerMissionNodeStatus.Available => "◆",
                CareerMissionNodeStatus.BlockedByExclusive => "×",
                _ => "·"
            };
        }

        private void ShowHoverTip(CareerMissionNodeDef node, CareerMissionNodeStatus status, RectTransform nodeRt)
        {
            if (_hoverTipRoot == null || node == null || (_detailRoot != null && _detailRoot.activeSelf))
                return;

            _hoverNodeId = node.id;
            _hoverTipTitle.text = node.title ?? node.id;
            _hoverTipGoal.text = !string.IsNullOrWhiteSpace(node.goal)
                ? node.goal
                : CareerMissionRules.StatusLabel(status);
            _hoverTipRoot.SetActive(true);
            _hoverTipRoot.transform.SetAsLastSibling();

            RectTransform tipRt = _hoverTipRoot.transform as RectTransform;
            RectTransform panelRt = tipRt != null ? tipRt.parent as RectTransform : null;
            if (tipRt == null || panelRt == null || nodeRt == null)
                return;

            Vector3[] corners = new Vector3[4];
            nodeRt.GetWorldCorners(corners);
            // corners[2] = top-right of node in world space
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRt,
                    RectTransformUtility.WorldToScreenPoint(null, corners[2]),
                    null,
                    out Vector2 local))
                return;

            Vector2 size = tipRt.sizeDelta;
            float panelW = panelRt.rect.width;
            float panelH = panelRt.rect.height;
            // Tip is anchored top-left of panel; convert center-local coords if panel is stretch.
            Vector2 panelLocalTopLeft = new Vector2(-panelW * 0.5f, panelH * 0.5f);
            Vector2 pos = local - panelLocalTopLeft + new Vector2(12f, -8f);
            pos.x = Mathf.Clamp(pos.x, 12f, Mathf.Max(12f, panelW - size.x - 12f));
            pos.y = Mathf.Clamp(pos.y, -(panelH - 12f), -12f);
            tipRt.anchoredPosition = pos;
        }

        private void HideHoverTip()
        {
            _hoverNodeId = null;
            if (_hoverTipRoot != null)
                _hoverTipRoot.SetActive(false);
        }

        private void ShowDetailPopup(CareerMissionNodeDef node, CareerMissionNodeStatus status)
        {
            if (_detailRoot == null || node == null)
                return;

            HideHoverTip();
            _detailNodeId = node.id;
            _detailTitle.text = node.title ?? node.id;
            _detailFlavor.text = !string.IsNullOrWhiteSpace(node.flavor)
                ? node.flavor
                : !string.IsNullOrWhiteSpace(node.description)
                    ? node.description
                    : "尚無描述。";
            _detailGoal.text = !string.IsNullOrWhiteSpace(node.goal)
                ? node.goal
                : "尚無任務條件。";
            _detailStatus.text = CareerMissionRules.StatusLabel(status);
            bool canClaim = status == CareerMissionNodeStatus.Completable;
            if (_detailClaimButton != null)
                _detailClaimButton.gameObject.SetActive(canClaim);

            UiChromeMotion.Show(_detailRoot);
            _detailRoot.transform.SetAsLastSibling();
        }

        private void HideDetailPopup()
        {
            _detailNodeId = null;
            UiChromeMotion.Hide(_detailRoot);
            if (_detailClaimButton != null)
                _detailClaimButton.gameObject.SetActive(false);
        }

        private void OnDetailClaimClicked()
        {
            if (string.IsNullOrWhiteSpace(_detailNodeId))
                return;
            OnClaimClicked(_detailNodeId);
        }

        private void OnClaimClicked(string nodeId)
        {
            if (_session == null)
                return;
            if (!_session.TryCompleteMissionNode(nodeId, out string failReason))
            {
                if (!string.IsNullOrWhiteSpace(failReason))
                    _onStatus?.Invoke(failReason);
                Refresh();
                return;
            }

            _onStatus?.Invoke("任務已完成。");
            _onChanged?.Invoke();
            Refresh();
        }

        private static void AddPointerTrigger(
            EventTrigger trigger, EventTriggerType type, Action<BaseEventData> handler)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => handler(data));
            trigger.triggers.Add(entry);
        }

        private static void EnableBestFit(Text text, int maxSize)
        {
            if (text == null)
                return;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = Mathf.Max(maxSize, text.fontSize);
        }

        private T CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            if (typeof(T) == typeof(RectTransform))
                return go.GetComponent<T>();
            return go.AddComponent<T>();
        }

        private Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            Text text = CreateUiObject<Text>(name, parent);
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        private void AddGlowLayer(
            Transform parent, string name, Vector2 min, Vector2 max, Color color, Sprite glowSprite)
        {
            Image glow = CreateUiObject<Image>(name, parent);
            SetRect(glow.rectTransform, min, max);
            glow.sprite = glowSprite;
            glow.type = Image.Type.Simple;
            glow.color = color;
            glow.raycastTarget = false;
        }
    }
}
