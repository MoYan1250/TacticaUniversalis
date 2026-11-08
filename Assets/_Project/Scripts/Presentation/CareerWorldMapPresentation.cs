using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
#endif

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        [Serializable]
        private sealed class MapHubFile
        {
            public int schemaVersion = 1;
            public MapHubDefinition[] hubs;
        }

        [Serializable]
        private sealed class MapHubDefinition
        {
            public string id;
            public string displayName;
            public string region;
            public string subtitle;
            public float x;
            public float y;
            public string color;
        }

        private sealed class MapHubView
        {
            public MapHubDefinition Definition;
            public RectTransform Root;
            public Image OuterGlow;
            public Image Glow;
            public Image Core;
            public Text Label;
            public Text Intel;
            public Vector2 BaseScale;
            public Color GlowColor;
        }

        private static readonly Color MapHubCoreColor = Color.white;
        private const float MapHubOuterGlowIdleAlpha = 0.34f;
        private const float MapHubOuterGlowHoverAlpha = 0.48f;
        private const float MapHubOuterGlowSelectedAlpha = 0.56f;
        private const float MapHubInnerGlowIdleAlpha = 0.72f;
        private const float MapHubInnerGlowHoverAlpha = 0.88f;
        private const float MapHubInnerGlowSelectedAlpha = 0.96f;
        private const float MapHubScaleIdle = 1f;
        private const float MapHubScaleHover = 1.22f;
        private const float MapHubScaleSelected = 1.18f;
        private const float MapHubScaleSelectedHover = 1.28f;

        private readonly List<MapHubView> _mapHubViews = new List<MapHubView>();
        private MapHubDefinition[] _mapHubs = Array.Empty<MapHubDefinition>();
        private RectTransform _mapLayer;
        private RectTransform _mapScrollContent;
        private ScrollRect _mapScroll;
        private RectTransform _mapTooltip;
        private Text _mapTooltipText;
        private GameObject _mapSidePanel;
        private GameObject _mapRightDock;
        private GameObject _mapDockOpenBtn;
        private Text _mapSideTitle;
        private Text _mapSideBody;
        private Transform _mapSideActions;
        private string _selectedHubId;
        private string _hoveredHubId;
        private Image _playerBaseMarker;
        private bool _mapRightDockOpen = true;

        private void BuildWorldMapHost(Transform contentPanel)
        {
            LoadMapHubs();

            Image host = CreateUiObject<Image>("MapHost", contentPanel);
            Stretch(host.rectTransform);
            host.color = CareerEditorialChrome.Stage;
            host.raycastTarget = true;
            _mapHost = host.gameObject;

            BuildMapScrollLayer(host.transform);

            GameObject particleGo = new GameObject("MapParticleField", typeof(RectTransform));
            particleGo.transform.SetParent(host.transform, false);
            Stretch(particleGo.GetComponent<RectTransform>());
            _mapParticles = particleGo.AddComponent<UiParticleField>();
            _mapParticles.Configure(UiRoundedRectUtility.GetRadialGlowSprite(), CareerEditorialChrome.Cyan);

            Sprite nodeGlowSprite = Resources.Load<Sprite>("UI/Maps/node_glow");
            Sprite radialGlowSprite = UiRoundedRectUtility.GetRadialGlowSprite();
            foreach (MapHubDefinition hub in _mapHubs)
            {
                Color glowColor = ParseHtmlColor(hub.color, AccentBlue);
                MapHubView view = CreateMapHubNode(_mapLayer, hub, nodeGlowSprite, radialGlowSprite, glowColor);
                _mapHubViews.Add(view);
            }

            _playerBaseMarker = CreateUiObject<Image>("PlayerBase", _mapLayer);
            _playerBaseMarker.sprite = radialGlowSprite;
            _playerBaseMarker.color = new Color(Accent.r, Accent.g, Accent.b, 0.72f);
            _playerBaseMarker.raycastTarget = false;
            _playerBaseMarker.rectTransform.sizeDelta = new Vector2(42f, 42f);
            _playerBaseMarker.gameObject.SetActive(false);

            BuildMapTooltip(host.transform);
            BuildMapRightDock(host.transform);
            BuildMapEventsNewsButton(host.transform);
            RebuildMapContentSize();
            _mapHost.SetActive(true);
        }

        private void BuildMapScrollLayer(Transform mapHost)
        {
            Image scrollHost = CreateUiObject<Image>("MapScroll", mapHost);
            Stretch(scrollHost.rectTransform);
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            _mapScroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _mapScroll.horizontal = true;
            _mapScroll.vertical = false;
            _mapScroll.movementType = ScrollRect.MovementType.Clamped;
            _mapScroll.scrollSensitivity = 48f;
            _mapScroll.inertia = true;
            _mapScroll.decelerationRate = 0.135f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _mapScroll.viewport = viewport.rectTransform;

            _mapScrollContent = new GameObject("MapContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapScrollContent.SetParent(viewport.transform, false);
            _mapScrollContent.anchorMin = new Vector2(0f, 0f);
            _mapScrollContent.anchorMax = new Vector2(0f, 1f);
            _mapScrollContent.pivot = new Vector2(0f, 0.5f);
            _mapScrollContent.anchoredPosition = Vector2.zero;
            _mapScrollContent.sizeDelta = new Vector2(1600f, 0f);
            _mapScroll.content = _mapScrollContent;

            Image mapImage = CreateUiObject<Image>("WorldMap", _mapScrollContent);
            Stretch(mapImage.rectTransform);
            mapImage.sprite = Resources.Load<Sprite>("UI/Maps/world_strategy");
            mapImage.preserveAspect = false;
            mapImage.color = Color.white;
            mapImage.raycastTarget = true;
            _mapLayer = _mapScrollContent;

            AspectRatioFitter fitter = _mapScrollContent.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            float aspect = 16f / 9f;
            if (mapImage.sprite != null)
            {
                Rect r = mapImage.sprite.rect;
                if (r.height > 0.01f)
                    aspect = r.width / r.height;
            }

            fitter.aspectRatio = aspect;
        }

        private void RebuildMapContentSize()
        {
            if (_mapScrollContent == null || _mapScroll == null || _mapScroll.viewport == null)
                return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_mapScroll.viewport);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_mapScrollContent);
            _mapScroll.horizontalNormalizedPosition = 0f;
        }

        private void BuildMapRightDock(Transform mapHost)
        {
            Image dock = CreateUiObject<Image>("MapRightDock", mapHost);
            SetRect(dock.rectTransform, new Vector2(0.72f, 0.02f), new Vector2(0.99f, 0.98f));
            dock.color = Color.clear;
            dock.raycastTarget = false;
            _mapRightDock = dock.gameObject;

            BuildMapSidePanel(dock.transform);
            BuildMapSchedulePanel(dock.transform);

            CreateGlassCapsuleButton(
                "DockCollapse", "收合", dock.transform,
                new Vector2(0.62f, 0.955f), new Vector2(0.98f, 0.995f),
                ButtonEdge, ButtonFace, () => SetMapRightDockOpen(false), _uiFont700, fontSize: 14);

            _mapDockOpenBtn = CreateGlassCapsuleButton(
                "DockOpen", "側欄", mapHost,
                new Vector2(0.955f, 0.42f), new Vector2(0.995f, 0.58f),
                ButtonEdge, ButtonFace, () => SetMapRightDockOpen(true), _uiFont700, fontSize: 14).gameObject;

            SetMapRightDockOpen(_mapRightDockOpen);
            dock.transform.SetAsLastSibling();
            if (_mapDockOpenBtn != null)
                _mapDockOpenBtn.transform.SetAsLastSibling();
        }

        private void SetMapRightDockOpen(bool open)
        {
            _mapRightDockOpen = open;
            if (_mapRightDock != null)
                _mapRightDock.SetActive(open);
            if (_mapDockOpenBtn != null)
                _mapDockOpenBtn.SetActive(!open);
        }

        private void BuildMapEventsNewsButton(Transform mapHost)
        {
            // Above the map horizontal scrollbar strip.
            Button btn = CreateGlassCapsuleButton(
                "MapEventsNewsBtn", "事件與新聞", mapHost,
                new Vector2(0.012f, 0.07f), new Vector2(0.155f, 0.145f),
                ButtonEdge, ButtonFace, ShowMapEventLog, _uiFont700);
            Text label = btn.transform.Find("Face/Text")?.GetComponent<Text>();
            ApplyFilledButtonLabel(label);
            btn.transform.SetAsLastSibling();
        }

        private void BuildMapSchedulePanel(Transform parent)
        {
            Image panel = UiLightChromeUtility.CreateCard(
                "MapSchedulePanel", parent,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.48f), Panel);

            Text title = CreateText("ScheduleTitle", panel.transform, 19, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.text = "賽事行程 · 報名";

            Image scrollHost = CreateUiObject<Image>("ScheduleScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.88f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            _mapScheduleScroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _mapScheduleScroll.horizontal = false;
            _mapScheduleScroll.vertical = true;
            _mapScheduleScroll.movementType = ScrollRect.MovementType.Clamped;
            _mapScheduleScroll.scrollSensitivity = 36f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _mapScheduleScroll.viewport = viewport.rectTransform;

            _mapScheduleContent = new GameObject("ScheduleContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _mapScheduleContent.SetParent(viewport.transform, false);
            _mapScheduleContent.anchorMin = new Vector2(0f, 1f);
            _mapScheduleContent.anchorMax = new Vector2(1f, 1f);
            _mapScheduleContent.pivot = new Vector2(0.5f, 1f);
            _mapScheduleContent.anchoredPosition = Vector2.zero;
            _mapScheduleContent.sizeDelta = new Vector2(0f, 0f);

            var layout = _mapScheduleContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4f;
            layout.padding = new RectOffset(4, 4, 2, 2);

            var fitter = _mapScheduleContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _mapScheduleScroll.content = _mapScheduleContent;
        }

        private MapHubView CreateMapHubNode(
            RectTransform layer, MapHubDefinition hub, Sprite nodeGlowSprite, Sprite radialGlowSprite, Color glowColor)
        {
            var rootGo = new GameObject("Hub_" + hub.id, typeof(RectTransform));
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.SetParent(layer, false);
            root.anchorMin = new Vector2(hub.x, hub.y);
            root.anchorMax = new Vector2(hub.x, hub.y);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(64f, 64f);
            root.anchoredPosition = Vector2.zero;

            Image outerGlow = CreateUiObject<Image>("OuterGlow", root);
            Stretch(outerGlow.rectTransform);
            outerGlow.rectTransform.offsetMin = new Vector2(-16f, -16f);
            outerGlow.rectTransform.offsetMax = new Vector2(16f, 16f);
            outerGlow.sprite = radialGlowSprite;
            outerGlow.type = Image.Type.Simple;
            outerGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, MapHubOuterGlowIdleAlpha);
            outerGlow.raycastTarget = false;

            Image glow = CreateUiObject<Image>("Glow", root);
            Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-4f, -4f);
            glow.rectTransform.offsetMax = new Vector2(4f, 4f);
            glow.sprite = nodeGlowSprite != null ? nodeGlowSprite : radialGlowSprite;
            glow.type = Image.Type.Simple;
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, MapHubInnerGlowIdleAlpha);
            glow.raycastTarget = false;

            Image core = CreateUiObject<Image>("Core", root);
            SetRect(core.rectTransform, new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f));
            core.sprite = nodeGlowSprite != null ? nodeGlowSprite : radialGlowSprite;
            core.color = MapHubCoreColor;
            core.raycastTarget = true;

            // Region name lives in the side panel / tooltip — keep the map itself label-free.
            Button button = rootGo.AddComponent<Button>();
            button.targetGraphic = core;
            ApplyButtonColors(button);
            string hubId = hub.id;
            button.onClick.AddListener(() => SelectMapHub(hubId));

            var trigger = rootGo.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => ShowMapTooltip(hubId, true));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, () => ShowMapTooltip(hubId, false));

            var view = new MapHubView
            {
                Definition = hub,
                Root = root,
                OuterGlow = outerGlow,
                Glow = glow,
                Core = core,
                Label = null,
                Intel = null,
                BaseScale = Vector2.one,
                GlowColor = glowColor
            };
            ApplyMapHubNodeVisual(view, false, false);
            return view;
        }

        private static void ApplyMapHubNodeVisual(MapHubView view, bool hovered, bool selected)
        {
            if (view?.Root == null)
                return;

            float outerAlpha = MapHubOuterGlowIdleAlpha;
            float innerAlpha = MapHubInnerGlowIdleAlpha;
            float scale = MapHubScaleIdle;
            if (selected && hovered)
            {
                outerAlpha = MapHubOuterGlowSelectedAlpha + 0.06f;
                innerAlpha = MapHubInnerGlowSelectedAlpha;
                scale = MapHubScaleSelectedHover;
            }
            else if (selected)
            {
                outerAlpha = MapHubOuterGlowSelectedAlpha;
                innerAlpha = MapHubInnerGlowSelectedAlpha;
                scale = MapHubScaleSelected;
            }
            else if (hovered)
            {
                outerAlpha = MapHubOuterGlowHoverAlpha;
                innerAlpha = MapHubInnerGlowHoverAlpha;
                scale = MapHubScaleHover;
            }

            Color c = view.GlowColor;
            if (view.OuterGlow != null)
                view.OuterGlow.color = new Color(c.r, c.g, c.b, outerAlpha);
            if (view.Glow != null)
                view.Glow.color = new Color(c.r, c.g, c.b, innerAlpha);
            view.Root.localScale = new Vector3(scale, scale, 1f);
        }

        private void BuildMapTooltip(Transform parent)
        {
            Image tip = CreateUiObject<Image>("MapTooltip", parent);
            SetRect(tip.rectTransform, new Vector2(0.02f, 0.72f), new Vector2(0.36f, 0.96f));
            UiLightChromeUtility.ApplySoftNavyPanel(tip);
            tip.raycastTarget = false;
            _mapTooltip = tip.rectTransform;

            _mapTooltipText = CreateText("Text", tip.transform, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            Stretch(_mapTooltipText.rectTransform);
            _mapTooltipText.rectTransform.offsetMin = new Vector2(10f, 8f);
            _mapTooltipText.rectTransform.offsetMax = new Vector2(-10f, -8f);
            _mapTooltipText.alignment = TextAnchor.UpperLeft;
            _mapTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _mapTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_mapTooltipText);
            tip.gameObject.SetActive(false);
        }

        private void BuildMapSidePanel(Transform parent)
        {
            Image panel = UiLightChromeUtility.CreateCard(
                "MapSidePanel", parent,
                new Vector2(0.02f, 0.50f), new Vector2(0.98f, 0.94f), Panel);
            _mapSidePanel = panel.gameObject;

            _mapSideTitle = CreateText("SideTitle", panel.transform, 22, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_mapSideTitle.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            _mapSideTitle.text = "賽區情報";

            _mapSideBody = CreateText("SideBody", panel.transform, 16, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_mapSideBody.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.86f));
            _mapSideBody.alignment = TextAnchor.UpperLeft;
            _mapSideBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _mapSideBody.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_mapSideBody);
            NarrativeUiText.Set(_mapSideBody, "點擊地圖節點查看本月可報名與進行中的賽事。\n空白鍵暫停／繼續 · 1x–16x 調速（1–5）。");

            Image actions = CreateUiObject<Image>("SideActions", panel.transform);
            SetRect(actions.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.26f));
            actions.color = Color.clear;
            actions.raycastTarget = false;
            _mapSideActions = actions.transform;
        }

        private void RefreshWorldMap()
        {
            if (_mapHost == null || _session == null) return;
            RebuildMapContentSize();
            CareerState state = _session.State;

            foreach (MapHubView view in _mapHubViews)
            {
                string intel = BuildHubIntelLine(view.Definition.region, state);
                if (view.Intel != null) view.Intel.text = intel;
                bool selected = string.Equals(_selectedHubId, view.Definition.id, StringComparison.OrdinalIgnoreCase);
                bool hovered = string.Equals(_hoveredHubId, view.Definition.id, StringComparison.OrdinalIgnoreCase);
                ApplyMapHubNodeVisual(view, hovered, selected);
            }

            PlacePlayerBaseMarker(state.playerClubId);
            if (!string.IsNullOrEmpty(_selectedHubId))
                PopulateMapSidePanel(_selectedHubId);
            else
            {
                _mapSideTitle.text = "賽區情報";
                NarrativeUiText.Set(_mapSideBody, "點擊地圖節點查看本月可報名與進行中的賽事。\nHover 節點可預覽地圖可見賽事。");
                ClearMapSideActions();
            }
        }

        private void PlacePlayerBaseMarker(string clubId)
        {
            // Hub node already marks the home region; a second glow beside Berlin
            // (and other hubs) looked like a duplicate light, so keep it off.
            if (_playerBaseMarker != null)
                _playerBaseMarker.gameObject.SetActive(false);
        }

        private void SelectMapHub(string hubId)
        {
            _selectedHubId = hubId;
            PopulateMapSidePanel(hubId);
            RefreshWorldMap();
        }

        private void ShowMapTooltip(string hubId, bool show)
        {
            MapHubView view = _mapHubViews.FirstOrDefault(x =>
                string.Equals(x.Definition.id, hubId, StringComparison.OrdinalIgnoreCase));
            if (view == null) return;

            if (!show)
            {
                if (string.Equals(_hoveredHubId, hubId, StringComparison.OrdinalIgnoreCase))
                    _hoveredHubId = null;
                if (_mapTooltip != null) _mapTooltip.gameObject.SetActive(false);
                bool selected = string.Equals(_selectedHubId, hubId, StringComparison.OrdinalIgnoreCase);
                ApplyMapHubNodeVisual(view, false, selected);
                return;
            }

            _hoveredHubId = hubId;
            bool isSelected = string.Equals(_selectedHubId, hubId, StringComparison.OrdinalIgnoreCase);
            ApplyMapHubNodeVisual(view, true, isSelected);
            if (_mapTooltip == null || _session == null) return;

            CompetitionDefinition active = FindActiveCompetition(view.Definition.region, _session.State);
            CompetitionDefinition upcoming = FindRegisterableCompetition(view.Definition.region, _session.State);
            RegionMapStats stats = GetRegionMapStats(view.Definition.region, _session.State);
            string body =
                $"{view.Definition.displayName} · {view.Definition.subtitle}\n" +
                $"隊伍 {stats.ClubCount} 支 · 賽事 {stats.VisibleCompetitionCount} 場";
            if (stats.LiveCompetitionCount > 0)
                body += $"\n進行中 {stats.LiveCompetitionCount} 場";
            if (stats.RegisterableCompetitionCount > 0)
                body += $"\n可報名 {stats.RegisterableCompetitionCount} 場";
            if (active != null)
                body += $"\n{active.displayName}";
            else if (upcoming != null)
                body += $"\n最近：{upcoming.displayName}";

            NarrativeUiText.Set(_mapTooltipText, body);
            _mapTooltip.gameObject.SetActive(true);
        }

        private void PopulateMapSidePanel(string hubId)
        {
            MapHubDefinition hub = _mapHubs.FirstOrDefault(x =>
                string.Equals(x.id, hubId, StringComparison.OrdinalIgnoreCase));
            if (hub == null || _session == null) return;

            CareerState state = _session.State;
            _mapSideTitle.text = hub.displayName;

            RegionMapStats stats = GetRegionMapStats(hub.region, state);
            var lines = new List<string>
            {
                hub.subtitle ?? string.Empty,
                string.Empty,
                $"隊伍：{stats.ClubCount} 支",
                $"賽事：{stats.VisibleCompetitionCount} 場"
            };
            if (stats.LiveCompetitionCount > 0)
                lines.Add($"進行中：{stats.LiveCompetitionCount} 場");
            if (stats.RegisterableCompetitionCount > 0)
                lines.Add($"本月可報名：{stats.RegisterableCompetitionCount} 場");

            if (_content.Clubs.TryGetValue(state.playerClubId, out CareerClubDefinition playerClub) &&
                string.Equals(
                    RmrRules.ResolveClubRmrRegion(playerClub),
                    RmrRules.NormalizeRmrRegion(hub.region),
                    StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(string.Empty);
                lines.Add($"★ 你的俱樂部：{playerClub.displayName}");
            }

            lines.Add(string.Empty);
            lines.Add("詳細賽程與籤表請見右側「賽事行程」。");

            NarrativeUiText.Set(_mapSideBody, string.Join("\n", lines));
            CompetitionDefinition active = FindActiveCompetition(hub.region, state);
            CompetitionDefinition next = FindRegisterableCompetition(hub.region, state);
            RebuildMapSideActions(hub, active, next);
        }

        private struct RegionMapStats
        {
            public int ClubCount;
            public int VisibleCompetitionCount;
            public int LiveCompetitionCount;
            public int RegisterableCompetitionCount;
        }

        private RegionMapStats GetRegionMapStats(string region, CareerState state)
        {
            CompetitionDefinition[] visible = CompetitionsInRegion(region)
                .Where(x => CareerCalendar.VisibleOnMap(x, _content.Balance, state.clockHour))
                .ToArray();
            return new RegionMapStats
            {
                ClubCount = ClubsInRegion(region).Count(),
                VisibleCompetitionCount = visible.Length,
                LiveCompetitionCount = visible.Count(x => CareerCalendar.IsLive(x, state.clockHour)),
                RegisterableCompetitionCount = visible.Count(x =>
                    CareerCalendar.IsRegisterableThisMonth(x, _content.Balance, state.clockHour))
            };
        }

        private IEnumerable<CompetitionDefinition> CompetitionsInRegion(string region) =>
            _content.Competitions.Values.Where(x =>
                string.Equals(x.region, region, StringComparison.OrdinalIgnoreCase));

        private void RebuildMapSideActions(
            MapHubDefinition hub, CompetitionDefinition active, CompetitionDefinition next)
        {
            ClearMapSideActions();
            if (_session == null) return;

            CompetitionDefinition target = null;
            CareerState state = _session.State;
            foreach (CompetitionDefinition competition in MapVisibleCompetitions(hub.region, state)
                         .OrderBy(x => x.startHour))
            {
                bool registered = state.competitionEntries.Any(x =>
                    x.clubId == state.playerClubId &&
                    x.competitionId == competition.id &&
                    x.registered && !x.withdrawn && !x.completed);
                bool rmrOk = !StockholmMajorRules.UsesRmrQualification(competition) ||
                             _session.IsRmrQualifiedForCompetition(competition.id);
                if (!registered &&
                    rmrOk &&
                    CareerCalendar.IsRegisterableThisMonth(competition, _content.Balance, state.clockHour))
                {
                    target = competition;
                    break;
                }
            }

            if (target == null) return;

            string competitionId = target.id;
            CreateGlassCapsuleButton(
                "Register",
                $"報名 {target.displayName}",
                _mapSideActions,
                new Vector2(0.08f, 0.22f),
                new Vector2(0.92f, 0.78f),
                AccentDim,
                Accent,
                () =>
                {
                    AttemptRegisterCompetition(competitionId);
                },
                _uiFont700,
                15);
        }

        private void ClearMapSideActions()
        {
            if (_mapSideActions == null) return;
            for (int i = _mapSideActions.childCount - 1; i >= 0; i--)
                DestroyImmediate(_mapSideActions.GetChild(i).gameObject);
        }

        private string BuildHubIntelLine(string region, CareerState state)
        {
            RegionMapStats stats = GetRegionMapStats(region, state);
            if (stats.LiveCompetitionCount > 0)
                return $"{stats.ClubCount} 隊 · {stats.VisibleCompetitionCount} 賽 · {stats.LiveCompetitionCount} 進行中";
            if (stats.RegisterableCompetitionCount > 0)
                return $"{stats.ClubCount} 隊 · {stats.RegisterableCompetitionCount} 可報名";
            return $"{stats.ClubCount} 隊 · {stats.VisibleCompetitionCount} 賽";
        }

        private string ResolveClubActivity(string clubId, string region, CareerState state)
        {
            CompetitionDefinition active = FindActiveCompetition(region, state);
            if (active != null)
            {
                CompetitionEntry entry = state.competitionEntries.FirstOrDefault(x =>
                    x.clubId == clubId &&
                    string.Equals(x.competitionId, active.id, StringComparison.OrdinalIgnoreCase));
                if (entry != null && entry.completed)
                    return $"已完賽 · 第 {entry.placement} 名";
                if (entry != null)
                    return $"比賽中：{active.displayName}";
                return $"賽事進行中：{active.displayName}";
            }

            CompetitionEntry pending = state.competitionEntries.FirstOrDefault(x =>
                x.clubId == clubId && !x.completed);
            if (pending != null &&
                _content.Competitions.TryGetValue(pending.competitionId, out CompetitionDefinition pendingDef) &&
                CompetitionMatchesHubRegion(pendingDef, region))
            {
                int joined = CompetitionRegistrationRules.RegisteredClubIds(state, pendingDef.id).Count;
                int cap = CompetitionRegistrationRules.BracketSlotCount(pendingDef, Math.Max(joined, 1));
                if (CareerCalendar.IsRegisterableThisMonth(pendingDef, _content.Balance, state.clockHour))
                    return $"報名中：{pendingDef.displayName}（{joined}/{cap} 隊）";
                return $"已報名：{pendingDef.displayName}";
            }

            return "集訓中";
        }

        private bool IsClubCompetingInRegion(string clubId, string region, CareerState state)
        {
            foreach (CompetitionEntry entry in state.competitionEntries.Where(x =>
                         x.clubId == clubId && !x.completed))
            {
                if (!_content.Competitions.TryGetValue(entry.competitionId, out CompetitionDefinition def))
                    continue;
                if (!CareerCalendar.IsLive(def, state.clockHour))
                    continue;
                if (CompetitionMatchesHubRegion(def, region) || string.IsNullOrEmpty(def.region))
                    return true;
            }

            return false;
        }

        private IEnumerable<CareerClubDefinition> ClubsInRegion(string region)
        {
            string hub = RmrRules.NormalizeRmrRegion(region);
            return _content.Clubs.Values.Where(x =>
                string.Equals(RmrRules.ResolveClubRmrRegion(x), hub, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<CompetitionDefinition> RelevantCompetitions(string region) =>
            _content.Competitions.Values.Where(x =>
                string.IsNullOrEmpty(x.region) || CompetitionMatchesHubRegion(x, region));

        private IEnumerable<CompetitionDefinition> MapVisibleCompetitions(string region, CareerState state) =>
            RelevantCompetitions(region).Where(x =>
                CareerCalendar.VisibleOnMap(x, _content.Balance, state.clockHour));

        private CompetitionDefinition FindActiveCompetition(string region, CareerState state) =>
            MapVisibleCompetitions(region, state)
                .Where(x => CareerCalendar.IsLive(x, state.clockHour))
                .OrderByDescending(x => x.prizePool)
                .FirstOrDefault();

        private CompetitionDefinition FindRegisterableCompetition(string region, CareerState state) =>
            MapVisibleCompetitions(region, state)
                .Where(x => CareerCalendar.IsRegisterableThisMonth(x, _content.Balance, state.clockHour))
                .OrderBy(x => x.startHour)
                .FirstOrDefault();

        private static bool CompetitionMatchesHubRegion(CompetitionDefinition competition, string hubRegion)
        {
            if (competition == null)
                return false;
            string hub = RmrRules.NormalizeRmrRegion(hubRegion);
            if (string.IsNullOrEmpty(hub))
                return false;
            if (RmrRules.TryResolveCompetitionRmr(competition, out _, out string rmrRegion) &&
                !string.IsNullOrEmpty(rmrRegion))
                return string.Equals(rmrRegion, hub, StringComparison.OrdinalIgnoreCase);
            string fromRegion = RmrRules.NormalizeRmrRegion(competition.region);
            return string.Equals(fromRegion, hub, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RegionMatches(string competitionRegion, string hubRegion)
        {
            if (string.IsNullOrEmpty(competitionRegion)) return true;
            string hub = RmrRules.NormalizeRmrRegion(hubRegion);
            string comp = RmrRules.NormalizeRmrRegion(competitionRegion);
            return string.Equals(comp, hub, StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveClubRegion(string clubId)
        {
            if (_content.Clubs.TryGetValue(clubId, out CareerClubDefinition club))
                return RmrRules.ResolveClubRmrRegion(club);
            return "Europe";
        }

        private void LoadMapHubs()
        {
            try
            {
                TextAsset asset = Resources.Load<TextAsset>("Generated/map_hubs");
                if (asset != null)
                {
                    MapHubFile file = JsonUtility.FromJson<MapHubFile>(asset.text);
                    if (file?.hubs != null && file.hubs.Length > 0)
                    {
                        _mapHubs = file.hubs;
                        return;
                    }
                }

#if UNITY_EDITOR
                string path = "Assets/_Project/Data/JSON/map_hubs.json";
                if (File.Exists(path))
                {
                    MapHubFile file = JsonUtility.FromJson<MapHubFile>(File.ReadAllText(path));
                    if (file?.hubs != null && file.hubs.Length > 0)
                    {
                        _mapHubs = file.hubs;
                        return;
                    }
                }
#endif
            }
            catch (Exception)
            {
                // fall through to defaults
            }

            _mapHubs = DefaultMapHubs();
        }

        private static MapHubDefinition[] DefaultMapHubs() =>
            new[]
            {
                new MapHubDefinition
                {
                    id = "europe", displayName = "歐洲", region = "Europe",
                    subtitle = "RMR 歐洲賽區",
                    x = 0.515f, y = 0.705f, color = "#6E9AA0"
                },
                new MapHubDefinition
                {
                    id = "cis", displayName = "獨聯體", region = "CIS",
                    subtitle = "RMR 獨聯體賽區",
                    x = 0.575f, y = 0.720f, color = "#C46A62"
                },
                new MapHubDefinition
                {
                    id = "south_america", displayName = "南美", region = "SouthAmerica",
                    subtitle = "RMR 南美賽區",
                    x = 0.335f, y = 0.335f, color = "#C6A15A"
                },
                new MapHubDefinition
                {
                    id = "north_america", displayName = "北美", region = "NorthAmerica",
                    subtitle = "RMR 北美賽區",
                    x = 0.175f, y = 0.595f, color = "#8FB4B8"
                },
                new MapHubDefinition
                {
                    id = "asia", displayName = "亞洲", region = "Asia",
                    subtitle = "RMR 亞洲賽區",
                    x = 0.785f, y = 0.555f, color = "#D4A89A"
                },
                new MapHubDefinition
                {
                    id = "oceania", displayName = "大洋洲", region = "Oceania",
                    subtitle = "RMR 大洋洲賽區",
                    x = 0.830f, y = 0.280f, color = "#D4BC86"
                }
            };

        private static Color ParseHtmlColor(string html, Color fallback)
        {
            if (string.IsNullOrEmpty(html)) return fallback;
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : fallback;
        }

        private static void AddEventTrigger(
            EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }
}
