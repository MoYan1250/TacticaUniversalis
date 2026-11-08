using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private enum ClubHubTab
        {
            History,
            Missions,
            Organization,
            Resolutions
        }

        private GameObject _clubHubRoot;
        private RectTransform _clubHubPanelRect;
        private Image _clubHubPanelFace;
        private Image _clubHubHeaderLogo;
        private Text _clubHubTitleText;
        private RectTransform _clubHubModifierRoot;
        private RectTransform _clubHubModifierGrid;
        private Text _clubHubModifierEmptyText;
        private ScrollRect _clubHubModifierScroll;
        private Coroutine _clubHubModifierHoverRoutine;
        private string _clubHubModifierHoverKey;
        private const float ClubHubModifierHoverDelaySeconds = 1f;
        private const float ClubHubModifierIconSize = 34f;
        private RectTransform _clubHubContentRoot;
        private ScrollRect _clubHubContentScroll;
        private readonly List<Button> _clubHubTabButtons = new List<Button>();
        private readonly List<Image> _clubHubTabFaces = new List<Image>();
        private ClubHubTab _clubHubTab = ClubHubTab.History;
        private bool _clubHubOpen;
        private bool _clubHubClosing;
        private bool _missionTreeOverlayDismissed;
        private Coroutine _clubHubSlideRoutine;
        private Coroutine _clubHubFlashRoutine;

        private const float ClubHubPanelAnchorWidth = 0.40f * (2f / 3f);
        private const float ClubHubDimOverlap = 0.04f;
        private static readonly Color ClubHubChromeBorder = UiLightChromeUtility.Border;
        private static readonly Color ClubHubChromeShadow = UiLightChromeUtility.Shadow;
        /// <summary>ContentHost nested fill — thinner than SoftNavyFace so outer panel frost shows through.</summary>
        private static readonly Color ClubHubContentWell = CareerEditorialChrome.Paper;
        private static readonly Color ClubHubFrostedRow = CareerEditorialChrome.Rule;

        private void BuildClubHubPanel(Transform canvas)
        {
            Image root = CreateUiObject<Image>("ClubHubOverlay", canvas);
            Stretch(root.rectTransform);
            root.color = Color.clear;
            root.raycastTarget = false;
            _clubHubRoot = root.gameObject;
            _clubHubRoot.SetActive(false);

            Image dimRight = CreateUiObject<Image>("ClubHubDim", root.transform);
            SetRect(dimRight.rectTransform,
                new Vector2(ClubHubPanelAnchorWidth - ClubHubDimOverlap, 0f),
                new Vector2(1f, 1f));
            dimRight.color = new Color(0.14f, 0.13f, 0.11f, 0.45f);
            dimRight.raycastTarget = true;
            Button dimBtn = dimRight.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = dimRight;
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseClubHubPanel);

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();

            GameObject panelGo = new GameObject("ClubHubPanel", typeof(RectTransform));
            panelGo.transform.SetParent(root.transform, false);
            _clubHubPanelRect = panelGo.GetComponent<RectTransform>();
            SetRect(_clubHubPanelRect, new Vector2(0f, 0.035f), new Vector2(ClubHubPanelAnchorWidth, 0.965f));

            Image shadow = CreateUiObject<Image>("Shadow", panelGo.transform);
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(3.5f, -3.5f);
            shadow.rectTransform.offsetMax = new Vector2(3.5f, -3.5f);
            shadow.sprite = rounded;
            shadow.type = Image.Type.Sliced;
            shadow.color = ClubHubChromeShadow;
            shadow.raycastTarget = false;

            Image panelFrame = CreateUiObject<Image>("Frame", panelGo.transform);
            Stretch(panelFrame.rectTransform);
            panelFrame.sprite = rounded;
            panelFrame.type = Image.Type.Sliced;
            panelFrame.color = ClubHubChromeBorder;
            panelFrame.raycastTarget = false;

            Image panel = CreateUiObject<Image>("Face", panelFrame.transform);
            Stretch(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(2.5f, 2.5f);
            panel.rectTransform.offsetMax = new Vector2(-2.5f, -2.5f);
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.raycastTarget = true;
            panel.color = CareerEditorialChrome.Paper;
            _clubHubPanelFace = panel;
            Button panelBlock = panel.gameObject.AddComponent<Button>();
            panelBlock.targetGraphic = panel;
            panelBlock.transition = Selectable.Transition.None;

            Image header = CreateClubHubInsetWell(
                "Header", panel.transform,
                new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f),
                UiLightChromeUtility.SoftNavyFace, rounded);

            Image headerLogo = CreateUiObject<Image>("Logo", header.transform);
            SetRect(headerLogo.rectTransform, new Vector2(0.02f, 0.025f), new Vector2(0.14f, 0.975f));
            headerLogo.preserveAspect = true;
            headerLogo.raycastTarget = false;
            _clubHubHeaderLogo = headerLogo;

            _clubHubTitleText = CreateText("Title", header.transform, 20, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_clubHubTitleText.rectTransform, new Vector2(0.19f, 0.1f), new Vector2(0.72f, 0.9f));
            _clubHubTitleText.text = "俱樂部";

            UiLightChromeUtility.CreateSquareCloseButton(
                "CloseHub", header.transform,
                new Vector2(0.935f, 0.22f), new Vector2(0.988f, 0.78f),
                CloseClubHubPanel, _uiFont700, 22);

            Image modHost = CreateClubHubInsetWell(
                "ModifierHost", panel.transform,
                new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.87f),
                UiLightChromeUtility.SoftNavyFace, rounded);

            _clubHubModifierScroll = modHost.gameObject.AddComponent<ScrollRect>();
            _clubHubModifierScroll.horizontal = false;
            _clubHubModifierScroll.vertical = true;
            _clubHubModifierScroll.movementType = ScrollRect.MovementType.Clamped;
            _clubHubModifierScroll.scrollSensitivity = 24f;

            Image modViewport = CreateUiObject<Image>("Viewport", modHost.transform);
            Stretch(modViewport.rectTransform);
            modViewport.color = Color.clear;
            modViewport.raycastTarget = true;
            modViewport.gameObject.AddComponent<RectMask2D>();
            _clubHubModifierScroll.viewport = modViewport.rectTransform;

            _clubHubModifierRoot = new GameObject("Modifiers", typeof(RectTransform)).GetComponent<RectTransform>();
            _clubHubModifierRoot.SetParent(modViewport.transform, false);
            Stretch(_clubHubModifierRoot);
            _clubHubModifierRoot.pivot = new Vector2(0.5f, 1f);
            _clubHubModifierRoot.anchorMin = new Vector2(0f, 1f);
            _clubHubModifierRoot.anchorMax = new Vector2(1f, 1f);
            _clubHubModifierRoot.anchoredPosition = Vector2.zero;
            _clubHubModifierRoot.sizeDelta = Vector2.zero;

            var modLayout = _clubHubModifierRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            modLayout.childAlignment = TextAnchor.UpperLeft;
            modLayout.childControlWidth = true;
            modLayout.childControlHeight = true;
            modLayout.childForceExpandWidth = true;
            modLayout.childForceExpandHeight = false;
            modLayout.spacing = 2f;
            modLayout.padding = new RectOffset(6, 6, 4, 4);
            var modFitter = _clubHubModifierRoot.gameObject.AddComponent<ContentSizeFitter>();
            modFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _clubHubModifierScroll.content = _clubHubModifierRoot;

            _clubHubModifierEmptyText = CreateText(
                "EmptyHint", _clubHubModifierRoot, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            LayoutElement emptyLe = _clubHubModifierEmptyText.gameObject.AddComponent<LayoutElement>();
            emptyLe.preferredHeight = 22f;
            emptyLe.flexibleWidth = 1f;
            _clubHubModifierEmptyText.text = "目前無特殊增益或減益。";
            _clubHubModifierEmptyText.gameObject.SetActive(false);

            _clubHubModifierGrid = new GameObject("IconGrid", typeof(RectTransform)).GetComponent<RectTransform>();
            _clubHubModifierGrid.SetParent(_clubHubModifierRoot, false);
            var gridLe = _clubHubModifierGrid.gameObject.AddComponent<LayoutElement>();
            gridLe.flexibleWidth = 1f;
            var modGrid = _clubHubModifierGrid.gameObject.AddComponent<GridLayoutGroup>();
            modGrid.cellSize = new Vector2(ClubHubModifierIconSize, ClubHubModifierIconSize);
            modGrid.spacing = new Vector2(4f, 4f);
            modGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            modGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            modGrid.childAlignment = TextAnchor.UpperLeft;
            modGrid.constraint = GridLayoutGroup.Constraint.Flexible;

            BuildClubHubTabButton(panel.transform, "歷史", ClubHubTab.History, 0.04f, 0.255f);
            BuildClubHubTabButton(panel.transform, "任務", ClubHubTab.Missions, 0.275f, 0.490f);
            BuildClubHubTabButton(panel.transform, "組織", ClubHubTab.Organization, 0.510f, 0.725f);
            BuildClubHubTabButton(panel.transform, "決議", ClubHubTab.Resolutions, 0.745f, 0.960f);

            Image contentHost = CreateClubHubInsetWell(
                "ContentHost", panel.transform,
                new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.61f), ClubHubContentWell, rounded);

            _clubHubContentScroll = contentHost.gameObject.AddComponent<ScrollRect>();
            _clubHubContentScroll.horizontal = false;
            _clubHubContentScroll.vertical = true;
            _clubHubContentScroll.movementType = ScrollRect.MovementType.Clamped;
            _clubHubContentScroll.scrollSensitivity = 36f;

            Image contentViewport = CreateUiObject<Image>("Viewport", contentHost.transform);
            Stretch(contentViewport.rectTransform);
            contentViewport.color = Color.clear;
            contentViewport.raycastTarget = true;
            contentViewport.gameObject.AddComponent<RectMask2D>();
            _clubHubContentScroll.viewport = contentViewport.rectTransform;

            _clubHubContentRoot = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _clubHubContentRoot.SetParent(contentViewport.transform, false);
            _clubHubContentRoot.anchorMin = new Vector2(0f, 1f);
            _clubHubContentRoot.anchorMax = new Vector2(1f, 1f);
            _clubHubContentRoot.pivot = new Vector2(0.5f, 1f);
            _clubHubContentRoot.anchoredPosition = Vector2.zero;
            _clubHubContentRoot.sizeDelta = Vector2.zero;

            var contentLayout = _clubHubContentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 8f;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            var contentFitter = _clubHubContentRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _clubHubContentScroll.content = _clubHubContentRoot;

            _clubHubPanelRect.anchoredPosition = new Vector2(-4000f, 0f);
        }

        private void BuildClubHubTabButton(
            Transform parent, string label, ClubHubTab tab, float x0, float x1)
        {
            float y0 = 0.645f;
            float y1 = 0.705f;
            Sprite softSquare = UiRoundedRectUtility.GetSoftSquareSprite();

            Image edge = CreateUiObject<Image>("HubTab_" + tab, parent);
            SetRect(edge.rectTransform, new Vector2(x0, y0), new Vector2(x1, y1));
            edge.sprite = softSquare;
            edge.type = Image.Type.Sliced;
            edge.color = UiLightChromeUtility.Border;
            edge.raycastTarget = true;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.sprite = softSquare;
            face.type = Image.Type.Sliced;
            face.color = UiLightChromeUtility.SoftNavyFace;
            face.raycastTarget = true;

            Button btn = edge.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            ApplyButtonColors(btn);
            btn.onClick.AddListener(() => SelectClubHubTab(tab));

            Text text = CreateText("Text", face.transform, 16, TextAnchor.MiddleCenter, TextPrimary, _uiFont700);
            text.text = label;
            ApplyFilledButtonLabel(text, Mathf.Max(36, 16 + 12));

            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;

            _clubHubTabButtons.Add(btn);
            _clubHubTabFaces.Add(face);
        }

        private void WireClubLogoHubButton()
        {
            if (_clubLogoImage == null)
                return;
            _clubLogoImage.raycastTarget = true;
            Button existing = _clubLogoImage.GetComponent<Button>();
            if (existing == null)
            {
                Button btn = _clubLogoImage.gameObject.AddComponent<Button>();
                btn.targetGraphic = _clubLogoImage;
                ApplyButtonColors(btn);
                btn.onClick.AddListener(OnClubLogoClicked);
            }
        }

        private void OnClubLogoClicked()
        {
            if (!_careerStarted || _session == null)
                return;
            if (_titleRoot != null && _titleRoot.activeSelf)
                return;
            // Closing in progress: reverse into open from current slide position.
            if (_clubHubClosing)
            {
                OpenClubHubPanel();
                return;
            }
            if (_clubHubOpen)
                CloseClubHubPanel();
            else
                OpenClubHubPanel();
        }

        private void OpenClubHubPanel()
        {
            if (_clubHubRoot == null)
                return;
            // Already fully open and idle — ignore duplicate opens.
            if (_clubHubOpen && !_clubHubClosing && _clubHubSlideRoutine == null)
                return;

            bool reversingClose = _clubHubClosing;
            _clubHubClosing = false;
            RefreshClubHubPanel();
            _clubHubRoot.SetActive(true);
            _clubHubRoot.transform.SetAsLastSibling();
            // Mission tree may open during RefreshClubHubPanel; keep it above the hub.
            BringMissionTreeAboveClubHub();
            _clubHubOpen = true;
            UiFrostedGlassHost frostHost = UiFrostedGlassHost.FindFor(_clubHubRoot.transform);
            if (frostHost != null)
                frostHost.RefreshBlur();
            if (!reversingClose)
            {
                if (_clubHubFlashRoutine != null)
                    StopCoroutine(_clubHubFlashRoutine);
                _clubHubFlashRoutine = StartCoroutine(FlashClubLogoBrief());
            }
            if (_clubHubSlideRoutine != null)
                StopCoroutine(_clubHubSlideRoutine);
            _clubHubSlideRoutine = StartCoroutine(SlideClubHubPanel(true));
        }

        private void CloseClubHubPanel()
        {
            if (_clubHubRoot == null || !_clubHubOpen)
                return;
            // Already closing: do not restart slide from fully-open, or the panel jumps.
            if (_clubHubClosing)
                return;

            CloseMissionTreeOverlay();
            CancelClubHubModifierHover();
            HideStatTooltip();
            _clubHubClosing = true;
            if (_clubHubSlideRoutine != null)
                StopCoroutine(_clubHubSlideRoutine);
            _clubHubSlideRoutine = StartCoroutine(SlideClubHubPanel(false));
        }

        private IEnumerator FlashClubLogoBrief()
        {
            if (_clubLogoImage == null)
                yield break;
            Color normal = Color.white;
            _clubLogoImage.color = Color.white;
            yield return new WaitForSecondsRealtime(0.07f);
            _clubLogoImage.color = normal;
            _clubHubFlashRoutine = null;
        }

        private IEnumerator SlideClubHubPanel(bool opening)
        {
            if (_clubHubPanelRect == null)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_clubHubPanelRect);
            float width = Mathf.Max(320f, _clubHubPanelRect.rect.width);
            float current = _clubHubPanelRect.anchoredPosition.x;
            // Continue from wherever the panel is (interrupted open/close), not a hard snap.
            float from = current;
            if (opening && current < -width - 1f)
                from = -width;
            float to = opening ? 0f : -width;
            if (Mathf.Abs(from - to) < 0.5f)
            {
                _clubHubPanelRect.anchoredPosition = new Vector2(to, 0f);
                if (!opening)
                {
                    _clubHubOpen = false;
                    _clubHubClosing = false;
                    if (_clubHubRoot != null)
                        _clubHubRoot.SetActive(false);
                }
                else
                    _clubHubClosing = false;
                _clubHubSlideRoutine = null;
                yield break;
            }

            const float fullDuration = 0.26f;
            float duration = fullDuration * Mathf.Clamp01(Mathf.Abs(to - from) / Mathf.Max(1f, width));
            float elapsed = 0f;
            _clubHubPanelRect.anchoredPosition = new Vector2(from, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                u = u * u * (3f - 2f * u);
                float x = Mathf.Lerp(from, to, u);
                _clubHubPanelRect.anchoredPosition = new Vector2(x, 0f);
                yield return null;
            }

            _clubHubPanelRect.anchoredPosition = new Vector2(to, 0f);
            if (!opening)
            {
                _clubHubOpen = false;
                _clubHubClosing = false;
                if (_clubHubRoot != null)
                    _clubHubRoot.SetActive(false);
            }
            else
                _clubHubClosing = false;

            _clubHubSlideRoutine = null;
        }

        private void SelectClubHubTab(ClubHubTab tab)
        {
            if (tab != ClubHubTab.Missions)
                CloseMissionTreeOverlay();
            else
                _missionTreeOverlayDismissed = false;
            _clubHubTab = tab;
            RefreshClubHubTabVisuals();
            RefreshClubHubTabContent();
        }

        private void RefreshClubHubTabVisuals()
        {
            for (int i = 0; i < _clubHubTabFaces.Count; i++)
            {
                bool on = i == (int)_clubHubTab;
                _clubHubTabFaces[i].color = on
                    ? UiLightChromeUtility.FrostedRow
                    : UiLightChromeUtility.SoftNavyFace;
                Image edge = _clubHubTabFaces[i].transform.parent != null
                    ? _clubHubTabFaces[i].transform.parent.GetComponent<Image>()
                    : null;
                if (edge != null)
                    edge.color = on ? Accent : UiLightChromeUtility.Border;
            }
        }

        private void RefreshClubHubPanel()
        {
            if (_session == null || _content == null)
                return;

            string clubId = _session.State.playerClubId;
            _content.Clubs.TryGetValue(clubId, out CareerClubDefinition def);
            ClubState club = _session.State.Club(clubId);

            if (_clubHubTitleText != null)
                _clubHubTitleText.text = def != null ? def.displayName : clubId;

            Image headerLogo = _clubHubHeaderLogo;
            if (headerLogo != null)
            {
                headerLogo.sprite = LoadClubLogo(clubId);
                headerLogo.color = headerLogo.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            }

            RebuildClubHubModifiers(club, def);
            RefreshClubHubTabVisuals();
            RefreshClubHubTabContent();
        }

        private void RebuildClubHubModifiers(ClubState club, CareerClubDefinition def)
        {
            if (_clubHubModifierGrid == null)
                return;

            CancelClubHubModifierHover();
            HideStatTooltip();

            for (int i = _clubHubModifierGrid.childCount - 1; i >= 0; i--)
                DestroyImmediate(_clubHubModifierGrid.GetChild(i).gameObject);

            List<ClubHubModifierEntry> entries = CollectClubHubModifiers(club, def);
            bool hasAny = entries.Count > 0;
            if (_clubHubModifierEmptyText != null)
                _clubHubModifierEmptyText.gameObject.SetActive(!hasAny);
            if (_clubHubModifierGrid != null)
                _clubHubModifierGrid.gameObject.SetActive(hasAny);

            for (int i = 0; i < entries.Count; i++)
                AddClubHubModifierIcon(entries[i], i);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_clubHubModifierRoot);
            if (_clubHubModifierScroll != null)
                _clubHubModifierScroll.verticalNormalizedPosition = 1f;
        }

        private readonly struct ClubHubModifierEntry
        {
            public ClubHubModifierEntry(bool buff, string id, string title, string description)
            {
                Buff = buff;
                Id = id;
                Title = title;
                Description = description;
            }

            public bool Buff { get; }
            public string Id { get; }
            public string Title { get; }
            public string Description { get; }
        }

        private void AddClubHubModifierIcon(ClubHubModifierEntry entry, int index)
        {
            Color accent = entry.Buff ? Positive : Danger;
            Image slot = CreateUiObject<Image>("ModIcon_" + entry.Id, _clubHubModifierGrid);
            slot.color = CareerEditorialChrome.Paper;
            slot.raycastTarget = true;

            Image border = CreateUiObject<Image>("Border", slot.transform);
            Stretch(border.rectTransform);
            border.rectTransform.offsetMin = new Vector2(1f, 1f);
            border.rectTransform.offsetMax = new Vector2(-1f, -1f);
            border.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            border.raycastTarget = false;

            Image face = CreateUiObject<Image>("Face", slot.transform);
            SetRect(face.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            face.color = accent;
            face.raycastTarget = false;

            Text abbrev = CreateText("Abbrev", face.transform, 16, TextAnchor.MiddleCenter, CareerEditorialChrome.Ink, _uiFontBlack);
            Stretch(abbrev.rectTransform);
            abbrev.text = ClubHubModifierAbbrev(entry.Title);
            abbrev.raycastTarget = false;

            string hoverKey = entry.Id + "_" + index;
            EventTrigger trigger = slot.gameObject.AddComponent<EventTrigger>();
            string capturedTitle = entry.Title;
            string capturedBody = entry.Description;
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => OnClubHubModifierPointerEnter(hoverKey, entry.Buff, capturedTitle, capturedBody));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnClubHubModifierPointerExit());
        }

        private static string ClubHubModifierAbbrev(string title)
        {
            if (string.IsNullOrEmpty(title))
                return "?";
            foreach (char c in title)
            {
                if (char.IsLetterOrDigit(c) || c >= 0x4e00)
                    return c.ToString();
            }
            return title.Length >= 2 ? title.Substring(0, 2) : title;
        }

        private void OnClubHubModifierPointerEnter(string hoverKey, bool buff, string title, string body)
        {
            CancelClubHubModifierHover();
            HideStatTooltip();
            _clubHubModifierHoverKey = hoverKey;
            _clubHubModifierHoverRoutine = StartCoroutine(
                ShowClubHubModifierTooltipAfterDelay(hoverKey, buff, title, body));
        }

        private void OnClubHubModifierPointerExit()
        {
            CancelClubHubModifierHover();
            HideStatTooltip();
        }

        private IEnumerator ShowClubHubModifierTooltipAfterDelay(
            string hoverKey, bool buff, string title, string body)
        {
            yield return new WaitForSecondsRealtime(ClubHubModifierHoverDelaySeconds);
            if (!string.Equals(_clubHubModifierHoverKey, hoverKey, StringComparison.Ordinal))
                yield break;

            Color accent = buff ? Positive : Danger;
            string richTitle =
                $"<color=#{ColorUtility.ToHtmlStringRGB(accent)}>{(buff ? "增益" : "減益")}</color> · {title}";
            ShowPlainTooltip(
                richTitle,
                body,
                new Vector2(340f, Mathf.Clamp(120f + body.Length * 0.55f, 150f, 260f)),
                Input.mousePosition);
            _clubHubModifierHoverRoutine = null;
        }

        private void CancelClubHubModifierHover()
        {
            if (_clubHubModifierHoverRoutine != null)
            {
                StopCoroutine(_clubHubModifierHoverRoutine);
                _clubHubModifierHoverRoutine = null;
            }
            _clubHubModifierHoverKey = null;
        }

        private List<ClubHubModifierEntry> CollectClubHubModifiers(
            ClubState club, CareerClubDefinition def)
        {
            var list = new List<ClubHubModifierEntry>();
            if (club == null)
                return list;

            switch (club.financialHealth)
            {
                case FinancialHealth.Wealthy:
                    list.Add(new ClubHubModifierEntry(
                        true, "finance_wealthy", "財務充裕",
                        "贊助談判與引援預算空間較大，董事會對中長期投資容忍度較高。"));
                    break;
                case FinancialHealth.Healthy:
                    list.Add(new ClubHubModifierEntry(
                        true, "finance_healthy", "財務健康",
                        "每月收支大致平衡，可穩定營運薪資結構。"));
                    break;
                case FinancialHealth.Balanced:
                    break;
                case FinancialHealth.Deficit:
                    list.Add(new ClubHubModifierEntry(
                        false, "finance_deficit", "收支吃緊",
                        "需留意薪資與轉會支出；長期赤字會惡化財務狀況與董事會信心。"));
                    break;
                case FinancialHealth.Crisis:
                    list.Add(new ClubHubModifierEntry(
                        false, "finance_crisis", "財務危機",
                        "高額轉會可能受限，並提高觸發問責事件的機率。"));
                    break;
                case FinancialHealth.Bankrupt:
                    list.Add(new ClubHubModifierEntry(
                        false, "finance_bankrupt", "瀕臨破產",
                        "重大決策與引援空間嚴重受限，需優先止血與重建現金流。"));
                    break;
            }

            if (club.consecutiveCrisisMonths >= 2)
            {
                list.Add(new ClubHubModifierEntry(
                    false, "crisis_streak", "連續危機",
                    $"已連續 {club.consecutiveCrisisMonths} 個月處於危機狀態，董事會壓力上升。"));
            }
            else if (club.consecutiveLossMonths >= 2)
            {
                list.Add(new ClubHubModifierEntry(
                    false, "loss_streak", "連續虧損",
                    $"已連續 {club.consecutiveLossMonths} 個月淨值為負，預算與引援可能收緊。"));
            }

            PlayerCareerState[] roster = _session.State.players
                .Where(p => string.Equals(p.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var estateHints = new List<(bool buff, string id, string title, string description)>();
            ClubEstateSatisfactionRules.AppendModifierHints(
                club, _session.State, _content.Balance, def, roster, estateHints);
            foreach ((bool buff, string id, string title, string description) in estateHints)
                list.Add(new ClubHubModifierEntry(buff, id, title, description));

            int sponsors = _session.State.sponsors.Count(x => x.clubId == club.clubId);
            if (sponsors >= 2)
            {
                list.Add(new ClubHubModifierEntry(
                    true, "sponsors_multi", "多家贊助",
                    $"目前 {sponsors} 家贊助合作，被動現金流較穩定。"));
            }
            else if (sponsors == 0)
            {
                list.Add(new ClubHubModifierEntry(
                    false, "sponsors_none", "尚無贊助",
                    "缺少贊助月費，商品與名氣相關收入相對偏低。"));
            }

            if (def != null && def.ambition >= 85)
            {
                list.Add(new ClubHubModifierEntry(
                    true, "ambition_high", "高雄心",
                    "俱樂部賽事目標與媒體曝光需求更高，成功獎勵與壓力同步上升。"));
            }

            var reputationHints = new List<(bool buff, string id, string title, string description)>();
            ClubReputationRules.AppendModifierHints(club, reputationHints);
            foreach ((bool buff, string id, string title, string description) in reputationHints)
                list.Add(new ClubHubModifierEntry(buff, id, title, description));

            var crisisHints = new List<(bool buff, string id, string title, string description)>();
            ClubCrisisRules.AppendModifierHints(_session?.State, club, crisisHints);
            foreach ((bool buff, string id, string title, string description) in crisisHints)
                list.Add(new ClubHubModifierEntry(buff, id, title, description));

            int rank = RankFor(club.clubId);
            if (rank > 0 && rank <= 5)
            {
                list.Add(new ClubHubModifierEntry(
                    true, "rank_top5", "世界前五",
                    $"排名 #{rank}：邀請賽資格與媒體關注度加成。"));
            }

            return list;
        }

        private void RefreshClubHubTabContent()
        {
            if (_clubHubContentRoot == null || _session == null)
                return;

            for (int i = _clubHubContentRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_clubHubContentRoot.GetChild(i).gameObject);

            string clubId = _session.State.playerClubId;
            ClubState club = _session.State.Club(clubId);
            _content.Clubs.TryGetValue(clubId, out CareerClubDefinition def);

            switch (_clubHubTab)
            {
                case ClubHubTab.History:
                    DrawClubHubHistory(club, def);
                    break;
                case ClubHubTab.Missions:
                    DrawClubHubMissions();
                    break;
                case ClubHubTab.Organization:
                    DrawClubHubOrganization(club, def);
                    break;
                case ClubHubTab.Resolutions:
                    DrawClubHubResolutions(club, def);
                    break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_clubHubContentRoot);
            if (_clubHubContentScroll != null)
                _clubHubContentScroll.verticalNormalizedPosition = 1f;
        }

        private void DrawClubHubHistory(ClubState club, CareerClubDefinition def)
        {
            AddClubHubContentTitle("歷史數據");
            AddClubHubContentMuted(
                "俱樂部生涯累積與本檔關鍵指標（更多圖表與賽季回顧開發中）。");

            int hltv = HltvPointsFor(club.clubId);
            int rank = RankFor(club.clubId);
            AddClubHubContentRow("HLTV points", hltv > 0 ? hltv.ToString() : "—");
            AddClubHubContentRow("世界排名", rank > 0 ? $"#{rank}" : "—");
            AddClubHubContentRow("現金", $"${club.cash:N0}");
            AddClubHubContentRow("粉絲", club.fans.ToString("N0"));
            AddClubHubContentRow("威望", club.exposure.ToString());
            AddClubHubContentRow("名氣", club.fame.ToString());
            AddClubHubContentRow("董事會", club.boardSatisfaction.ToString());

            if (def != null)
            {
                AddClubHubContentRow("地區", string.IsNullOrEmpty(def.region) ? "—" : def.region);
                AddClubHubContentRow("開局名氣", def.fame.ToString());
                AddClubHubContentRow("雄心", def.ambition.ToString());
            }

            DrawClubHubTrophyCabinet(club.clubId);

            string playerClubId = _session.State.playerClubId;
            CareerEvent[] recent = (_session.State.events ?? new List<CareerEvent>())
                .Where(e => IsEventVisibleInClubRecentHistory(e, playerClubId))
                .Reverse()
                .Take(8)
                .ToArray();
            AddClubHubContentTitle("近期紀錄");
            if (recent.Length == 0)
                AddClubHubContentMuted("尚無事件。");
            else
            {
                foreach (CareerEvent evt in recent)
                    AddClubHubContentMuted(ResolveClubRecentHistoryLine(evt));
            }
        }

        private static string ResolveClubRecentHistoryLine(CareerEvent evt)
        {
            if (evt == null)
                return string.Empty;
            if (!string.IsNullOrWhiteSpace(evt.message))
                return evt.message.Trim();
            if (!string.IsNullOrWhiteSpace(evt.title))
                return evt.title.Trim();
            return evt.type.ToString();
        }

        private bool IsEventVisibleInClubRecentHistory(CareerEvent evt, string playerClubId)
        {
            if (evt == null || string.IsNullOrWhiteSpace(playerClubId))
                return false;
            if (!string.Equals(evt.clubId, playerClubId, StringComparison.OrdinalIgnoreCase))
                return false;
            return IsEventVisibleInNews(evt, playerClubId);
        }

        private void DrawClubHubTrophyCabinet(string clubId)
        {
            AddClubHubContentTitle("隊史獎盃");

            CompetitionEntry[] cabinet = _session.State.competitionEntries
                .Where(e => string.Equals(e.clubId, clubId, StringComparison.OrdinalIgnoreCase) &&
                            e.completed && e.placement > 0 && e.placement <= 16)
                .OrderBy(e => e.placement)
                .ThenByDescending(e => e.registeredHour)
                .ToArray();

            if (cabinet.Length == 0)
            {
                AddClubHubContentMuted("本檔生涯尚無已結算的賽事獎盃紀錄。贏得 Major / S 級賽事後會顯示於此。");
                return;
            }

            foreach (CompetitionEntry entry in cabinet)
            {
                if (!_content.Competitions.TryGetValue(entry.competitionId, out CompetitionDefinition competition))
                    continue;

                string when = entry.registeredHour > 0
                    ? CareerCalendar.FormatShort(_content.Balance, entry.registeredHour)
                    : "—";

                Image row = CreateUiObject<Image>("TrophyRow", _clubHubContentRoot);
                LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 40f;
                le.flexibleWidth = 1f;
                ApplyClubHubFrostedListRow(row);

                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 6f;
                layout.padding = new RectOffset(8, 8, 6, 6);
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;

                AddPlacementBadge(row.transform, entry.placement, preferredHeight: 24f, fontSize: 12);
                AddTierBadge(row.transform, competition.tier, preferredHeight: 24f, fontSize: 11);

                AddClubHubTrophyNameScroll(row.transform, competition.displayName);

                Text meta = CreateText("When", row.transform, 12, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
                meta.text = when;
                var metaLe = meta.gameObject.AddComponent<LayoutElement>();
                metaLe.preferredWidth = 88f;
                metaLe.flexibleWidth = 0f;
            }
        }

        private static string TrophyLabel(int placement)
        {
            if (TryGetPlacementBadgeStyle(placement, out _, out string label))
                return label;
            return $"Top {placement}";
        }

        private void AddClubHubTrophyNameScroll(Transform parent, string displayName)
        {
            Image scrollHost = CreateUiObject<Image>("NameScroll", parent);
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;
            var hostLe = scrollHost.gameObject.AddComponent<LayoutElement>();
            hostLe.flexibleWidth = 1f;
            hostLe.minWidth = 48f;
            hostLe.preferredHeight = 28f;

            ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.rectTransform.offsetMin = new Vector2(0f, 6f);
            viewport.rectTransform.offsetMax = Vector2.zero;
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            Text name = CreateText("Name", content, 13, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            name.text = displayName ?? string.Empty;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;
            var textLe = name.gameObject.AddComponent<LayoutElement>();
            textLe.preferredHeight = 22f;
            textLe.minWidth = 48f;

            scroll.content = content;

            Image sbTrack = CreateUiObject<Image>("HScrollTrack", scrollHost.transform);
            RectTransform sbRt = sbTrack.rectTransform;
            sbRt.anchorMin = new Vector2(0f, 0f);
            sbRt.anchorMax = new Vector2(1f, 0f);
            sbRt.pivot = new Vector2(0.5f, 0f);
            sbRt.sizeDelta = new Vector2(0f, 5f);
            sbRt.anchoredPosition = Vector2.zero;
            sbTrack.color = new Color(0.12f, 0.13f, 0.16f, 0.85f);
            sbTrack.raycastTarget = true;

            Image sbHandle = CreateUiObject<Image>("Handle", sbTrack.transform);
            Stretch(sbHandle.rectTransform);
            sbHandle.rectTransform.offsetMin = new Vector2(1f, 1f);
            sbHandle.rectTransform.offsetMax = new Vector2(-1f, -1f);
            sbHandle.color = new Color(0.45f, 0.48f, 0.54f, 0.95f);

            Scrollbar hBar = sbTrack.gameObject.AddComponent<Scrollbar>();
            hBar.handleRect = sbHandle.rectTransform;
            hBar.targetGraphic = sbHandle;
            hBar.direction = Scrollbar.Direction.LeftToRight;
            scroll.horizontalScrollbar = hBar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void DrawClubHubMissions()
        {
            CareerMissionTreeDef tree = _session?.GetPlayerMissionTree();
            if (tree == null || tree.nodes == null || tree.nodes.Length == 0)
            {
                AddClubHubContentTitle("任務樹");
                AddClubHubContentMuted("此俱樂部目前沒有可用的任務樹。");
                return;
            }

            AddClubHubContentTitle(string.IsNullOrWhiteSpace(tree.title) ? "任務樹" : tree.title);

            if (_missionTreeOverlayDismissed)
            {
                AddClubHubContentMuted("全螢幕任務樹已關閉。可點下方按鈕重新開啟，或再次點選任務分頁。");
            }
            else
            {
                AddClubHubContentMuted("全螢幕任務樹已開啟。游標停在節點上可查看背景與目標。");
                if (_missionTreePresentation == null || !_missionTreePresentation.IsOpen)
                    OpenMissionTreeOverlay();
            }

            Image openRow = CreateUiObject<Image>("MissionOpenRow", _clubHubContentRoot);
            LayoutElement openLe = openRow.gameObject.AddComponent<LayoutElement>();
            openLe.preferredHeight = 44f;
            openLe.flexibleWidth = 1f;
            ApplyClubHubFrostedListRow(openRow);
            CreateGlassCapsuleButton(
                "OpenMissionTree", "開啟任務樹", openRow.transform,
                new Vector2(0.04f, 0.14f), new Vector2(0.42f, 0.86f),
                ButtonEdge, ButtonFace, ReopenMissionTreeOverlay, _uiFont700);

            CareerMissionNodeDef[] ordered = tree.nodes
                .Where(n => n != null && !string.IsNullOrWhiteSpace(n.id))
                .OrderBy(n => n.uiRow)
                .ThenBy(n => n.uiColumn)
                .ThenBy(n => n.id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (int i = 0; i < ordered.Length; i++)
                DrawClubHubMissionNodeRow(ordered[i]);
        }

        private void DrawClubHubMissionNodeRow(CareerMissionNodeDef node)
        {
            if (node == null || _session == null)
                return;

            CareerMissionNodeStatus status = _session.GetMissionNodeStatus(node.id);
            bool completable = status == CareerMissionNodeStatus.Completable;

            Image row = CreateUiObject<Image>("MissionNode_" + node.id, _clubHubContentRoot);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = completable ? 56f : 40f;
            le.flexibleWidth = 1f;
            ApplyClubHubFrostedListRow(row);

            Text title = CreateText(
                "Title", row.transform, 14, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(
                title.rectTransform,
                new Vector2(0.04f, completable ? 0.42f : 0.12f),
                new Vector2(completable ? 0.58f : 0.62f, completable ? 0.92f : 0.88f));
            title.text = string.IsNullOrWhiteSpace(node.title) ? node.id : node.title;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text meta = CreateText(
                "Status", row.transform, 12, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(
                meta.rectTransform,
                new Vector2(completable ? 0.58f : 0.62f, completable ? 0.42f : 0.12f),
                new Vector2(0.96f, completable ? 0.92f : 0.88f));
            meta.alignment = TextAnchor.MiddleRight;
            meta.text = CareerMissionRules.StatusLabel(status);

            if (!completable)
                return;

            string capturedId = node.id;
            CreateGlassCapsuleButton(
                "Claim_" + node.id, "完成", row.transform,
                new Vector2(0.04f, 0.08f), new Vector2(0.32f, 0.38f),
                ButtonEdge, ButtonFace,
                () => OnClubHubMissionClaimClicked(capturedId),
                _uiFont700);
        }

        private void OnClubHubMissionClaimClicked(string nodeId)
        {
            if (_session == null)
                return;
            if (!_session.TryCompleteMissionNode(nodeId, out string failReason))
            {
                if (!string.IsNullOrWhiteSpace(failReason))
                    SetStatus(failReason);
                RefreshClubHubTabContent();
                _missionTreePresentation?.Refresh();
                return;
            }

            SetStatus("任務已完成。");
            RefreshAll();
            if (_clubHubOpen && _clubHubTab == ClubHubTab.Missions)
                RefreshClubHubTabContent();
            _missionTreePresentation?.Refresh();
        }

        private void DismissMissionTreeOverlay()
        {
            _missionTreeOverlayDismissed = true;
            _missionTreePresentation?.Close();
            if (_clubHubOpen && _clubHubTab == ClubHubTab.Missions)
                RefreshClubHubTabContent();
        }

        private void ReopenMissionTreeOverlay()
        {
            _missionTreeOverlayDismissed = false;
            OpenMissionTreeOverlay();
        }

        private void OpenMissionTreeOverlay()
        {
            if (_session == null)
                return;

            Transform canvas = _shellRoot != null ? _shellRoot.transform.root : transform;
            if (_canvasRect != null)
                canvas = _canvasRect;

            if (_missionTreePresentation == null)
            {
                _missionTreePresentation = new CareerMissionTreePresentation(
                    _session,
                    _uiFont700 ?? _uiFont,
                    () =>
                    {
                        RefreshAll();
                        if (_clubHubOpen)
                            RefreshClubHubPanel();
                        if (_missionTreePresentation != null && _missionTreePresentation.IsOpen)
                            _missionTreePresentation.Refresh();
                    },
                    msg => SetStatus(msg),
                    DismissMissionTreeOverlay);
            }

            _missionTreePresentation.Open(canvas);
            BringMissionTreeAboveClubHub();
        }

        private void BringMissionTreeAboveClubHub()
        {
            if (_missionTreeOverlayDismissed)
                return;
            _missionTreePresentation?.BringToFront();
        }

        private void CloseMissionTreeOverlay()
        {
            _missionTreePresentation?.Close();
        }

        private void DrawClubHubOrganization(ClubState club, CareerClubDefinition def)
        {
            AddClubHubContentTitle("組織階級");
            AddClubHubContentMuted(
                "本頁列出俱樂部內各利益相關方及其滿意度（0–100）。滿意度低於 30% 時產生減益，高於 70% 時獲得增益；將游標停留於列上可檢視說明與當前效果。部分組織須滿足條件後方視為已形成。");

            PlayerCareerState[] roster = _session.State.players
                .Where(p => string.Equals(p.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                _content.Balance, _session.State, club, def, roster);

            foreach (ClubEstateStatus estate in estates)
            {
                string value;
                if (!estate.Formed)
                    value = "未形成";
                else
                    value = $"{estate.Satisfaction}%";

                string body = estate.Def.Description ?? string.Empty;
                if (estate.Formed)
                {
                    body += $"\n\n滿意度：{estate.Satisfaction}%";
                    if (ClubEstateSatisfactionRules.TryGetActiveModifier(
                            estate.Def.Id, true, estate.Stance, out ClubEstateActiveModifier mod))
                    {
                        string tag = mod.Buff ? "增益" : "減益";
                        body += $"\n<color=#{ColorUtility.ToHtmlStringRGB(mod.Buff ? Positive : Danger)}>[{tag}] {mod.Title}</color>\n{mod.Description}";
                    }
                    else
                        body += "\n（滿意度介於 30%–70%，無額外增益或減益）";
                }

                if (!estate.Formed && !string.IsNullOrWhiteSpace(estate.Def.UnlockHint))
                    body += "\n\n解鎖：" + estate.Def.UnlockHint;
                AddClubHubEstateRow(estate.Def.Title, value, estate.Formed, estate.Stance, body);
            }
        }

        private void DrawClubHubResolutions(ClubState club, CareerClubDefinition def)
        {
            AddClubHubContentTitle("決議");
            AddClubHubContentMuted(
                "通過決議將消耗現金並調整各組織滿意度；同一決議通過後進入冷卻，冷卻結束前不可再次執行。");

            PlayerCareerState[] roster = _session.State.players
                .Where(p => string.Equals(p.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            IReadOnlyList<ClubEstateResolutionStatus> resolutions = ClubEstateResolutionRules.Evaluate(
                _session.State, club, _content.Balance, def, roster);

            foreach (ClubEstateResolutionStatus status in resolutions)
            {
                ClubEstateResolutionDef res = status.Def;
                string cost = res.cashCost > 0 ? $"${res.cashCost:N0}" : "免費";
                AddClubHubResolutionRow(res, status, cost);
            }
        }

        private void AddClubHubResolutionRow(
            ClubEstateResolutionDef def, ClubEstateResolutionStatus status, string costLabel)
        {
            Image row = CreateUiObject<Image>("ResolutionRow", _clubHubContentRoot);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;
            ApplyClubHubFrostedListRow(row);

            Text title = CreateText("Title", row.transform, 14, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.62f, 0.92f));
            title.text = def.title;
            title.raycastTarget = false;

            Text cost = CreateText("Cost", row.transform, 12, TextAnchor.UpperLeft, ProfileValue, _uiFont700);
            SetRect(cost.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.28f, 0.50f));
            cost.text = costLabel;
            cost.raycastTarget = false;

            Text sub = CreateText("Sub", row.transform, 12, TextAnchor.UpperLeft, TextPrimary, _uiFont);
            SetRect(sub.rectTransform, new Vector2(0.29f, 0.12f), new Vector2(0.62f, 0.50f));
            sub.text = def.description ?? string.Empty;
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            sub.raycastTarget = false;

            string capturedId = def.id;
            CreateGlassCapsuleButton(
                "Enact", status.CanEnact ? "通過" : "—",
                row.transform,
                new Vector2(0.66f, 0.18f), new Vector2(0.96f, 0.82f),
                ButtonEdge, status.CanEnact ? ButtonFace : PanelRow,
                () => OnClubHubResolutionClicked(capturedId),
                _uiFont700);

            string hoverKey = "resolution_" + def.id;
            string tooltip = BuildResolutionTooltip(def, status);
            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => OnClubHubModifierPointerEnter(hoverKey, status.CanEnact, def.title, tooltip));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnClubHubModifierPointerExit());
        }

        private static string BuildResolutionTooltip(
            ClubEstateResolutionDef def, ClubEstateResolutionStatus status)
        {
            var lines = new List<string> { def.description ?? string.Empty, string.Empty, "滿意度變化：" };
            if (def.effects != null)
            {
                foreach (ClubEstateResolutionEffect effect in def.effects)
                {
                    if (effect == null || effect.satisfactionDelta == 0)
                        continue;
                    string sign = effect.satisfactionDelta > 0 ? "+" : string.Empty;
                    string color = effect.satisfactionDelta > 0 ? "55CC88" : "E85555";
                    string estateName = ClubEstateRules.DisplayTitle(effect.estateId);
                    lines.Add(
                        $"<color=#{color}>{estateName} {sign}{effect.satisfactionDelta}</color>");
                }
            }

            if (!status.CanEnact && !string.IsNullOrWhiteSpace(status.BlockReason))
                lines.Add("\n" + status.BlockReason);
            return string.Join("\n", lines);
        }

        private void OnClubHubResolutionClicked(string resolutionId)
        {
            if (_session == null)
                return;
            if (!_session.TryEnactClubResolution(resolutionId, out string failure))
            {
                if (!string.IsNullOrWhiteSpace(failure))
                    ShowPlainTooltip("決議", failure, new Vector2(280f, 90f), Input.mousePosition);
                return;
            }

            RefreshClubHubPanel();
        }

        private void AddClubHubEstateRow(
            string title, string status, bool formed, ClubEstateSatisfactionStance stance, string tooltipBody)
        {
            Image row = CreateUiObject<Image>("EstateRow", _clubHubContentRoot);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.flexibleWidth = 1f;
            ApplyClubHubFrostedListRow(row);
            row.raycastTarget = true;

            Text l = CreateText("L", row.transform, 14, TextAnchor.MiddleLeft,
                TextPrimary, _uiFont700);
            SetRect(l.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.48f, 0.9f));
            l.text = title;
            l.raycastTarget = false;

            Color valueColor = TextPrimary;
            if (formed)
            {
                valueColor = stance switch
                {
                    ClubEstateSatisfactionStance.Loyal => Positive,
                    ClubEstateSatisfactionStance.Angry => Danger,
                    _ => TextPrimary
                };
            }

            Text v = CreateText("V", row.transform, 13, TextAnchor.MiddleRight,
                valueColor, _uiFont700);
            SetRect(v.rectTransform, new Vector2(0.50f, 0.1f), new Vector2(0.96f, 0.9f));
            v.text = status;
            v.raycastTarget = false;

            string hoverKey = "estate_" + title;
            string capturedTitle = title;
            string capturedBody = tooltipBody ?? string.Empty;
            bool capturedFormed = formed;
            bool capturedBuff = stance == ClubEstateSatisfactionStance.Loyal;
            bool capturedAngry = stance == ClubEstateSatisfactionStance.Angry;
            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
                _ => OnClubHubEstatePointerEnter(
                    hoverKey, capturedFormed, capturedBuff, capturedAngry, capturedTitle, capturedBody));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnClubHubModifierPointerExit());
        }

        private void OnClubHubEstatePointerEnter(
            string hoverKey, bool formed, bool loyal, bool angry, string title, string body)
        {
            CancelClubHubModifierHover();
            HideStatTooltip();
            _clubHubModifierHoverKey = hoverKey;
            _clubHubModifierHoverRoutine = StartCoroutine(
                ShowClubHubEstateTooltipAfterDelay(hoverKey, formed, loyal, angry, title, body));
        }

        private IEnumerator ShowClubHubEstateTooltipAfterDelay(
            string hoverKey, bool formed, bool loyal, bool angry, string title, string body)
        {
            yield return new WaitForSecondsRealtime(ClubHubModifierHoverDelaySeconds);
            if (!string.Equals(_clubHubModifierHoverKey, hoverKey, StringComparison.Ordinal))
                yield break;

            Color accent = Warning;
            string prefix = "未形成";
            if (formed)
            {
                if (loyal)
                {
                    accent = Positive;
                    prefix = "增益";
                }
                else if (angry)
                {
                    accent = Danger;
                    prefix = "減益";
                }
                else
                {
                    accent = Accent;
                    prefix = "已形成";
                }
            }

            string richTitle =
                $"<color=#{ColorUtility.ToHtmlStringRGB(accent)}>{prefix}</color> · {title}";
            ShowPlainTooltip(
                richTitle,
                body,
                new Vector2(360f, Mathf.Clamp(130f + body.Length * 0.6f, 160f, 320f)),
                Input.mousePosition);
            _clubHubModifierHoverRoutine = null;
        }

        private void DrawClubHubPlaceholderSection(string title, string body)
        {
            AddClubHubContentTitle(title);
            AddClubHubContentMuted(body);
        }

        private void AddClubHubContentTitle(string text)
        {
            Text t = CreateText("Section", _clubHubContentRoot, 16, TextAnchor.MiddleLeft, Accent, _uiFont700);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;
            t.text = text;
        }

        private void AddClubHubContentMuted(string text)
        {
            Text t = CreateText("Muted", _clubHubContentRoot, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = Mathf.Max(36f, 14f * (1 + text.Length / 42));
            le.flexibleWidth = 1f;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void AddClubHubContentRow(string label, string value)
        {
            Image row = CreateUiObject<Image>("Row", _clubHubContentRoot);
            LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;
            ApplyClubHubFrostedListRow(row);

            Text l = CreateText("L", row.transform, 13, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(l.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.48f, 0.9f));
            l.text = label;

            Text v = CreateText("V", row.transform, 14, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(v.rectTransform, new Vector2(0.50f, 0.1f), new Vector2(0.96f, 0.9f));
            v.text = value;
        }

        private static Image CreateClubHubInsetWell(
            string name, Transform parent, Vector2 min, Vector2 max, Color fill, Sprite rounded)
        {
            Image frame = CreateUiObject<Image>(name + "Frame", parent);
            SetRect(frame.rectTransform, min, max);
            frame.sprite = rounded;
            frame.type = Image.Type.Sliced;
            frame.color = ClubHubChromeBorder;
            frame.raycastTarget = false;

            Image well = CreateUiObject<Image>(name, frame.transform);
            Stretch(well.rectTransform);
            well.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            well.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            well.sprite = rounded;
            well.type = Image.Type.Sliced;
            // Nested fill only — parent Face already has one frost layer (no second slice).
            well.color = fill.a > 0.001f ? fill : UiLightChromeUtility.SoftNavyFace;
            return well;
        }

        private static void ApplyClubHubFrostedListRow(Image row)
        {
            if (row == null)
                return;
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            row.sprite = rounded;
            row.type = Image.Type.Sliced;
            row.color = ClubHubFrostedRow;
        }
    }
}
