using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController : MonoBehaviour
    {
        private static readonly string[] Tabs =
        {
            "地圖", "選手", "經濟", "談判", "基地", "戰術", "社群", "圖鑑"
        };

        /// <summary>Left index. Content starts at MainColumnX0.</summary>
        private const int TabRailSlotCount = 8;
        private const float RightRailX0 = 0.012f;
        private const float RightRailX1 = 0.078f;
        private const float MainColumnX0 = 0.090f;
        private const float HeaderY0 = 0.935f;
        private const float HeaderY1 = 0.985f;
        private const float MainColumnX1 = 0.985f;

        // Print system. Stage type is white. Paper copy is ink.
        private static readonly Color Bg = CareerEditorialChrome.Stage;
        private static readonly Color Panel = CareerEditorialChrome.Paper;
        private static readonly Color PanelInner = CareerEditorialChrome.Paper;
        private static readonly Color PanelRow = CareerEditorialChrome.Paper;
        private static readonly Color Accent = CareerEditorialChrome.Mustard;
        private static readonly Color AccentDim = CareerEditorialChrome.Mustard;
        private static readonly Color AccentBlue = CareerEditorialChrome.Cyan;
        private static readonly Color TextPrimary = CareerEditorialChrome.Ink;
        private static readonly Color TextMuted = CareerEditorialChrome.InkMuted;
        private static readonly Color TitleIndexMuted = CareerEditorialChrome.StageMuted;
        private static readonly Color ButtonFace = CareerEditorialChrome.Paper;
        private static readonly Color ButtonEdge = CareerEditorialChrome.Rule;
        private static readonly Color TabIdle = CareerEditorialChrome.Stage;
        private static readonly Color Positive = CareerEditorialChrome.Mustard;
        private static readonly Color Warning = CareerEditorialChrome.Mustard;
        private static readonly Color Danger = CareerEditorialChrome.Red;
        private static readonly Color RankGold = CareerEditorialChrome.Mustard;
        private static readonly Color ProfileValue = CareerEditorialChrome.Mustard;
        private static readonly Color ProfileTraitName = CareerEditorialChrome.Cyan;

        private static readonly Color BgDeep = CareerEditorialChrome.Stage;
        private static readonly Color BgLift = new Color(0.08f, 0.07f, 0.05f, 1f);

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        /// <summary>
        /// Dark stage wash with a slow mote field underneath the career chrome.
        /// </summary>
        private void BuildStarryNavyAtmosphereUnderlay(Transform parent)
        {
            Image root = CreateUiObject<Image>("StarryNavyAtmosphere", parent);
            Stretch(root.rectTransform);
            root.color = Color.clear;
            root.raycastTarget = false;
            root.transform.SetAsFirstSibling();

            Transform t = root.transform;
            Sprite fade = UiRoundedRectUtility.GetVerticalFadeSprite();

            Image topWash = CreateUiObject<Image>("NavyTopWash", t);
            SetRect(topWash.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 1.05f));
            topWash.sprite = fade;
            topWash.type = Image.Type.Simple;
            topWash.color = new Color(BgDeep.r, BgDeep.g, BgDeep.b, 0.62f);
            topWash.raycastTarget = false;

            Image bottomLift = CreateUiObject<Image>("NavyBottomLift", t);
            SetRect(bottomLift.rectTransform, new Vector2(0f, -0.05f), new Vector2(1f, 0.55f));
            bottomLift.sprite = fade;
            bottomLift.type = Image.Type.Simple;
            bottomLift.color = new Color(BgLift.r, BgLift.g, BgLift.b, 0.28f);
            bottomLift.raycastTarget = false;
            bottomLift.rectTransform.localScale = new Vector3(1f, -1f, 1f);

            Image cornerLift = CreateUiObject<Image>("NavyCornerLift", t);
            SetRect(cornerLift.rectTransform, new Vector2(0.45f, 0.35f), new Vector2(1.15f, 1.05f));
            cornerLift.sprite = UiRoundedRectUtility.GetRadialGlowSprite();
            cornerLift.type = Image.Type.Simple;
            cornerLift.color = new Color(Bg.r, Bg.g, Bg.b, 0.28f);
            cornerLift.raycastTarget = false;

            UiParticleField field = root.gameObject.AddComponent<UiParticleField>();
            field.Configure(UiRoundedRectUtility.GetRadialGlowSprite(), CareerEditorialChrome.Mustard);
            if (_shellParticles == null)
                _shellParticles = field;
        }

        private void RefreshFrostedGlass(UiFrostedGlassHost host, Transform root)
        {
            if (host == null || root == null || !root.gameObject.activeInHierarchy)
                return;

            var hide = new System.Collections.Generic.List<GameObject>(root.childCount);
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;
                if (child.name == "StarryNavyAtmosphere")
                    continue;
                hide.Add(child.gameObject);
            }

            host.RefreshBlur(hide);
        }

        private CareerContent _content;
        private ContentDatabase _matchContent;
        private CareerSession _session;
        private CareerPersistence _persistence;
        private MapBanPickPresentation _mapBanPick;
        private CareerMissionTreePresentation _missionTreePresentation;
        private int _activeSaveSlot = CareerPersistence.FirstManualSlot;
        private const string ActiveSaveSlotPrefsKey = "CareerActiveSaveSlot";
        /// <summary>UI gate only; market target drawing code is retained for later re-enable.</summary>
        private static readonly bool MarketTargetsUiEnabled = false;
        private CareerDifficulty _newDifficulty = CareerDifficulty.Normal;
        private string _selectedClubId = "navi_2021";
        private string _status = "請選擇俱樂部開始生涯";
        private CareerUiNavLink _statusNavLink;
        private Text _statusJumpHint;
        private string _pendingMapCompetitionFocusId;
        private int _tab;
        private bool _mapEventLogFocus;
        private Font _uiFont;
        private Font _uiFont700;
        private Font _uiFontAnton;
        private Texture2D _budgetIncomePieTex;
        private Texture2D _budgetExpensePieTex;
        private Sprite _budgetIncomePieSprite;
        private Sprite _budgetExpensePieSprite;

        private struct BudgetChartSlice
        {
            public float Weight;
            public Color Color;
        }
        private Font _playerIdFont;
        private bool _careerStarted;
        private string _selectedPlayerId;
        private bool _trainingPanelOpen;
        private string _trainingUiPlayerId;
        private readonly List<string> _trainingUiAttrs = new List<string>();
        private readonly List<string> _trainingUiMaps = new List<string>();
        private string _trainingUiRole = "Rifler";
        private readonly Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _marketOfferDrafts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private GameObject _shellRoot;
        private Image _contentPanel;
        private UiParticleField _shellParticles;
        private UiParticleField _mapParticles;
        private Text _chapterWord;
        private Image _chapterPigment;
        private bool _chapterFromRight = true;
        private Text _titleNameGhost;
        private GameObject _titleRoot;
        private UiFrostedGlassHost _canvasFrostHost;
        private UiFrostedGlassHost _titleFrostHost;
        private UiFrostedGlassHost _shellFrostHost;
        private SettingsPanelPresentation _settingsPanel;
        private bool _pausedBeforeSettings;
        private float _autoSaveTimer;
        private GameObject _rankingRoot;
        private GameObject _competitionDetailRoot;
        private Text _competitionDetailBody;
        private Text _competitionDetailSideMeta;
        private RectTransform _competitionDetailBracketRoot;
        private ScrollRect _competitionDetailScroll;
        private RectTransform _competitionDetailActions;
        private string _competitionDetailId;
        private GameObject _sponsorNegotiationRoot;
        private Text _sponsorNegotiationTitle;
        private Text _sponsorNegotiationBody;
        private Button _sponsorNegotiationConfirmButton;
        private string _pendingSponsorNegotiationId;
        private GameObject _clubBriefingRoot;
        private Text _clubBriefingTitle;
        private Text _clubBriefingRegion;
        private Text _clubBriefingBody;
        private RectTransform _clubBriefingBodyContent;
        private RectTransform _rankingListRoot;
        private ScrollRect _rankingScroll;
        private Text _rankingTitleText;
        private Text _rankingHeaderRank;
        private Text _rankingHeaderClub;
        private Text _rankingHeaderPts;
        private Text _rankingAllButtonLabel;
        private bool _rankingShowAllClubs;
        private bool _rankingShowRmr;
        private int _rankingRmrRegionIndex;
        private Text _rankingModeWorldLabel;
        private Text _rankingModeRmrLabel;
        private Text _titleHint;
        private Image _titleSelectedLogo;
        private Text _titleSelectedName;
        private Text _titleSelectedFullName;
        private Text _titleSelectedRegion;
        private Text _titleEraIntro;
        private RectTransform _titleEraIntroContent;
        private Text _titleEraButtonLabel;
        private Text _titleCashText;
        private Text _titleFansText;
        private Text _titlePrestigeText;
        private Text _titleFameText;
        private Text _titleRankText;
        private RectTransform _titleRosterRoot;
        private readonly RectTransform[] _titleMotionBlocks = new RectTransform[6];
        private readonly List<Image> _clubSelectMarks = new List<Image>();
        private RectTransform _canvasRect;
        private Image _titleHoverPopup;
        private Text _titleHoverName;
        private Text _titleHoverMeta;
        private RectTransform _titleHoverContent;
        private Coroutine _titleHoverRoutine;
        private string _titleHoverPlayerId;
        private bool _titlePlayerHoverIncludeLore = true;
        private const float TitleProfileHoverDelaySeconds = 1f;
        private Image _statTooltipRoot;
        private Text _statTooltipTitle;
        private Text _statTooltipBody;
        private Coroutine _statTooltipRoutine;
        private string _statTooltipKey;
        private const float HeaderStatHoverDelaySeconds = 1f;
        private const float DifficultyHoverDelaySeconds = 1f;
        private Coroutine _difficultyHoverRoutine;
        private CareerDifficulty? _difficultyHoverTarget;
        private readonly List<Image> _clubLogoImages = new List<Image>();
        private readonly List<GameObject> _titleRosterRows = new List<GameObject>();
        private GameObject _rankingClubIntelRoot;
        private Image _rankingClubIntelLogo;
        private Text _rankingClubIntelName;
        private Text _rankingClubIntelFullName;
        private Text _rankingClubIntelRegion;
        private Text _rankingClubIntelCash;
        private Text _rankingClubIntelFans;
        private Text _rankingClubIntelPrestige;
        private Text _rankingClubIntelFame;
        private Text _rankingClubIntelRank;
        private RectTransform _rankingClubIntelRosterRoot;
        private readonly List<GameObject> _rankingClubIntelRosterRows = new List<GameObject>();
        private string _selectedEraId = "2021_01_01";
        private Image _clubLogoImage;
        private Text _cashText;
        private Text _fansText;
        private Text _prestigeText;
        private Text _fameText;
        private Text _rankText;
        private Text _seasonLabel;
        private Text _statusText;
        private Text _pauseButtonLabel;
        private Image _pauseButtonFace;
        private readonly Image[] _speedFaces = new Image[5];
        private readonly Text[] _speedLabels = new Text[5];
        private bool _timePaused = true;
        private int _timeSpeed = 1;
        private float _hourAccumulator;
        private float _uiRefreshTimer;
        // Index 1–5: 1x, 2x, 4x, 8x, 16x (base = 1 career hour / real second at 1x).
        private static readonly float[] HoursPerRealSecond =
            { 0f, 1f, 2f, 4f, 8f, 16f };
        private static readonly string[] TimeSpeedLabels =
            { "1x", "2x", "4x", "8x", "16x" };
        private readonly List<Image> _tabFaces = new List<Image>();
        private readonly List<Image> _tabAccents = new List<Image>();
        private readonly List<Text> _tabLabels = new List<Text>();
        private readonly List<Image> _clubFaces = new List<Image>();
        private readonly List<Text> _clubLabels = new List<Text>();
        private readonly List<string> _clubIds = new List<string>();
        private readonly List<Image> _difficultyFaces = new List<Image>();
        private readonly List<Text> _difficultyLabels = new List<Text>();
        private RectTransform _contentRoot;
        private RectTransform _uiListRoot;
        private RectTransform _mapScheduleContent;
        private ScrollRect _mapScheduleScroll;
        private ScrollRect _scroll;
        private GameObject _scrollHost;
        private GameObject _mapHost;
        private GameObject _officialMatchBanner;
        private GameObject _playerDetailRoot;
        private Text _playerDetailCaption;
        private ScrollRect _playerDetailScroll;
        private RectTransform _playerDetailContent;

        private static readonly string[] PlayableClubIds = { "navi_2021", "gambit_2021" };

        private void Awake()
        {
            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("GenYoMin2.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont700;
            _uiFontAnton = LoadProjectFont("Anton.ttf") ?? _uiFontBlack;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont700;
            GameSettingsRuntime.LoadAndApply();
            try
            {
                _content = RuntimeCareerContentLoader.Load();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                BuildBootstrapErrorUi(ex.Message);
                return;
            }

            try
            {
                _matchContent = RuntimeContentLoader.Load();
                DemoZh.ConfigurePlayerNames(_matchContent.Players.Values);
            }
            catch (Exception) { _matchContent = null; }
            _persistence = new CareerPersistence();
            _activeSaveSlot = LoadActiveSaveSlotPreference();
            BuildInterface();
            ShowTitleScreen();
        }

        private void BuildBootstrapErrorUi(string message)
        {
            var canvasGo = new GameObject("BootstrapErrorCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.07f, 0.09f, 0.96f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var textGo = new GameObject("Message");
            textGo.transform.SetParent(panel.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text =
                "生涯資料載入失敗，畫面無法建立（不是載入中）。\n\n" +
                message + "\n\n" +
                "請在 Unity 選單執行：Tactica Universalis → Import JSON Content\n" +
                "並查看 Console 完整錯誤。";
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.08f, 0.12f);
            textRect.anchorMax = new Vector2(0.92f, 0.88f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void Start()
        {
            if (!CareerMatchLaunchContext.TryTakeCompletedResult(out MatchResult result))
                return;
            if (!_persistence.Exists(_activeSaveSlot) || _matchContent == null)
                return;

            CareerState loaded = _persistence.Load(_activeSaveSlot);
            if (loaded == null) return;

            if (!PlayableClubIds.Contains(loaded.playerClubId))
                loaded.playerClubId = "navi_2021";
            BuildSession(loaded);
            _careerStarted = true;
            _timeSpeed = 1;
            _timePaused = true;
            _hourAccumulator = 0f;
            _uiRefreshTimer = 0f;
            if (_titleRoot != null) _titleRoot.SetActive(false);
            if (_shellRoot != null) _shellRoot.SetActive(true);

            try
            {
                bool seriesDone = _session.TryApplyOfficialMatchResult(result, _matchContent);
                if (seriesDone)
                {
                    _persistence.Save(_session.State, _activeSaveSlot);
                    ApplyOfficialMatchReturnUi();
                }
                else if (_session.OfficialSeriesNeedsNextMap)
                {
                    _persistence.Save(_session.State, _activeSaveSlot);
                    CareerMapSeriesState series = _session.GetOfficialMapSeries();
                    SetStatus(
                        $"地圖結束 · 系列 {series.mapWinsPlayer}-{series.mapWinsOpponent} · 即將進入下一張圖");
                    // Always refresh header/logo even if match apply failed mid-way.
                    RefreshAll();
                    StartCoroutine(DeferredOfficialMatchReturnRefresh());
                    StartCoroutine(DeferredLaunchNextSeriesMap());
                    return;
                }
                else
                    SetStatus("無法套用賽事結果，已載入存檔");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("套用賽事結果時發生錯誤，已載入存檔");
            }

            // Always refresh header/logo even if match apply failed mid-way.
            RefreshAll();
            StartCoroutine(DeferredOfficialMatchReturnRefresh());
        }

        private IEnumerator DeferredLaunchNextSeriesMap()
        {
            yield return null;
            LaunchOfficialCompetitionMatch();
        }

        private IEnumerator DeferredOfficialMatchReturnRefresh()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (!_careerStarted || _session == null)
                yield break;
            RefreshAll();
            RefreshTimeControlVisuals();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshAll();
        }

        private void ApplyOfficialMatchReturnUi()
        {
            if (_session == null) return;

            bool pauseOnReturn = GameSettingsRuntime.Current.pauseOnMatchEnd;

            if (_session.AwaitsOfficialPlayerMatch)
            {
                SetStatus("正式賽下一場對戰 · 時間已暫停",
                    new CareerUiNavLink(CareerUiDestination.OfficialMatch,
                        _session.State.competitionRun?.competitionId));
                _timePaused = true;
            }
            else if (_session.HasScheduledOfficialPlayerMatch)
            {
                int wait = _session.HoursUntilScheduledOfficialPlayerMatch;
                if (pauseOnReturn)
                {
                    SetStatus($"下一場正式賽約 {FormatCountdownHours(wait)} 後 · 時間已暫停",
                        new CareerUiNavLink(CareerUiDestination.MapCompetition,
                            _session.State.competitionRun?.competitionId));
                    _timePaused = true;
                }
                else
                {
                    SetStatus($"下一場正式賽約 {FormatCountdownHours(wait)} 後 · 可繼續推進時間",
                        new CareerUiNavLink(CareerUiDestination.MapCompetition,
                            _session.State.competitionRun?.competitionId));
                    _timePaused = false;
                }
            }
            else if (pauseOnReturn)
            {
                SetStatus("正式賽事已結算 · 時間已暫停",
                    new CareerUiNavLink(CareerUiDestination.Map));
                _timePaused = true;
            }
            else
            {
                SetStatus("正式賽事已結算",
                    new CareerUiNavLink(CareerUiDestination.Map));
                _timePaused = false;
            }

            _hourAccumulator = 0f;
            RefreshTimeControlVisuals();
        }

        private void Update()
        {
            if (TryHandleEscapeBack())
                return;
            if (!_careerStarted || _session == null) return;
            HandleTimeHotkeys();
            TickEventToast();
            TickCareerClock();
            TickOptionalAutoSave();
        }

        /// <summary>
        /// ESC closes the topmost dismissible overlay / sub-page (same as 關閉／取消／返回).
        /// Scripted events require an option and are not dismissed.
        /// </summary>
        private bool TryHandleEscapeBack()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return false;

            // Modal choice required — do not dismiss, and do not close anything underneath.
            if (_scriptedEventRoot != null && _scriptedEventRoot.activeSelf)
                return true;
            if (_registerConfirmRoot != null && _registerConfirmRoot.activeSelf)
            {
                HideRegisterConfirm();
                return true;
            }

            if (_registerChooseTwoRoot != null && _registerChooseTwoRoot.activeSelf)
            {
                HideRegisterChooseTwo();
                return true;
            }

            if (_newsDetailRoot != null && _newsDetailRoot.activeSelf)
            {
                HideNewsDetail();
                return true;
            }

            if (_missionTreePresentation != null && _missionTreePresentation.IsOpen)
            {
                DismissMissionTreeOverlay();
                return true;
            }

            GameObject top = null;
            int topIndex = int.MinValue;

            void Consider(GameObject go)
            {
                if (go == null || !go.activeInHierarchy)
                    return;
                int idx = go.transform.GetSiblingIndex();
                if (idx < topIndex)
                    return;
                topIndex = idx;
                top = go;
            }

            Consider(_settingsPanel != null ? _settingsPanel.Root : null);
            Consider(_playerDetailRoot);
            Consider(_coachDetailRoot);
            Consider(_rankingClubIntelRoot);
            Consider(_rankingRoot);
            Consider(_competitionDetailRoot);
            Consider(_sponsorNegotiationRoot);
            if (_clubHubOpen)
                Consider(_clubHubRoot);
            Consider(_clubBriefingRoot);

            if (top != null)
            {
                if (_settingsPanel != null && top == _settingsPanel.Root)
                    HideSettingsPanel();
                else if (top == _playerDetailRoot)
                    ClosePlayerDetail();
                else if (top == _coachDetailRoot)
                    CloseCoachDetail();
                else if (top == _rankingClubIntelRoot)
                    HideRankingClubIntel();
                else if (top == _rankingRoot)
                    HideWorldRankingPanel();
                else if (top == _competitionDetailRoot)
                    HideCompetitionDetailPanel();
                else if (top == _sponsorNegotiationRoot)
                    HideSponsorNegotiationPanel();
                else if (top == _clubHubRoot)
                    CloseClubHubPanel();
                else if (top == _clubBriefingRoot)
                    HideClubBriefing();
                return true;
            }

            if (_titleHoverPopup != null && _titleHoverPopup.gameObject.activeSelf)
            {
                HideTitleHoverPopup();
                CancelTitleHover();
                return true;
            }

            if (_statTooltipRoot != null && _statTooltipRoot.gameObject.activeSelf)
            {
                HideStatTooltip();
                CancelStatTooltip();
                return true;
            }

            return false;
        }

        private void OnApplicationFocus(bool hasFocus) =>
            GameSettingsRuntime.HandleFocus(hasFocus);

        private void BuildTimeControls(Transform headerBar)
        {
            Button pauseBtn = CreateGlassCapsuleButton(
                "Pause", "❚❚", headerBar,
                new Vector2(0.728f, 0.12f), new Vector2(0.768f, 0.88f),
                AccentDim, Accent, TogglePause);
            _pauseButtonFace = pauseBtn.transform.Find("Face")?.GetComponent<Image>();
            _pauseButtonLabel = pauseBtn.GetComponentInChildren<Text>();
            ApplyFilledButtonLabel(_pauseButtonLabel, 72, heightFill: 0.9f);

            float x0 = 0.772f;
            float slot = 0.036f;
            for (int i = 0; i < 5; i++)
            {
                int speed = i + 1;
                float min = x0 + i * slot;
                float max = min + slot - 0.004f;
                Button btn = CreateGlassCapsuleButton(
                    "Speed" + speed, TimeSpeedLabels[i], headerBar,
                    new Vector2(min, 0.12f), new Vector2(max, 0.88f),
                    ButtonEdge, ButtonFace, () => SetTimeSpeed(speed));
                _speedFaces[i] = btn.transform.Find("Face")?.GetComponent<Image>();
                _speedLabels[i] = btn.GetComponentInChildren<Text>();
                ApplyFilledButtonLabel(_speedLabels[i], 72, heightFill: 0.9f);
            }

            RefreshTimeControlVisuals();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("CareerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = canvasGo.GetComponent<RectTransform>();
            Stretch(_canvasRect);

            _canvasFrostHost = canvasGo.GetComponent<UiFrostedGlassHost>();
            if (_canvasFrostHost == null)
                _canvasFrostHost = canvasGo.AddComponent<UiFrostedGlassHost>();

            Image bg = CreateUiObject<Image>("Background", canvasGo.transform);
            Stretch(bg.rectTransform);
            bg.color = Bg;
            bg.raycastTarget = false;

            Image topAccent = CreateUiObject<Image>("TopAccent", canvasGo.transform);
            SetRect(topAccent.rectTransform, new Vector2(0f, 0.992f), new Vector2(1f, 1f));
            topAccent.color = Color.clear;
            topAccent.raycastTarget = false;

            Image shell = CreateUiObject<Image>("Shell", canvasGo.transform);
            SetRect(shell.rectTransform, new Vector2(0.012f, 0.015f), new Vector2(0.988f, 0.985f));
            _shellRoot = shell.gameObject;
            _shellFrostHost = shell.gameObject.AddComponent<UiFrostedGlassHost>();
            shell.color = Color.clear;
            CareerEditorialChrome.CreateCornerTicks(shell.transform);
            BuildStarryNavyAtmosphereUnderlay(shell.transform);

            Image header = CreateUiObject<Image>("Header", shell.transform);
            SetRect(header.rectTransform, new Vector2(MainColumnX0, HeaderY0), new Vector2(0.90f, HeaderY1));
            header.color = Color.clear;
            header.raycastTarget = false;

            _clubLogoImage = CreateUiObject<Image>("ClubLogo", header.transform);
            SetRect(_clubLogoImage.rectTransform, new Vector2(0.008f, 0.12f), new Vector2(0.046f, 0.88f));
            _clubLogoImage.color = Color.white;
            _clubLogoImage.preserveAspect = true;
            _clubLogoImage.raycastTarget = true;

            _cashText = CreateIconStat("Cash", header.transform, "UI/Icons/cash",
                new Vector2(0.050f, 0.08f), new Vector2(0.145f, 0.92f), CareerEditorialChrome.StageType, "cash",
                () =>
                {
                    CancelStatTooltip();
                    SelectTab(2);
                });
            _fansText = CreateIconStat("Fans", header.transform, "UI/Icons/fans",
                new Vector2(0.147f, 0.08f), new Vector2(0.242f, 0.92f), CareerEditorialChrome.StageType, "fans");
            _prestigeText = CreateIconStat("Prestige", header.transform, "UI/Icons/prestige",
                new Vector2(0.244f, 0.08f), new Vector2(0.339f, 0.92f), CareerEditorialChrome.StageType, "prestige");
            _fameText = CreateIconStat("Fame", header.transform, "UI/Icons/fame",
                new Vector2(0.341f, 0.08f), new Vector2(0.436f, 0.92f), CareerEditorialChrome.StageType, "fame");
            _rankText = CreateIconStat("Rank", header.transform, "UI/Icons/rank",
                new Vector2(0.438f, 0.08f), new Vector2(0.533f, 0.92f), CareerEditorialChrome.Mustard, "rank", OpenWorldRankingPanel);

            _seasonLabel = CreateText("SeasonLabel", header.transform, 28, TextAnchor.MiddleLeft, CareerEditorialChrome.StageType, _uiFont700);
            SetRect(_seasonLabel.rectTransform, new Vector2(0.540f, 0.125f), new Vector2(0.720f, 0.875f));
            _seasonLabel.text = "❚❚ 2021.1.1 0:00";
            _seasonLabel.fontStyle = FontStyle.Bold;
            _seasonLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _seasonLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _seasonLabel.resizeTextForBestFit = true;
            _seasonLabel.resizeTextMinSize = 14;
            _seasonLabel.resizeTextMaxSize = 28;

            BuildTimeControls(header.transform);

            CreateIconButton("Settings", shell.transform, "UI/Icons/settings",
                new Vector2(0.905f, HeaderY0), new Vector2(0.985f, HeaderY1),
                ToggleSettingsPanel);

            Image statusBar = CreateUiObject<Image>("Status", shell.transform);
            SetRect(statusBar.rectTransform, new Vector2(MainColumnX0, 0.895f), new Vector2(MainColumnX1, 0.930f));
            statusBar.color = Color.clear;
            Button statusButton = statusBar.gameObject.AddComponent<Button>();
            statusButton.targetGraphic = statusBar;
            ApplyButtonColors(statusButton);
            statusButton.onClick.AddListener(OnStatusBarNavigateClicked);

            _statusText = CreateText("StatusText", statusBar.transform, 14, TextAnchor.MiddleLeft, CareerEditorialChrome.StageMuted);
            SetRect(_statusText.rectTransform, new Vector2(0.015f, 0.1f), new Vector2(0.72f, 0.9f));

            _statusJumpHint = CreateText("JumpHint", statusBar.transform, 14, TextAnchor.MiddleRight, AccentBlue, _uiFont700);
            SetRect(_statusJumpHint.rectTransform, new Vector2(0.74f, 0.1f), new Vector2(0.985f, 0.9f));
            _statusJumpHint.text = "點擊前往 ›";
            _statusJumpHint.raycastTarget = false;
            _statusJumpHint.gameObject.SetActive(false);

            Image contentPanel = CreateUiObject<Image>("ContentPanel", shell.transform);
            SetRect(contentPanel.rectTransform, new Vector2(MainColumnX0, 0.02f), new Vector2(MainColumnX1, 0.835f));
            contentPanel.color = CareerEditorialChrome.Stage;
            contentPanel.raycastTarget = false;
            _contentPanel = contentPanel;
            if (contentPanel.GetComponent<UiChapterEnter>() == null)
                contentPanel.gameObject.AddComponent<UiChapterEnter>();

            Image chapterBand = CreateUiObject<Image>("ChapterBand", shell.transform);
            SetRect(chapterBand.rectTransform, new Vector2(MainColumnX0, 0.845f), new Vector2(0.72f, 0.890f));
            chapterBand.color = Color.clear;
            chapterBand.raycastTarget = false;
            chapterBand.gameObject.AddComponent<RectMask2D>();

            _chapterPigment = CareerEditorialChrome.CreateBlock(
                chapterBand.transform, "ChapterPigment",
                new Vector2(0f, 0.06f), new Vector2(0.16f, 0.16f),
                CareerEditorialChrome.Cyan);

            _chapterWord = CreateText("ChapterWord", chapterBand.transform, 40, TextAnchor.MiddleLeft, CareerEditorialChrome.StageType, _uiFontBlack);
            SetRect(_chapterWord.rectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 0.96f));
            _chapterWord.fontStyle = FontStyle.Bold;
            _chapterWord.horizontalOverflow = HorizontalWrapMode.Wrap;
            _chapterWord.verticalOverflow = VerticalWrapMode.Truncate;
            _chapterWord.raycastTarget = false;
            _chapterWord.text = Tabs[0];

            BuildWorldMapHost(contentPanel.transform);

            _scroll = CreateUiObject<ScrollRect>("Scroll", contentPanel.transform);
            _scrollHost = _scroll.gameObject;
            RectTransform scrollRect = _scroll.GetComponent<RectTransform>();
            Stretch(scrollRect);
            scrollRect.offsetMin = new Vector2(10f, 10f);
            scrollRect.offsetMax = new Vector2(-10f, -10f);
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 42f;
            _scrollHost.SetActive(false);

            Image viewportBg = CreateUiObject<Image>("Viewport", _scroll.transform);
            Stretch(viewportBg.rectTransform);
            viewportBg.color = Color.clear;
            viewportBg.raycastTarget = true;
            viewportBg.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport = viewportBg.rectTransform;

            _contentRoot = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _contentRoot.SetParent(viewportBg.transform, false);
            _contentRoot.anchorMin = new Vector2(0f, 1f);
            _contentRoot.anchorMax = new Vector2(1f, 1f);
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            _contentRoot.anchoredPosition = Vector2.zero;
            _contentRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);

            var fitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll.content = _contentRoot;

            Image tabBar = CreateUiObject<Image>("Tabs", shell.transform);
            SetRect(tabBar.rectTransform, new Vector2(RightRailX0, 0.02f), new Vector2(RightRailX1, 0.835f));
            tabBar.color = Color.clear;
            tabBar.raycastTarget = false;
            BuildTabs(tabBar.transform);

            BuildPlayerDetailOverlay(canvasGo.transform);
            BuildCoachStaffDetailOverlay(canvasGo.transform);
            BuildTitleScreen(canvasGo.transform);
            BuildSettingsPanel(canvasGo.transform);
            BuildWorldRankingPanel(canvasGo.transform);
            BuildCompetitionDetailOverlay(canvasGo.transform);
            BuildSponsorNegotiationOverlay(canvasGo.transform);
            BuildScriptedEventOverlay(canvasGo.transform);
            BuildCompetitionRegisterOverlays(canvasGo.transform);
            BuildStatTooltipPopup(canvasGo.transform);
            BuildClubHubPanel(canvasGo.transform);
            WireClubLogoHubButton();
        }

        private void BuildPlayerDetailOverlay(Transform canvas)
        {
            Image backdrop = CreateUiObject<Image>("PlayerDetailOverlay", canvas);
            Stretch(backdrop.rectTransform);
            backdrop.color = new Color(0f, 0f, 0f, 0.78f);
            backdrop.raycastTarget = true;
            _playerDetailRoot = backdrop.gameObject;

            Button dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = backdrop;
            ColorBlock colors = dismiss.colors;
            colors.normalColor = backdrop.color;
            colors.highlightedColor = backdrop.color;
            colors.pressedColor = backdrop.color;
            colors.selectedColor = backdrop.color;
            dismiss.colors = colors;
            dismiss.onClick.AddListener(ClosePlayerDetail);

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur.
            Image card = UiLightChromeUtility.CreateCard(
                "DetailCard", backdrop.transform,
                new Vector2(0.16f, 0.04f), new Vector2(0.96f, 0.96f), Panel);
            Image cardFace = UiLightChromeUtility.CreateInsetFace(card.transform, PanelInner);

            _playerDetailCaption = CreateText("Caption", cardFace.transform, 20, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_playerDetailCaption.rectTransform, new Vector2(0.03f, 0.93f), new Vector2(0.70f, 0.985f));
            _playerDetailCaption.text = "個人資料";

            CreateGlassCapsuleButton("CloseDetail", "關閉", cardFace.transform,
                new Vector2(0.84f, 0.935f), new Vector2(0.97f, 0.98f), ButtonEdge, ButtonFace, ClosePlayerDetail,
                _uiFont700);

            Image scrollHost = CreateUiObject<Image>("DetailScrollHost", cardFace.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.92f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            _playerDetailScroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _playerDetailScroll.horizontal = false;
            _playerDetailScroll.vertical = true;
            _playerDetailScroll.movementType = ScrollRect.MovementType.Clamped;
            _playerDetailScroll.scrollSensitivity = 48f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _playerDetailScroll.viewport = viewport.rectTransform;

            _playerDetailContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _playerDetailContent.SetParent(viewport.transform, false);
            _playerDetailContent.anchorMin = new Vector2(0f, 1f);
            _playerDetailContent.anchorMax = new Vector2(1f, 1f);
            _playerDetailContent.pivot = new Vector2(0.5f, 1f);
            _playerDetailContent.anchoredPosition = Vector2.zero;
            _playerDetailContent.sizeDelta = new Vector2(0f, 0f);

            var layout = _playerDetailContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 12, 6, 14);

            var fitter = _playerDetailContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _playerDetailScroll.content = _playerDetailContent;

            ClosePlayerDetail();
        }

        private void BuildTitleScreen(Transform canvas)
        {
            Image title = CreateUiObject<Image>("TitleScreen", canvas);
            Stretch(title.rectTransform);
            title.color = Bg;
            title.raycastTarget = true;
            _titleRoot = title.gameObject;
            _titleFrostHost = title.gameObject.AddComponent<UiFrostedGlassHost>();
            BuildStarryNavyAtmosphereUnderlay(title.transform);
            BuildGoldDrift(title.transform, "GoldDiscA", new Vector2(0.82f, -0.18f), new Vector2(1.18f, 0.36f), 0.22f, 9f);
            BuildGoldDrift(title.transform, "GoldDiscB", new Vector2(-0.22f, 0.78f), new Vector2(0.14f, 1.16f), 0.16f, 13f);

            Text brand = CreateText("TitleBrand", title.transform, 15, TextAnchor.UpperLeft, CareerEditorialChrome.StageMuted, _uiFont700);
            SetRect(brand.rectTransform, new Vector2(0.04f, 0.90f), new Vector2(0.46f, 0.96f));
            brand.text = "TACTICA UNIVERSALIS";

            _titleSelectedName = CreateText("SelectedName", title.transform, 64, TextAnchor.LowerLeft, CareerEditorialChrome.StageType, _uiFontBlack);
            SetRect(_titleSelectedName.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.52f, 0.90f));
            _titleSelectedName.fontStyle = FontStyle.Bold;
            _titleSelectedName.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleSelectedName.verticalOverflow = VerticalWrapMode.Truncate;
            _titleSelectedName.resizeTextForBestFit = true;
            _titleSelectedName.resizeTextMinSize = 28;
            _titleSelectedName.resizeTextMaxSize = 64;
            BindTitleMotion(_titleSelectedName.rectTransform, 0);

            _titleNameGhost = CreateText("NameGhost", title.transform, 64, TextAnchor.LowerLeft, CareerEditorialChrome.Mustard, _uiFontBlack);
            SetRect(_titleNameGhost.rectTransform, new Vector2(0.034f, 0.628f), new Vector2(0.524f, 0.908f));
            _titleNameGhost.fontStyle = FontStyle.Bold;
            _titleNameGhost.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleNameGhost.verticalOverflow = VerticalWrapMode.Truncate;
            _titleNameGhost.raycastTarget = false;
            _titleNameGhost.color = new Color(CareerEditorialChrome.Mustard.r, CareerEditorialChrome.Mustard.g, CareerEditorialChrome.Mustard.b, 0f);
            _titleNameGhost.resizeTextForBestFit = true;
            _titleNameGhost.resizeTextMinSize = 28;
            _titleNameGhost.resizeTextMaxSize = 64;

            _titleSelectedFullName = CreateText("SelectedFullName", title.transform, 16, TextAnchor.MiddleLeft, CareerEditorialChrome.StageType, _uiFont700);
            SetRect(_titleSelectedFullName.rectTransform, new Vector2(0.04f, 0.56f), new Vector2(0.52f, 0.62f));
            _titleSelectedRegion = CreateText("SelectedRegion", title.transform, 14, TextAnchor.MiddleLeft, CareerEditorialChrome.StageMuted, _uiFont700);
            SetRect(_titleSelectedRegion.rectTransform, new Vector2(0.04f, 0.51f), new Vector2(0.52f, 0.56f));

            Image eraPanel = CreateUiObject<Image>("EraPanel", title.transform);
            SetRect(eraPanel.rectTransform, new Vector2(0.04f, 0.44f), new Vector2(0.42f, 0.50f));
            eraPanel.color = Color.clear;
            Button eraBtn = CreateTitleHairlineButton(
                "EraSelect", "2021.1.1", eraPanel.transform,
                new Vector2(0f, 0.1f), new Vector2(1f, 0.9f),
                OnEraButtonClicked, 18);
            _titleEraButtonLabel = eraBtn.GetComponentInChildren<Text>();
            if (_titleEraButtonLabel != null)
                _titleEraButtonLabel.color = CareerEditorialChrome.StageType;

            Image rosterPanel = CreateUiObject<Image>("ClubRoster", title.transform);
            SetRect(rosterPanel.rectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.46f, 0.44f));
            rosterPanel.color = Color.clear;
            BindTitleMotion(rosterPanel.rectTransform, 1);

            _clubIds.Clear();
            _clubFaces.Clear();
            _clubLabels.Clear();
            _clubLogoImages.Clear();
            _clubSelectMarks.Clear();
            BuildClubChoice(rosterPanel.transform, 0f, "navi_2021", "Natus Vincere");
            BuildClubChoice(rosterPanel.transform, 1f, "gambit_2021", "Gambit Esports");

            Image intelPanel = CreateUiObject<Image>("ClubIntel", title.transform);
            SetRect(intelPanel.rectTransform, new Vector2(0.56f, 0.08f), new Vector2(0.96f, 0.90f));
            intelPanel.sprite = UiRoundedRectUtility.GetRoundedRectSprite();
            intelPanel.type = Image.Type.Sliced;
            intelPanel.color = CareerEditorialChrome.Paper;
            BindTitleMotion(intelPanel.rectTransform, 2);

            Image logoFrame = CreateUiObject<Image>("LogoFrame", intelPanel.transform);
            SetRect(logoFrame.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.34f, 0.96f));
            logoFrame.color = Color.clear;
            logoFrame.raycastTarget = false;

            _titleSelectedLogo = CreateUiObject<Image>("SelectedLogo", logoFrame.transform);
            SetRect(_titleSelectedLogo.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            _titleSelectedLogo.preserveAspect = true;
            _titleSelectedLogo.color = Color.white;
            _titleSelectedLogo.raycastTarget = false;

            Text nameKicker = CreateText("NameKicker", intelPanel.transform, 14, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(nameKicker.rectTransform, new Vector2(0.38f, 0.86f), new Vector2(0.94f, 0.96f));
            nameKicker.text = "俱樂部";

            Image statsHost = CreateUiObject<Image>("HeaderStats", intelPanel.transform);
            SetRect(statsHost.rectTransform, new Vector2(0.36f, 0.55f), new Vector2(0.96f, 0.84f));
            statsHost.color = Color.clear;
            statsHost.raycastTarget = false;

            _titleCashText = CreateLabeledIconStat(
                "Cash", statsHost.transform, "UI/Icons/cash", "現金",
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.98f), TextPrimary);
            _titleFansText = CreateLabeledIconStat(
                "Fans", statsHost.transform, "UI/Icons/fans", "粉絲",
                new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.78f), TextPrimary);
            _titlePrestigeText = CreateLabeledIconStat(
                "Prestige", statsHost.transform, "UI/Icons/prestige", "威望",
                new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.58f), TextPrimary);
            _titleFameText = CreateLabeledIconStat(
                "Fame", statsHost.transform, "UI/Icons/fame", "名氣",
                new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.38f), TextPrimary);
            _titleRankText = CreateLabeledIconStat(
                "Rank", statsHost.transform, "UI/Icons/rank", "排名",
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.18f), RankGold);

            Text playersCaption = CreateText("PlayersCaption", intelPanel.transform, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(playersCaption.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.33f));
            playersCaption.text = "開局陣容";

            Image rosterHost = CreateUiObject<Image>("RosterList", intelPanel.transform);
            SetRect(rosterHost.rectTransform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.28f));
            rosterHost.color = Color.clear;
            _titleRosterRoot = rosterHost.rectTransform;

            Image introPanel = CreateUiObject<Image>("EraIntro", intelPanel.transform);
            SetRect(introPanel.rectTransform, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.52f));
            introPanel.color = Color.clear;
            BindTitleMotion(introPanel.rectTransform, 3);

            Text introCaption = CreateText("IntroCaption", introPanel.transform, 14, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(introCaption.rectTransform, new Vector2(0.02f, 0.88f), new Vector2(0.40f, 0.98f));
            introCaption.text = "時代簡介";

            Image eraScrollHost = CreateUiObject<Image>("EraScrollHost", introPanel.transform);
            SetRect(eraScrollHost.rectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.86f));
            eraScrollHost.color = Color.clear;
            eraScrollHost.raycastTarget = true;

            ScrollRect eraScroll = eraScrollHost.gameObject.AddComponent<ScrollRect>();
            eraScroll.horizontal = false;
            eraScroll.vertical = true;
            eraScroll.movementType = ScrollRect.MovementType.Clamped;
            eraScroll.scrollSensitivity = 36f;

            Image eraViewport = CreateUiObject<Image>("Viewport", eraScrollHost.transform);
            Stretch(eraViewport.rectTransform);
            eraViewport.color = Color.clear;
            eraViewport.raycastTarget = true;
            eraViewport.gameObject.AddComponent<RectMask2D>();
            eraScroll.viewport = eraViewport.rectTransform;

            _titleEraIntro = CreateText("IntroBody", eraViewport.transform, 17, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            _titleEraIntroContent = _titleEraIntro.rectTransform;
            _titleEraIntroContent.anchorMin = new Vector2(0f, 1f);
            _titleEraIntroContent.anchorMax = new Vector2(1f, 1f);
            _titleEraIntroContent.pivot = new Vector2(0.5f, 1f);
            _titleEraIntroContent.anchoredPosition = Vector2.zero;
            _titleEraIntroContent.sizeDelta = new Vector2(-4f, 120f);
            _titleEraIntro.alignment = TextAnchor.UpperLeft;
            _titleEraIntro.horizontalOverflow = HorizontalWrapMode.Wrap;
            _titleEraIntro.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_titleEraIntro);
            eraScroll.content = _titleEraIntroContent;

            Image diffPanel = CreateUiObject<Image>("DifficultyPanel", title.transform);
            SetRect(diffPanel.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.50f, 0.20f));
            diffPanel.color = Color.clear;
            BindTitleMotion(diffPanel.rectTransform, 4);

            Text diffTitle = CreateText("DiffTitle", diffPanel.transform, 16, TextAnchor.MiddleLeft, CareerEditorialChrome.StageMuted, _uiFont700);
            SetRect(diffTitle.rectTransform, new Vector2(0f, 0.72f), new Vector2(0.4f, 0.98f));
            diffTitle.fontStyle = FontStyle.Bold;
            diffTitle.text = "選擇難度";
            diffTitle.resizeTextForBestFit = true;
            diffTitle.resizeTextMinSize = 12;
            diffTitle.resizeTextMaxSize = 20;
            diffTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            diffTitle.verticalOverflow = VerticalWrapMode.Truncate;
            EventTrigger diffTitleTrigger = diffPanel.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(diffTitleTrigger, EventTriggerType.PointerEnter,
                _ => OnDifficultyPointerEnter(_newDifficulty));
            AddPointerTrigger(diffTitleTrigger, EventTriggerType.PointerExit, _ => OnDifficultyPointerExit());

            _difficultyFaces.Clear();
            _difficultyLabels.Clear();
            BuildDifficultyChoice(diffPanel.transform, 0f, CareerDifficulty.Normal, "普通");
            BuildDifficultyChoice(diffPanel.transform, 1f, CareerDifficulty.Hard, "困難");
            BuildDifficultyChoice(diffPanel.transform, 2f, CareerDifficulty.Hardcore, "硬核");

            Image startPanel = CreateUiObject<Image>("StartPanel", intelPanel.transform);
            SetRect(startPanel.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.62f, 0.16f));
            startPanel.color = Color.clear;
            BindTitleMotion(startPanel.rectTransform, 5);

            CreateTitleHairlineButton(
                "StartCareer", "選擇俱樂部", startPanel.transform,
                new Vector2(0f, 0.1f), new Vector2(1f, 0.9f),
                OpenClubBriefing, 22);

            intelPanel.transform.SetAsLastSibling();
            BuildTitleHoverPopup(canvas);
            BuildClubBriefingOverlay(canvas);
            RefreshTitleChoices();
            StartCoroutine(DeferredRefreshTitleFrost());
        }

        private System.Collections.IEnumerator DeferredRefreshTitleFrost()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            if (_titleRoot != null && _titleRoot.activeInHierarchy)
                RefreshFrostedGlass(_titleFrostHost, _titleRoot.transform);
        }

        private void BuildClubChoice(Transform parent, float index, string clubId, string label)
        {
            float y1 = 0.92f - index * 0.42f;
            float y0 = y1 - 0.36f;

            Image edge = CreateUiObject<Image>("Club_" + clubId, parent);
            SetRect(edge.rectTransform, new Vector2(0f, y0), new Vector2(1f, y1));
            edge.color = Color.clear;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.color = Color.clear;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            string captured = clubId;
            button.onClick.AddListener(() =>
            {
                if (string.Equals(_selectedClubId, captured, StringComparison.OrdinalIgnoreCase))
                    return;
                _selectedClubId = captured;
                RefreshTitleChoices();
                PlayIntelSettle();
            });

            Image logo = CreateUiObject<Image>("Logo", face.transform);
            SetRect(logo.rectTransform, new Vector2(0f, 0.15f), new Vector2(0.16f, 0.85f));
            logo.sprite = LoadClubLogo(clubId);
            logo.preserveAspect = true;
            logo.color = Color.white;
            logo.raycastTarget = false;

            Text text = CreateText("Label", face.transform, 22, TextAnchor.MiddleLeft, TitleIndexMuted, _uiFontBlack);
            SetRect(text.rectTransform, new Vector2(0.20f, 0.1f), new Vector2(0.96f, 0.9f));
            text.text = label;
            text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 22;

            Image mark = CreateUiObject<Image>("SelectMark", face.transform);
            SetRect(mark.rectTransform, new Vector2(0f, 0.08f), new Vector2(0.12f, 0.16f));
            mark.color = CareerEditorialChrome.Mustard;
            mark.raycastTarget = false;

            _clubIds.Add(clubId);
            _clubFaces.Add(face);
            _clubLabels.Add(text);
            _clubLogoImages.Add(logo);
            _clubSelectMarks.Add(mark);
        }

        private void BuildDifficultyChoice(Transform parent, float index, CareerDifficulty difficulty, string label)
        {
            float y1 = 0.68f - index * 0.22f;
            float y0 = y1 - 0.20f;
            bool available = difficulty == CareerDifficulty.Normal;

            Image edge = CreateUiObject<Image>("Diff_" + difficulty, parent);
            SetRect(edge.rectTransform, new Vector2(0f, y0), new Vector2(0.7f, y1));
            edge.color = Color.clear;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.color = Color.clear;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.interactable = available;
            if (available)
            {
                CareerDifficulty captured = difficulty;
                button.onClick.AddListener(() =>
                {
                    _newDifficulty = captured;
                    RefreshTitleChoices();
                });
            }

            Text text = CreateText("Label", face.transform, 22, TextAnchor.MiddleLeft,
                available ? CareerEditorialChrome.StageType : TitleIndexMuted, _uiFontBlack);
            SetRect(text.rectTransform, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));
            text.text = label;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (!available)
            {
                text.color = TitleIndexMuted;

                Text unavailable = CreateText(
                    "Unavailable", face.transform, 10, TextAnchor.MiddleCenter,
                    new Color(0.45f, 0.46f, 0.50f, 1f), _uiFont700);
                SetRect(unavailable.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.40f));
                unavailable.text = "待開發，敬請期待";
                unavailable.resizeTextForBestFit = true;
                unavailable.resizeTextMinSize = 7;
                unavailable.resizeTextMaxSize = 10;
            }

            _difficultyFaces.Add(face);
            _difficultyLabels.Add(text);

            EventTrigger trigger = edge.gameObject.AddComponent<EventTrigger>();
            CareerDifficulty hoverDifficulty = difficulty;
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => OnDifficultyPointerEnter(hoverDifficulty));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnDifficultyPointerExit());
        }

        /// <summary>Make label occupy ~heightFill of the button face and scale up to fill it.</summary>
        private static void ApplyFilledButtonLabel(Text text, int maxSize = 48, float heightFill = 0.9f)
        {
            if (text == null) return;
            text.fontStyle = FontStyle.Bold;
            float yInset = Mathf.Clamp01((1f - Mathf.Clamp(heightFill, 0.2f, 1f)) * 0.5f);
            SetRect(text.rectTransform, new Vector2(0.05f, yInset), new Vector2(0.95f, 1f - yInset));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = Mathf.Max(maxSize, text.fontSize);
        }

        private void OnEraButtonClicked()
        {
            _selectedEraId = "2021_01_01";
            _status = "目前僅開放時代：2021.1.1";
            RefreshTitleChoices();
        }

        private void RefreshTitleChoices()
        {
            for (int i = 0; i < _clubFaces.Count; i++)
            {
                bool on = string.Equals(_clubIds[i], _selectedClubId, StringComparison.OrdinalIgnoreCase);
                _clubFaces[i].color = Color.clear;
                Image clubEdge = _clubFaces[i].transform.parent != null
                    ? _clubFaces[i].transform.parent.GetComponent<Image>()
                    : null;
                if (clubEdge != null)
                    clubEdge.color = Color.clear;
                _clubLabels[i].color = on ? CareerEditorialChrome.StageType : TitleIndexMuted;
                _clubLabels[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
                if (i < _clubSelectMarks.Count && _clubSelectMarks[i] != null)
                    _clubSelectMarks[i].gameObject.SetActive(on);
                if (i < _clubLogoImages.Count && _clubLogoImages[i] != null)
                    _clubLogoImages[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            }

            CareerDifficulty[] difficulties =
                { CareerDifficulty.Normal, CareerDifficulty.Hard, CareerDifficulty.Hardcore };
            for (int i = 0; i < _difficultyFaces.Count && i < difficulties.Length; i++)
            {
                bool available = difficulties[i] == CareerDifficulty.Normal;
                Image diffEdge = _difficultyFaces[i].transform.parent != null
                    ? _difficultyFaces[i].transform.parent.GetComponent<Image>()
                    : null;
                if (!available)
                {
                    _difficultyFaces[i].color = Color.clear;
                    if (diffEdge != null)
                        diffEdge.color = Color.clear;
                    _difficultyLabels[i].color = TitleIndexMuted;
                    continue;
                }
                bool on = _newDifficulty == difficulties[i];
                _difficultyFaces[i].color = Color.clear;
                if (diffEdge != null)
                    diffEdge.color = Color.clear;
                _difficultyLabels[i].color = on ? CareerEditorialChrome.StageType : TitleIndexMuted;
                _difficultyLabels[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }

            if (_titleSelectedLogo != null)
                _titleSelectedLogo.sprite = LoadClubLogo(_selectedClubId);
            if (_titleSelectedName != null)
            {
                _titleSelectedName.text = ClubDisplayName(_selectedClubId);
                _titleSelectedName.color = CareerEditorialChrome.StageType;
            }
            if (_titleNameGhost != null)
                _titleNameGhost.text = ClubDisplayName(_selectedClubId);
            if (_titleSelectedFullName != null)
            {
                _titleSelectedFullName.text = ClubFullDisplayName(_selectedClubId);
                _titleSelectedFullName.color = CareerEditorialChrome.StageType;
            }
            if (_titleSelectedRegion != null)
            {
                _titleSelectedRegion.text = ClubRegionLabel(_selectedClubId);
                _titleSelectedRegion.color = CareerEditorialChrome.StageMuted;
            }
            if (_titleEraButtonLabel != null)
                _titleEraButtonLabel.text = EraButtonLabel(_selectedEraId);
            if (_titleEraIntro != null)
            {
                NarrativeUiText.Set(_titleEraIntro, EraIntroPlaceholder(_selectedEraId));
                ResizeTitleEraIntroContent();
            }
            RefreshTitleHeaderStats(_selectedClubId);
            RebuildTitleRoster(_selectedClubId);
            HideTitleHoverPopup();
        }

        private void RefreshTitleHeaderStats(string clubId)
        {
            if (_content == null || !_content.Clubs.TryGetValue(clubId, out CareerClubDefinition club))
            {
                if (_titleCashText != null) _titleCashText.text = "—";
                if (_titleFansText != null) _titleFansText.text = "—";
                if (_titlePrestigeText != null) _titlePrestigeText.text = "—";
                if (_titleFameText != null) _titleFameText.text = "—";
                if (_titleRankText != null) _titleRankText.text = "#—";
                return;
            }

            // Match CareerSession.CreateNew starting values.
            int cash = club.startingCash > 0 ? club.startingCash : _content.Balance.startingCash;
            int fame = club.fame;
            int fans = 15000 + club.fame * 1000;
            // Opening save seeds exposure from fame; they diverge in-career.
            int prestige = club.fame;
            int rank = StartingRankFor(clubId);

            if (_titleCashText != null)
            {
                _titleCashText.text = $"${cash:N0}";
                _titleCashText.color = cash < 0 ? Danger : TextPrimary;
            }
            if (_titleFansText != null)
                _titleFansText.text = fans.ToString("N0");
            if (_titlePrestigeText != null)
                _titlePrestigeText.text = prestige.ToString();
            if (_titleFameText != null)
                _titleFameText.text = fame.ToString();
            if (_titleRankText != null)
            {
                _titleRankText.text = rank > 0 ? $"#{rank}" : "#—";
                _titleRankText.color = RankGold;
            }
        }

        private int HltvPointsFor(string clubId)
        {
            if (_session?.State != null)
            {
                RankingEntry entry = _session.State.rankings.FirstOrDefault(x =>
                    string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                    return (int)Math.Round(RankingRules.ClampHltvPoints(
                        WorldPointsLabel(clubId, entry, _session.State)));
            }

            if (_content != null &&
                _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                club.hltvPoints > 0)
                return (int)Math.Round(RankingRules.ClampHltvPoints(club.hltvPoints));
            return 0;
        }

        private bool UsesHltvWorldRankSnapshot(CareerState state)
        {
            if (_content == null || state == null) return true;
            if (state.rankingBatches == null || state.rankingBatches.Count == 0)
                return true;
            if (!state.rankingBatches.All(x => x.seedSnapshot || (x.earnedHour == 0 && string.IsNullOrEmpty(x.competitionId))))
                return false;
            foreach (RankingEntry entry in state.rankings)
            {
                if (!_content.Clubs.TryGetValue(entry.clubId, out CareerClubDefinition club) ||
                    club.hltvPoints <= 0)
                    continue;
                if (Mathf.Abs(entry.points - club.hltvPoints) > 0.01f)
                    return false;
            }
            return true;
        }

        private int WorldRankLabel(string clubId, RankingEntry entry, CareerState state)
        {
            if (_content != null &&
                _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                club.hltvRank > 0 &&
                UsesHltvWorldRankSnapshot(state))
                return club.hltvRank;
            return entry?.rank ?? 0;
        }

        private float WorldPointsLabel(string clubId, RankingEntry entry, CareerState state)
        {
            if (_content != null &&
                _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                club.hltvPoints > 0 &&
                UsesHltvWorldRankSnapshot(state))
                return RankingRules.ClampHltvPoints(club.hltvPoints);
            return RankingRules.ClampHltvPoints(entry?.points ?? 0f);
        }

        private int StartingRankFor(string clubId)
        {
            if (_content == null || string.IsNullOrEmpty(clubId)) return 0;
            if (_content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) && club.hltvRank > 0)
                return club.hltvRank;
            return 0;
        }

        private void RebuildTitleRoster(string clubId) =>
            RebuildClubIntelRoster(_titleRosterRoot, _titleRosterRows, clubId, includePlayerLoreInHover: true);

        private void RebuildClubIntelRoster(
            RectTransform rosterRoot,
            List<GameObject> rowStorage,
            string clubId,
            bool includePlayerLoreInHover)
        {
            if (rosterRoot == null)
                return;

            foreach (GameObject row in rowStorage)
            {
                if (row != null)
                    Destroy(row);
            }
            rowStorage.Clear();
            CancelTitleHover();

            string[] ids = GetClubRosterPlayerIds(clubId);
            if (ids.Length == 0)
                return;

            for (int i = 0; i < ids.Length; i++)
            {
                string playerId = ids[i];
                if (!_content.Players.TryGetValue(playerId, out CareerPlayerDefinition player))
                    continue;

                float rowH = 1f / Math.Max(5, ids.Length);
                float y1 = 1f - i * rowH;
                float y0 = y1 - rowH;

                Image row = CreateUiObject<Image>("Player_" + playerId, rosterRoot);
                SetRect(row.rectTransform, new Vector2(0.02f, y0 + 0.02f * rowH), new Vector2(0.98f, y1 - 0.02f * rowH));
                row.color = i % 2 == 0
                    ? CareerEditorialChrome.Paper
                    : new Color(0.90f, 0.86f, 0.78f, 1f);
                row.raycastTarget = false;

                Text name = CreateText("Name", row.transform, 16, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontBlack);
                SetRect(name.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.40f, 0.88f));
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.text = player.displayName;
                name.raycastTarget = true;

                Text role = CreateText("Role", row.transform, 14, TextAnchor.MiddleLeft, CareerEditorialChrome.InkMuted, _uiFont700);
                SetRect(role.rectTransform, new Vector2(0.42f, 0.12f), new Vector2(0.68f, 0.88f));
                role.horizontalOverflow = HorizontalWrapMode.Wrap;
                role.verticalOverflow = VerticalWrapMode.Truncate;
                string roleLabel = RoleLabel(player.role);
                if (ClubEstateRules.IsReserveSquadStatus(player.squadStatus))
                    roleLabel = "替補 · " + roleLabel;
                role.text = roleLabel;
                role.raycastTarget = false;

                PlayerDefinition match = null;
                if (_matchContent != null)
                    _matchContent.Players.TryGetValue(playerId, out match);
                string historicalRaw = PlayerStandingRules.ResolveRaw(match?.historicalStatus, match?.evaluation);
                string currentRaw = PlayerStandingRules.ResolveRaw(match?.currentStatus, match?.evaluation);
                if (string.IsNullOrEmpty(historicalRaw) && string.IsNullOrEmpty(currentRaw))
                {
                    string fromFame = PlayerStandingRules.FromFame(player.fame);
                    historicalRaw = fromFame;
                    currentRaw = fromFame;
                }
                else if (string.IsNullOrEmpty(historicalRaw))
                    historicalRaw = currentRaw;
                else if (string.IsNullOrEmpty(currentRaw))
                    currentRaw = historicalRaw;

                // Historical left, current right — leave clear gap between the two icons.
                PlaceStandingPair(row.transform, historicalRaw, currentRaw, 0.78f, 0.92f, 22f);

                // 選手資訊只綁名字；地位圖示另有獨立說明，互不疊加。
                EventTrigger trigger = name.gameObject.AddComponent<EventTrigger>();
                string captured = playerId;
                bool includeLore = includePlayerLoreInHover;
                AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ =>
                {
                    HideStatTooltip();
                    OnClubIntelPlayerPointerEnter(captured, includeLore);
                });
                AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnTitlePlayerPointerExit());

                rowStorage.Add(row.gameObject);
            }
        }

        private string[] GetClubRosterPlayerIds(string clubId)
        {
            if (_content == null || string.IsNullOrEmpty(clubId))
                return Array.Empty<string>();

            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(string id)
            {
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    return;
                ordered.Add(id);
            }

            if (_content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                club.playerIds != null)
            {
                for (int i = 0; i < club.playerIds.Length; i++)
                    TryAdd(club.playerIds[i]);
            }

            if (_session?.State?.players != null)
            {
                foreach (PlayerCareerState player in _session.State.players)
                {
                    if (player == null) continue;
                    if (!string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    TryAdd(player.playerId);
                }
            }
            else
            {
                foreach (CareerPlayerDefinition player in _content.Players.Values)
                {
                    if (player == null) continue;
                    if (!string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    TryAdd(player.id);
                }
            }

            return ordered.ToArray();
        }

        private void OnClubIntelPlayerPointerEnter(string playerId, bool includeLore)
        {
            _titlePlayerHoverIncludeLore = includeLore;
            OnTitlePlayerPointerEnter(playerId);
        }

        private void BuildTitleHoverPopup(Transform canvas)
        {
            _titleHoverPopup = CreateUiObject<Image>("TitlePlayerHover", canvas);
            _titleHoverPopup.color = new Color(Panel.r, Panel.g, Panel.b, 0.98f);
            _titleHoverPopup.raycastTarget = false;
            RectTransform rect = _titleHoverPopup.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(520f, 520f);

            Image edge = CreateUiObject<Image>("Edge", _titleHoverPopup.transform);
            Stretch(edge.rectTransform);
            edge.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            edge.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            edge.color = PanelInner;
            edge.raycastTarget = false;

            Image accentBar = CreateUiObject<Image>("AccentBar", edge.transform);
            SetRect(accentBar.rectTransform, new Vector2(0f, 0.985f), new Vector2(1f, 1f));
            accentBar.color = Accent;
            accentBar.raycastTarget = false;

            Text caption = CreateText("Caption", edge.transform, 12, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(caption.rectTransform, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.98f));
            caption.text = "選手資料";
            caption.raycastTarget = false;

            _titleHoverName = CreateText("Name", edge.transform, 22, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_titleHoverName.rectTransform, new Vector2(0.04f, 0.855f), new Vector2(0.62f, 0.925f));
            _titleHoverName.raycastTarget = false;

            _titleHoverMeta = CreateText("Meta", edge.transform, 13, TextAnchor.MiddleRight, TextMuted, _uiFont700);
            SetRect(_titleHoverMeta.rectTransform, new Vector2(0.62f, 0.855f), new Vector2(0.96f, 0.925f));
            _titleHoverMeta.raycastTarget = false;

            Image divider = CreateUiObject<Image>("Divider", edge.transform);
            SetRect(divider.rectTransform, new Vector2(0.04f, 0.845f), new Vector2(0.96f, 0.848f));
            divider.color = AccentDim;
            divider.raycastTarget = false;

            Image contentHost = CreateUiObject<Image>("Content", edge.transform);
            SetRect(contentHost.rectTransform, new Vector2(0.03f, 0.025f), new Vector2(0.97f, 0.835f));
            contentHost.color = Color.clear;
            contentHost.raycastTarget = false;
            _titleHoverContent = contentHost.rectTransform;

            _titleHoverPopup.gameObject.SetActive(false);
        }

        private void BuildStatTooltipPopup(Transform canvas)
        {
            _statTooltipRoot = CreateUiObject<Image>("HeaderStatTooltip", canvas);
            _statTooltipRoot.color = new Color(Panel.r, Panel.g, Panel.b, 0.98f);
            _statTooltipRoot.raycastTarget = false;
            RectTransform rect = _statTooltipRoot.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(360f, 210f);

            Image edge = CreateUiObject<Image>("Edge", _statTooltipRoot.transform);
            Stretch(edge.rectTransform);
            edge.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            edge.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            edge.color = PanelInner;
            edge.raycastTarget = false;

            Image accentBar = CreateUiObject<Image>("AccentBar", edge.transform);
            SetRect(accentBar.rectTransform, new Vector2(0f, 0.975f), new Vector2(1f, 1f));
            accentBar.color = Accent;
            accentBar.raycastTarget = false;

            _statTooltipTitle = CreateText("Title", edge.transform, 16, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_statTooltipTitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
            _statTooltipTitle.supportRichText = true;
            _statTooltipTitle.raycastTarget = false;

            Image divider = CreateUiObject<Image>("Divider", edge.transform);
            SetRect(divider.rectTransform, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.76f));
            divider.color = AccentDim;
            divider.raycastTarget = false;

            _statTooltipBody = CreateText("Body", edge.transform, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_statTooltipBody.rectTransform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.72f));
            _statTooltipBody.alignment = TextAnchor.UpperLeft;
            _statTooltipBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statTooltipBody.verticalOverflow = VerticalWrapMode.Truncate;
            _statTooltipBody.supportRichText = true;
            NarrativeUiText.ApplyBodyStyle(_statTooltipBody);
            _statTooltipBody.raycastTarget = false;

            _statTooltipRoot.gameObject.SetActive(false);
        }

        private void OnDifficultyPointerEnter(CareerDifficulty difficulty)
        {
            CancelDifficultyHover();
            HideStatTooltip();
            _difficultyHoverTarget = difficulty;
            _difficultyHoverRoutine = StartCoroutine(ShowDifficultyTooltipAfterDelay(difficulty));
        }

        private void OnDifficultyPointerExit()
        {
            CancelDifficultyHover();
            HideStatTooltip();
        }

        private IEnumerator ShowDifficultyTooltipAfterDelay(CareerDifficulty difficulty)
        {
            yield return new WaitForSecondsRealtime(DifficultyHoverDelaySeconds);
            if (_difficultyHoverTarget != difficulty)
                yield break;
            ShowPlainTooltip(
                CareerDifficultyRules.DisplayName(difficulty),
                CareerDifficultyRules.HoverDescription(difficulty),
                new Vector2(400f, 280f),
                Input.mousePosition);
            _difficultyHoverRoutine = null;
        }

        private void CancelDifficultyHover()
        {
            if (_difficultyHoverRoutine != null)
            {
                StopCoroutine(_difficultyHoverRoutine);
                _difficultyHoverRoutine = null;
            }
            _difficultyHoverTarget = null;
        }

        private void ShowPlainTooltip(string title, string body, Vector2 size, Vector2 screenPosition)
        {
            if (_statTooltipRoot == null)
                return;
            _statTooltipTitle.text = title;
            NarrativeUiText.Set(_statTooltipBody, body);
            _statTooltipRoot.rectTransform.sizeDelta = size;
            _statTooltipBody.verticalOverflow = VerticalWrapMode.Overflow;
            _statTooltipRoot.gameObject.SetActive(true);
            _statTooltipRoot.transform.SetAsLastSibling();
            PositionStatTooltip(screenPosition);
        }

        private void OnHeaderStatPointerEnter(string key)
        {
            if (!_careerStarted || _session == null) return;
            if (_titleRoot != null && _titleRoot.activeSelf) return;
            CancelStatTooltip();
            HideStatTooltip();
            _statTooltipKey = key;
            _statTooltipRoutine = StartCoroutine(ShowStatTooltipAfterDelay(key));
        }

        private void OnHeaderStatPointerExit()
        {
            CancelStatTooltip();
            HideStatTooltip();
        }

        private IEnumerator ShowStatTooltipAfterDelay(string key)
        {
            yield return new WaitForSecondsRealtime(HeaderStatHoverDelaySeconds);
            if (!string.Equals(_statTooltipKey, key, StringComparison.OrdinalIgnoreCase))
                yield break;
            ShowStatTooltip(key, Input.mousePosition);
            _statTooltipRoutine = null;
        }

        private void CancelStatTooltip()
        {
            if (_statTooltipRoutine != null)
            {
                StopCoroutine(_statTooltipRoutine);
                _statTooltipRoutine = null;
            }
            _statTooltipKey = null;
        }

        private void HideStatTooltip()
        {
            if (_statTooltipRoot != null)
                _statTooltipRoot.gameObject.SetActive(false);
        }

        private void ShowStatTooltip(string key, Vector2 screenPosition)
        {
            if (_statTooltipRoot == null || _session == null) return;
            if (!TryBuildHeaderStatTooltip(key, out string title, out string body))
                return;

            _statTooltipTitle.text = title;
            NarrativeUiText.Set(_statTooltipBody, body);
            _statTooltipBody.verticalOverflow = VerticalWrapMode.Overflow;
            Canvas.ForceUpdateCanvases();

            const float width = 440f;
            float bodyHeight = Mathf.Max(80f, _statTooltipBody.preferredHeight);
            // Title band + divider + padding; grow with body so long copy is not clipped.
            float height = Mathf.Clamp(70f + bodyHeight + 28f, 220f, 620f);
            _statTooltipRoot.rectTransform.sizeDelta = new Vector2(width, height);
            _statTooltipRoot.gameObject.SetActive(true);
            _statTooltipRoot.transform.SetAsLastSibling();
            PositionStatTooltip(screenPosition);
        }

        private void PositionStatTooltip(Vector2 screenPosition)
        {
            if (_statTooltipRoot == null || _canvasRect == null) return;
            RectTransform rect = _statTooltipRoot.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPosition, null, out Vector2 local))
                return;

            float pad = 16f;
            Vector2 size = rect.sizeDelta;
            Vector2 pos = local + new Vector2(14f, -10f);
            Vector2 canvasSize = _canvasRect.rect.size;
            float minX = -canvasSize.x * 0.5f + pad;
            float maxX = canvasSize.x * 0.5f - size.x - pad;
            float minY = -canvasSize.y * 0.5f + size.y + pad;
            float maxY = canvasSize.y * 0.5f - pad;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            rect.anchoredPosition = pos;
        }

        private bool TryBuildHeaderStatTooltip(string key, out string title, out string body)
        {
            title = null;
            body = null;
            if (_session == null) return false;

            ClubState club = _session.State.Club(_session.State.playerClubId);
            switch (key)
            {
                case "cash":
                {
                    MonthlyBudgetPreview budget = _session.GetMonthlyBudgetPreview(club.clubId);
                    title = "現金";
                    body = BuildCashHeaderTooltip(club, budget);
                    return true;
                }
                case "fans":
                    title = "粉絲";
                    body = BuildFansHeaderTooltip(club);
                    return true;
                case "prestige":
                    title = "威望";
                    body = BuildPrestigeHeaderTooltip(club);
                    return true;
                case "fame":
                    title = "名氣";
                    body = BuildFameHeaderTooltip(club);
                    return true;
                case "rank":
                {
                    title = "世界排名";
                    body = BuildRankHeaderTooltip(club);
                    return true;
                }
                default:
                    return false;
            }
        }

        private string BuildCashHeaderTooltip(ClubState club, MonthlyBudgetPreview budget)
        {
            var sb = new StringBuilder();
            sb.AppendLine("俱樂部可用資金，用於薪資、轉會與賽事報名。");
            sb.AppendLine();
            sb.AppendLine($"目前：${club.cash:N0}");
            sb.AppendLine();
            sb.AppendLine("本月收支：");
            if (budget.sponsorIncome != 0)
                AppendSignedModifierLine(sb, "贊助月費", budget.sponsorIncome);
            if (budget.merchandiseIncome != 0)
                AppendSignedModifierLine(sb, "商品銷售", budget.merchandiseIncome);
            if (budget.prizeIncome != 0)
                AppendSignedModifierLine(sb, "賽事獎金", budget.prizeIncome);
            if (budget.salaryExpense != 0)
                AppendSignedModifierLine(sb, "選手薪資", -budget.salaryExpense);
            if (budget.coachSalaryExpense != 0)
                AppendSignedModifierLine(sb, "教練組薪資", -budget.coachSalaryExpense);
            sb.AppendLine();
            sb.Append($"合計：{FormatSignedRich(budget.Net)}");
            if (club.consecutiveLossMonths >= 2)
            {
                sb.AppendLine();
                sb.AppendLine();
                AppendModifierNote(sb, false, "連續虧損",
                    $"已連續 {club.consecutiveLossMonths} 個月淨值為負，預算與引援空間可能收緊。");
            }
            return sb.ToString();
        }

        private string BuildFansHeaderTooltip(ClubState club)
        {
            var sb = new StringBuilder();
            sb.AppendLine("支持者規模，直接影響商品銷售收入。");
            sb.AppendLine();
            sb.AppendLine($"目前：{club.fans:N0}");
            sb.AppendLine();
            sb.AppendLine("現行修正：");
            int stars = _session.State.players.Count(x =>
                string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase) && x.fame >= 80);
            if (ClubReputationRules.IsHigh(club.fame))
                AppendModifierNote(sb, true, "高名氣", "較易維持並擴大支持者基數，商品銷售也較熱絡。");
            else if (ClubReputationRules.IsLow(club.fame))
                AppendModifierNote(sb, false, "低名氣", "支持者成長偏慢，商品與媒體曝光較難帶動粉絲增長。");
            else
                AppendModifierNote(sb, true, "名氣中性", "粉絲增減主要來自賽事成績與社群活動，無額外名氣門檻修正。");

            if (stars > 0)
                AppendModifierNote(sb, true, "明星陣容", $"目前有 {stars} 名高名氣選手，有利吸引與留住支持者。");

            AppendModifierNote(sb, true, "賽事與活動", "正式賽成績、社群經營與劇本事件會增減粉絲；月結本身不套用固定衰減。");
            return sb.ToString();
        }

        private string BuildPrestigeHeaderTooltip(ClubState club)
        {
            var sb = new StringBuilder();
            sb.AppendLine("職業圈對俱樂部的觀感與聲譽，影響邀請賽、引援吸引力與贊助滿意度。");
            sb.AppendLine();
            sb.AppendLine($"目前：{club.exposure}");
            sb.AppendLine();
            sb.AppendLine("現行修正：");
            if (ClubReputationRules.IsHigh(club.exposure))
                AppendModifierNote(sb, true, "高威望", "贊助月費條件較佳，邀請賽與引援談判較有利。");
            else if (ClubReputationRules.IsLow(club.exposure))
                AppendModifierNote(sb, false, "低威望", "贊助滿意度較易下滑，邀約與談判條件較差。");
            else
                AppendModifierNote(sb, true, "威望中性", "無高低威望門檻修正，變動主要來自賽事表現與事件。");

            AppendModifierNote(sb, true, "賽事表現", "正式賽勝負會提高威望；較佳成績帶來的增幅通常較高。");
            int sponsors = _session.State.sponsors.Count(x =>
                string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase));
            if (sponsors > 0)
                AppendModifierNote(sb, true, "贊助檢核", "每月結算時，威望若低於合約門檻會降低贊助滿意度。");
            return sb.ToString();
        }

        private string BuildFameHeaderTooltip(ClubState club)
        {
            var sb = new StringBuilder();
            sb.AppendLine("大眾知名度與辨識度，影響商品銷售、媒體關注與贊助能見度。");
            sb.AppendLine();
            sb.AppendLine($"目前：{club.fame}");
            sb.AppendLine();
            sb.AppendLine("現行修正：");
            if (ClubReputationRules.IsHigh(club.fame))
                AppendModifierNote(sb, true, "高名氣", "商品銷售係數提高，贊助與媒體相關收入較易成長。");
            else if (ClubReputationRules.IsLow(club.fame))
                AppendModifierNote(sb, false, "低名氣", "商品銷售係數降低，曝光與合作邀約成長較慢。");
            else
                AppendModifierNote(sb, true, "名氣中性", "無名氣門檻加成或減益，變動主要來自賽事與社群事件。");

            int stars = _session.State.players.Count(x =>
                string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase) && x.fame >= 80);
            if (stars > 0)
                AppendModifierNote(sb, true, "明星選手", $"陣中有 {stars} 名個人名氣達標選手，提高商品銷售預估。");

            AppendModifierNote(sb, true, "賽事與社群", "比賽曝光、社群活動與劇本事件會增減名氣。");
            return sb.ToString();
        }

        private string BuildRankHeaderTooltip(ClubState club)
        {
            int rank = RankFor(club.clubId);
            RankingEntry entry = _session.State.rankings
                .FirstOrDefault(x => string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase));
            string pts = entry != null
                ? $"{WorldPointsLabel(club.clubId, entry, _session.State):0}"
                : "—";
            string recent = entry != null
                ? $"{RankingRules.ClampHltvPoints(entry.recentPoints):0}"
                : "—";
            int halfLifeWeeks = CareerCalendar.ResolveRankingHalfLifeHours(_content.Balance) /
                                CareerCalendar.HoursPerWeek;

            var sb = new StringBuilder();
            sb.AppendLine("依 52 週窗口內、帶時間衰減的 HLTV points 排序，積分上限 1000。");
            sb.AppendLine("世界排名每七天更新一次；賽事積分會先入帳，下次更新時才反映在名次上。");
            sb.AppendLine();
            sb.AppendLine($"目前：{(rank > 0 ? $"#{rank}" : "#—")} · HLTV points {pts} · 近期分數 {recent}");
            sb.AppendLine();
            sb.AppendLine("積分變動：");
            AppendModifierNote(sb, true, "賽事名次", "正式賽名次會增加 HLTV points；Major 與 Super-Elite 權重較高。");
            AppendModifierNote(sb, true, "淘汰賽勝場", "淘汰賽勝利另有少量積分加成。");
            AppendModifierNote(sb, false, "時間衰減",
                $"既有積分依約 {halfLifeWeeks} 週半衰期遞減；近期分數統計過去 13 週有效分數。");
            return sb.ToString();
        }

        private void AppendSignedModifierLine(StringBuilder sb, string label, int signedAmount)
        {
            sb.AppendLine($"{FormatSignedRich(signedAmount)}  {label}");
        }

        private void AppendModifierNote(StringBuilder sb, bool buff, string title, string detail)
        {
            Color color = buff ? Positive : Danger;
            string sign = buff ? "+" : "−";
            sb.AppendLine(
                $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{sign}{title}</color>：{detail}");
        }

        private static string FormatSignedRich(int amount)
        {
            if (amount > 0)
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Positive)}>+${amount:N0}</color>";
            if (amount < 0)
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Danger)}>-${Math.Abs(amount):N0}</color>";
            return "$0";
        }

        private static void AddPointerTrigger(
            EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void OnTitlePlayerPointerEnter(string playerId)
        {
            CancelTitleHover();
            HideTitleHoverPopup();
            _titleHoverPlayerId = playerId;
            _titleHoverRoutine = StartCoroutine(ShowTitleHoverAfterDelay(playerId));
        }

        private void OnTitlePlayerPointerExit()
        {
            CancelTitleHover();
            HideTitleHoverPopup();
        }

        private IEnumerator ShowTitleHoverAfterDelay(string playerId)
        {
            yield return new WaitForSecondsRealtime(TitleProfileHoverDelaySeconds);
            if (!string.Equals(_titleHoverPlayerId, playerId, StringComparison.OrdinalIgnoreCase))
                yield break;
            ShowTitleHoverPopup(playerId, Input.mousePosition);
            _titleHoverRoutine = null;
        }

        private void CancelTitleHover()
        {
            if (_titleHoverRoutine != null)
            {
                StopCoroutine(_titleHoverRoutine);
                _titleHoverRoutine = null;
            }
            _titleHoverPlayerId = null;
        }

        private void ShowTitleHoverPopup(string playerId, Vector2 screenPosition)
        {
            if (_titleHoverPopup == null || _content == null) return;
            if (!_content.Players.TryGetValue(playerId, out CareerPlayerDefinition player))
                return;

            PlayerDefinition match = null;
            if (_matchContent != null)
                _matchContent.Players.TryGetValue(playerId, out match);

            _titleHoverName.text = player.displayName;
            string role = RoleLabel(match?.primaryRole ?? player.role);
            _titleHoverMeta.text = role;
            RebuildTitleHoverContent(player.id, match);
            bool hasLore = _titlePlayerHoverIncludeLore &&
                           !string.IsNullOrWhiteSpace(PlayerLoreBlurb(player.id));
            _titleHoverPopup.rectTransform.sizeDelta = hasLore
                ? new Vector2(560f, 820f)
                : new Vector2(520f, 560f);
            _titleHoverPopup.gameObject.SetActive(true);
            _titleHoverPopup.transform.SetAsLastSibling();
            PositionTitleHoverPopup(screenPosition);
        }

        private void HideTitleHoverPopup()
        {
            if (_titleHoverPopup != null)
                _titleHoverPopup.gameObject.SetActive(false);
        }

        private void PositionTitleHoverPopup(Vector2 screenPosition)
        {
            if (_titleHoverPopup == null || _canvasRect == null) return;
            RectTransform rect = _titleHoverPopup.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, null, out Vector2 localPoint);

            Vector2 size = rect.sizeDelta;
            Vector2 canvasSize = _canvasRect.rect.size;
            float minX = -canvasSize.x * 0.5f + 8f;
            float maxX = canvasSize.x * 0.5f - size.x - 8f;
            float minY = -canvasSize.y * 0.5f + size.y + 8f;
            float maxY = canvasSize.y * 0.5f - 8f;
            localPoint.x = Mathf.Clamp(localPoint.x + 18f, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y - 12f, minY, maxY);
            rect.anchoredPosition = localPoint;
        }

        private void RebuildTitleHoverContent(string playerId, PlayerDefinition match)
        {
            if (_titleHoverContent == null) return;
            for (int i = _titleHoverContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_titleHoverContent.GetChild(i).gameObject);

            string lore = _titlePlayerHoverIncludeLore ? PlayerLoreBlurb(playerId) : null;
            bool hasLore = !string.IsNullOrWhiteSpace(lore);

            // Keep combat / role / map tall enough for section titles + grade/proficiency values.
            // Lore is compressed; featured blurbs rarely fill a large strip.
            float combatMinY = hasLore ? 0.76f : 0.70f;
            float roleMinY = hasLore ? 0.54f : 0.38f;
            float roleMaxY = hasLore ? 0.74f : 0.68f;
            float mapMinY = hasLore ? 0.28f : 0.02f;
            float mapMaxY = hasLore ? 0.52f : 0.36f;
            float loreMaxY = 0.26f;

            PlaceTitleHoverSection(
                _titleHoverContent, "戰鬥屬性",
                new Vector2(0f, combatMinY), new Vector2(1f, 1f),
                new[] { "瞄準", "反應", "意識", "道具", "配合", "溝通", "首殺", "殘局" },
                match?.attributes != null
                    ? new[]
                    {
                        match.attributes.aim.ToString(), match.attributes.reaction.ToString(),
                        match.attributes.awareness.ToString(), match.attributes.utility.ToString(),
                        match.attributes.teamwork.ToString(), match.attributes.communication.ToString(),
                        match.attributes.opening.ToString(), match.attributes.clutch.ToString()
                    }
                    : FillMissing(8),
                4);

            PlaceTitleHoverSection(
                _titleHoverContent, "位置適性",
                new Vector2(0f, roleMinY), new Vector2(1f, roleMaxY),
                new[] { "突破", "狙擊", "步槍", "自由人", "輔助", "指揮" },
                BuildRoleFitnessValues(match),
                3);

            PlaceTitleHoverSection(
                _titleHoverContent, "地圖適性",
                new Vector2(0f, mapMinY), new Vector2(1f, mapMaxY),
                MapOrder,
                BuildMapFitnessValues(match),
                4);

            if (!hasLore)
                return;

            Image lorePanel = CreateUiObject<Image>("Lore", _titleHoverContent);
            SetRect(lorePanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, loreMaxY));
            UiLightChromeUtility.ApplySoftNavyPanel(lorePanel);
            lorePanel.raycastTarget = false;

            Text loreCaption = CreateText("LoreCaption", lorePanel.transform, 13, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(loreCaption.rectTransform, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.98f));
            loreCaption.text = "選手簡介";
            loreCaption.raycastTarget = false;

            Text loreBody = CreateText("LoreBody", lorePanel.transform, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(loreBody.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.78f));
            loreBody.alignment = TextAnchor.UpperLeft;
            loreBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            loreBody.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(loreBody);
            NarrativeUiText.Set(loreBody, lore);
            loreBody.raycastTarget = false;
        }

        private static string[] FillMissing(int count)
        {
            var parts = new string[count];
            for (int i = 0; i < count; i++) parts[i] = "—";
            return parts;
        }

        private static string StandingFromFame(int fame) =>
            PlayerStandingRules.DisplayName(PlayerStandingRules.FromFame(fame));

        /// <summary>Optional flavor text. Return null/empty for players without a featured blurb.</summary>
        private static string PlayerLoreBlurb(string playerId)
        {
            switch ((playerId ?? string.Empty).ToLowerInvariant())
            {
                case "s1mple":
                    return "被公認為CS：GO歷史上最具天賦的選手。擁有無解的瞄準能力、匪夷所思的反應速度，以及超越時代的遊戲理解。他不僅是狙擊手，更是能拿起任何槍械統治賽場的全能武器庫。2021年的s1mple正處於個人能力的絕對巔峰，他的存在本身便是對對手心理防線的持續碾壓。若說NAVI是王朝，s1mple便是那頂王冠上最耀眼的黑鑽。";
                case "electronic":
                    return "NAVI的沉默殺手與戰術副手。當s1mple以個人能力撕裂防線時，electroNic負責的是維繫整個系統的精密運轉——他的殘局處理能力堪稱藝術，總能在人數劣勢中找到不可能的解決方案。2021年的electroNic已從純粹的槍男進化為全能戰士，既能扛起隊伍前進，也能在最關鍵的時刻為s1mple創造輸出空間。";
                case "flamie":
                    return "NAVI開局先發步槍手，也是陪伴這支黑金王朝走過多年征程的元老之一。經驗深厚、槍感穩定，能在關鍵回合承擔補位與對槍責任。2021年初他仍是主力輪換的核心成員，但外界目光已開始投向青訓新星，更衣室的壓力與狀態波動正悄悄累積。";
                case "b1t":
                    return "NAVI青訓體系培養出的天才少年，開局時仍在替補席等待機會。年輕、無畏、出手果決，擁有與年齡不符的沈穩與紀律。一旦獲得穩定上場時間，他有機會釋放s1mple與electroNic的壓力，把火力點從雙核心推向更完整的體系。";
                case "boombl4":
                    return "NAVI的戰術總監與節奏掌控者。在外界眼中，他的個人數據或許不如隊友亮眼，但他肩負的是整個黑金王朝的戰略藍圖——何時加速、何時收縮、何時賭點、何時存錢，所有的宏觀決策皆出自他手。2021年的Boombl4證明了一件事：一支擁有超級巨星的隊伍若想登頂，需要一位能駕馭這些天才的指揮官，而他，正是那個人。";
                case "perfecto":
                    return "賽場上最被低估的無名英雄。Perfecto不追求華麗的擊殺數據，而是將一切奉獻給隊伍的勝利——投擲物的精準、補槍的時機、為隊友擋子彈的犧牲精神。他的存在讓NAVI的防線如銅牆鐵壁，任何試圖繞後或偷襲的企圖都會在他手中粉碎。沒有Perfecto的支撐，NAVI的明星體系將無從運轉。";
                case "sh1ro":
                    return "Gambit的王牌狙擊手與數據捕食者。不同於s1mple的狂暴壓制或ZywOo的全能統治，sh1ro的風格是「冷靜到近乎冷酷」——他總能在地圖上最意想不到的位置出現，以近乎病態的生存率與擊殺效率收割對手。2021年的sh1ro是Rating榜單上的常客，他的狙擊槍不僅是殺人利器，更是Gambit動態體系中最重要的「資訊黑洞」。";
                case "ax1le":
                    return "Gambit的步槍核心與殘局之神。擁有不可思議的瞄準天賦與比賽閱讀能力，他總能在隊伍最需要的時候站出來——無論是突破防線的關鍵擊殺，還是人數劣勢下的逆轉殘局。2021年的Ax1Le從青訓新星一躍成為世界頂級步槍手，他的成長曲線證明了Gambit青訓體系的恐怖造星能力。";
                case "hobbit":
                    return "Gambit的靈魂人物與精神支柱，也是隊內唯一一位曾捧起Major冠軍獎盃的沙場老將。2017年便隨Gambit登頂世界之巔的他，在2021年以「紅星導師」的身份回歸，率領四名自家青訓梯隊培養的少年征戰沙場。他的經驗是這支年輕隊伍最寶貴的資產——無論是關鍵局的沈穩決策，還是大賽舞台上的心理調節，HObbit的存在讓Gambit從「天才集中營」昇華為「冠軍競爭者」。";
                case "nafany":
                    return "Gambit的年輕指揮官與節奏引擎。年僅20歲便肩負起整支隊伍的戰略佈局，他的指揮風格與西歐體系截然不同——捨棄繁複的爆彈戰術，改以「動態默認」與「全域同步推進」將對手的地圖控制蠶食殆盡。2021年的nafany證明了年輕不等於稚嫩，他的大局觀與臨場應變能力，讓Gambit的每一次進攻都像一場精心計算的棋局。";
                case "interz":
                    return "Gambit的無名基石與防守中樞。在所有光芒都被sh1ro和Ax1Le吸引時，interz默默承擔著最骯髒的工作——資訊蒐集、地圖控制、補槍支援、死亡犧牲。他的數據從不華麗，但他的存在讓Gambit的體系得以完美運轉。2021年的interz是這支年輕軍團中最被低估的螺絲釘，而正是這些螺絲釘，鎖住了Gambit通往世界之巔的每一塊踏板。";
                default:
                    return null;
            }
        }

        private void BuildClubBriefingOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("ClubBriefingOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            dim.raycastTarget = true;
            _clubBriefingRoot = dim.gameObject;
            _clubBriefingRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideClubBriefing);

            Image panel = UiLightChromeUtility.CreateCard(
                "BriefingPanel", dim.transform,
                new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.90f), Panel);
            Image face = UiLightChromeUtility.CreateInsetFace(panel.transform, UiLightChromeUtility.GlassInset);

            Image accent = CreateUiObject<Image>("Accent", face.transform);
            SetRect(accent.rectTransform, new Vector2(0f, 0.975f), new Vector2(1f, 1f));
            accent.color = Accent;
            accent.raycastTarget = false;

            _clubBriefingTitle = CreateText("Title", face.transform, 24, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_clubBriefingTitle.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.72f, 0.96f));
            _clubBriefingTitle.text = "俱樂部簡報";

            _clubBriefingRegion = CreateText("Region", face.transform, 14, TextAnchor.MiddleRight, TextMuted, _uiFont700);
            SetRect(_clubBriefingRegion.rectTransform, new Vector2(0.55f, 0.88f), new Vector2(0.96f, 0.96f));
            _clubBriefingRegion.text = "地區局勢";

            Image scrollHost = CreateUiObject<Image>("ScrollHost", face.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.86f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            _clubBriefingBody = CreateText("Body", viewport.transform, 18, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            _clubBriefingBodyContent = _clubBriefingBody.rectTransform;
            _clubBriefingBodyContent.anchorMin = new Vector2(0f, 1f);
            _clubBriefingBodyContent.anchorMax = new Vector2(1f, 1f);
            _clubBriefingBodyContent.pivot = new Vector2(0.5f, 1f);
            _clubBriefingBodyContent.anchoredPosition = Vector2.zero;
            _clubBriefingBodyContent.sizeDelta = new Vector2(0f, 520f);
            _clubBriefingBody.alignment = TextAnchor.UpperLeft;
            _clubBriefingBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _clubBriefingBody.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(_clubBriefingBody);
            scroll.content = _clubBriefingBodyContent;

            // Short CTAs ~20% width; re-apply fill so Best Fit max is not capped by fontSize 18.
            Button backBtn = CreateGlassCapsuleButton("Back", "返回", face.transform,
                new Vector2(0.18f, 0.04f), new Vector2(0.38f, 0.14f), ButtonEdge, ButtonFace, HideClubBriefing, _uiFont, 18);
            Button confirmBtn = CreateGlassCapsuleButton("Confirm", "確認選擇", face.transform,
                new Vector2(0.62f, 0.04f), new Vector2(0.82f, 0.14f), AccentDim, Accent, ConfirmClubBriefingAndStart, _uiFont, 18);
            ApplyFilledButtonLabel(backBtn.GetComponentInChildren<Text>(), 96, heightFill: 0.9f);
            ApplyFilledButtonLabel(confirmBtn.GetComponentInChildren<Text>(), 96, heightFill: 0.9f);
        }

        private void OpenClubBriefing()
        {
            if (!PlayableClubIds.Contains(_selectedClubId))
                _selectedClubId = "navi_2021";

            HideTitleHoverPopup();
            CancelTitleHover();
            CancelDifficultyHover();
            HideStatTooltip();

            if (_clubBriefingTitle != null)
                _clubBriefingTitle.text = ClubFullDisplayName(_selectedClubId);
            if (_clubBriefingRegion != null)
                _clubBriefingRegion.text = RegionSituationTitle(_selectedClubId);
            if (_clubBriefingBody != null)
            {
                NarrativeUiText.Set(_clubBriefingBody, BuildClubBriefingText(_selectedClubId));
                ResizeClubBriefingContent();
            }

            if (_clubBriefingRoot != null)
            {
                _clubBriefingRoot.SetActive(true);
                _clubBriefingRoot.transform.SetAsLastSibling();
                if (_canvasFrostHost != null)
                    _canvasFrostHost.RefreshBlur();
            }
        }

        private void HideClubBriefing()
        {
            if (_clubBriefingRoot != null)
                _clubBriefingRoot.SetActive(false);
        }

        private void ConfirmClubBriefingAndStart()
        {
            HideClubBriefing();
            StartSelectedCareer();
        }

        private static string RegionSituationTitle(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "dragons":
                case "phoenix":
                    return "亞洲局勢";
                case "sentinels":
                case "vortex":
                    return "美洲局勢";
                default:
                    return "歐洲局勢";
            }
        }

        private string BuildClubBriefingText(string clubId)
        {
            string region = "Europe";
            if (_content != null && _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                !string.IsNullOrEmpty(club.region))
                region = club.region;

            var sb = new StringBuilder();
            sb.Append("【地區局勢】\n\n");
            sb.Append(RegionSituationBlurb(region));
            sb.Append("\n\n【俱樂部歷史】\n\n");
            sb.Append(ClubHistoryBlurb(clubId));
            sb.Append("\n\n【當前現況】\n\n");
            sb.Append(ClubCurrentBlurb(clubId));
            sb.Append("\n\n確認後將以目前難度與時代設定開始生涯。");
            return sb.ToString();
        }

        private void ResizeTitleEraIntroContent()
        {
            if (_titleEraIntro == null || _titleEraIntroContent == null) return;
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(72f, _titleEraIntro.preferredHeight + 6f);
            _titleEraIntroContent.sizeDelta = new Vector2(_titleEraIntroContent.sizeDelta.x, height);
        }

        private void ResizeClubBriefingContent()
        {
            if (_clubBriefingBody == null || _clubBriefingBodyContent == null) return;
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(320f, _clubBriefingBody.preferredHeight + 8f);
            _clubBriefingBodyContent.sizeDelta = new Vector2(_clubBriefingBodyContent.sizeDelta.x, height);
        }

        private static string RegionSituationBlurb(string region)
        {
            switch ((region ?? string.Empty).Trim())
            {
                case "Asia":
                    return "亞洲賽區正努力縮小與歐美的體系差距。線上賽事仍是主力舞台，頂級俱樂部開始更積極引進國際經驗，但獎金結構與世界大賽名額仍相對緊繃。";
                case "Americas":
                    return "美洲賽區擁有市場與關注度，卻在國際對抗上仍需證明穩定性。北美與南美俱樂部各自尋找能在歐洲主戰場立足的陣容公式。";
                default:
                    return CareerIntroCopy.EuropeSituation2021;
            }
        }

        private static string ClubHistoryBlurb(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "gambit_2021":
                    return @"2017年對Gambit而言是隊史最輝煌的一年。由Dosia、AdreN、mou、Zeus與HObbit組成的陣容，在PGL克拉科夫Major上演灰姑娘童話。這支平均年齡27歲的「老年戰隊」先是在半決賽擊敗丹麥霸主Astralis，決賽再以讓一追二的戲碼逆轉Immortals，為獨聯體賽區贏得隊史首座，也是該賽區第一座Major冠軍獎盃。然而輝煌僅維持17天，奪冠後Zeus與教練Kane離隊重返NAVI，Gambit從此一蹶不振。

自2017年拿下獨聯體第一座major冠軍後，2018年至2019年，Gambit陷入漫長的陣痛期。2018年初的ELEAGUE Boston Major，Gambit在小組賽即遭俄羅斯戰隊QBF擊敗。此後陣容持續動盪，2019年5月俱樂部將主隊陣容暫時凍結，同年9月將Dosia、mou等選手掛上轉會名單。與此同時，Gambit於2019年4月啟動青訓計畫Gambit Youngsters，選拔過程甚至邀請了現任NAVI教練B1ad3參與。這支青訓隊在2019年底的MSI MGA總決賽中擊敗ViCi與ex Epsilon奪冠，首次闖入世界前30名。

2020年，青訓隊在年初以來已贏下7場線上賽事，並擊敗過NAVI、MIBR、forZe與Virtus.pro等強隊。7月，俱樂部迎來2017年Major冠軍老將HObbit回歸，以六人陣容帶領這批少年征戰。10月，Gambit正式宣布將青訓隊整體提拔為主隊，陣容為nafany、sh1ro、interz、Ax1Le、HObbit與教練groove。這支二代紅星在2020年以69.2%的勝率高居年度榜首，靜靜等待著2021年的全面爆發。";
                case "navi_2021":
                default:
                    return @"2018年的NAVI迎來了真正的蛻變。由Zeus、Edward、flamie、s1mple與electroNic組成的陣容，在年初ELEAGUE Boston Major止步四強後，接連拿下SL-i群星聯賽S5、CS:GO亞洲邀請賽與ESL One科隆站等五座冠軍。s1mple以1.33的Rating獲封HLTV年度TOP 1，打破了歷屆紀錄。然而在FACEIT倫敦Major決賽中，NAVI再次敗於Astralis，成為丹麥王朝崛起的墊腳石。

2019年對NAVI而言是動盪的一年。年初他們在StarSeries S7以3：0橫掃fnatic奪冠，但在IEM卡托維茲Major與StarLadder柏林Major分別止步四強與八強。5月，俱樂部迎來重大變革，老將Edward被下放，Boombl4入隊。9月的BLAST Pro Series莫斯科站結束後，Zeus宣布退役，教練Kane離隊，Boombl4正式接過指揮職責。GuardiaN短暫回歸、B1ad3接任教練，但隊伍陷入長期低潮，12月HLTV排名跌至隊史最低的14名。

2020年初，NAVI再度重組，下放GuardiaN、簽下Perfecto。新陣容迅速展現威力，在ICE Challenge 2020拿下亞軍後，NAVI於IEM卡托維茲以3：0橫掃G2奪冠，時隔十年再次捧起這座獎盃，並於3月重返世界第一。然而疫情迫使賽事轉為線上後，NAVI的統治力急轉直下。全年經歷了ESL One科隆歐洲站的慘淡出局、EPL S12決賽遭Astralis讓二追三，以及IEM北京海淀決賽再度被Vitality讓二追三的連串打擊。下半年顆粒無收的NAVI，帶著起伏與遺憾走向2021年。等待他們的，將是BLAST全球總決賽的救贖與黑金王朝的序幕。";
            }
        }

        private static string ClubCurrentBlurb(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "gambit_2021":
                    return @"2020年對Gambit而言是浴火重生的一年。10月，俱樂部正式宣布將青訓隊Gambit Youngsters整體提拔為主隊。陣容由nafany、sh1ro、interz、Ax1Le與2017年Major冠軍老將HObbit組成，教練則由groove擔任。這支二代紅星在2020年以穩定的陣容征戰全年，成為HLTV年度前二十名隊伍中唯一一支全年未曾更換陣容的隊伍。

這批從青訓體系中殺出的年輕人以驚人的統治力震驚了整個世界。他們全年拿下11座賽事冠軍，在368張地圖上打出68.2%的勝率，高居HLTV年度戰隊勝率榜榜首。Gambit以HLTV世界排名第15名結束2020年，靜靜等待著2021年的全面爆發。";
                case "navi_2021":
                default:
                    return @"2020年對NAVI而言是充滿矛盾的一年。年初以新陣容：s1mple、electroNic、Boombl4、Perfecto與flamie，在IEM卡托維茲線下賽以3：0橫掃G2奪冠，展現了絕對的統治力。然而隨著疫情爆發、賽事全面轉為線上，NAVI的狀態急速下滑，不僅在IEM紐約接連輸給Virtus.pro與forZe，更在EPL S12決賽與IEM北京海淀決賽中，兩度在2：0領先的大好局面下被Astralis與Vitality讓二追三逆轉。下半年顆粒無收的NAVI，帶著起伏與遺憾結束了這一年。

2020年12月，NAVI做出了改變未來的關鍵決定—將青訓隊的17歲天才Valeriy “B1T” Vakhovskiy提拔至主隊，正式啟用六人輪換陣容。教練B1ad3對此表示：「在正確的操作下，擴大陣容的模型非常成功，長遠來看這絕對會強化我們的陣容。」此時NAVI以HLTV世界第三的排名結束2020年，是前十名中唯一的獨聯體代表。全隊在年末賽事結束後進入休假，預計2021年1月中旬歸隊，迎接即將到來的BLAST Premier全球總決賽—這場賽事將決定他們能否以一座冠軍為動盪的2020年畫下句點，並揭開黑金王朝的序幕。";
            }
        }

        private void PlaceTitleHoverSection(
            RectTransform parent, string title, Vector2 min, Vector2 max,
            string[] headers, string[] values, int columnsPerRow)
        {
            Image section = CreateUiObject<Image>(title, parent);
            SetRect(section.rectTransform, min, max);
            UiLightChromeUtility.ApplySoftNavyPanel(section);
            section.raycastTarget = false;

            Image underline = CreateUiObject<Image>("Underline", section.transform);
            SetRect(underline.rectTransform, new Vector2(0.02f, 0.90f), new Vector2(0.20f, 0.925f));
            underline.color = AccentDim;
            underline.raycastTarget = false;

            Text titleText = CreateText("SectionTitle", section.transform, 13, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(titleText.rectTransform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.99f));
            titleText.text = title;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            titleText.raycastTarget = false;

            if (headers == null || values == null || headers.Length == 0)
            {
                Text missing = CreateText("Missing", section.transform, 12, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
                SetRect(missing.rectTransform, new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.75f));
                missing.text = "缺資料";
                missing.raycastTarget = false;
                return;
            }

            int count = Math.Min(headers.Length, values.Length);
            int rows = (count + columnsPerRow - 1) / columnsPerRow;
            float tableTop = 0.86f;
            float tableBottom = 0.04f;
            float rowSpan = (tableTop - tableBottom) / Math.Max(1, rows);

            for (int r = 0; r < rows; r++)
            {
                int start = r * columnsPerRow;
                int cols = Math.Min(columnsPerRow, count - start);
                float y1 = tableTop - r * rowSpan;
                float y0 = y1 - rowSpan;
                float mid = (y0 + y1) * 0.5f;
                float headerY0 = Mathf.Min(mid + rowSpan * 0.06f, y1 - 0.01f);
                float headerY1 = y1 - rowSpan * 0.04f;
                float valueY0 = y0 + rowSpan * 0.06f;
                float valueY1 = Mathf.Max(mid - rowSpan * 0.02f, valueY0 + 0.02f);
                if (headerY0 > headerY1)
                {
                    headerY0 = mid;
                    headerY1 = y1 - 0.01f;
                }

                float[] weights = NormalizeColumnWeights(null, cols);
                float x = 0.02f;
                float usable = 0.96f;
                for (int c = 0; c < cols; c++)
                {
                    float w = weights[c] * usable;
                    float left = x + 0.006f;
                    float right = x + w - 0.006f;

                    Text header = CreateText($"H{r}_{c}", section.transform, 11, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
                    SetRect(header.rectTransform, new Vector2(left, headerY0), new Vector2(right, headerY1));
                    header.text = headers[start + c];
                    header.verticalOverflow = VerticalWrapMode.Overflow;
                    header.raycastTarget = false;

                    Text value = CreateText($"V{r}_{c}", section.transform, 14, TextAnchor.MiddleCenter, ProfileValue, _uiFont700);
                    SetRect(value.rectTransform, new Vector2(left, valueY0), new Vector2(right, valueY1));
                    value.text = values[start + c] ?? "—";
                    value.verticalOverflow = VerticalWrapMode.Overflow;
                    value.resizeTextForBestFit = true;
                    value.resizeTextMinSize = 10;
                    value.resizeTextMaxSize = 14;
                    value.raycastTarget = false;

                    x += w;
                }
            }
        }

        private string ClubDisplayName(string clubId)
        {
            if (_content != null &&
                _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                !string.IsNullOrWhiteSpace(club.displayName))
                return club.displayName;
            return clubId;
        }

        private string ClubFullDisplayName(string clubId)
        {
            if (string.Equals(clubId, "navi_2021", StringComparison.OrdinalIgnoreCase))
                return "Natus Vincere";
            if (string.Equals(clubId, "gambit_2021", StringComparison.OrdinalIgnoreCase))
                return "Gambit Esports";
            if (string.Equals(clubId, "virtuspro_2021", StringComparison.OrdinalIgnoreCase))
                return "Virtus.pro";
            if (string.Equals(clubId, "cph_flames_2021", StringComparison.OrdinalIgnoreCase))
                return "Copenhagen Flames";
            if (string.Equals(clubId, "riders_2021", StringComparison.OrdinalIgnoreCase))
                return "Movistar Riders";
            return ClubDisplayName(clubId);
        }

        private string ClubRegionLabel(string clubId)
        {
            if (_content != null && _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club) &&
                !string.IsNullOrEmpty(club.region))
                return club.region.ToUpperInvariant();
            return "EUROPE";
        }

        private static string EraButtonLabel(string eraId)
        {
            if (string.Equals(eraId, "2021_01_01", StringComparison.OrdinalIgnoreCase))
                return "2021.1.1 · 獨聯體狂潮";
            return eraId;
        }

        private static string EraClockLabel(string eraId)
        {
            if (string.Equals(eraId, "2021_01_01", StringComparison.OrdinalIgnoreCase))
                return "2021.1.1 0:00";
            return eraId;
        }

        private static string EraIntroPlaceholder(string eraId)
        {
            if (string.Equals(eraId, "2021_01_01", StringComparison.OrdinalIgnoreCase))
                return CareerIntroCopy.EraCisSurge2021;

            return "（此時代尚未開放。）";
        }

        private void ShowTitleScreen()
        {
            _careerStarted = false;
            ClosePlayerDetail();
            CloseCoachDetail();
            CancelTitleHover();
            HideTitleHoverPopup();
            CancelStatTooltip();
            HideStatTooltip();
            HideClubBriefing();
            if (_shellRoot != null) _shellRoot.SetActive(false);
            if (_titleRoot != null) _titleRoot.SetActive(true);
            RefreshTitleChoices();
            PlayTitleEntrance();
            RefreshFrostedGlass(_titleFrostHost, _titleRoot != null ? _titleRoot.transform : null);
        }

        private void PlayTitleEntrance()
        {
            for (int i = 0; i < _titleMotionBlocks.Length; i++)
            {
                RectTransform rect = _titleMotionBlocks[i];
                if (rect == null)
                    continue;
                UiSettleSlide slide = rect.GetComponent<UiSettleSlide>();
                if (slide == null)
                    slide = rect.gameObject.AddComponent<UiSettleSlide>();
                slide.Play(i, false);
            }
        }

        private void PlayIntelSettle()
        {
            if (_titleMotionBlocks.Length < 3 || _titleMotionBlocks[2] == null)
                return;
            UiSettleSlide slide = _titleMotionBlocks[2].GetComponent<UiSettleSlide>();
            if (slide != null)
                slide.Play(0, true);
            if (_titleNameGhost != null)
            {
                UiPigmentGhost ghost = _titleNameGhost.GetComponent<UiPigmentGhost>();
                if (ghost == null)
                    ghost = _titleNameGhost.gameObject.AddComponent<UiPigmentGhost>();
                ghost.Flash();
            }
        }

        private void StartSelectedCareer()
        {
            if (!PlayableClubIds.Contains(_selectedClubId))
            {
                _selectedClubId = "navi_2021";
            }

            CreateCareer();
            _careerStarted = true;
            _timePaused = true;
            _timeSpeed = 1;
            _hourAccumulator = 0f;
            if (_titleRoot != null) _titleRoot.SetActive(false);
            if (_shellRoot != null) _shellRoot.SetActive(true);
            _status = $"生涯開始 · {ClubDisplayName(_selectedClubId)} · 時間暫停（空白鍵繼續）";
            RefreshAll();
            PlayChapterMotion();
            RefreshFrostedGlass(_shellFrostHost, _shellRoot != null ? _shellRoot.transform : null);
            if (_canvasFrostHost != null)
                _canvasFrostHost.RefreshBlur();
        }

        private static string DifficultyLabel(CareerDifficulty difficulty) =>
            CareerDifficultyRules.DisplayName(difficulty);

        private void BuildTabs(Transform parent)
        {
            _tabFaces.Clear();
            _tabAccents.Clear();
            _tabLabels.Clear();

            Sprite tabFrame = UiRoundedRectUtility.GetSoftSquareSprite();
            for (int i = 0; i < Tabs.Length; i++)
            {
                float y1 = 0.96f - i * 0.105f;
                float y0 = y1 - 0.08f;

                Image edge = CreateUiObject<Image>($"Tab_{i}", parent);
                SetRect(edge.rectTransform, new Vector2(0.02f, y0), new Vector2(0.98f, y1));
                edge.sprite = tabFrame;
                edge.type = Image.Type.Sliced;
                edge.color = new Color(
                    CareerEditorialChrome.StageType.r,
                    CareerEditorialChrome.StageType.g,
                    CareerEditorialChrome.StageType.b,
                    0.62f);

                Image face = CreateUiObject<Image>("Face", edge.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(2f, 2f);
                face.rectTransform.offsetMax = new Vector2(-2f, -2f);
                face.sprite = tabFrame;
                face.type = Image.Type.Sliced;
                face.color = CareerEditorialChrome.Stage;

                Image accent = CreateUiObject<Image>("Accent", face.transform);
                SetRect(accent.rectTransform, new Vector2(0f, 0.08f), new Vector2(0.14f, 0.22f));
                accent.color = CareerEditorialChrome.Mustard;
                accent.raycastTarget = false;

                int index = i;
                Button button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => SelectTab(index));

                Text label = CreateText("Label", face.transform, 22, TextAnchor.MiddleLeft,
                    CareerEditorialChrome.StageMuted, _uiFontBlack);
                SetRect(label.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.98f, 0.92f));
                label.text = Tabs[i];
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;

                accent.gameObject.SetActive(false);
                _tabFaces.Add(face);
                _tabAccents.Add(accent);
                _tabLabels.Add(label);
            }
        }

        private void SelectTab(int index)
        {
            if (_tab == index && !(index == 0 && _mapEventLogFocus)) return;
            ClosePlayerDetail();
            CloseCoachDetail();
            if (index != 0 || _tab != index)
                _mapEventLogFocus = false;
            _tab = index;
            RefreshTabs();
            RebuildContent();
            PlayChapterMotion();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        private void PlayChapterMotion()
        {
            if (_contentPanel != null)
            {
                UiChapterEnter enter = _contentPanel.GetComponent<UiChapterEnter>();
                if (enter == null)
                    enter = _contentPanel.gameObject.AddComponent<UiChapterEnter>();
                _chapterFromRight = !_chapterFromRight;
                enter.Play(_chapterFromRight);
            }

            if (_chapterWord != null)
            {
                UiLateType late = _chapterWord.GetComponent<UiLateType>();
                if (late == null)
                    late = _chapterWord.gameObject.AddComponent<UiLateType>();
                late.Play();
            }

            Color chapterPigment = CareerEditorialChrome.PigmentForTab(_tab);
            if (_chapterPigment != null)
            {
                UiPigmentTint tint = _chapterPigment.GetComponent<UiPigmentTint>();
                if (tint == null)
                    tint = _chapterPigment.gameObject.AddComponent<UiPigmentTint>();
                tint.Set(chapterPigment);
            }

            if (_shellParticles != null)
                _shellParticles.SetPigment(chapterPigment);
            if (_mapParticles != null)
                _mapParticles.SetPigment(chapterPigment);

            if (_tab >= 0 && _tab < _tabAccents.Count)
            {
                Image accent = _tabAccents[_tab];
                UiAccentGrow grow = accent.GetComponent<UiAccentGrow>();
                if (grow == null)
                    grow = accent.gameObject.AddComponent<UiAccentGrow>();
                grow.Play();
            }
        }

        private void RefreshTabs()
        {
            Color pigment = CareerEditorialChrome.PigmentForTab(_tab);
            for (int i = 0; i < _tabFaces.Count; i++)
            {
                bool on = i == _tab;
                _tabFaces[i].color = CareerEditorialChrome.Stage;
                Image edge = _tabFaces[i].transform.parent != null
                    ? _tabFaces[i].transform.parent.GetComponent<Image>()
                    : null;
                if (edge != null)
                    edge.color = on
                        ? pigment
                        : new Color(
                            CareerEditorialChrome.StageType.r,
                            CareerEditorialChrome.StageType.g,
                            CareerEditorialChrome.StageType.b,
                            0.62f);
                _tabAccents[i].color = pigment;
                _tabAccents[i].gameObject.SetActive(on);
                _tabLabels[i].color = on ? CareerEditorialChrome.StageType : CareerEditorialChrome.StageMuted;
                _tabLabels[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }

            if (_chapterWord != null && _tab >= 0 && _tab < Tabs.Length)
            {
                _chapterWord.text = Tabs[_tab];
                _chapterWord.color = CareerEditorialChrome.StageType;
            }
        }

        /// <summary>
        /// Header stat display: 1,000+ → K、1,000,000+ → M（皆小數點後一位）；其餘照原數。
        /// 具體數字仍可透過滑鼠停留的 tooltip 查看。
        /// </summary>
        private static string AbbreviateStat(long value)
        {
            long abs = Math.Abs(value);
            string body;
            if (abs >= 1_000_000)
                body = (abs / 1_000_000d).ToString("0.0") + "M";
            else if (abs >= 1_000)
                body = (abs / 1_000d).ToString("0.0") + "K";
            else
                return value.ToString();
            return value < 0 ? "-" + body : body;
        }

        private void RefreshAll()
        {
            if (!_careerStarted || _session == null) return;
            CareerState state = _session.State;
            if (!state.TryGetClub(state.playerClubId, out ClubState club) || club == null)
                return;

            if (_clubLogoImage != null)
            {
                _clubLogoImage.sprite = LoadClubLogo(state.playerClubId);
                _clubLogoImage.color = Color.white;
                _clubLogoImage.enabled = true;
            }
            if (_cashText != null)
            {
                _cashText.text = (club.cash < 0 ? "-$" : "$") + AbbreviateStat(Math.Abs((long)club.cash));
                _cashText.color = club.cash < 0 ? Danger : CareerEditorialChrome.StageType;
            }
            if (_fansText != null)
                _fansText.text = AbbreviateStat(club.fans);
            if (_prestigeText != null)
                _prestigeText.text = AbbreviateStat(club.exposure);
            if (_fameText != null)
                _fameText.text = AbbreviateStat(club.fame);
            if (_rankText != null)
            {
                int rank = RankFor(club.clubId);
                _rankText.text = rank > 0 ? $"#{rank}" : "#—";
                _rankText.color = RankGold;
            }
            RefreshSeasonTimeline(state);
            if (_statusText != null)
                _statusText.text = _status;
            RefreshStatusNavigateHint();

            RefreshTabs();
            RebuildContent();
            if (_rankingRoot != null && _rankingRoot.activeSelf)
                RefreshWorldRankingPanel();
            if (_clubHubOpen)
                RefreshClubHubPanel();
            if (!string.IsNullOrEmpty(_selectedPlayerId))
                RefreshPlayerDetail();
            if (!string.IsNullOrEmpty(_selectedCoachId))
                RefreshCoachDetail();
            TryShowPendingScriptedEvent();
        }

        private void RefreshSeasonTimeline(CareerState state)
        {
            if (_seasonLabel != null)
            {
                string mark = _timePaused ? "❚❚" : "▶";
                _seasonLabel.text = $"{mark} {CareerCalendar.FormatHeader(_content.Balance, state)}";
            }

            RefreshTimeControlVisuals();
        }

        private void HandleTimeHotkeys()
        {
            if (_titleRoot != null && _titleRoot.activeSelf) return;
            if (_settingsPanel != null && _settingsPanel.IsVisible) return;
            if (_playerDetailRoot != null && _playerDetailRoot.activeSelf) return;
            if (_coachDetailRoot != null && _coachDetailRoot.activeSelf) return;
            if (_rankingRoot != null && _rankingRoot.activeSelf) return;
            if (_competitionDetailRoot != null && _competitionDetailRoot.activeSelf) return;
            if (_sponsorNegotiationRoot != null && _sponsorNegotiationRoot.activeSelf) return;
            if (_clubHubOpen) return;
            if (_scriptedEventRoot != null && _scriptedEventRoot.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.Space))
                TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                SetTimeSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                SetTimeSpeed(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                SetTimeSpeed(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                SetTimeSpeed(4);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                SetTimeSpeed(5);
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) ||
                Input.GetKeyDown(KeyCode.KeypadPlus))
                SetTimeSpeed(Math.Min(5, _timeSpeed + 1));
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                SetTimeSpeed(Math.Max(1, _timeSpeed - 1));
        }

        private void TickCareerClock()
        {
            if (_timePaused || _session.State.gameOver) return;

            float rate = HoursPerRealSecond[Mathf.Clamp(_timeSpeed, 1, 5)];
            _hourAccumulator += Time.unscaledDeltaTime * rate;
            int hours = (int)_hourAccumulator;
            if (hours <= 0) return;
            _hourAccumulator -= hours;
            hours = Math.Min(hours, CareerCalendar.HoursPerDay * 2);

            CareerAdvanceResult result = _session.AdvanceHours(hours);
            if (CareerDifficultyRules.UsesIronmanAutoSave(_session.State.difficulty) && result.hoursAdvanced > 0)
                _persistence.Save(_session.State, 0);

            HandleAdvanceEventFeedback(result);
            if (result.ShouldAutoPause)
            {
                PauseTime(AutoPauseReason(result), NavLinkFromAdvanceResult(result));
                RefreshAll();
                return;
            }

            _uiRefreshTimer -= Time.unscaledDeltaTime;
            if (_uiRefreshTimer <= 0f)
            {
                _uiRefreshTimer = 0.25f;
                RefreshAll();
            }
            else if (result.scriptedEventPending || result.eventToastPending)
            {
                TryShowPendingScriptedEvent();
            }
            else
            {
                RefreshSeasonTimeline(_session.State);
                if (_statusText != null)
                    _statusText.text = _status;
            }
        }

        private static string AutoPauseReason(CareerAdvanceResult result)
        {
            if (result.gameOver) return "生涯結束 · 時間已暫停";
            if (result.playerMatchRequired) return "正式賽事對戰 · 時間已暫停";
            if (result.scriptedEventShouldPause || result.eventToastShouldPause)
                return "事件 · 時間已暫停";
            if (result.playerCompetitionResolved) return "賽事結算 · 時間已暫停";
            if (TransferNegotiationRules.QuoteSystemEnabled)
            {
                if (result.transferDealInterrupted) return "轉會談判被打斷 · 時間已暫停";
                if (result.personalTermsDue) return "待談個人條款 · 時間已暫停";
                if (result.newTransferOfferForPlayer) return "收到轉會報價 · 時間已暫停";
            }
            return "時間已暫停";
        }

        private void TogglePause()
        {
            if (!_careerStarted || _session == null) return;
            if (_session.State.gameOver)
            {
                _timePaused = true;
                RefreshTimeControlVisuals();
                return;
            }

            if (_timePaused) ResumeTime();
            else PauseTime("已暫停");
        }

        private void PauseTime(string reason, CareerUiNavLink nav = default)
        {
            _timePaused = true;
            _hourAccumulator = 0f;
            if (!string.IsNullOrEmpty(reason))
                SetStatus(reason, nav);
            if (_session != null && CareerDifficultyRules.UsesIronmanAutoSave(_session.State.difficulty))
                _persistence.Save(_session.State, 0);
            RefreshTimeControlVisuals();
            if (_seasonLabel != null && _session != null)
                RefreshSeasonTimeline(_session.State);
            if (_statusText != null)
                _statusText.text = _status;
            RefreshStatusNavigateHint();
        }

        private CareerUiNavLink NavLinkFromAdvanceResult(CareerAdvanceResult result)
        {
            if (result.playerMatchRequired)
            {
                string cid = _session?.State.competitionRun?.competitionId;
                return new CareerUiNavLink(CareerUiDestination.OfficialMatch, cid);
            }
            if (result.transferDealInterrupted || result.personalTermsDue ||
                (result.newTransferOfferForPlayer && TransferNegotiationRules.QuoteSystemEnabled))
                return new CareerUiNavLink(CareerUiDestination.TransferMarket);
            if (result.playerCompetitionResolved)
                return new CareerUiNavLink(CareerUiDestination.Map);
            return CareerUiNavLink.None;
        }

        private void SetStatus(string message, CareerUiNavLink nav = default)
        {
            _status = message;
            _statusNavLink = nav.IsActive ? nav : CareerUiNavLink.None;
        }

        private void RefreshStatusNavigateHint()
        {
            bool active = _careerStarted && _statusNavLink.IsActive;
            if (_statusJumpHint != null)
                _statusJumpHint.gameObject.SetActive(active);
            if (_statusText != null)
                _statusText.color = active ? AccentBlue : TextMuted;
        }

        private void OnStatusBarNavigateClicked()
        {
            if (!_statusNavLink.IsActive) return;
            ApplyCareerNavigation(_statusNavLink);
        }

        private void ApplyCareerNavigation(CareerUiNavLink link)
        {
            if (!link.IsActive || !_careerStarted || _session == null) return;

            ClosePlayerDetail();
            CloseCoachDetail();

            switch (link.Destination)
            {
                case CareerUiDestination.Map:
                    NavigateToTab(0);
                    break;
                case CareerUiDestination.MapCompetition:
                    _pendingMapCompetitionFocusId = link.SubjectId;
                    NavigateToTab(0);
                    break;
                case CareerUiDestination.OfficialMatch:
                    if (!string.IsNullOrEmpty(link.SubjectId))
                        _pendingMapCompetitionFocusId = link.SubjectId;
                    else if (_session.State.competitionRun != null)
                        _pendingMapCompetitionFocusId = _session.State.competitionRun.competitionId;
                    NavigateToTab(0);
                    break;
                case CareerUiDestination.PlayerRoster:
                    NavigateToTab(1);
                    _sectionExpanded["roster_starters"] = true;
                    break;
                case CareerUiDestination.TransferMarket:
                    NavigateToTab(3);
                    _sectionExpanded["market_offers"] = true;
                    break;
                case CareerUiDestination.Training:
                    NavigateToTab(1);
                    _trainingPanelOpen = true;
                    if (!string.IsNullOrEmpty(link.SubjectId))
                        _trainingUiPlayerId = link.SubjectId;
                    break;
                case CareerUiDestination.Finance:
                    NavigateToTab(2);
                    _sectionExpanded["base_finance"] = true;
                    _sectionExpanded["base_ledger"] = true;
                    break;
                case CareerUiDestination.Sponsors:
                    NavigateToTab(2);
                    _sectionExpanded["base_sponsors"] = true;
                    break;
                case CareerUiDestination.Facilities:
                    NavigateToTab(4);
                    _sectionExpanded["base_facilities"] = true;
                    break;
                case CareerUiDestination.EventLog:
                    NavigateToTab(0);
                    _mapEventLogFocus = true;
                    break;
                case CareerUiDestination.Tactics:
                    NavigateToTab(5);
                    break;
                case CareerUiDestination.WorldRanking:
                    OpenWorldRankingPanel();
                    return;
                case CareerUiDestination.PlayerProfile:
                    NavigateToTab(1);
                    _sectionExpanded["roster_starters"] = true;
                    if (!string.IsNullOrEmpty(link.SubjectId) &&
                        _content.Players.ContainsKey(link.SubjectId))
                        OpenPlayerDetail(link.SubjectId);
                    break;
            }

            RebuildContent();
        }

        private void NavigateToTab(int index)
        {
            _tab = index;
            RefreshTabs();
        }

        private void ScrollMapScheduleToRow(RectTransform row)
        {
            if (_mapScheduleScroll == null || row == null || _mapScheduleScroll.content == null) return;
            Canvas.ForceUpdateCanvases();
            RectTransform content = _mapScheduleScroll.content;
            RectTransform viewport = _mapScheduleScroll.viewport;
            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            if (contentHeight <= viewportHeight + 1f) return;

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, row);
            float rowCenterY = -bounds.center.y;
            float scrollRange = contentHeight - viewportHeight;
            float target = Mathf.Clamp(rowCenterY - viewportHeight * 0.35f, 0f, scrollRange);
            _mapScheduleScroll.verticalNormalizedPosition = 1f - target / scrollRange;
        }

        private void FocusPendingMapCompetitionRow()
        {
            if (string.IsNullOrEmpty(_pendingMapCompetitionFocusId) || _mapScheduleContent == null)
                return;

            string competitionId = _pendingMapCompetitionFocusId;
            _pendingMapCompetitionFocusId = null;
            Transform row = _mapScheduleContent.Find("ScheduleRow_" + competitionId);
            if (row is RectTransform rect)
                ScrollMapScheduleToRow(rect);
        }

        private void AddCareerEventRow(CareerEvent evt)
        {
            RectTransform row = AddRow(42f);
            Image rowImage = row.GetComponent<Image>();
            rowImage.raycastTarget = true;

            AddRowLabel(row, 0.015f, 0.20f,
                CareerCalendar.FormatShort(_content.Balance, evt.clockHour),
                TextMuted, 12);

            string headline = ResolveNewsHeadline(evt);
            string body = ResolveNewsBody(evt);
            AddRowLabel(row, 0.21f, 0.48f, headline, Accent, 13);
            AddRowLabel(row, 0.49f, 0.97f, TruncateNewsOneLine(body), TextPrimary, 12,
                TextAnchor.MiddleLeft, 0.08f, 0.92f, wrap: false);

            CareerEvent captured = evt;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = rowImage;
            ApplyButtonColors(button);
            button.onClick.AddListener(() => ShowNewsDetail(captured));
        }

        private string ResolveNewsHeadline(CareerEvent evt)
        {
            if (evt == null)
                return "新聞";
            if (!string.IsNullOrWhiteSpace(evt.title))
                return evt.title;
            if (evt.type == CareerEventType.ScriptedEvent &&
                !string.IsNullOrEmpty(evt.subjectId) &&
                _session != null &&
                _session.TryGetScriptedEventDefinition(evt.subjectId, out CareerScriptedEventDef def) &&
                !string.IsNullOrWhiteSpace(def.title))
                return def.title;
            return string.IsNullOrWhiteSpace(evt.message) ? "新聞" : evt.message;
        }

        private string ResolveNewsBody(CareerEvent evt)
        {
            if (evt == null)
                return string.Empty;
            if (!string.IsNullOrWhiteSpace(evt.message))
                return evt.message;
            if (evt.type == CareerEventType.ScriptedEvent &&
                !string.IsNullOrEmpty(evt.subjectId) &&
                _session != null &&
                _session.TryGetScriptedEventDefinition(evt.subjectId, out CareerScriptedEventDef def))
                return _session.ResolveScriptedEventBody(def);
            return evt.title ?? string.Empty;
        }

        private static string TruncateNewsOneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            string flat = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
            const int maxChars = 36;
            if (flat.Length <= maxChars)
                return flat;
            return flat.Substring(0, maxChars).TrimEnd() + "...";
        }

        private void PauseTime(string reason)
        {
            PauseTime(reason, CareerUiNavLink.None);
        }

        private void ResumeTime()
        {
            if (_session == null || _session.State.gameOver) return;
            if (_session.State.pendingEvents != null && _session.State.pendingEvents.Count > 0 &&
                _session.PendingEventBlocksClock())
            {
                TryShowPendingScriptedEvent();
                return;
            }
            _timePaused = false;
            RefreshTimeControlVisuals();
            if (_seasonLabel != null)
                RefreshSeasonTimeline(_session.State);
            if (_session.GetPendingEventDefinition() != null)
                TryShowPendingScriptedEvent();
        }

        private void SetTimeSpeed(int speed)
        {
            _timeSpeed = Mathf.Clamp(speed, 1, 5);
            if (_careerStarted && _session != null)
                RefreshSeasonTimeline(_session.State);
            else
                RefreshTimeControlVisuals();
        }

        private void RefreshTimeControlVisuals()
        {
            if (_pauseButtonLabel != null)
            {
                _pauseButtonLabel.text = _timePaused ? "▶" : "❚❚";
                _pauseButtonLabel.color = _timePaused
                    ? CareerEditorialChrome.Ink
                    : CareerEditorialChrome.StageType;
            }
            if (_pauseButtonFace != null)
            {
                _pauseButtonFace.color = _timePaused
                    ? CareerEditorialChrome.Paper
                    : Color.clear;
                Image pauseEdge = _pauseButtonFace.transform.parent != null
                    ? _pauseButtonFace.transform.parent.GetComponent<Image>()
                    : null;
                if (pauseEdge != null)
                    pauseEdge.color = Color.clear;
            }

            for (int i = 0; i < _speedFaces.Length; i++)
            {
                bool selected = i + 1 == _timeSpeed;
                if (_speedFaces[i] != null)
                {
                    _speedFaces[i].color = selected ? CareerEditorialChrome.Paper : Color.clear;
                    Image edge = _speedFaces[i].transform.parent != null
                        ? _speedFaces[i].transform.parent.GetComponent<Image>()
                        : null;
                    if (edge != null)
                        edge.color = Color.clear;
                }
                if (_speedLabels[i] != null)
                    _speedLabels[i].color = selected
                        ? CareerEditorialChrome.Ink
                        : CareerEditorialChrome.StageType;
            }
        }

        private void RebuildContent()
        {
            if (_session == null) return;

            bool mapVisible = _tab == 0 && !_mapEventLogFocus;
            if (_mapHost != null) _mapHost.SetActive(mapVisible);
            if (_scrollHost != null) _scrollHost.SetActive(_tab != 0 || _mapEventLogFocus);

            if (mapVisible)
            {
                RefreshWorldMap();
                RefreshOfficialMatchBanner();
                RefreshMapSchedule();
                FocusPendingMapCompetitionRow();
                return;
            }

            if (_tab == 0 && _mapEventLogFocus)
            {
                if (_officialMatchBanner != null)
                    _officialMatchBanner.SetActive(false);
                RebuildScrollTab(DrawMapEventLog);
                return;
            }

            if (_officialMatchBanner != null)
                _officialMatchBanner.SetActive(false);

            switch (_tab)
            {
                case 1: RebuildScrollTab(DrawPlayerTab); break;
                case 2: RebuildScrollTab(DrawEconomyTab); break;
                case 3: RebuildScrollTab(DrawNegotiationTab); break;
                case 4: RebuildScrollTab(DrawBaseFacilitiesTab); break;
                case 5: RebuildScrollTab(DrawTacticsTab); break;
                case 6: RebuildScrollTab(DrawCommunityTab); break;
                case 7: RebuildScrollTab(DrawCodexTab); break;
            }
        }

        private void RebuildScrollTab(Action draw)
        {
            if (_contentRoot == null || draw == null) return;

            float mainScrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_contentRoot.GetChild(i).gameObject);

            draw();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            for (int i = 0; i < _contentRoot.childCount; i++)
                UiRowStagger.Play(_contentRoot.GetChild(i), i);
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = mainScrollPos;
        }

        private void DrawTabIntro(string title, string subtitle, params string[] bullets)
        {
            AddSectionTitle(title);
            AddMutedLine(subtitle);
            if (bullets == null) return;
            foreach (string line in bullets)
            {
                if (string.IsNullOrEmpty(line)) continue;
                AddMutedLine("· " + line);
            }
        }

        private void DrawPlaceholderSection(string key, string heading, string note)
        {
            if (!AddCollapsibleSection(key, heading)) return;
            AddMutedLine(string.IsNullOrEmpty(note) ? "此功能開發中。" : note);
        }

        private void ShowMapEventLog()
        {
            _mapEventLogFocus = true;
            NavigateToTab(0);
            RebuildContent();
        }

        private void DrawMapEventLog()
        {
            RectTransform backRow = AddRow(44f);
            AddRowButton(backRow, 0.02f, 0.32f, "返回地圖", () =>
            {
                _mapEventLogFocus = false;
                RebuildContent();
            });
            DrawEvents();
        }

        private void DrawPlayerTab()
        {
            DrawTabIntro("選手管理", "陣容調整、訓練分配、狀態檢視、青訓學院");
            DrawRoster();
            DrawPlaceholderSection("player_youth", "青訓學院", "待開發。");
        }

        private void DrawEconomyTab()
        {
            DrawTabIntro("財務管理", "營收報表、支出明細、贊助商合約、商品銷售");
            DrawEconomyHero();
            DrawEconomyFinance();
            DrawPlaceholderSection("econ_merch", "商品銷售", null);
        }

        private void DrawNegotiationTab()
        {
            DrawTabIntro("轉會與合約", "轉會市場、合約談判、AI 報價、租借管理");
            DrawMarket();
            DrawPlaceholderSection("nego_contracts", "合約談判", null);
            DrawPlaceholderSection("nego_loans", "租借管理", null);
        }

        private void DrawBaseFacilitiesTab()
        {
            DrawCoachStaffPanel();
            DrawPlaceholderSection("base_facilities", "設施", "待開發。");
            DrawPlaceholderSection("base_medical", "醫療中心", null);
        }

        private void DrawTacticsTab()
        {
            DrawTabIntro("戰術與數據", "牌組編輯、對手分析、Meta 趨勢、數據顯示");
            DrawPlaceholderSection("tactics_deck", "牌組編輯", null);
            DrawPlaceholderSection("tactics_scout", "對手分析", null);
            DrawPlaceholderSection("tactics_meta", "Meta 趨勢", null);
            DrawPlaceholderSection("tactics_data", "數據顯示", null);
        }

        private void DrawCodexTab()
        {
            DrawTabIntro("資料庫與教學", "選手圖鑑、俱樂部圖鑑、遊戲教學、術語辭典");
            DrawPlaceholderSection("codex_players", "選手圖鑑", null);
            DrawPlaceholderSection("codex_clubs", "俱樂部圖鑑", null);
            DrawPlaceholderSection("codex_tutorial", "遊戲教學", null);
            DrawPlaceholderSection("codex_glossary", "術語辭典", null);
        }

        private void DrawEconomyHero()
        {
            if (_session == null)
                return;
            CareerState state = _session.State;
            if (!state.TryGetClub(state.playerClubId, out ClubState club) || club == null)
                return;

            RectTransform row = AddRow(96f);
            Image background = row.GetComponent<Image>();
            if (background != null)
                background.color = CareerEditorialChrome.Paper;

            CareerEditorialChrome.CreateBlock(
                row, "CashPigment",
                new Vector2(0.02f, 0.18f), new Vector2(0.045f, 0.82f),
                club.cash < 0 ? CareerEditorialChrome.Red : CareerEditorialChrome.Mustard);

            Text kicker = CreateText("CashKicker", row, 16, TextAnchor.UpperLeft, CareerEditorialChrome.InkMuted, _uiFont700);
            SetRect(kicker.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.96f, 0.92f));
            kicker.text = "現金";

            Text number = CreateText("CashWord", row, 64, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontAnton);
            SetRect(number.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.96f, 0.68f));
            number.horizontalOverflow = HorizontalWrapMode.Overflow;
            number.verticalOverflow = VerticalWrapMode.Truncate;
            number.resizeTextForBestFit = true;
            number.resizeTextMinSize = 28;
            number.resizeTextMaxSize = 64;
            number.raycastTarget = false;
            long cash = (long)club.cash;
            number.text = (cash < 0 ? "-" : "") + Math.Abs(cash).ToString("N0");
        }

        private void DrawEconomyFinance()
        {
            CareerState state = _session.State;
            ClubState club = state.Club(state.playerClubId);
            MonthlyBudgetPreview budget = _session.GetMonthlyBudgetPreview(club.clubId);

            if (AddCollapsibleSection("base_finance", "本月收支"))
                DrawMonthlyBudgetPanel(budget);

            if (AddCollapsibleSection("base_ledger", "最近現金流"))
            {
                LedgerEntry[] ledger = state.ledger.Where(x => x.clubId == club.clubId)
                    .Reverse<LedgerEntry>().Take(8).ToArray();
                if (ledger.Length == 0)
                    AddMutedLine("尚無帳目紀錄。");
                foreach (LedgerEntry entry in ledger)
                {
                    RectTransform row = AddRow(36f);
                    AddRowLabel(row, 0.02f, 0.28f,
                        CareerCalendar.FormatShort(_content.Balance, entry.clockHour),
                        TextMuted, 13);
                    AddRowLabel(row, 0.30f, 0.62f, entry.category, TextPrimary, 13);
                    Color amtColor = entry.amount >= 0 ? Positive : Danger;
                    AddRowLabel(row, 0.64f, 0.82f, $"{entry.amount:+#,0;-#,0;0}", amtColor, 13, TextAnchor.MiddleRight);
                    AddRowLabel(row, 0.84f, 0.98f, entry.note ?? string.Empty, TextMuted, 12);
                }
            }

            if (AddCollapsibleSection("base_sponsors", "贊助"))
            {
                SponsorContract[] active = state.sponsors
                    .Where(x => x.clubId == club.clubId)
                    .OrderByDescending(x => x.tier)
                    .ThenBy(x => x.sponsorId)
                    .ToArray();
                int monthKey = SponsorRules.CalendarMonthKey(_content.Balance, state.clockHour);
                bool signedThisMonth = state.lastSponsorSignMonthKey == monthKey;
                AddMutedLine(
                    $"合作中 {active.Length}/{SponsorRules.MaxActiveSponsorsPerClub} · " +
                    (signedThisMonth ? "本月已簽約，下月可再洽談一家" : "本月尚可簽訂一家新贊助"));

                if (active.Length == 0)
                    AddMutedLine("目前無贊助合約。");
                foreach (SponsorContract contract in active)
                {
                    if (!_content.Sponsors.TryGetValue(contract.sponsorId, out SponsorDefinition def))
                        continue;
                    RectTransform row = AddRow(40f);
                    AddRowLabel(row, 0.02f, 0.72f,
                        $"{def.displayName} · 滿意度 {contract.satisfaction} · 剩餘 {contract.remainingMonths} 月",
                        TextPrimary, 14);
                    AddRowLabel(row, 0.74f, 0.98f,
                        $"${contract.monthlyFee:N0}/月",
                        TextMuted, 13, TextAnchor.MiddleRight);
                }

                if (active.Length < SponsorRules.MaxActiveSponsorsPerClub)
                {
                    AddMutedLine("可洽談贊助：");
                    foreach (SponsorDefinition definition in _content.Sponsors.Values.OrderBy(x => x.tier))
                    {
                        if (active.Any(x => string.Equals(x.sponsorId, definition.id, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        RectTransform row = AddRow(42f);
                        AddRowLabel(row, 0.02f, 0.72f,
                            $"{definition.displayName} · {definition.tier} · ${definition.baseMonthlyFee:N0}/月",
                            TextPrimary, 14);
                        string sponsorId = definition.id;
                        AddRowButton(row, 0.76f, 0.98f, "洽談", () => ShowSponsorNegotiationPanel(sponsorId));
                    }
                }
            }
        }

        private void RefreshOfficialMatchBanner()
        {
            if (_mapHost == null || _session == null || !_session.AwaitsOfficialPlayerMatch)
            {
                if (_officialMatchBanner != null)
                    _officialMatchBanner.SetActive(false);
                return;
            }

            if (_officialMatchBanner == null)
                BuildOfficialMatchBanner();
            else
            {
                // Rebuild leftover framed / titled / wide layouts from older Play sessions.
                RectTransform rt = _officialMatchBanner.GetComponent<RectTransform>();
                float width = rt != null ? rt.anchorMax.x - rt.anchorMin.x : 1f;
                bool legacyChrome =
                    _officialMatchBanner.transform.Find("Face/BannerText") != null ||
                    _officialMatchBanner.transform.Find("Face/PlayOfficial") != null ||
                    _officialMatchBanner.GetComponent<Button>() == null ||
                    width > 0.16f;
                if (legacyChrome)
                {
                    Destroy(_officialMatchBanner);
                    _officialMatchBanner = null;
                    BuildOfficialMatchBanner();
                }
            }

            _officialMatchBanner.SetActive(true);
            Text playLabel = _officialMatchBanner.transform.Find("Face/Text")?.GetComponent<Text>();
            if (playLabel != null)
            {
                playLabel.text = "比賽開始";
                ApplyFilledButtonLabel(playLabel);
            }
        }

        private void BuildOfficialMatchBanner()
        {
            // Standalone CTA only — no black frame / match-name strip. Half prior banner width (0.28 → 0.14).
            Button play = CreateGlassCapsuleButton(
                "PlayOfficial", "比賽開始", _mapHost.transform,
                new Vector2(0.40f, 0.07f), new Vector2(0.54f, 0.175f),
                AccentDim, Accent, LaunchOfficialCompetitionMatch, _uiFont700, fontSize: 20);
            Text playLabel = play.transform.Find("Face/Text")?.GetComponent<Text>();
            ApplyFilledButtonLabel(playLabel);
            _officialMatchBanner = play.gameObject;
            play.transform.SetAsLastSibling();
        }

        private void LaunchOfficialCompetitionMatch()
        {
            if (!_careerStarted || _session == null)
            {
                _status = "請先開始生涯";
                if (_statusText != null) _statusText.text = _status;
                return;
            }

            if (_session.NeedsOfficialMapVeto)
            {
                if (_mapBanPick == null)
                {
                    _mapBanPick = new MapBanPickPresentation(
                        _session,
                        _uiFont700 ?? _uiFont,
                        () => LaunchOfficialCompetitionMatchAfterVeto(),
                        msg =>
                        {
                            _status = msg;
                            if (_statusText != null) _statusText.text = _status;
                        },
                        _uiFontBlack,
                        _uiFontAnton);
                }

                Transform canvas = _shellRoot != null ? _shellRoot.transform.root : transform;
                if (_canvasRect != null)
                    canvas = _canvasRect;
                _mapBanPick.Open(canvas);
                _status = "請完成地圖禁用與選擇";
                if (_statusText != null) _statusText.text = _status;
                return;
            }

            LaunchOfficialCompetitionMatchAfterVeto();
        }

        private void LaunchOfficialCompetitionMatchAfterVeto()
        {
            if (_matchContent == null || !_session.TryBuildOfficialMatchLaunch(_matchContent, out CareerMatchLaunchContext.LaunchRequest launch))
            {
                _status = "目前陣容無法進入戰術對戰（對手或陣容資料不完整），改用快速結算";
                if (_session.AutoPlayPendingOfficialMatch())
                {
                    _persistence.Save(_session.State, _activeSaveSlot);
                    _status = "已以快速結算完成本場正式賽事";
                }
                RefreshAll();
                return;
            }

            try
            {
                _persistence.Save(_session.State, _activeSaveSlot);
                CareerMatchLaunchContext.SetLaunch(launch);
                _status = "正在載入正式對戰…";
                if (_statusText != null) _statusText.text = _status;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Dust2_QuickSim");
            }
            catch (Exception ex)
            {
                _status = "無法進入對戰：" + ex.Message;
                if (_statusText != null) _statusText.text = _status;
            }
        }

        private void DrawRoster()
        {
            CareerState state = _session.State;
            PlayerCareerState[] roster = state.players
                .Where(x => x.clubId == state.playerClubId)
                .ToArray();

            PlayerCareerState[] starters = roster
                .Where(p => !ClubEstateRules.IsReserveSquadStatus(p.squadStatus))
                .ToArray();
            PlayerCareerState[] bench = roster
                .Where(p => string.Equals(
                    ClubEstateRules.NormalizeSquadStatus(p.squadStatus),
                    ClubEstateRules.SquadBench,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            PlayerCareerState[] academyOrPromoted = roster
                .Where(p =>
                {
                    string s = ClubEstateRules.NormalizeSquadStatus(p.squadStatus);
                    return string.Equals(s, ClubEstateRules.SquadAcademy, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(s, ClubEstateRules.SquadPromoted, StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();

            if (AddCollapsibleSection("roster_starters", "陣容"))
            {
                AddMutedLine("點選手名查看個人資訊 · squadStatus：starter／bench／academy／promoted（影響儲備力量）");
                AddRosterCardRow(starters);
            }

            if (bench.Length > 0 && AddCollapsibleSection("roster_bench", "替補"))
                AddRosterCardRow(bench);

            if (academyOrPromoted.Length > 0 && AddCollapsibleSection("roster_reserves", "青訓／新提拔"))
                AddRosterCardRow(academyOrPromoted);

            DrawTrainingBlock(roster);
        }

        private void AddRosterCardRow(PlayerCareerState[] players)
        {
            if (players == null || players.Length == 0)
            {
                AddMutedLine("無選手。");
                return;
            }

            for (int i = 0; i < players.Length; i++)
            {
                RectTransform row = AddRow(204f);
                CreateRosterCard(row, players[i], 0f, 1f);
            }
        }

        private void CreateRosterCard(RectTransform row, PlayerCareerState career, float x0, float x1)
        {
            CareerPlayerDefinition careerDef = _content.Players[career.playerId];
            PlayerDefinition match = null;
            if (_matchContent != null)
                _matchContent.Players.TryGetValue(career.playerId, out match);
            ContractState contract = _session.State.contracts.FirstOrDefault(x => x.playerId == career.playerId);

            Image card = CreateUiObject<Image>("Card_" + career.playerId, row);
            SetRect(card.rectTransform, new Vector2(x0, 0.04f), new Vector2(x1, 0.96f));
            UiLightChromeUtility.ApplySoftNavyPanel(card);
            card.raycastTarget = true;

            string playerId = career.playerId;
            string displayName = string.IsNullOrWhiteSpace(careerDef.displayName) ? career.playerId : careerDef.displayName;

            Button open = card.gameObject.AddComponent<Button>();
            open.targetGraphic = card;
            open.transition = Selectable.Transition.None;
            open.onClick.AddListener(() => OpenPlayerDetail(playerId));

            Text name = CreateText("Name", card.transform, 34, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontBlack);
            SetRect(name.rectTransform, new Vector2(0.03f, 0.74f), new Vector2(0.62f, 0.97f));
            name.text = displayName;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;

            CareerEditorialChrome.CreateBlock(
                card.transform, "NameRule",
                new Vector2(0.03f, 0.70f), new Vector2(0.28f, 0.732f),
                CareerEditorialChrome.Mustard);

            int loyalty = career.loyalty;
            int pressure = career.pressure;
            int emotion = match != null ? match.emotion : 0;
            int salary = contract != null
                ? contract.monthlySalary
                : (match != null ? match.monthlySalary : careerDef.monthlySalary);
            int health = career.health;
            bool hasMatch = match != null;

            string[] headers = { "忠誠", "壓力", "情緒", "薪資", "健康" };
            string[] values =
            {
                loyalty.ToString(),
                pressure.ToString(),
                hasMatch ? emotion.ToString() : "—",
                $"${salary:N0}",
                health.ToString()
            };
            bool[] numeric = { true, true, hasMatch, true, true };
            float[] weights = NormalizeColumnWeights(null, headers.Length);

            const float belowTop = 0.684f;
            const float belowBottom = 0.20f;
            float valueHeight = (belowTop - belowBottom) * 0.80f;
            float headerTop = belowTop - 0.008f;
            float headerBottom = headerTop - 0.072f;
            float valueTop = headerBottom - 0.01f;
            float valueBottom = valueTop - valueHeight;
            PlaceInlineTable(
                card.rectTransform, headers, values, numeric, weights,
                headerBottom, headerTop, valueBottom, valueTop,
                headerSize: 18, valueSize: 28, fillValueHeight: true);

            Text focusHint = CreateText("Focus", card.transform, 22, TextAnchor.LowerCenter, TextMuted, _uiFont700);
            SetRect(focusHint.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.185f));
            focusHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            focusHint.verticalOverflow = VerticalWrapMode.Truncate;
            focusHint.resizeTextForBestFit = true;
            focusHint.resizeTextMinSize = 16;
            focusHint.resizeTextMaxSize = 28;
            focusHint.text = FormatTrainingFocusShort(career.trainingFocus);
        }

        private void PlaceInlineTable(
            RectTransform parent, string[] headers, string[] values, bool[] numeric, float[] weights,
            float yHeader0, float yHeader1, float yValue0, float yValue1,
            int headerSize = 10, int valueSize = 12, bool fillValueHeight = false)
        {
            float x = 0f;
            for (int i = 0; i < headers.Length; i++)
            {
                float w = weights[i];
                const float inset = 0.006f;
                float left = x + inset;
                float right = x + w - inset;

                Text header = CreateText($"H{i}", parent, headerSize, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
                SetRect(header.rectTransform, new Vector2(left, yHeader0), new Vector2(right, yHeader1));
                header.horizontalOverflow = HorizontalWrapMode.Wrap;
                header.verticalOverflow = VerticalWrapMode.Truncate;
                header.resizeTextForBestFit = fillValueHeight;
                header.resizeTextMinSize = 10;
                header.resizeTextMaxSize = headerSize;
                header.text = headers[i];

                bool isNumeric = numeric != null && i < numeric.Length && numeric[i];
                Text value = CreateText($"V{i}", parent, valueSize, TextAnchor.MiddleCenter,
                    isNumeric ? ProfileValue : TextPrimary, _uiFont700);
                SetRect(value.rectTransform, new Vector2(left, yValue0), new Vector2(right, yValue1));
                value.horizontalOverflow = HorizontalWrapMode.Wrap;
                value.verticalOverflow = VerticalWrapMode.Truncate;
                if (fillValueHeight)
                {
                    value.resizeTextForBestFit = true;
                    value.resizeTextMinSize = 22;
                    value.resizeTextMaxSize = Mathf.Max(valueSize, 84);
                }

                value.text = values[i] ?? string.Empty;

                x += w;
            }
        }

        private void DrawTrainingBlock(PlayerCareerState[] roster)
        {
            AddSectionTitle("每週訓練");
            RectTransform toggleRow = AddRow(42f);
            AddRowLabel(toggleRow, 0.02f, 0.55f,
                _trainingPanelOpen
                    ? "同時設定：能力（最多2）／地圖（最多3）／位置（1）"
                    : "展開訓練面板以設定每週訓練計畫",
                TextMuted, 13);
            AddRowButton(toggleRow, 0.70f, 0.98f, _trainingPanelOpen ? "收起訓練" : "訓練", () =>
            {
                _trainingPanelOpen = !_trainingPanelOpen;
                if (_trainingPanelOpen && string.IsNullOrEmpty(_trainingUiPlayerId) && roster.Length > 0)
                {
                    _trainingUiPlayerId = roster[0].playerId;
                    SyncTrainingUiFromPlayer(_trainingUiPlayerId);
                }
                RebuildContent();
            });

            if (!_trainingPanelOpen)
                return;

            if (roster.Length == 0)
            {
                AddMutedLine("沒有可訓練的選手。");
                return;
            }

            if (string.IsNullOrEmpty(_trainingUiPlayerId) ||
                roster.All(x => !string.Equals(x.playerId, _trainingUiPlayerId, StringComparison.OrdinalIgnoreCase)))
            {
                _trainingUiPlayerId = roster[0].playerId;
                SyncTrainingUiFromPlayer(_trainingUiPlayerId);
            }

            int rustWeeks = _content?.Balance?.inactivityRustWeeks ?? 8;
            AddMutedLine(
                rustWeeks > 0
                    ? $"訓練時每小時微量成長，熟練度顯示至小數一位。連續 {rustWeeks} 週未訓練且未比賽時，熟練度會因閒置下滑。"
                    : "訓練時每小時微量成長，熟練度顯示至小數一位。閒置退步目前已關閉。");

            AddMutedLine("1. 選擇選手");
            RectTransform playerRow = AddRow(44f);
            int shown = Mathf.Min(roster.Length, 8);
            for (int i = 0; i < shown; i++)
            {
                PlayerCareerState p = roster[i];
                string id = p.playerId;
                string label = _content.Players[id].displayName;
                float step = 1f / shown;
                float a = i * step + 0.01f;
                float b = (i + 1) * step - 0.01f;
                bool on = string.Equals(_trainingUiPlayerId, id, StringComparison.OrdinalIgnoreCase);
                CreateGlassCapsuleButton(
                    "TrainPick_" + id, label, playerRow,
                    new Vector2(a, 0.15f), new Vector2(b, 0.85f),
                    ButtonEdge, on ? AccentDim : ButtonFace,
                    () =>
                    {
                        _trainingUiPlayerId = id;
                        SyncTrainingUiFromPlayer(id);
                        RebuildContent();
                    },
                    _playerIdFont);
            }

            string attrEff = _trainingUiAttrs.Count == 1 ? "效率 ×2" :
                _trainingUiAttrs.Count == 2 ? "效率 ×1" : "未選";
            AddMutedLine($"2. 能力（最多 2 · 單項翻倍）· {attrEff}");
            RectTransform attrRow = AddRow(42f);
            string[] attrs = TrainingPlanRules.AttributeIds;
            for (int i = 0; i < attrs.Length; i++)
            {
                string attr = attrs[i];
                float step = 1f / attrs.Length;
                bool on = _trainingUiAttrs.Contains(attr, StringComparer.OrdinalIgnoreCase);
                CreateGlassCapsuleButton(
                    "TrainAttr_" + attr, TrainingPlanRules.AttributeLabel(attr), attrRow,
                    new Vector2(i * step + 0.002f, 0.15f), new Vector2((i + 1) * step - 0.002f, 0.85f),
                    ButtonEdge, on ? AccentDim : ButtonFace,
                    () =>
                    {
                        ToggleTrainingListItem(_trainingUiAttrs, attr, TrainingPlanRules.MaxAttributes);
                        RebuildContent();
                    },
                    _uiFont700);
            }

            string mapEff = _trainingUiMaps.Count == 1 ? "效率 ×2" :
                _trainingUiMaps.Count == 2 ? "效率 ×1.5" :
                _trainingUiMaps.Count == 3 ? "效率 ×1" : "未選";
            AddMutedLine($"3. 地圖（最多 3 · 兩張×1.5／單張×2）· {mapEff}");
            RectTransform mapRow = AddRow(42f);
            string[] maps = MapOrder;
            for (int i = 0; i < maps.Length; i++)
            {
                string map = maps[i];
                float step = 1f / maps.Length;
                bool on = _trainingUiMaps.Contains(map, StringComparer.OrdinalIgnoreCase);
                CreateGlassCapsuleButton(
                    "TrainMap_" + map, map, mapRow,
                    new Vector2(i * step + 0.002f, 0.15f), new Vector2((i + 1) * step - 0.002f, 0.85f),
                    ButtonEdge, on ? AccentDim : ButtonFace,
                    () =>
                    {
                        ToggleTrainingListItem(_trainingUiMaps, map, TrainingPlanRules.MaxMaps);
                        RebuildContent();
                    },
                    _uiFont700);
            }

            AddMutedLine("4. 位置（擇一）");
            RectTransform roleRow = AddRow(42f);
            string[] roles = RoleOrder;
            for (int i = 0; i < roles.Length; i++)
            {
                string role = roles[i];
                float step = 1f / roles.Length;
                bool on = string.Equals(_trainingUiRole, role, StringComparison.OrdinalIgnoreCase);
                CreateGlassCapsuleButton(
                    "TrainRole_" + role, RoleLabel(role), roleRow,
                    new Vector2(i * step + 0.005f, 0.15f), new Vector2((i + 1) * step - 0.005f, 0.85f),
                    ButtonEdge, on ? AccentDim : ButtonFace,
                    () =>
                    {
                        _trainingUiRole = string.Equals(_trainingUiRole, role, StringComparison.OrdinalIgnoreCase)
                            ? string.Empty
                            : role;
                        RebuildContent();
                    },
                    _uiFont700);
            }

            RectTransform applyRow = AddRow(52f);
            string preview = BuildTrainingFocusValueFromUi();
            AddRowLabel(applyRow, 0.02f, 0.58f, $"預覽：{FormatTrainingFocusShort(preview)}", ProfileValue, 12);
            AddRowButton(applyRow, 0.60f, 0.78f, "休息", () =>
            {
                if (!_session.SetTrainingFocus(_trainingUiPlayerId, string.Empty))
                    _status = "無法設定訓練";
                else
                {
                    SyncTrainingUiFromPlayer(_trainingUiPlayerId);
                    string name = _content.Players[_trainingUiPlayerId].displayName;
                    SetStatus($"{name} 本週休息（不訓練）",
                        new CareerUiNavLink(CareerUiDestination.Training, _trainingUiPlayerId));
                }
                RefreshAll();
            });
            AddRowButton(applyRow, 0.80f, 0.98f, "套用", () =>
            {
                string focus = BuildTrainingFocusValueFromUi();
                if (!_session.SetTrainingFocus(_trainingUiPlayerId, focus))
                {
                    _status = "無法設定訓練";
                }
                else
                {
                    string name = _content.Players[_trainingUiPlayerId].displayName;
                    SetStatus($"{name} 訓練：{FormatTrainingFocusShort(focus)}",
                        new CareerUiNavLink(CareerUiDestination.Training, _trainingUiPlayerId));
                }
                RefreshAll();
            });
        }

        private static void ToggleTrainingListItem(List<string> list, string id, int max)
        {
            int idx = list.FindIndex(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                list.RemoveAt(idx);
                return;
            }
            if (list.Count >= max)
                list.RemoveAt(0);
            list.Add(id);
        }

        private void SyncTrainingUiFromPlayer(string playerId)
        {
            _trainingUiAttrs.Clear();
            _trainingUiMaps.Clear();
            _trainingUiRole = string.Empty;
            PlayerCareerState player = _session?.State.Player(playerId);
            if (player == null)
                return;

            CareerTrainingPlan plan = TrainingPlanRules.Parse(player.trainingFocus);
            if (plan.attributes != null)
                _trainingUiAttrs.AddRange(plan.attributes.Take(TrainingPlanRules.MaxAttributes));
            if (plan.maps != null)
                _trainingUiMaps.AddRange(plan.maps.Take(TrainingPlanRules.MaxMaps));
            _trainingUiRole = plan.role ?? string.Empty;
        }

        private string BuildTrainingFocusValueFromUi()
        {
            return TrainingPlanRules.Serialize(new CareerTrainingPlan
            {
                attributes = new List<string>(_trainingUiAttrs),
                maps = new List<string>(_trainingUiMaps),
                role = _trainingUiRole
            });
        }

        private static string FormatTrainingFocusShort(string focus)
        {
            CareerTrainingPlan plan = TrainingPlanRules.Parse(focus);
            return TrainingPlanRules.FormatShort(plan, RoleLabel);
        }

        private void OpenPlayerDetail(string playerId)
        {
            if (string.IsNullOrEmpty(playerId) || _session == null) return;
            if (!_content.Players.ContainsKey(playerId)) return;
            if (_session.State.Player(playerId) == null) return;

            CloseCoachDetail();
            _selectedPlayerId = playerId;
            RefreshPlayerDetail(scrollToTop: true);
            if (_playerDetailRoot != null)
            {
                _playerDetailRoot.SetActive(true);
                _playerDetailRoot.transform.SetAsLastSibling();
                RefreshCanvasFrost();
            }
        }

        private void RefreshCanvasFrost()
        {
            if (_canvasFrostHost != null)
                _canvasFrostHost.RefreshBlur();
        }

        private void ClosePlayerDetail()
        {
            _selectedPlayerId = null;
            if (_playerDetailRoot != null)
                _playerDetailRoot.SetActive(false);
        }

        private void RefreshPlayerDetail(bool scrollToTop = false)
        {
            if (_playerDetailRoot == null || string.IsNullOrEmpty(_selectedPlayerId) || _session == null)
                return;

            float scrollPos = !scrollToTop && _playerDetailScroll != null
                ? _playerDetailScroll.verticalNormalizedPosition
                : 1f;

            PlayerCareerState career = _session.State.Player(_selectedPlayerId);
            if (career == null || !_content.Players.TryGetValue(_selectedPlayerId, out CareerPlayerDefinition careerDef))
            {
                ClosePlayerDetail();
                return;
            }

            PlayerDefinition match = null;
            if (_matchContent != null)
                _matchContent.Players.TryGetValue(_selectedPlayerId, out match);

            ContractState contract = _session.State.contracts.FirstOrDefault(x => x.playerId == career.playerId);

            string detailName = string.IsNullOrWhiteSpace(careerDef.displayName)
                ? career.playerId
                : careerDef.displayName;
            if (_playerDetailCaption != null)
            {
                _playerDetailCaption.font = _playerIdFont;
                _playerDetailCaption.fontStyle = FontStyle.Normal;
                _playerDetailCaption.text = detailName;
            }

            ClearPlayerDetailContent();
            _activeDetailContent = _playerDetailContent;
            BuildPlayerDetailContent(career, careerDef, match, contract);

            Canvas.ForceUpdateCanvases();
            if (_playerDetailContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_playerDetailContent);
            if (_playerDetailScroll != null)
                _playerDetailScroll.verticalNormalizedPosition = scrollPos;
        }

        private void ClearPlayerDetailContent()
        {
            if (_playerDetailContent == null) return;
            for (int i = _playerDetailContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_playerDetailContent.GetChild(i).gameObject);
        }

        private void BuildPlayerDetailContent(
            PlayerCareerState career, CareerPlayerDefinition careerDef,
            PlayerDefinition match, ContractState contract)
        {
            const string missing = "缺資料";

            AddProfileSection("個人資料");
            AddProfileTable(
                new[] { "職業ID", "資歷", "年齡", "地區/國家", "語言歸類" },
                match != null
                    ? new[]
                    {
                        match.id,
                        match.careerStartYear.ToString(),
                        (career.age > 0 ? career.age : match.age).ToString(),
                        string.IsNullOrEmpty(match.country) ? missing : match.country,
                        FormatLanguages(match.languages)
                    }
                    : new[] { career.playerId, missing, career.age.ToString(), missing, missing },
                new[] { false, true, true, false, false },
                new[] { 1.1f, 0.7f, 0.7f, 1.2f, 3.3f },
                headerHeight: 32f,
                valueHeight: 56f);

            DevelopmentRules.EnsureProgressLists(
                career, careerDef?.role, match?.attributes,
                match?.roleFitness, match?.mapFitness, match?.dust2Grade);
            int combatTotal = DevelopmentRules.SumCombatAttributes(career.attributes);
            if (combatTotal <= 0 && match?.attributes != null)
                combatTotal = DevelopmentRules.SumCombatAttributes(match.attributes);
            AddProfileSection(combatTotal > 0 ? $"戰鬥屬性 {combatTotal}" : "戰鬥屬性");
            if (career.attributes != null && career.attributes.Count > 0)
            {
                AddProfileTable(
                    new[] { "瞄準", "反應", "意識", "道具", "配合", "溝通", "首殺", "殘局" },
                    BuildCareerAttributeValues(career),
                    new[] { true, true, true, true, true, true, true, true },
                    headerHeight: 30f,
                    valueHeight: 38f);
            }
            else if (match?.attributes != null)
            {
                CombatAttributes a = match.attributes;
                AddProfileTable(
                    new[] { "瞄準", "反應", "意識", "道具", "配合", "溝通", "首殺", "殘局" },
                    new[]
                    {
                        a.aim.ToString(), a.reaction.ToString(), a.awareness.ToString(), a.utility.ToString(),
                        a.teamwork.ToString(), a.communication.ToString(), a.opening.ToString(), a.clutch.ToString()
                    },
                    new[] { true, true, true, true, true, true, true, true },
                    headerHeight: 30f,
                    valueHeight: 38f);
            }
            else
            {
                AddProfileMutedLine(missing);
            }

            AddProfileSection("位置適性");
            AddProfileTable(
                new[] { "突破", "狙擊", "步槍", "自由人", "輔助", "指揮" },
                BuildRoleFitnessValues(match, career),
                new[] { true, true, true, true, true, true },
                headerHeight: 30f,
                valueHeight: 38f);

            AddProfileSection("地圖適性");
            AddProfileTable(
                MapOrder,
                BuildMapFitnessValues(match, career),
                new[] { true, true, true, true, true, true, true, true },
                headerHeight: 30f,
                valueHeight: 38f);

            AddProfileKeyValue("主職業", RoleLabel(match?.primaryRole ?? careerDef.role), false);
            AddProfileKeyValue("副職業", RoleLabel(match?.secondaryRole), false);

            AddProfileSection("評級系統");
            if (match != null)
            {
                int marketValue = contract != null
                    ? MarketRules.CalculateMarketValue(career, careerDef, contract,
                        careerDef.role == "Awper" ? 120 : 100)
                    : match.marketValue;
                string historicalRaw = PlayerStandingRules.ResolveRaw(match.historicalStatus, match.evaluation)
                                       ?? PlayerStandingRules.FromFame(match.fame);
                string currentRaw = PlayerStandingRules.ResolveRaw(match.currentStatus, match.evaluation)
                                    ?? PlayerStandingRules.FromFame(match.fame);
                AddProfileRatingTableWithStandingIcons(
                    match.prestige,
                    match.fame,
                    match.potential,
                    $"${marketValue:N0}",
                    historicalRaw,
                    currentRaw);
            }
            else
            {
                AddProfileMutedLine(missing);
            }

            AddProfileSection("合約");
            if (contract != null)
            {
                AddProfileTable(
                    new[] { "月薪", "到期剩餘時間", "簽約金", "解約金", "獎金分成", "出場保證", "禁賣", "選手選擇權" },
                    new[]
                    {
                        $"${contract.monthlySalary:N0}",
                        $"{contract.remainingMonths} 月",
                        $"${contract.signingBonus:N0}",
                        $"${contract.releaseClause:N0}",
                        $"{contract.prizeShare:P0}",
                        $"{contract.appearanceGuarantee:P0}",
                        contract.noTrade ? "是" : "否",
                        contract.playerOption ? "是" : "否"
                    },
                    new[] { true, true, true, true, true, true, false, false },
                    new[] { 1.15f, 1.55f, 1.15f, 1.15f, 1.15f, 1.15f, 0.75f, 1.35f },
                    headerHeight: 42f,
                    valueHeight: 40f);
            }
            else if (match != null)
            {
                AddProfileTable(
                    new[] { "月薪", "到期剩餘時間", "簽約金", "解約金", "獎金分成", "出場保證", "禁賣", "選手選擇權" },
                    new[]
                    {
                        $"${match.monthlySalary:N0}", missing, missing, missing,
                        $"{match.bonusShare:P0}", missing, missing, missing
                    },
                    new[] { true, false, false, false, true, false, false, false },
                    new[] { 1.15f, 1.55f, 1.15f, 1.15f, 1.15f, 1.15f, 0.75f, 1.35f },
                    headerHeight: 42f,
                    valueHeight: 40f);
            }
            else
            {
                AddProfileMutedLine(missing);
            }

            AddProfileCombatAndDailyTraits(match);

            AddProfileSection("關係");
            RelationshipDefinition[] relations = match?.relationships ?? Array.Empty<RelationshipDefinition>();
            if (relations.Length == 0)
            {
                AddProfileMutedLine(match == null ? missing : "無");
            }
            else
            {
                float[] relationWeights = { 1.6f, 1f, 1f };
                AddProfileTableHeader(new[] { "對象", "好感度", "默契度" }, relationWeights, 30f);
                foreach (RelationshipDefinition rel in relations)
                {
                    string target = DemoZh.PlayerName(rel.targetId);
                    if (string.IsNullOrEmpty(target)) target = rel.targetId;
                    string affinity = rel.affinity >= 0 ? $"+{rel.affinity}" : rel.affinity.ToString();
                    AddProfileTableValues(
                        new[] { target, affinity, rel.chemistry.ToString() },
                        new[] { false, true, true },
                        relationWeights,
                        38f);
                }
            }
        }

        private void AddProfileSection(string title)
        {
            RectTransform row = CreateProfileRow(36f);
            Text text = CreateText("Section", row, 18, TextAnchor.MiddleLeft, Accent, _uiFont700);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(2f, 0f);
            text.text = title;

            Image underline = CreateUiObject<Image>("Underline", row);
            float underlineWidth = title != null && title.Length > 6 ? 0.28f : 0.16f;
            SetRect(underline.rectTransform, new Vector2(0f, 0.05f), new Vector2(underlineWidth, 0.18f));
            underline.color = AccentDim;
            underline.raycastTarget = false;
        }

        private void AddProfileMutedLine(string message)
        {
            RectTransform row = CreateProfileRow(32f);
            Text text = CreateText("Muted", row, 16, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            Stretch(text.rectTransform);
            text.text = message;
        }

        private void AddProfileKeyValue(string label, string value, bool numeric)
        {
            RectTransform row = CreateProfileRow(34f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 10f;
            layout.padding = new RectOffset(2, 2, 0, 0);

            Text labelText = CreateText("Label", row, 16, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            labelText.text = label + "：";
            var labelLe = labelText.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 88f;
            labelLe.minWidth = 88f;

            Text valueText = CreateText("Value", row, 16, TextAnchor.MiddleLeft,
                numeric ? ProfileValue : TextPrimary, _uiFont700);
            valueText.text = value;
            var valueLe = valueText.gameObject.AddComponent<LayoutElement>();
            valueLe.flexibleWidth = 1f;
        }

        private void AddProfileCombatAndDailyTraits(PlayerDefinition match)
        {
            Image row = CreateUiObject<Image>("TraitsSplitRow", ActiveDetailContent);
            row.color = Color.clear;
            row.raycastTarget = false;
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;
            rowLe.minHeight = 72f;

            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 16f;
            hlg.padding = new RectOffset(0, 0, 0, 4);

            var rowFitter = row.gameObject.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Transform combatCol = CreateTraitColumn(row.transform, "CombatTraits");
            Transform dailyCol = CreateTraitColumn(row.transform, "DailyTraits");

            string[] combatIds = match?.traitIds ?? Array.Empty<string>();
            string[] dailyIds = match?.dailyTraitIds ?? Array.Empty<string>();
            FillTraitColumn(
                combatCol,
                "戰鬥特性",
                combatIds,
                match == null,
                id => DemoZh.ProfileTraitName(id),
                id => DemoZh.ProfileTraitDescription(id));
            FillTraitColumn(
                dailyCol,
                "日常特性",
                dailyIds,
                match == null,
                id => DemoZh.ProfileTraitName(id),
                id => DemoZh.ProfileTraitDescription(id));
        }

        private static Transform CreateTraitColumn(Transform parent, string name)
        {
            Image col = CreateUiObject<Image>(name, parent);
            col.color = Color.clear;
            col.raycastTarget = false;
            var le = col.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minWidth = 120f;

            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            var fitter = col.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return col.transform;
        }

        private void FillTraitColumn(
            Transform column,
            string title,
            string[] traitIds,
            bool dataMissing,
            System.Func<string, string> nameOf,
            System.Func<string, string> descriptionOf)
        {
            Text titleText = CreateText("TraitColumnTitle", column, 18, TextAnchor.MiddleLeft, Accent, _uiFont700);
            titleText.text = title;
            var titleLe = titleText.gameObject.AddComponent<LayoutElement>();
            titleLe.minHeight = 28f;
            titleLe.preferredHeight = 28f;

            Image underline = CreateUiObject<Image>("Underline", column);
            underline.color = AccentDim;
            underline.raycastTarget = false;
            var underLe = underline.gameObject.AddComponent<LayoutElement>();
            underLe.minHeight = 3f;
            underLe.preferredHeight = 3f;
            underLe.preferredWidth = 72f;
            underLe.flexibleWidth = 0f;

            if (dataMissing)
            {
                AddTraitColumnMuted(column, "缺資料");
                return;
            }

            string[] ids = traitIds ?? Array.Empty<string>();
            if (ids.Length == 0)
            {
                AddTraitColumnMuted(column, "無");
                return;
            }

            bool any = false;
            foreach (string traitId in ids)
            {
                if (string.IsNullOrWhiteSpace(traitId))
                    continue;
                any = true;
                string name = nameOf != null ? nameOf(traitId) : traitId;
                string desc = descriptionOf != null ? descriptionOf(traitId) : string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                    name = traitId;
                if (string.IsNullOrWhiteSpace(desc))
                    desc = "無";
                AddProfileTrait(column, name, desc);
            }

            if (!any)
                AddTraitColumnMuted(column, "無");
        }

        private void AddTraitColumnMuted(Transform column, string message)
        {
            Text text = CreateText("Muted", column, 16, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            text.text = message;
            var le = text.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;
        }

        private void AddProfileTrait(string name, string description) =>
            AddProfileTrait(ActiveDetailContent, name, description);

        private void AddProfileTrait(Transform parent, string name, string description)
        {
            Image block = CreateUiObject<Image>("TraitBlock", parent);
            block.color = Color.clear;
            block.raycastTarget = false;
            var blockLe = block.gameObject.AddComponent<LayoutElement>();
            blockLe.flexibleWidth = 1f;
            blockLe.minHeight = 48f;

            var vlg = block.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3f;
            vlg.padding = new RectOffset(2, 2, 0, 8);

            var blockFitter = block.gameObject.AddComponent<ContentSizeFitter>();
            blockFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            blockFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text nameText = CreateText("TraitName", block.transform, 17, TextAnchor.MiddleLeft, ProfileTraitName, _uiFont700);
            nameText.text = name;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            var nameLe = nameText.gameObject.AddComponent<LayoutElement>();
            nameLe.minHeight = 26f;
            var nameFitter = nameText.gameObject.AddComponent<ContentSizeFitter>();
            nameFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text descText = CreateText("TraitDesc", block.transform, 15, TextAnchor.UpperLeft, ProfileValue, _uiFont700);
            descText.alignment = TextAnchor.UpperLeft;
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descText.verticalOverflow = VerticalWrapMode.Overflow;
            NarrativeUiText.ApplyBodyStyle(descText);
            NarrativeUiText.Set(descText, string.IsNullOrWhiteSpace(description) ? "無" : description);
            var descLe = descText.gameObject.AddComponent<LayoutElement>();
            descLe.minHeight = 22f;
            var descFitter = descText.gameObject.AddComponent<ContentSizeFitter>();
            descFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            descFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void AddProfileTable(
            string[] headers, string[] values, bool[] numeric,
            float[] weights = null, float headerHeight = 30f, float valueHeight = 38f)
        {
            float[] normalized = NormalizeColumnWeights(weights, headers.Length);
            AddProfileTableHeader(headers, normalized, headerHeight);
            AddProfileTableValues(values, numeric, normalized, valueHeight);
        }

        private void AddProfileTableHeader(string[] headers, float[] weights, float height = 30f)
        {
            float[] normalized = NormalizeColumnWeights(weights, headers.Length);
            RectTransform row = CreateProfileRow(height);
            float x = 0f;
            for (int i = 0; i < headers.Length; i++)
            {
                float w = normalized[i];
                PlaceProfileCell(row, $"H{i}", headers[i], x, x + w, 15, TextMuted, TextAnchor.MiddleCenter);
                x += w;
            }
        }

        private void AddProfileTableValues(
            string[] values, bool[] numeric, float[] weights, float height = 38f)
        {
            float[] normalized = NormalizeColumnWeights(weights, values.Length);
            RectTransform row = CreateProfileRow(height);
            float x = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float w = normalized[i];
                bool isNumeric = numeric != null && i < numeric.Length && numeric[i];
                Color color = isNumeric ? ProfileValue : TextPrimary;
                PlaceProfileCell(row, $"V{i}", values[i] ?? string.Empty, x, x + w, 16, color, TextAnchor.MiddleCenter);
                x += w;
            }
        }

        private void PlaceProfileCell(
            RectTransform row, string name, string content,
            float x0, float x1, int size, Color color, TextAnchor align)
        {
            // Inset slightly so adjacent columns don't visually collide.
            const float inset = 0.004f;
            float left = Mathf.Clamp01(x0 + inset);
            float right = Mathf.Clamp01(x1 - inset);
            if (right <= left)
            {
                left = Mathf.Clamp01(x0);
                right = Mathf.Clamp01(x1);
            }

            Text cell = CreateText(name, row, size, align, color, _uiFont700);
            SetRect(cell.rectTransform, new Vector2(left, 0.08f), new Vector2(right, 0.92f));
            cell.horizontalOverflow = HorizontalWrapMode.Wrap;
            cell.verticalOverflow = VerticalWrapMode.Truncate;
            cell.text = content;
        }

        private static float[] NormalizeColumnWeights(float[] weights, int count)
        {
            if (count <= 0) return Array.Empty<float>();
            if (weights == null || weights.Length != count)
            {
                float equal = 1f / count;
                var evenly = new float[count];
                for (int i = 0; i < count; i++) evenly[i] = equal;
                return evenly;
            }

            float sum = 0f;
            for (int i = 0; i < count; i++)
                sum += Mathf.Max(0.01f, weights[i]);

            var normalized = new float[count];
            for (int i = 0; i < count; i++)
                normalized[i] = Mathf.Max(0.01f, weights[i]) / sum;
            return normalized;
        }

        private RectTransform _activeDetailContent;

        private RectTransform ActiveDetailContent =>
            _activeDetailContent != null ? _activeDetailContent : _playerDetailContent;

        private RectTransform CreateProfileRow(float height)
        {
            Image row = CreateUiObject<Image>("ProfileRow", ActiveDetailContent);
            row.color = Color.clear;
            row.raycastTarget = false;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return row.rectTransform;
        }

        private static readonly string[] RoleOrder =
            { "Entry", "Awper", "Rifler", "Lurker", "Support", "Igl" };

        private static readonly string[] MapOrder =
            { "Vertigo", "Overpass", "Ancient", "Mirage", "Nuke", "Inferno", "Dust II", "Train" };

        private static string[] BuildCareerAttributeValues(PlayerCareerState career)
        {
            var parts = new string[TrainingPlanRules.AttributeIds.Length];
            for (int i = 0; i < TrainingPlanRules.AttributeIds.Length; i++)
            {
                string id = TrainingPlanRules.AttributeIds[i];
                CareerNamedStat stat = career.attributes?.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                parts[i] = stat == null ? "缺資料" : DevelopmentRules.FormatProficiency(stat.value);
            }
            return parts;
        }

        private static string[] BuildRoleFitnessValues(PlayerDefinition match, PlayerCareerState career = null)
        {
            var parts = new string[RoleOrder.Length];
            for (int i = 0; i < RoleOrder.Length; i++)
            {
                string roleId = RoleOrder[i];
                CareerNamedStat careerStat = career?.roleProficiencies?.FirstOrDefault(x =>
                    string.Equals(x.id, roleId, StringComparison.OrdinalIgnoreCase));
                RoleFitness fit = match?.roleFitness?.FirstOrDefault(x =>
                    string.Equals(x.role, roleId, StringComparison.OrdinalIgnoreCase));

                string grade = FirstNonEmpty(careerStat?.grade, fit?.grade);
                float proficiency = careerStat != null
                    ? careerStat.value
                    : fit != null ? fit.proficiency : 0f;

                if (!string.IsNullOrWhiteSpace(grade))
                    parts[i] = FormatFitness(grade, proficiency);
                else if (careerStat != null)
                    parts[i] = DevelopmentRules.FormatProficiency(careerStat.value);
                else
                    parts[i] = "缺資料";
            }
            return parts;
        }

        private static string[] BuildMapFitnessValues(PlayerDefinition match, PlayerCareerState career = null)
        {
            var parts = new string[MapOrder.Length];
            MapFitness[] maps = match?.mapFitness ?? Array.Empty<MapFitness>();
            for (int i = 0; i < MapOrder.Length; i++)
            {
                string mapName = MapOrder[i];
                CareerNamedStat careerStat = career?.mapProficiencies?.FirstOrDefault(x =>
                    string.Equals(x.id, mapName, StringComparison.OrdinalIgnoreCase));
                MapFitness fit = maps.FirstOrDefault(x =>
                    string.Equals(x.map, mapName, StringComparison.OrdinalIgnoreCase));

                string grade = FirstNonEmpty(careerStat?.grade, fit?.grade);
                if (string.IsNullOrWhiteSpace(grade) &&
                    string.Equals(mapName, "Dust II", StringComparison.OrdinalIgnoreCase) &&
                    match != null)
                    grade = match.dust2Grade;

                float proficiency = careerStat != null
                    ? careerStat.value
                    : fit != null
                        ? fit.proficiency
                        : string.Equals(mapName, "Dust II", StringComparison.OrdinalIgnoreCase) && match != null
                            ? match.dust2Proficiency
                            : 0f;

                if (!string.IsNullOrWhiteSpace(grade))
                    parts[i] = FormatFitness(grade, proficiency);
                else if (careerStat != null)
                    parts[i] = DevelopmentRules.FormatProficiency(careerStat.value);
                else
                    parts[i] = "缺資料";
            }

            return parts;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return null;
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static string FormatFitness(string grade, float proficiency)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return "缺資料";
            string normalized = GradeScale.Normalize(grade);
            if (GradeScale.IsMax(normalized))
                return normalized + "/MAX";
            return $"{normalized}/{DevelopmentRules.FormatProficiency(Mathf.Clamp(proficiency, 0f, 100f))}";
        }

        private static string FormatFitness(string grade, int proficiency) =>
            FormatFitness(grade, (float)proficiency);

        private static string FormatLanguages(string[] languages)
        {
            if (languages == null || languages.Length == 0)
                return "缺資料";
            // Compact but still list-like; keeps the language column width stable.
            return string.Join(", ", languages);
        }

        private static string StatusOrMissing(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary)) return primary;
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;
            return "缺資料";
        }

        private static string RoleLabel(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "無";
            switch (role)
            {
                case "Entry": return "突破手";
                case "Awper": return "狙擊手";
                case "Rifler": return "步槍手";
                case "Lurker": return "自由人";
                case "Support": return "輔助";
                case "Igl": return "指揮";
                case "Trade": return "補槍";
                default: return role;
            }
        }

        private void DrawMarket()
        {
            CareerState state = _session.State;
            bool quoteEnabled = TransferNegotiationRules.QuoteSystemEnabled;

            if (AddCollapsibleSection("market_offers", "收到的報價"))
            {
                if (!quoteEnabled)
                    AddMutedLine("報價系統暫時關閉。");
                else
                {
                TransferOffer[] incoming = state.offers.Where(x =>
                    x.sellerClubId == state.playerClubId &&
                    (x.status == TransferOfferStatus.Pending || x.status == TransferOfferStatus.Countered)).ToArray();
                if (incoming.Length == 0)
                    AddMutedLine("目前沒有待處理報價。");

                foreach (TransferOffer offer in incoming)
                {
                    RectTransform row = AddRow(44f);
                    AddRowLabel(row, 0.015f, 0.52f,
                        $"{offer.buyerClubId} 報價 {_content.Players[offer.playerId].displayName} ${offer.fee:N0}",
                        TextPrimary, 14);
                    string offerId = offer.id;
                    double fee = offer.fee;
                    AddRowButton(row, 0.55f, 0.66f, "暫允", () =>
                    {
                        _session.ResolvePlayerOffer(offerId, TransferOfferStatus.Accepted);
                        SetStatus("已暫允轉會費，等待個人條款", new CareerUiNavLink(CareerUiDestination.TransferMarket));
                        RefreshAll();
                    });
                    AddRowButton(row, 0.68f, 0.79f, "拒絕", () =>
                    {
                        _session.ResolvePlayerOffer(offerId, TransferOfferStatus.Rejected);
                        _status = "已拒絕轉會報價";
                        RefreshAll();
                    });
                    AddRowButton(row, 0.81f, 0.99f, "反提案 +20%", () =>
                    {
                        _session.ResolvePlayerOffer(offerId, TransferOfferStatus.Countered,
                            CareerMath.RoundMoney(fee * 1.2));
                        _status = "已提出反提案";
                        RefreshAll();
                    });
                }
                }
            }

            if (AddCollapsibleSection("market_active", "進行中的談判"))
            {
                if (!quoteEnabled)
                    AddMutedLine("報價系統暫時關閉。");
                else
                {
                TransferOffer[] active = state.offers.Where(x =>
                    (x.buyerClubId == state.playerClubId || x.sellerClubId == state.playerClubId) &&
                    (x.status == TransferOfferStatus.ClubAgreed ||
                     x.status == TransferOfferStatus.PersonalTerms)).ToArray();
                TransferOffer[] outbid = state.offers.Where(x =>
                    (x.buyerClubId == state.playerClubId || x.sellerClubId == state.playerClubId) &&
                    x.status == TransferOfferStatus.Outbid).Take(5).ToArray();

                if (active.Length == 0 && outbid.Length == 0)
                    AddMutedLine("目前沒有進行中的談判。");

                foreach (TransferOffer offer in active)
                {
                    string name = _content.Players.TryGetValue(offer.playerId, out CareerPlayerDefinition def)
                        ? def.displayName
                        : offer.playerId;
                    string phase = offer.status == TransferOfferStatus.ClubAgreed
                        ? "轉會費已暫允"
                        : "個人條款談判中";
                    string side = offer.buyerClubId == state.playerClubId ? "買方" : "賣方";
                    RectTransform row = AddRow(48f);
                    AddRowLabel(row, 0.015f, 0.58f,
                        $"{side} · {name} · {phase} · ${offer.fee:N0}",
                        TextPrimary, 13);

                    if (offer.buyerClubId == state.playerClubId)
                    {
                        string offerId = offer.id;
                        int salary = offer.offeredSalary > 0
                            ? offer.offeredSalary
                            : offer.askedSalary > 0
                                ? offer.askedSalary
                                : MarketRules.AskingSalary(offer.fee, 24, 50);
                        AddRowButton(row, 0.60f, 0.78f, "提條款", () =>
                        {
                            if (_session.ProposePlayerContract(offerId, salary, CareerMath.RoundMoney(salary * 0.5d), 24))
                                _status = "已提出個人條款";
                            else
                                _status = "無法提出個人條款";
                            RefreshAll();
                        });
                        AddRowButton(row, 0.80f, 0.99f, "確認簽約", () =>
                        {
                            if (offer.status != TransferOfferStatus.PersonalTerms)
                                _session.ProposePlayerContract(offerId, salary, CareerMath.RoundMoney(salary * 0.5d), 24);
                            if (_session.ResolvePlayerContractTerms(offerId, true))
                                SetStatus("轉會完成", new CareerUiNavLink(CareerUiDestination.TransferMarket));
                            else
                                _status = "球員尚未接受個人條款，可再調整後重試";
                            RefreshAll();
                        });
                    }
                }

                foreach (TransferOffer offer in outbid)
                {
                    string name = _content.Players.TryGetValue(offer.playerId, out CareerPlayerDefinition def)
                        ? def.displayName
                        : offer.playerId;
                    AddMutedLine($"被搶標：{name} · 原報價 ${offer.fee:N0}");
                }
                }
            }

            if (AddCollapsibleSection("market_targets", "市場目標（窗口第 1–4、27–30 週）"))
            {
                if (!quoteEnabled)
                    AddMutedLine("報價系統暫時關閉。");
                else if (!MarketTargetsUiEnabled)
                {
                    AddMutedLine("暫時關閉。");
                }
                else
                {
                AddMutedLine("製作中，未完善。");
                AddMutedLine("列表僅顯示可能最低價；實際報價請自行填寫。詳細屬性請按資訊。困難模式他隊能力需分析師揭露。");
                foreach (PlayerCareerState player in state.players.Where(x =>
                             !string.IsNullOrEmpty(x.clubId) && x.clubId != state.playerClubId).Take(12))
                {
                    CareerPlayerDefinition definition = _content.Players[player.playerId];
                    ContractState contract = state.contracts.FirstOrDefault(x => x.playerId == player.playerId);
                    int minFee = _session.EstimateMinimumTransferFee(player.playerId);
                    string historical = ResolveHistoricalStanding(player.playerId, player);
                    string current = ResolveCurrentStanding(player.playerId, player);
                    string combatTotalText = FormatMarketCombatTotal(player, definition);
                    string playerLabel = string.IsNullOrWhiteSpace(definition.displayName)
                        ? player.playerId
                        : definition.displayName;

                    if (!_marketOfferDrafts.ContainsKey(player.playerId))
                        _marketOfferDrafts[player.playerId] = minFee.ToString();

                    RectTransform info = AddRow(52f);
                    AddRowLabel(info, 0.015f, 0.40f,
                        $"{playerLabel} 能力值 : {combatTotalText}", TextPrimary, 13);
                    string detailId = player.playerId;
                    AddRowButton(info, 0.405f, 0.50f, "資訊", () => OpenPlayerDetail(detailId), 13);
                    PlaceStandingPair(info, historical, current, 0.56f, 0.62f, 28f);
                    AddRowLabel(info, 0.68f, 0.99f,
                        $"可能最低 ${minFee:N0}", TextMuted, 11, TextAnchor.MiddleRight);

                    RectTransform offerRow = AddRow(44f);
                    string playerId = player.playerId;
                    int clause = contract?.releaseClause ?? 0;
                    AddRowLabel(offerRow, 0.015f, 0.12f, "報價", TextMuted, 12);
                    InputField feeInput = AddRowInputField(
                        offerRow, 0.13f, 0.42f, _marketOfferDrafts[playerId]);
                    string capturedId = playerId;
                    feeInput.onEndEdit.AddListener(text =>
                    {
                        _marketOfferDrafts[capturedId] = text ?? string.Empty;
                    });
                    AddRowButton(offerRow, 0.44f, 0.68f, "提出報價", () =>
                    {
                        _marketOfferDrafts.TryGetValue(capturedId, out string draft);
                        if (feeInput != null && !string.IsNullOrWhiteSpace(feeInput.text))
                            draft = feeInput.text;
                        if (!TryParseMoney(draft, out int fee) || fee <= 0)
                        {
                            _status = "請輸入有效的轉會費金額";
                            return;
                        }
                        _marketOfferDrafts[capturedId] = fee.ToString();
                        TransferOffer made = _session.MakeTransferOffer(capturedId, fee);
                        if (made != null)
                            _status = made.releaseClauseBid
                                ? "已觸發違約金，進入個人條款"
                                : "已提出報價，等待對方俱樂部回應";
                        else if (!_session.ClubInterestedInSigning(capturedId))
                            _status = "陣容已滿：僅在明顯戰力提升或缺少狙擊手時才會積極引援";
                        else
                            _status = "無法提出報價：窗口、資金或合約條件不符";
                        RefreshAll();
                    });
                    if (clause > 0)
                    {
                        int clauseFee = clause;
                        AddRowButton(offerRow, 0.70f, 0.99f, "違約金", () =>
                        {
                            TransferOffer made = _session.MakeTransferOffer(capturedId, clauseFee);
                            if (made != null && made.releaseClauseBid)
                                _status = "已觸發違約金買斷，請談妥個人條款";
                            else if (made != null)
                                _status = "已提出等同違約金的報價";
                            else
                                _status = "無法觸發違約金：窗口或資金不足";
                            RefreshAll();
                        });
                    }
                    else
                    {
                        AddRowButton(offerRow, 0.70f, 0.99f, "用最低價", () =>
                        {
                            int floor = _session.EstimateMinimumTransferFee(capturedId);
                            _marketOfferDrafts[capturedId] = floor.ToString();
                            if (feeInput != null)
                                feeInput.text = floor.ToString();
                            _status = $"已填入可能最低價 ${floor:N0}，請再按提出報價";
                        });
                    }
                }
                }
            }
        }

        private string ResolveHistoricalStanding(string playerId, PlayerCareerState career)
        {
            PlayerDefinition match = null;
            _matchContent?.Players.TryGetValue(playerId, out match);
            string raw = PlayerStandingRules.ResolveRaw(match?.historicalStatus, match?.evaluation);
            if (string.IsNullOrEmpty(raw))
                raw = PlayerStandingRules.FromFame(career?.fame ?? match?.fame ?? 50);
            return raw;
        }

        private string ResolveCurrentStanding(string playerId, PlayerCareerState career)
        {
            PlayerDefinition match = null;
            _matchContent?.Players.TryGetValue(playerId, out match);
            string raw = PlayerStandingRules.ResolveRaw(match?.currentStatus, match?.evaluation);
            if (!string.IsNullOrEmpty(raw))
                return raw;

            int fame = career?.fame ?? match?.fame ?? 50;
            float rating = career?.recentRating ?? 1f;
            int blended = (int)CareerMath.Clamp(fame + (rating - 1f) * 40f, 0, 100);
            return PlayerStandingRules.FromFame(blended);
        }

        private string FormatMarketCombatTotal(PlayerCareerState player, CareerPlayerDefinition definition)
        {
            CareerState state = _session.State;
            PlayerDefinition match = null;
            _matchContent?.Players.TryGetValue(player.playerId, out match);
            DevelopmentRules.EnsureProgressLists(
                player, definition?.role, match?.attributes,
                match?.roleFitness, match?.mapFitness, match?.dust2Grade);

            if (!CareerDifficultyRules.CanRevealOpponentAttributes(state, state.playerClubId) &&
                !string.Equals(player.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase))
                return "需分析師";

            int total = DevelopmentRules.SumCombatAttributes(player.attributes);
            if (total <= 0 && match != null)
                total = DevelopmentRules.SumCombatAttributes(match.attributes);
            return total.ToString();
        }

        private static bool TryParseMoney(string text, out int amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            string cleaned = text.Trim().Replace(",", "").Replace("$", "").Replace(" ", "");
            return int.TryParse(cleaned, out amount) && amount > 0;
        }

        private InputField AddRowInputField(RectTransform row, float x0, float x1, string text)
        {
            Image box = CreateUiObject<Image>("Input", row);
            SetRect(box.rectTransform, new Vector2(x0, 0.12f), new Vector2(x1, 0.88f));
            box.color = PanelInner;

            Text value = CreateText("Text", box.transform, 13, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(value.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            value.supportRichText = false;

            Text placeholder = CreateText("Placeholder", box.transform, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(placeholder.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            placeholder.text = "金額";
            placeholder.supportRichText = false;

            InputField field = box.gameObject.AddComponent<InputField>();
            field.textComponent = value;
            field.placeholder = placeholder;
            field.contentType = InputField.ContentType.IntegerNumber;
            field.text = text ?? string.Empty;
            return field;
        }

        private void RefreshMapSchedule()
        {
            if (_mapScheduleContent == null || _session == null) return;

            // First populate (or empty list) should open at the top (January), not December.
            bool restoreScroll = _mapScheduleContent.childCount > 0;
            float scrollPos = restoreScroll && _mapScheduleScroll != null
                ? _mapScheduleScroll.verticalNormalizedPosition
                : 1f;

            for (int i = _mapScheduleContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(_mapScheduleContent.GetChild(i).gameObject);

            _uiListRoot = _mapScheduleContent;
            try
            {
                DrawCompetitionSchedule();
            }
            finally
            {
                _uiListRoot = null;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_mapScheduleContent);
            if (_mapScheduleScroll != null)
            {
                // Layout rebuild can reset the bar; apply twice so the top sticks.
                _mapScheduleScroll.verticalNormalizedPosition = scrollPos;
                Canvas.ForceUpdateCanvases();
                _mapScheduleScroll.verticalNormalizedPosition = scrollPos;
            }
        }

        private void DrawCompetitionSchedule()
        {
            CareerState state = _session.State;
            int now = state.clockHour;
            DateTime from = CareerCalendar.ToDateTime(_content.Balance, now);
            DateTime to = from.AddHours(CareerCalendar.ScheduleHorizonHours);
            AddSectionTitle($"賽事行程 · {from:yyyy.M.d} — {to:yyyy.M.d}");

            CompetitionDefinition[] schedule = _content.Competitions.Values
                .Where(x => CareerCalendar.InScheduleWindow(
                    x, _content.Balance, now, CareerCalendar.ScheduleHorizonHours, state.flags))
                .OrderBy(x => x.startHour)
                .ToArray();

            if (schedule.Length == 0)
                AddMutedLine("近六個月內尚無排程賽事。");

            int lastYear = -1;
            int lastMonth = -1;
            foreach (CompetitionDefinition competition in schedule)
            {
                DateTime start = CareerCalendar.ToDateTime(_content.Balance, competition.startHour);
                if (start.Year != lastYear || start.Month != lastMonth)
                {
                    lastYear = start.Year;
                    lastMonth = start.Month;
                    AddSectionTitle($"{lastYear}年{lastMonth}月");
                }

                CompetitionEntry entry = state.competitionEntries.FirstOrDefault(x =>
                    x.clubId == state.playerClubId && x.competitionId == competition.id);
                bool live = CareerCalendar.IsLive(competition, now);
                bool registerable = CareerCalendar.IsRegisterableThisMonth(
                    competition, _content.Balance, now);

                bool showEnterMatch = _session.AwaitsOfficialPlayerMatch &&
                                        state.competitionRun?.competitionId == competition.id;

                string dateLine = CareerCalendar.FormatDateRange(_content.Balance, competition);
                string lineBottom = competition.displayName;

                const float rowHeight = 112f;
                AddMapScheduleCompetitionRow(
                    competition, entry, live, registerable, showEnterMatch, now,
                    dateLine,
                    lineBottom,
                    rowHeight);
            }
        }

        private void AddMapScheduleCompetitionRow(
            CompetitionDefinition competition,
            CompetitionEntry entry,
            bool live,
            bool registerable,
            bool showEnterMatch,
            int now,
            string dateLine,
            string lineBottom,
            float rowHeight = 72f)
        {
            RectTransform row = AddRow(rowHeight);
            row.gameObject.name = "ScheduleRow_" + competition.id;
            Image paper = row.GetComponent<Image>();
            if (paper != null)
                paper.color = new Color(
                    CareerEditorialChrome.Ink.r,
                    CareerEditorialChrome.Ink.g,
                    CareerEditorialChrome.Ink.b,
                    0.78f);
            Image rowFace = CreateUiObject<Image>("RowFace", row);
            Stretch(rowFace.rectTransform);
            rowFace.rectTransform.offsetMin = new Vector2(6f, 6f);
            rowFace.rectTransform.offsetMax = new Vector2(-6f, -6f);
            rowFace.color = CareerEditorialChrome.Paper;
            rowFace.raycastTarget = false;

            string tierBit = string.Empty;
            if (TryGetTierBadgeStyle(competition.tier, out _, out string tierLabel) && !string.IsNullOrEmpty(tierLabel))
                tierBit = tierLabel + "  ";

            Text date = CreateText("Date", row, 14, TextAnchor.MiddleLeft, CareerEditorialChrome.InkMuted, _uiFont700);
            SetRect(date.rectTransform, new Vector2(0.03f, 0.62f), new Vector2(0.58f, 0.94f));
            date.fontStyle = FontStyle.Normal;
            date.text = tierBit + dateLine;
            date.horizontalOverflow = HorizontalWrapMode.Wrap;
            date.verticalOverflow = VerticalWrapMode.Truncate;
            date.raycastTarget = false;

            Text name = CreateText("Name", row, 22, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontBlack);
            SetRect(name.rectTransform, new Vector2(0.03f, 0.10f), new Vector2(0.58f, 0.62f));
            name.fontStyle = FontStyle.Normal;
            name.text = lineBottom;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.raycastTarget = false;

            BuildMapScheduleInfoButton(row, competition);
            BuildMapScheduleRegistrationButton(row, competition, entry, live, registerable, showEnterMatch, now);
        }

        private void BuildMapScheduleInfoButton(RectTransform row, CompetitionDefinition competition)
        {
            Image edge = CreateUiObject<Image>("InfoSlot", row);
            SetRect(edge.rectTransform, new Vector2(0.60f, 0.16f), new Vector2(0.74f, 0.86f));
            edge.color = Color.clear;

            Text label = CreateText("Label", edge.transform, 18, TextAnchor.MiddleCenter, CareerEditorialChrome.Ink, _uiFont700);
            Stretch(label.rectTransform);
            label.fontStyle = FontStyle.Normal;
            label.text = "資訊";
            label.raycastTarget = false;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = edge;
            button.transition = Selectable.Transition.None;
            string competitionId = competition.id;
            button.onClick.AddListener(() => ShowCompetitionDetailPanel(competitionId));
        }

        private void BuildMapScheduleRegistrationButton(
            RectTransform row,
            CompetitionDefinition competition,
            CompetitionEntry entry,
            bool live,
            bool registerable,
            bool showEnterMatch,
            int now)
        {
            bool registered = entry != null && entry.registered && !entry.withdrawn && !entry.completed;
            bool completed = entry != null && entry.completed;
            bool rmrMajor = StockholmMajorRules.UsesRmrQualification(competition);
            bool rmrQualified = rmrMajor && _session.IsRmrQualifiedForCompetition(competition.id);
            bool canRegister = (entry == null || entry.withdrawn || !entry.registered) &&
                               registerable &&
                               (!rmrMajor || rmrQualified);
            // RMR Major seats cannot be cancelled once registered.
            bool canCancel = registered && !live && !rmrMajor;

            Color faceColor;
            Color edgeColor;
            Color labelColor;
            string caption;
            string countdown;
            bool interactable;

            if (showEnterMatch)
            {
                // 開戰：金色強調，可點進正式賽。
                faceColor = Accent;
                edgeColor = AccentDim;
                labelColor = UiLightChromeUtility.CapsuleLabel;
                caption = "開戰";
                countdown = "現在";
                interactable = true;
            }
            else if (completed)
            {
                if (TryGetPlacementBadgeStyle(entry.placement, out Color placeFill, out string placeLabel))
                {
                    faceColor = placeFill;
                    edgeColor = placeFill;
                    labelColor = CareerEditorialChrome.Paper;
                    caption = placeLabel;
                }
                else
                {
                    faceColor = CareerEditorialChrome.Paper;
                    edgeColor = CareerEditorialChrome.Ink;
                    labelColor = TextPrimary;
                    caption = $"第{entry.placement}名";
                }
                countdown = "完賽";
                interactable = false;
            }
            else if (registered)
            {
                if (live)
                {
                    // 比賽中：冷灰狀態色，避免大面積環境藍光。
                    faceColor = CareerEditorialChrome.Cyan;
                    edgeColor = CareerEditorialChrome.Cyan;
                    labelColor = CareerEditorialChrome.Paper;
                    caption = "比賽中";
                }
                else if (rmrMajor)
                {
                    // RMR Major 席位已鎖定。
                    faceColor = Accent;
                    edgeColor = AccentDim;
                    labelColor = UiLightChromeUtility.CapsuleLabel;
                    caption = "已取得資格";
                }
                else
                {
                    // 取消：暖紅，與報名／開戰區隔。
                faceColor = CareerEditorialChrome.Red;
                edgeColor = CareerEditorialChrome.Red;
                labelColor = CareerEditorialChrome.Paper;
                    caption = "取消";
                }

                countdown = live
                    ? FormatCountdownHours(Math.Max(0, competition.endHour - now))
                    : FormatCountdownHours(HoursUntilCompetitionStart(competition, now));
                interactable = canCancel;
            }
            else if (rmrMajor && !rmrQualified)
            {
                faceColor = CareerEditorialChrome.Paper;
                edgeColor = CareerEditorialChrome.Ink;
                labelColor = CareerEditorialChrome.Ink;
                caption = "未取得資格";
                countdown = FormatCountdownHours(HoursUntilCompetitionStart(competition, now));
                interactable = false;
            }
            else if (canRegister)
            {
                faceColor = CareerEditorialChrome.Paper;
                edgeColor = CareerEditorialChrome.Ink;
                labelColor = CareerEditorialChrome.Ink;
                caption = "報名";
                countdown = FormatCountdownHours(HoursUntilCompetitionStart(competition, now));
                interactable = true;
            }
            else
            {
                faceColor = CareerEditorialChrome.Paper;
                edgeColor = CareerEditorialChrome.Ink;
                labelColor = CareerEditorialChrome.Ink;
                caption = live ? "進行中" : "未開放";
                countdown = FormatCountdownHours(HoursUntilRegistrationOpens(competition, now));
                interactable = false;
            }

            Image edge = CreateUiObject<Image>("RegSlot", row);
            SetRect(edge.rectTransform, new Vector2(0.75f, 0.12f), new Vector2(0.98f, 0.88f));
            edge.color = Color.clear;

            bool paperFace = faceColor.r > 0.9f && faceColor.g > 0.9f && faceColor.b > 0.85f;
            Color actionBar = paperFace ? edgeColor : faceColor;
            if (labelColor.a < 0.01f)
                actionBar = CareerEditorialChrome.Ink;
            CareerEditorialChrome.CreateBlock(
                edge.transform, "ActionBar",
                new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.18f),
                actionBar);

            Text countdownText = CreateText("Countdown", edge.transform, 14, TextAnchor.MiddleCenter, CareerEditorialChrome.InkMuted, _uiFont700);
            SetRect(countdownText.rectTransform, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.92f));
            countdownText.fontStyle = FontStyle.Normal;
            countdownText.text = countdown;
            countdownText.raycastTarget = false;

            Text captionText = CreateText("Caption", edge.transform, 18, TextAnchor.MiddleCenter, CareerEditorialChrome.Ink, _uiFont700);
            SetRect(captionText.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.58f));
            captionText.fontStyle = FontStyle.Normal;
            captionText.text = caption;
            captionText.raycastTarget = false;

            if (!interactable)
                return;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = edge;
            ApplyButtonColors(button);
            string competitionId = competition.id;
            if (showEnterMatch)
                button.onClick.AddListener(LaunchOfficialCompetitionMatch);
            else if (canCancel)
            {
                button.onClick.AddListener(() =>
                {
                    CompetitionWithdrawEvaluation preview = _session.EvaluateCompetitionWithdraw(
                        competitionId, CompetitionWithdrawReason.Voluntary);
                    bool ok = _session.CancelCompetitionRegistration(
                        competitionId, CompetitionWithdrawReason.Voluntary, out string msg);
                    SetStatus(ok ? msg : preview.blockReason ?? "無法取消報名",
                        ok ? new CareerUiNavLink(CareerUiDestination.MapCompetition, competitionId) : CareerUiNavLink.None);
                    RefreshAll();
                });
            }
            else
            {
                button.onClick.AddListener(() =>
                {
                    AttemptRegisterCompetition(competitionId);
                });
            }
        }

        private void BuildMapScheduleSummaryPanel(
            RectTransform row,
            CompetitionTier tier,
            string dateLine,
            string lineBottom)
        {
            Image host = CreateUiObject<Image>("InfoSummary", row);
            UiLightChromeUtility.ApplyFrostedListRow(host);
            var hostLayout = host.gameObject.AddComponent<LayoutElement>();
            hostLayout.flexibleWidth = 1f;
            hostLayout.minWidth = 80f;
            hostLayout.minHeight = 64f;

            Image topRow = CreateUiObject<Image>("TopRow", host.transform);
            SetRect(topRow.rectTransform, new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.96f));
            topRow.color = Color.clear;
            topRow.raycastTarget = false;
            var topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 6f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = true;
            topLayout.padding = new RectOffset(0, 0, 0, 0);

            AddTierBadge(topRow.transform, tier, preferredHeight: 20f, fontSize: 11);

            Text date = CreateText("Date", topRow.transform, 11, TextAnchor.MiddleLeft, TextMuted, _uiFont);
            date.text = dateLine;
            date.horizontalOverflow = HorizontalWrapMode.Overflow;
            var dateLe = date.gameObject.AddComponent<LayoutElement>();
            dateLe.flexibleWidth = 1f;
            dateLe.minWidth = 40f;

            Text bottom = CreateText("LineBottom", host.transform, 15, TextAnchor.LowerLeft, TextPrimary, _uiFont700);
            SetRect(bottom.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.54f));
            bottom.horizontalOverflow = HorizontalWrapMode.Wrap;
            bottom.verticalOverflow = VerticalWrapMode.Truncate;
            bottom.lineSpacing = 1.05f;
            bottom.text = lineBottom;
        }

        private int HoursUntilRegistrationOpens(CompetitionDefinition competition, int now)
        {
            DateTime start = CareerCalendar.ToDateTime(_content.Balance, competition.startHour);
            var regOpen = new DateTime(start.Year, start.Month, 1, 0, 0, 0);
            int regOpenHour = CareerCalendar.FromDateTime(_content.Balance, regOpen);
            return Math.Max(0, regOpenHour - now);
        }

        private static int HoursUntilCompetitionStart(CompetitionDefinition competition, int now) =>
            Math.Max(0, competition.startHour - now);

        private string BracketClubLabel(string clubId)
        {
            if (string.IsNullOrEmpty(clubId))
                return "暫定";
            return _content.Clubs.TryGetValue(clubId, out CareerClubDefinition club)
                ? club.displayName
                : clubId;
        }

        private void BuildCompetitionDetailOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("CompetitionDetailOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            _competitionDetailRoot = dim.gameObject;
            _competitionDetailRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideCompetitionDetailPanel);

            // D3 light_chrome: Shadow > Frame > Face; opaque Panel fill; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "CompetitionDetailPanel", dim.transform,
                new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f), Panel);
            UiChromeMotion.BindFace(_competitionDetailRoot, panel);

            Button closeBtn = UiLightChromeUtility.CreateSquareCloseButton(
                "CloseCompetitionDetail", panel.transform,
                new Vector2(0.935f, 0.935f), new Vector2(0.985f, 0.985f),
                HideCompetitionDetailPanel, _uiFont700, 22);
            closeBtn.transform.parent.SetAsLastSibling();

            Image actionsHost = CreateUiObject<Image>("DetailActions", panel.transform);
            SetRect(actionsHost.rectTransform, new Vector2(0.04f, 0.935f), new Vector2(0.72f, 0.985f));
            actionsHost.color = Color.clear;
            actionsHost.raycastTarget = false;
            _competitionDetailActions = actionsHost.rectTransform;
            var actionsLayout = actionsHost.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 8f;
            actionsLayout.childAlignment = TextAnchor.MiddleLeft;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = false;
            actionsLayout.childForceExpandHeight = true;

            _competitionDetailSideMeta = CreateText("SideMeta", panel.transform, 20, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(_competitionDetailSideMeta.rectTransform, new Vector2(0.66f, 0.12f), new Vector2(0.94f, 0.88f));
            _competitionDetailSideMeta.alignment = TextAnchor.MiddleRight;
            _competitionDetailSideMeta.horizontalOverflow = HorizontalWrapMode.Wrap;
            _competitionDetailSideMeta.verticalOverflow = VerticalWrapMode.Overflow;
            _competitionDetailSideMeta.lineSpacing = 1.3f;
            _competitionDetailSideMeta.resizeTextForBestFit = true;
            _competitionDetailSideMeta.resizeTextMinSize = 14;
            _competitionDetailSideMeta.resizeTextMaxSize = 28;
            _competitionDetailSideMeta.raycastTarget = false;

            Image scrollHost = CreateUiObject<Image>("DetailScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.62f, 0.92f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _competitionDetailScroll = scroll;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 56f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(12, 12, 12, 12);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            _competitionDetailBody = CreateText("Body", content, 15, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            _competitionDetailBody.alignment = TextAnchor.UpperLeft;
            _competitionDetailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _competitionDetailBody.verticalOverflow = VerticalWrapMode.Overflow;
            _competitionDetailBody.lineSpacing = 1.2f;
            var bodyLayout = _competitionDetailBody.gameObject.AddComponent<LayoutElement>();
            bodyLayout.flexibleWidth = 1f;
            var bodyFitter = _competitionDetailBody.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _competitionDetailBracketRoot = new GameObject("BracketRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _competitionDetailBracketRoot.SetParent(content, false);
            var bracketLayout = _competitionDetailBracketRoot.gameObject.AddComponent<LayoutElement>();
            bracketLayout.flexibleWidth = 1f;
            bracketLayout.minHeight = 0f;
            var bracketGroup = _competitionDetailBracketRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            bracketGroup.childAlignment = TextAnchor.UpperLeft;
            bracketGroup.childControlWidth = true;
            bracketGroup.childControlHeight = true;
            bracketGroup.childForceExpandWidth = true;
            bracketGroup.childForceExpandHeight = false;
            bracketGroup.spacing = 8f;
            bracketGroup.padding = new RectOffset(0, 0, 8, 0);
        }

        private void BuildSponsorNegotiationOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("SponsorNegotiationOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            _sponsorNegotiationRoot = dim.gameObject;
            _sponsorNegotiationRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideSponsorNegotiationPanel);

            // D3 light_chrome: Shadow > Frame > Face; opaque Panel fill; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "SponsorNegotiationPanel", dim.transform,
                new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.78f), Panel);
            UiChromeMotion.BindFace(_sponsorNegotiationRoot, panel);

            _sponsorNegotiationTitle = CreateText("Title", panel.transform, 22, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_sponsorNegotiationTitle.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.96f));

            Image bodyHost = CreateUiObject<Image>("BodyHost", panel.transform);
            SetRect(bodyHost.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.84f));
            UiLightChromeUtility.ApplySoftNavyPanel(bodyHost);

            _sponsorNegotiationBody = CreateText("Body", bodyHost.transform, 15, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            Stretch(_sponsorNegotiationBody.rectTransform);
            _sponsorNegotiationBody.rectTransform.offsetMin = new Vector2(12f, 12f);
            _sponsorNegotiationBody.rectTransform.offsetMax = new Vector2(-12f, -12f);
            _sponsorNegotiationBody.alignment = TextAnchor.UpperLeft;
            _sponsorNegotiationBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _sponsorNegotiationBody.verticalOverflow = VerticalWrapMode.Overflow;
            _sponsorNegotiationBody.lineSpacing = 1.25f;

            // Short CTAs ~28% width, centered in each half.
            CreateGlassCapsuleButton("CancelSponsorNegotiation", "取消", panel.transform,
                new Vector2(0.12f, 0.06f), new Vector2(0.40f, 0.16f), ButtonEdge, ButtonFace,
                HideSponsorNegotiationPanel);

            _sponsorNegotiationConfirmButton = CreateGlassCapsuleButton("ConfirmSponsorNegotiation", "確認簽約", panel.transform,
                new Vector2(0.60f, 0.06f), new Vector2(0.88f, 0.16f), AccentDim, Accent,
                ConfirmSponsorNegotiation, _uiFont700);
        }

        private void ShowSponsorNegotiationPanel(string sponsorId)
        {
            if (_sponsorNegotiationRoot == null || _session == null)
                return;
            _pendingSponsorNegotiationId = sponsorId;
            SponsorSignEvaluation evaluation = _session.EvaluateSponsorSign(sponsorId);
            if (evaluation.definition == null)
                return;

            _sponsorNegotiationTitle.text = $"贊助洽談 · {evaluation.definition.displayName}";
            NarrativeUiText.Set(_sponsorNegotiationBody, BuildSponsorNegotiationText(evaluation));
            if (_sponsorNegotiationConfirmButton != null)
                _sponsorNegotiationConfirmButton.interactable = evaluation.canSign;

            UiChromeMotion.Show(_sponsorNegotiationRoot);
            _sponsorNegotiationRoot.transform.SetAsLastSibling();
        }

        private void HideSponsorNegotiationPanel()
        {
            _pendingSponsorNegotiationId = null;
            UiChromeMotion.Hide(_sponsorNegotiationRoot);
        }

        private void ConfirmSponsorNegotiation()
        {
            if (string.IsNullOrEmpty(_pendingSponsorNegotiationId) || _session == null)
                return;
            string sponsorId = _pendingSponsorNegotiationId;
            if (_session.TrySignSponsor(sponsorId, out string failure))
            {
                _status = "贊助簽約成功";
                HideSponsorNegotiationPanel();
            }
            else
                _status = failure ?? "無法簽約";
            RefreshAll();
        }

        private static string BudgetSponsorIncomeLabel(MonthlyBudgetPreview budget)
        {
            if (!budget.hasSponsor)
                return "贊助月費（無合約）";
            return budget.activeSponsorCount > 1
                ? $"贊助月費（{budget.activeSponsorCount} 家）"
                : "贊助月費";
        }

        private static string RequirementLine(bool met, string text) =>
            (met ? "✓ " : "✗ ") + text;

        private string BuildSponsorNegotiationText(SponsorSignEvaluation evaluation)
        {
            SponsorDefinition def = evaluation.definition;
            var sb = new StringBuilder();
            sb.AppendLine("贊助商要求與你的現況：");
            sb.AppendLine();
            sb.AppendLine(RequirementLine(evaluation.meetsFame,
                $"名望 ≥ {evaluation.requiredFame}（目前 {evaluation.currentFame}）"));
            sb.AppendLine(RequirementLine(evaluation.meetsExposure,
                $"曝光 ≥ {evaluation.requiredExposure}（目前 {evaluation.currentExposure}）"));
            sb.AppendLine(RequirementLine(evaluation.meetsRank,
                $"世界排名百分位 ≥ {evaluation.requiredRankPercentile}%（目前 {evaluation.currentRankPercentile}%）"));
            sb.AppendLine();
            sb.AppendLine($"合約：{SponsorRules.DefaultContractMonths} 個月");
            sb.AppendLine($"月費底價：${def.baseMonthlyFee:N0}");
            sb.AppendLine($"賽事成績獎金（單次）：${def.baseResultBonus:N0}");
            sb.AppendLine($"等級：{def.tier}");
            sb.AppendLine();
            sb.AppendLine($"名額：{evaluation.activeSponsorCount}/{SponsorRules.MaxActiveSponsorsPerClub}");
            if (evaluation.monthlyLimitReached)
                sb.AppendLine("本月已簽過新贊助，下個月才能再簽一家。");
            if (evaluation.alreadySigned)
                sb.AppendLine("你已與此贊助商合作中。");
            if (evaluation.rosterFull)
                sb.AppendLine("已達同時合作上限。");
            if (!evaluation.canSign && !string.IsNullOrEmpty(evaluation.blockReason))
                sb.AppendLine();
            if (!evaluation.canSign)
                sb.AppendLine($"無法簽約：{evaluation.blockReason}");
            else
                sb.AppendLine("條件符合時可按「確認簽約」。");
            return sb.ToString();
        }

        private void ShowCompetitionDetailPanel(string competitionId)
        {
            if (_competitionDetailRoot == null || _session == null ||
                !_content.Competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
                return;

            _competitionDetailId = competitionId;
            BuildCompetitionDetailPanels(competition);
            PopulateCompetitionBracketUi(competition.id);
            RefreshCompetitionDetailActions(competition);
            UiChromeMotion.Show(_competitionDetailRoot);
            _competitionDetailRoot.transform.SetAsLastSibling();
            ResetCompetitionDetailScrollToTop();
            RefreshCanvasFrost();
        }

        private void RefreshCompetitionDetailActions(CompetitionDefinition competition)
        {
            if (_competitionDetailActions == null)
                return;
            ClearTransformChildren(_competitionDetailActions);
            // 取消報名改由地圖賽程表操作；傷病取消由事件觸發，詳情頁不再放手動按鈕。
        }

        private void ResetCompetitionDetailScrollToTop()
        {
            if (_competitionDetailScroll == null || _competitionDetailScroll.content == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_competitionDetailScroll.content);
            if (_competitionDetailBracketRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_competitionDetailBracketRoot);
            Canvas.ForceUpdateCanvases();

            _competitionDetailScroll.StopMovement();
            _competitionDetailScroll.velocity = Vector2.zero;
            _competitionDetailScroll.verticalNormalizedPosition = 1f;
        }

        private void HideCompetitionDetailPanel()
        {
            HideRankingClubIntel();
            UiChromeMotion.Hide(_competitionDetailRoot);
        }

        private void BuildCompetitionDetailPanels(CompetitionDefinition competition)
        {
            CareerState state = _session.State;
            int now = state.clockHour;
            bool live = CareerCalendar.IsLive(competition, now);
            bool registerable = CareerCalendar.IsRegisterableThisMonth(competition, _content.Balance, now);
            CompetitionEntry entry = state.competitionEntries.FirstOrDefault(x =>
                x.clubId == state.playerClubId && x.competitionId == competition.id);
            bool activelyRegistered = entry != null && entry.registered && !entry.withdrawn && !entry.completed;

            string location = string.IsNullOrEmpty(competition.locationDisplay)
                ? (string.IsNullOrEmpty(competition.region) ? "—" : competition.region)
                : competition.locationDisplay;
            string formatLabel = FormatLabel(competition.format);
            string venue = FormatCompetitionVenue(formatLabel, location);
            string status = live ? "進行中"
                : activelyRegistered
                    ? (StockholmMajorRules.UsesRmrQualification(competition)
                        ? "已取得資格 · 不可取消報名"
                        : "已報名")
                : StockholmMajorRules.UsesRmrQualification(competition)
                    ? (_session.IsRmrQualifiedForCompetition(competition.id)
                        ? "已取得資格 · 可確認入場"
                        : "未取得資格")
                : registerable ? "本月可報名"
                : entry != null && entry.completed
                    ? TryGetPlacementBadgeStyle(entry.placement, out _, out string placeLabel)
                        ? $"已完賽 · {placeLabel}"
                        : $"已完賽 · 第 {entry.placement} 名"
                : "尚未開放";

            int joined = CompetitionRegistrationRules.RegisteredClubIds(state, competition.id).Count;
            int field = CompetitionRegistrationRules.EffectiveFieldSize(competition);

            var left = new List<string>
            {
                string.IsNullOrWhiteSpace(competition.displayName) ? competition.id : competition.displayName,
                $"Tier：{TierLabel(competition.tier)}",
                $"狀態：{status}"
            };
            if (StockholmMajorRules.IsStockholmMajorStructure(competition))
            {
                left.Add(StockholmMajorRules.FormatDetailRulesBlurb());
                RmrMajorStage seat = _session.GetPlayerMajorSeatStage(competition.id);
                if (seat != RmrMajorStage.None)
                    left.Add($"你的席位：{RmrRules.StageDisplayName(seat)}");
            }
            if (activelyRegistered && !StockholmMajorRules.UsesRmrQualification(competition))
            {
                CompetitionWithdrawEvaluation late = CompetitionWithdrawRules.Evaluate(
                    competition, entry, now, CompetitionWithdrawReason.Voluntary);
                if (late.isLate)
                    left.Add("注意：開賽前一週內取消需付替補費用，無故取消另禁賽同級別下一場同主辦方賽事。Major 不受禁賽。");
                else
                    left.Add("開賽一週前可免費取消報名。");
            }
            if (CompetitionTravelRules.RequiresTravel(competition))
            {
                CompetitionTravelSubsidy subsidy = CompetitionTravelRules.ResolveSubsidyTier(competition);
                left.Add($"差旅補貼：{CompetitionTravelRules.SubsidyLabel(subsidy)}");
                if (entry != null && entry.registered && !entry.withdrawn)
                {
                    string travel = CompetitionTravelRules.BuildTravelSummary(competition, entry, _content.Balance, now);
                    if (!string.IsNullOrEmpty(travel))
                        left.Add(travel);
                }
                else
                {
                    CompetitionTravelEstimate preview = _session.GetCompetitionTravelEstimate(competition.id);
                    if (preview.flightHours > 0)
                    {
                        left.Add(
                            $"預估提前 {preview.earlyArrivalDays} 天抵達 · 飛行約 {preview.flightHours} 小時");
                        left.Add(
                            $"預估艙等 {CompetitionTravelRules.FlightClassLabel(preview.flightClass)} · " +
                            $"自付 ${preview.clubOutOfPocket:N0} · 補貼 ${preview.subsidyAmount:N0}");
                    }
                }
            }

            var right = new List<string>
            {
                $"日期：{CareerCalendar.FormatDateRange(_content.Balance, competition)}",
                $"獎金 ${competition.prizePool:N0}",
                $"報名費 ${competition.entryFee:N0}",
                $"隊伍上限 {competition.maximumTeams}",
                venue,
                StockholmMajorRules.UsesRmrQualification(competition)
                    ? $"目前入場 {joined} / {field} 隊"
                    : $"目前報名 {joined} / {field} 隊"
            };
            if (competition.maximumWorldRank > 0 && !StockholmMajorRules.UsesRmrQualification(competition))
            {
                int soft = CompetitionRegistrationRules.SoftMaximumWorldRank(competition);
                right.Add($"排名限制：世界前 {soft} 名");
            }
            if (!string.IsNullOrEmpty(competition.region))
                right.Add($"賽區：{competition.region}");

            NarrativeUiText.Set(_competitionDetailBody, string.Join("\n\n", left));
            if (_competitionDetailSideMeta != null)
                _competitionDetailSideMeta.text = string.Join("\n\n", right);
        }

        private static void ClearTransformChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
        }

        private void AddBracketSectionTitle(Transform parent, string title)
        {
            Text label = CreateText("BracketTitle", parent, 14, TextAnchor.MiddleLeft, Accent, _uiFont700);
            label.text = title;
            var layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 22f;
            layout.preferredHeight = 22f;
        }

        private void AddBracketPlaceholder(Transform parent, string message)
        {
            Image box = CreateUiObject<Image>("BracketEmpty", parent);
            box.color = PanelRow;
            var boxLayout = box.gameObject.AddComponent<LayoutElement>();
            boxLayout.minHeight = 40f;
            boxLayout.preferredHeight = 40f;
            Text text = CreateText("Message", box.transform, 14, TextAnchor.MiddleCenter, TextMuted);
            Stretch(text.rectTransform);
            text.text = message;
        }

        private void AddSwissStandingsTable(Transform parent, CompetitionBracketSnapshot snapshot)
        {
            Image box = CreateUiObject<Image>("SwissTable", parent);
            box.color = BracketCard;
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var boxLayout = box.gameObject.AddComponent<LayoutElement>();
            boxLayout.minHeight = 120f;

            Text header = CreateText("Header", box.transform, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            header.text = "戰績 · 勝-敗 · 狀態";
            var headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.minHeight = 20f;
            headerLayout.preferredHeight = 20f;

            foreach (MajorSwissStanding row in snapshot.swissStandings
                         .OrderByDescending(x => x.advanced)
                         .ThenByDescending(x => x.wins)
                         .ThenBy(x => x.losses)
                         .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase))
            {
                string status = row.advanced ? "晉級" : row.eliminated ? "淘汰" : "進行中";
                Text line = CreateText("Row", box.transform, 13, TextAnchor.MiddleLeft, TextPrimary, _uiFont);
                line.text = $"{BracketClubLabel(row.clubId)}  {row.wins}-{row.losses}  {status}";
                var lineLayout = line.gameObject.AddComponent<LayoutElement>();
                lineLayout.minHeight = 18f;
                lineLayout.preferredHeight = 18f;
            }
        }

        private void AddSwissPairingsRow(Transform parent, CompetitionBracketSnapshot snapshot)
        {
            AddBracketSectionTitle(parent, "本輪對陣");
            foreach (MajorSwissPairing pair in snapshot.swissPairings)
            {
                string result = pair.completed
                    ? $" → {BracketClubLabel(pair.winnerClubId)}"
                    : string.Empty;
                AddBracketInfoChip(parent,
                    $"{BracketClubLabel(pair.clubAId)} vs {BracketClubLabel(pair.clubBId)}{result}");
            }
        }

        private void AddBracketProgressRow(Transform parent, CompetitionBracketSnapshot snapshot)
        {
            Image row = CreateUiObject<Image>("ProgressRow", parent);
            row.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.95f);
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;

            string status = snapshot.registrationOpen
                ? $"報名 {snapshot.registeredCount} / {snapshot.bracketCapacity} · 陸續加入"
                : snapshot.live
                    ? $"參賽 {snapshot.registeredCount} 隊 · 進行中"
                    : $"籤位 {snapshot.bracketCapacity} · 已報名 {snapshot.registeredCount} 隊";
            Text caption = CreateText("Caption", row.transform, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(caption.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.92f));
            caption.text = status;

            Image track = CreateUiObject<Image>("Track", row.transform);
            SetRect(track.rectTransform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.46f));
            track.color = new Color(0.12f, 0.13f, 0.16f, 1f);

            float fill = snapshot.bracketCapacity <= 0
                ? 0f
                : Mathf.Clamp01(snapshot.registeredCount / (float)snapshot.bracketCapacity);
            Image fillImg = CreateUiObject<Image>("Fill", track.transform);
            SetRect(fillImg.rectTransform, new Vector2(0f, 0f), new Vector2(fill, 1f));
            fillImg.color = snapshot.registrationOpen
                ? Accent
                : new Color(Accent.r, Accent.g, Accent.b, 0.70f);
        }

        private void AddBracketInfoChip(Transform parent, string message)
        {
            Image chip = CreateUiObject<Image>("InfoChip", parent);
            chip.color = new Color(Accent.r, Accent.g, Accent.b, 0.14f);
            var chipLayout = chip.gameObject.AddComponent<LayoutElement>();
            chipLayout.minHeight = 32f;
            chipLayout.preferredHeight = 32f;
            Text text = CreateText("Text", chip.transform, 13, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(text.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            text.text = message;
        }

        private static string FormatCountdownHours(int hours)
        {
            if (hours <= 0) return "即將";
            if (hours >= 24 * 7) return $"{hours / (24 * 7)}周";
            if (hours >= 24) return $"{hours / 24}天";
            return $"{hours}時";
        }

        private static string FormatLabel(CompetitionFormat format)
        {
            switch (format)
            {
                case CompetitionFormat.Online: return "Online";
                case CompetitionFormat.LAN: return "LAN";
                case CompetitionFormat.Hybrid: return "Hybrid";
                case CompetitionFormat.Cancelled: return "Cancelled";
                default: return format.ToString();
            }
        }

        /// <summary>
        /// Avoid "Online · Online · Europe" when locationDisplay already includes the format.
        /// </summary>
        private static string FormatCompetitionVenue(string formatLabel, string location)
        {
            if (string.IsNullOrWhiteSpace(location) || location == "—")
                return formatLabel;
            if (string.IsNullOrWhiteSpace(formatLabel))
                return location;

            if (location.StartsWith(formatLabel, StringComparison.OrdinalIgnoreCase))
                return location;

            // location is only the format word, or equals it.
            if (string.Equals(location, formatLabel, StringComparison.OrdinalIgnoreCase))
                return formatLabel;

            return $"{formatLabel} · {location}";
        }

        private static string TierLabel(CompetitionTier tier)
        {
            switch (tier)
            {
                case CompetitionTier.SuperElite: return "Super-Elite-Tier";
                case CompetitionTier.Major: return "Major-Tier";
                case CompetitionTier.S: return "S-Tier";
                case CompetitionTier.Elite: return "Elite-Tier";
                case CompetitionTier.A: return "A-Tier";
                case CompetitionTier.B: return "B-Tier";
                case CompetitionTier.C: return "C-Tier";
                case CompetitionTier.D: return "D-Tier";
                case CompetitionTier.Exhibition: return "Exhibition";
                default: return tier + "-Tier";
            }
        }

        private void DrawEvents()
        {
            AddSectionTitle("新聞");
            string playerClubId = _session.State.playerClubId;
            CareerEvent[] events = _session.State.events
                .Where(evt => IsEventVisibleInNews(evt, playerClubId))
                .Reverse()
                .Take(100)
                .ToArray();
            if (events.Length == 0)
                AddMutedLine("尚無新聞。");
            foreach (CareerEvent evt in events)
                AddCareerEventRow(evt);
        }

        private bool IsEventVisibleInNews(CareerEvent evt, string playerClubId)
        {
            if (evt == null)
                return false;

            switch (evt.type)
            {
                // System / sim noise — not news.
                case CareerEventType.PrizeAwarded:
                case CareerEventType.MatchCompleted:
                case CareerEventType.RankingUpdated:
                case CareerEventType.TrainingApplied:
                case CareerEventType.AchievementUnlocked:
                case CareerEventType.HoursAdvanced:
                case CareerEventType.MonthClosed:
                case CareerEventType.WeekAdvanced:
                    return false;

                case CareerEventType.ScriptedEvent:
                {
                    ScriptedEventTier tier = evt.eventTier;
                    if (tier == ScriptedEventTier.Unset &&
                        !string.IsNullOrEmpty(evt.subjectId) &&
                        _session.TryGetScriptedEventDefinition(evt.subjectId, out CareerScriptedEventDef def))
                    {
                        tier = ScriptedEventTierRules.Resolve(def);
                        return ScriptedEventTierRules.IsVisibleInNews(
                            tier, def.publicVisible || evt.publicVisible, evt.clubId, playerClubId);
                    }

                    if (tier == ScriptedEventTier.Unset)
                        tier = ScriptedEventTier.B;
                    return ScriptedEventTierRules.IsVisibleInNews(
                        tier, evt.publicVisible, evt.clubId, playerClubId);
                }

                // Completed transfers (player or AI historical) — always news.
                case CareerEventType.TransferCompleted:
                    return true;

                // Own club: successful sponsorship only (not termination).
                case CareerEventType.SponsorChanged:
                    return string.Equals(evt.clubId, playerClubId, StringComparison.OrdinalIgnoreCase) &&
                           !string.IsNullOrEmpty(evt.message) &&
                           evt.message.IndexOf("簽訂贊助", StringComparison.Ordinal) >= 0;

                // Own club: manual community posts only (auto daily_update excluded).
                case CareerEventType.CommunityPost:
                    return string.Equals(evt.clubId, playerClubId, StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(evt.subjectId, CommunityOperationsRules.DailyUpdateTemplateId,
                               StringComparison.OrdinalIgnoreCase);

                // Negotiation / facilities / finance / injuries — not news.
                // Mission triggers will be added when the mission tree ships.
                case CareerEventType.ContractChanged:
                case CareerEventType.FacilityChanged:
                case CareerEventType.FinancialStatusChanged:
                case CareerEventType.PlayerInjured:
                    return false;

                default:
                    return false;
            }
        }

        private void AdvanceTime(int hours)
        {
            if (!_careerStarted || _session == null || hours <= 0) return;
            CareerAdvanceResult result = _session.AdvanceHours(hours);
            SetStatus($"已推進至 {CareerCalendar.FormatHeader(_content.Balance, _session.State)}");
            if (CareerDifficultyRules.UsesIronmanAutoSave(_session.State.difficulty))
                _persistence.Save(_session.State, 0);
            HandleAdvanceEventFeedback(result);
            if (result.ShouldAutoPause)
                PauseTime(AutoPauseReason(result), NavLinkFromAdvanceResult(result));
            RefreshAll();
        }

        private void AdvanceWeek() => AdvanceTime(CareerCalendar.HoursPerWeek);

        private void SaveCareer(int slot = -1)
        {
            if (!_careerStarted || _session == null) return;
            if (!CareerDifficultyRules.AllowsManualSave(_session.State.difficulty))
                return;

            int target = ResolveManualSlot(slot);
            SetActiveSaveSlot(target);
            _persistence.Save(_session.State, target);
            _status = CareerDifficultyRules.UsesIronmanAutoSave(_session.State.difficulty)
                ? "硬核模式已寫入自動存檔"
                : $"已儲存槽位 {target}";
            RefreshAll();
            _settingsPanel?.Refresh();
        }

        private void LoadCareer(int slot = -1)
        {
            CareerState probe = _session?.State;
            if (probe != null && !CareerDifficultyRules.AllowsManualLoad(probe.difficulty))
            {
                _status = "硬核模式無法讀檔";
                if (_careerStarted) RefreshAll();
                return;
            }

            int target = ResolveManualSlot(slot);
            SetActiveSaveSlot(target);

            CareerState loaded = _persistence.Load(target);
            if (loaded != null && !CareerDifficultyRules.AllowsManualLoad(loaded.difficulty))
            {
                _status = "此存檔為硬核模式，無法讀取";
                if (_careerStarted) RefreshAll();
                return;
            }

            if (loaded != null)
            {
                if (!PlayableClubIds.Contains(loaded.playerClubId))
                    loaded.playerClubId = "navi_2021";
                BuildSession(loaded);
                _careerStarted = true;
                _timePaused = true;
                _hourAccumulator = 0f;
                if (_titleRoot != null) _titleRoot.SetActive(false);
                if (_shellRoot != null) _shellRoot.SetActive(true);
                _status = $"已讀取槽位 {target} · {CareerCalendar.FormatHeader(_content.Balance, loaded)} · 時間暫停";
            }
            else
            {
                _status = $"槽位 {target} 沒有存檔";
            }

            if (_careerStarted) RefreshAll();
            else if (_titleHint != null) _titleHint.text = _status;
            _settingsPanel?.Refresh();
        }

        private void DeleteCareerSlot(int slot)
        {
            int target = ResolveManualSlot(slot);
            if (_persistence == null || !CareerPersistence.IsManualSlot(target))
                return;

            if (!_persistence.Exists(target))
            {
                _status = $"槽位 {target} 沒有存檔";
                _settingsPanel?.Refresh();
                return;
            }

            if (_persistence.Delete(target))
                _status = $"已刪除槽位 {target} 的存檔";
            else
                _status = $"槽位 {target} 沒有存檔";

            if (_careerStarted) RefreshAll();
            else if (_titleHint != null) _titleHint.text = _status;
            _settingsPanel?.Refresh();
        }

        private int ResolveManualSlot(int slot)
        {
            if (CareerPersistence.IsManualSlot(slot))
                return slot;
            return CareerPersistence.IsManualSlot(_activeSaveSlot)
                ? _activeSaveSlot
                : CareerPersistence.FirstManualSlot;
        }

        private void SetActiveSaveSlot(int slot)
        {
            if (!CareerPersistence.IsManualSlot(slot))
                return;
            _activeSaveSlot = slot;
            PlayerPrefs.SetInt(ActiveSaveSlotPrefsKey, slot);
            PlayerPrefs.Save();
        }

        private static int LoadActiveSaveSlotPreference()
        {
            int slot = PlayerPrefs.GetInt(ActiveSaveSlotPrefsKey, CareerPersistence.FirstManualSlot);
            return CareerPersistence.IsManualSlot(slot) ? slot : CareerPersistence.FirstManualSlot;
        }

        private string DescribeSaveSlot(int slot)
        {
            if (!CareerPersistence.IsManualSlot(slot) || _persistence == null)
                return $"槽位 {slot}";

            CareerSaveSlotInfo info = _persistence.PeekSlot(slot);
            if (!info.Exists)
                return "空槽位";

            string clubName = info.PlayerClubId;
            if (_content?.Clubs != null &&
                !string.IsNullOrWhiteSpace(info.PlayerClubId) &&
                _content.Clubs.TryGetValue(info.PlayerClubId, out CareerClubDefinition club) &&
                club != null &&
                !string.IsNullOrWhiteSpace(club.displayName))
            {
                clubName = club.displayName;
            }

            string date = _content?.Balance != null
                ? CareerCalendar.FormatShort(_content.Balance, info.ClockHour)
                : $"第 {info.ClockHour} 時";
            string difficulty = CareerDifficultyRules.DisplayName(info.Difficulty);
            return $"{clubName} · {difficulty} · {date}";
        }

        private void CreateCareer()
        {
            CareerState state = CareerSession.CreateNew(
                _content.Balance, _content.Players.Values, _content.Clubs.Values,
                _selectedClubId, _newDifficulty, 2021, _content.Coaches.Values);
            BuildSession(state);
        }

        private void BuildSession(CareerState state)
        {
            ICareerMatchBridge bridge = _matchContent == null
                ? (ICareerMatchBridge)new DeterministicCareerMatchBridge()
                : new QuickMatchCareerBridge(_matchContent);
            _session = new CareerSession(
                state, _content.Balance, _content.Players.Values, _content.Clubs.Values,
                _content.Sponsors.Values, _content.Competitions.Values,
                _content.Facilities.Values, bridge, _matchContent,
                _content.ScriptedEvents.Values, _content.Achievements.Values,
                _content.Coaches.Values, _content.MissionTrees.Values);
            WireScriptedEventPausePreference();
            SeedCareerCombatAttributesFromMatch();
        }

        /// <summary>
        /// Career state used to fill every combat attr from a single power number.
        /// Re-seed from match players.json when the list is still that flat baseline.
        /// </summary>
        private void SeedCareerCombatAttributesFromMatch()
        {
            if (_session?.State?.players == null || _matchContent?.Players == null)
                return;

            foreach (PlayerCareerState player in _session.State.players)
            {
                if (player == null || string.IsNullOrEmpty(player.playerId))
                    continue;
                if (!_matchContent.Players.TryGetValue(player.playerId, out PlayerDefinition match))
                    continue;

                string role = null;
                if (_content.Players.TryGetValue(player.playerId, out CareerPlayerDefinition def))
                    role = def.role;
                DevelopmentRules.EnsureProgressLists(
                    player, role ?? match.primaryRole, match.attributes,
                    match.roleFitness, match.mapFitness, match.dust2Grade);
            }
        }

        private string VisiblePotential(PlayerCareerState player) =>
            CareerDifficultyRules.FormatVisiblePotential(_session.State.difficulty, player.potential);

        private int RankFor(string clubId)
        {
            if (_session == null)
                return StartingRankFor(clubId);
            RankingEntry entry = _session.State.rankings
                .FirstOrDefault(x => x.clubId == clubId);
            return WorldRankLabel(clubId, entry, _session.State);
        }

        private static string HealthLabel(FinancialHealth health)
        {
            switch (health)
            {
                case FinancialHealth.Wealthy: return "富裕";
                case FinancialHealth.Healthy: return "健康";
                case FinancialHealth.Balanced: return "平衡";
                case FinancialHealth.Deficit: return "赤字";
                case FinancialHealth.Crisis: return "危機";
                case FinancialHealth.Bankrupt: return "破產";
                default: return health.ToString();
            }
        }

        private static Color HealthColor(FinancialHealth health)
        {
            switch (health)
            {
                case FinancialHealth.Wealthy:
                case FinancialHealth.Healthy:
                    return Positive;
                case FinancialHealth.Balanced:
                    return Warning;
                default:
                    return Danger;
            }
        }

        private void AddSectionTitle(string title)
        {
            bool onMapSchedule = _uiListRoot != null;
            RectTransform row = AddRow(onMapSchedule ? 64f : 56f);
            Image underline = CreateUiObject<Image>("Underline", row);
            SetRect(underline.rectTransform, new Vector2(0f, 0.06f), new Vector2(0.16f, 0.14f));
            underline.color = CareerEditorialChrome.PigmentForTab(_tab);
            underline.raycastTarget = false;
            int size = onMapSchedule ? 28 : 26;
            Text text = CreateText("Title", row, size, TextAnchor.MiddleLeft, CareerEditorialChrome.Ink, _uiFontBlack);
            SetRect(text.rectTransform, new Vector2(0.01f, 0.15f), new Vector2(0.99f, 1f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = title;
        }

        private bool IsSectionExpanded(string key)
        {
            if (!_sectionExpanded.TryGetValue(key, out bool expanded))
                return true;
            return expanded;
        }

        private void ToggleSectionExpanded(string key)
        {
            float scrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            _sectionExpanded[key] = !IsSectionExpanded(key);
            RebuildContent();
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = scrollPos;
        }

        /// <summary>Clickable section header. Returns true when body should be drawn (default expanded).</summary>
        private bool AddCollapsibleSection(string key, string title)
        {
            bool expanded = IsSectionExpanded(key);
            RectTransform row = AddRow(36f);
            Image rowImage = row.GetComponent<Image>();
            rowImage.raycastTarget = true;

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = rowImage;
            ApplyButtonColors(button);
            string sectionKey = key;
            button.onClick.AddListener(() => ToggleSectionExpanded(sectionKey));

            Image underline = CreateUiObject<Image>("Underline", row);
            SetRect(underline.rectTransform, new Vector2(0f, 0f), new Vector2(0.18f, 0.12f));
            underline.color = AccentDim;
            underline.raycastTarget = false;

            Text text = CreateText("Title", row, 17, TextAnchor.MiddleLeft, Accent);
            SetRect(text.rectTransform, new Vector2(0.01f, 0.15f), new Vector2(0.72f, 1f));
            text.text = (expanded ? "▼  " : "▶  ") + title;
            text.raycastTarget = false;

            Text hint = CreateText("Hint", row, 12, TextAnchor.MiddleRight, TextMuted);
            SetRect(hint.rectTransform, new Vector2(0.74f, 0.15f), new Vector2(0.98f, 1f));
            hint.text = expanded ? "收起" : "展開";
            hint.raycastTarget = false;

            return expanded;
        }

        private void AddMutedLine(string message)
        {
            int size = _uiListRoot != null ? 14 : 13;
            RectTransform row = AddRow(_uiListRoot != null ? 36f : 32f);
            AddRowLabel(row, 0.015f, 0.99f, message, TextMuted, size);
        }

        private void DrawMonthlyBudgetPanel(MonthlyBudgetPreview budget)
        {
            const float panelHeight = 420f;
            RectTransform host = AddRow(panelHeight);
            host.GetComponent<Image>().color = Color.clear;

            Image frame = CreateUiObject<Image>("BudgetPanel", host);
            Stretch(frame.rectTransform);
            frame.rectTransform.offsetMin = new Vector2(2f, 2f);
            frame.rectTransform.offsetMax = new Vector2(-2f, -2f);
            frame.color = CareerEditorialChrome.Paper;
            frame.raycastTarget = false;

            Image edge = CreateUiObject<Image>("Edge", frame.transform);
            Stretch(edge.rectTransform);
            edge.color = new Color(CareerEditorialChrome.Ink.r, CareerEditorialChrome.Ink.g, CareerEditorialChrome.Ink.b, 0.28f);
            edge.raycastTarget = false;
            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            face.color = CareerEditorialChrome.Paper;
            face.raycastTarget = false;

            // Header: title + treasury
            Image header = CreateUiObject<Image>("Header", face.transform);
            SetRect(header.rectTransform, new Vector2(0.015f, 0.86f), new Vector2(0.985f, 0.98f));
            header.color = new Color(0.90f, 0.86f, 0.78f, 1f);
            header.raycastTarget = false;

            Text title = CreateText("Title", header.transform, 15, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.40f, 0.92f));
            title.text = "本月收支（預估）";

            Text cashLabel = CreateText("CashLabel", header.transform, 13, TextAnchor.MiddleRight, TextMuted, _uiFont700);
            SetRect(cashLabel.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.62f, 0.92f));
            cashLabel.text = "庫存現金";

            Text cashValue = CreateText("CashValue", header.transform, 16, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(cashValue.rectTransform, new Vector2(0.63f, 0.08f), new Vector2(0.98f, 0.92f));
            cashValue.text = $"${budget.cash:N0}";

            // Income block (left) — only categories with amount this month.
            var incomeLines = new List<BudgetPanelLineSpec>();
            var incomeSlices = new List<BudgetChartSlice>();
            if (budget.sponsorIncome > 0)
            {
                incomeLines.Add(new BudgetPanelLineSpec
                {
                    Label = BudgetSponsorIncomeLabel(budget),
                    Amount = budget.sponsorIncome,
                    Color = Positive,
                    Signed = true,
                    DetailTitle = "贊助月費明細",
                    DetailBody = FormatBudgetBreakdown(budget.sponsorDetails, "目前無贊助合約")
                });
                incomeSlices.Add(new BudgetChartSlice
                {
                    Weight = budget.sponsorIncome,
                    Color = new Color(0.28f, 0.68f, 0.46f, 1f)
                });
            }

            if (budget.merchandiseIncome > 0)
            {
                incomeLines.Add(new BudgetPanelLineSpec
                {
                    Label = "商品銷售",
                    Amount = budget.merchandiseIncome,
                    Color = Positive,
                    Signed = true,
                    DetailTitle = "商品銷售明細",
                    DetailBody = FormatBudgetBreakdown(budget.merchandiseDetails, "本月無商品預估")
                });
                incomeSlices.Add(new BudgetChartSlice
                {
                    Weight = budget.merchandiseIncome,
                    Color = new Color(0.50f, 0.86f, 0.62f, 1f)
                });
            }

            if (budget.prizeIncome > 0)
            {
                incomeLines.Add(new BudgetPanelLineSpec
                {
                    Label = "賽事獎金",
                    Amount = budget.prizeIncome,
                    Color = Positive,
                    Signed = true,
                    DetailTitle = "賽事獎金明細",
                    DetailBody = FormatBudgetBreakdown(budget.prizeDetails, "本月尚無賽事獎金入帳")
                });
                incomeSlices.Add(new BudgetChartSlice
                {
                    Weight = budget.prizeIncome,
                    Color = new Color(0.72f, 0.92f, 0.48f, 1f)
                });
            }

            Image incomeBlock = CreateUiObject<Image>("IncomeBlock", face.transform);
            SetRect(incomeBlock.rectTransform, new Vector2(0.015f, 0.18f), new Vector2(0.492f, 0.84f));
            incomeBlock.color = new Color(0.93f, 0.91f, 0.84f, 1f);
            incomeBlock.raycastTarget = false;
            FillBudgetBlock(
                incomeBlock.transform,
                "收入",
                Positive,
                incomeLines.ToArray(),
                ("收入合計", budget.IncomeTotal, Accent),
                incomeSlices.ToArray(),
                ref _budgetIncomePieTex,
                ref _budgetIncomePieSprite);

            // Expense block (right) — only categories with amount this month.
            var expenseLines = new List<BudgetPanelLineSpec>();
            var expenseSlices = new List<BudgetChartSlice>();
            if (budget.salaryExpense > 0)
            {
                expenseLines.Add(new BudgetPanelLineSpec
                {
                    Label = "選手薪資",
                    Amount = -budget.salaryExpense,
                    Color = Danger,
                    Signed = true,
                    DetailTitle = "選手薪資明細",
                    DetailBody = FormatBudgetBreakdown(budget.salaryDetails, "目前無選手合約")
                });
                expenseSlices.Add(new BudgetChartSlice
                {
                    Weight = budget.salaryExpense,
                    Color = new Color(0.78f, 0.26f, 0.30f, 1f)
                });
            }

            if (budget.coachSalaryExpense > 0)
            {
                expenseLines.Add(new BudgetPanelLineSpec
                {
                    Label = "教練組薪資",
                    Amount = -budget.coachSalaryExpense,
                    Color = Danger,
                    Signed = true,
                    DetailTitle = "教練組薪資明細",
                    DetailBody = FormatBudgetBreakdown(budget.coachSalaryDetails, "目前無教練組")
                });
                expenseSlices.Add(new BudgetChartSlice
                {
                    Weight = budget.coachSalaryExpense,
                    Color = new Color(0.92f, 0.55f, 0.32f, 1f)
                });
            }

            Image expenseBlock = CreateUiObject<Image>("ExpenseBlock", face.transform);
            SetRect(expenseBlock.rectTransform, new Vector2(0.508f, 0.18f), new Vector2(0.985f, 0.84f));
            expenseBlock.color = new Color(0.93f, 0.86f, 0.82f, 1f);
            expenseBlock.raycastTarget = false;
            FillBudgetBlock(
                expenseBlock.transform,
                "支出",
                Danger,
                expenseLines.ToArray(),
                ("支出合計", -budget.ExpenseTotal, Accent),
                expenseSlices.ToArray(),
                ref _budgetExpensePieTex,
                ref _budgetExpensePieSprite);

            // Net footer
            Image footer = CreateUiObject<Image>("Footer", face.transform);
            SetRect(footer.rectTransform, new Vector2(0.015f, 0.02f), new Vector2(0.985f, 0.155f));
            footer.color = budget.Net >= 0
                ? new Color(0.93f, 0.91f, 0.84f, 1f)
                : new Color(0.93f, 0.86f, 0.82f, 1f);
            footer.raycastTarget = false;

            Text netLabel = CreateText("NetLabel", footer.transform, 15, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(netLabel.rectTransform, new Vector2(0.03f, 0.1f), new Vector2(0.45f, 0.9f));
            netLabel.text = "本月淨值";

            Color netColor = budget.Net >= 0 ? Positive : Danger;
            Text netValue = CreateText("NetValue", footer.transform, 20, TextAnchor.MiddleRight, netColor, _uiFont700);
            SetRect(netValue.rectTransform, new Vector2(0.45f, 0.1f), new Vector2(0.97f, 0.9f));
            netValue.text = budget.Net >= 0
                ? $"+${budget.Net:N0}"
                : $"-${Math.Abs(budget.Net):N0}";
        }

        private sealed class BudgetPanelLineSpec
        {
            public string Label;
            public int Amount;
            public Color Color;
            public bool Signed;
            public string DetailTitle;
            public string DetailBody;
        }

        private static string FormatBudgetBreakdown(IReadOnlyList<BudgetBreakdownLine> lines, string emptyMessage)
        {
            if (lines == null || lines.Count == 0)
                return emptyMessage;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                BudgetBreakdownLine line = lines[i];
                if (line == null)
                    continue;
                if (sb.Length > 0)
                    sb.Append('\n');
                string label = string.IsNullOrWhiteSpace(line.label) ? "項目" : line.label;
                sb.Append(label).Append("  $").Append(Math.Abs(line.amount).ToString("N0"));
            }
            return sb.Length == 0 ? emptyMessage : sb.ToString();
        }

        private void FillBudgetBlock(
            Transform parent,
            string headerTitle,
            Color headerAccent,
            BudgetPanelLineSpec[] lines,
            (string label, int amount, Color color) total,
            BudgetChartSlice[] pieSlices,
            ref Texture2D pieTexture,
            ref Sprite pieSprite)
        {
            Image header = CreateUiObject<Image>("BlockHeader", parent);
            SetRect(header.rectTransform, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.96f));
            header.color = Color.clear;
            header.raycastTarget = false;

            Image accent = CreateUiObject<Image>("Accent", header.transform);
            SetRect(accent.rectTransform, new Vector2(0f, 0.15f), new Vector2(0.025f, 0.85f));
            accent.color = headerAccent;
            accent.raycastTarget = false;

            Text title = CreateText("HeaderTitle", header.transform, 14, TextAnchor.MiddleLeft, headerAccent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.98f, 1f));
            title.text = headerTitle;

            Image pieHost = CreateUiObject<Image>("PieHost", parent);
            SetRect(pieHost.rectTransform, new Vector2(0.04f, 0.22f), new Vector2(0.40f, 0.80f));
            pieHost.color = Color.clear;
            pieHost.raycastTarget = false;

            Text pieCaption = CreateText("PieCaption", pieHost.transform, 11, TextAnchor.UpperCenter, TextMuted);
            SetRect(pieCaption.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            pieCaption.text = "構成";

            Image pieImage = CreateUiObject<Image>("Pie", pieHost.transform);
            SetRect(pieImage.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.86f));
            pieImage.sprite = GetBudgetPieSprite(pieSlices, ref pieTexture, ref pieSprite);
            pieImage.preserveAspect = true;
            pieImage.color = Color.white;
            pieImage.raycastTarget = false;

            Image linesHost = CreateUiObject<Image>("Lines", parent);
            SetRect(linesHost.rectTransform, new Vector2(0.42f, 0.04f), new Vector2(0.98f, 0.80f));
            linesHost.color = Color.clear;
            linesHost.raycastTarget = false;

            int totalAbs = 0;
            for (int i = 0; i < lines.Length; i++)
                totalAbs += Math.Abs(lines[i].Amount);

            float yMax = 0.94f;
            float rowH = lines.Length >= 3 ? 0.18f : 0.22f;
            for (int i = 0; i < lines.Length; i++)
            {
                float y1 = yMax - i * rowH;
                float y0 = y1 - rowH + 0.03f;
                int absAmount = Math.Abs(lines[i].Amount);
                string pct = totalAbs > 0 && absAmount > 0
                    ? $" · {absAmount * 100f / totalAbs:0}%"
                    : string.Empty;
                // Color swatch only for items that appear in the pie (positive weight).
                Color? swatch = null;
                if (pieSlices != null && i < pieSlices.Length && pieSlices[i].Weight > 0.01f)
                    swatch = pieSlices[i].Color;
                AddBudgetPanelLine(
                    linesHost.transform,
                    lines[i].Label + pct,
                    lines[i].Amount,
                    lines[i].Color,
                    lines[i].Signed,
                    emphasize: false,
                    swatchColor: swatch,
                    y0,
                    y1,
                    lines[i].DetailTitle,
                    lines[i].DetailBody);
            }

            if (lines.Length == 0)
            {
                Text empty = CreateText("EmptyLines", linesHost.transform, 13, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
                SetRect(empty.rectTransform, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.75f));
                empty.text = "本月尚無此類項目";
                empty.raycastTarget = false;
            }

            Image divider = CreateUiObject<Image>("Divider", linesHost.transform);
            SetRect(divider.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.20f));
            divider.color = CareerEditorialChrome.Rule;
            divider.raycastTarget = false;

            AddBudgetPanelLine(
                linesHost.transform,
                total.label,
                total.amount,
                total.color,
                signed: true,
                emphasize: true,
                swatchColor: null,
                0.02f,
                0.16f,
                null,
                null);
        }

        private static Sprite GetBudgetPieSprite(
            BudgetChartSlice[] slices,
            ref Texture2D texture,
            ref Sprite sprite)
        {
            const int size = 160;
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            FillBudgetPieTexture(texture, slices);
            if (sprite == null)
                sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }

        private static void FillBudgetPieTexture(Texture2D texture, BudgetChartSlice[] slices)
        {
            int size = texture.width;
            Color[] pixels = new Color[size * size];
            float cx = (size - 1) * 0.5f;
            float cy = cx;
            float radius = size * 0.46f;

            float[] fractions = BuildBudgetPieFractions(slices);
            if (fractions.Length == 0)
            {
                FillDisk(pixels, size, cx, cy, radius, new Color(0.38f, 0.40f, 0.48f, 1f));
            }
            else
            {
                float start = -Mathf.PI * 0.5f;
                for (int i = 0; i < fractions.Length; i++)
                {
                    if (slices == null || i >= slices.Length)
                        break;
                    float sweep = fractions[i] * Mathf.PI * 2f;
                    if (sweep <= 0.0001f)
                        continue;
                    float end = start + sweep;
                    FillPieWedge(pixels, size, cx, cy, radius, start, end, slices[i].Color, i == fractions.Length - 1);
                    start = end;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        /// <summary>
        /// Normalizes positive slice weights only. Zero-weight categories get 0 and are skipped when drawing.
        /// </summary>
        private static float[] BuildBudgetPieFractions(BudgetChartSlice[] slices)
        {
            if (slices == null || slices.Length == 0)
                return Array.Empty<float>();

            const float minFraction = 0.05f;
            float sum = 0f;
            int activeCount = 0;
            for (int i = 0; i < slices.Length; i++)
            {
                float w = Math.Max(0f, slices[i].Weight);
                if (w <= 0.01f)
                    continue;
                sum += w;
                activeCount++;
            }

            if (activeCount == 0 || sum <= 0.01f)
                return Array.Empty<float>();

            var fractions = new float[slices.Length];
            if (activeCount == 1)
            {
                for (int i = 0; i < slices.Length; i++)
                {
                    if (slices[i].Weight > 0.01f)
                        fractions[i] = 1f;
                }
                return fractions;
            }

            float boostedSum = 0f;
            for (int i = 0; i < slices.Length; i++)
            {
                float w = Math.Max(0f, slices[i].Weight);
                if (w <= 0.01f)
                    continue;
                fractions[i] = Math.Max(w / sum, minFraction);
                boostedSum += fractions[i];
            }

            for (int i = 0; i < fractions.Length; i++)
            {
                if (fractions[i] > 0f)
                    fractions[i] /= boostedSum;
            }

            return fractions;
        }

        private static void FillDisk(Color[] pixels, int size, float cx, float cy, float radius, Color color)
        {
            FillPieWedge(pixels, size, cx, cy, radius, -Mathf.PI, Mathf.PI, color, includeEndAngle: true);
        }

        private static void FillPieWedge(
            Color[] pixels,
            int size,
            float cx,
            float cy,
            float radius,
            float startRad,
            float endRad,
            Color color,
            bool includeEndAngle = false)
        {
            int xMin = Mathf.Max(0, (int)(cx - radius) - 1);
            int xMax = Mathf.Min(size - 1, (int)(cx + radius) + 1);
            int yMin = Mathf.Max(0, (int)(cy - radius) - 1);
            int yMax = Mathf.Min(size - 1, (int)(cy + radius) + 1);
            float rSq = radius * radius;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy > rSq)
                        continue;
                    if (!AngleInSweep(Mathf.Atan2(dy, dx), startRad, endRad, includeEndAngle))
                        continue;
                    pixels[y * size + x] = color;
                }
            }
        }

        private static bool AngleInSweep(float angle, float start, float end, bool includeEnd)
        {
            // Atan2 is in [-π, π]; wedges are drawn from start increasing to end (may exceed π).
            // Compare angle relative to start so arcs crossing the ±π cut are filled correctly.
            const float twoPi = Mathf.PI * 2f;
            float sweep = end - start;
            if (sweep <= 0f)
                sweep += twoPi;

            float rel = angle - start;
            while (rel < 0f)
                rel += twoPi;
            while (rel >= twoPi)
                rel -= twoPi;

            const float eps = 1e-5f;
            if (includeEnd)
                return rel <= sweep + eps;
            return rel < sweep - eps;
        }

        private void AddBudgetPanelLine(
            Transform parent,
            string label,
            int amount,
            Color amountColor,
            bool signed,
            bool emphasize,
            Color? swatchColor,
            float y0,
            float y1,
            string detailTitle,
            string detailBody)
        {
            Image row = CreateUiObject<Image>("Line", parent);
            SetRect(row.rectTransform, new Vector2(0.02f, y0), new Vector2(0.98f, y1));
            row.color = emphasize ? new Color(0.86f, 0.82f, 0.74f, 1f) : Color.clear;
            bool hasDetail = !string.IsNullOrWhiteSpace(detailTitle) && !string.IsNullOrWhiteSpace(detailBody);
            row.raycastTarget = hasDetail;

            float labelMinX = 0.02f;
            if (swatchColor.HasValue)
            {
                Image swatch = CreateUiObject<Image>("Swatch", row.transform);
                SetRect(swatch.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.08f, 0.72f));
                swatch.color = swatchColor.Value;
                swatch.raycastTarget = false;
                labelMinX = 0.10f;
            }

            int size = emphasize ? 14 : 13;
            Color labelColor = emphasize ? TextPrimary : TextMuted;
            Text labelText = CreateText("Label", row.transform, size, TextAnchor.MiddleLeft, labelColor, _uiFont700);
            SetRect(labelText.rectTransform, new Vector2(labelMinX, 0.05f), new Vector2(0.58f, 0.95f));
            labelText.text = label;
            labelText.raycastTarget = false;

            string value = signed
                ? (amount >= 0 ? $"+${amount:N0}" : $"-${Math.Abs(amount):N0}")
                : $"${amount:N0}";
            Text valueText = CreateText("Value", row.transform, size, TextAnchor.MiddleRight, amountColor, _uiFont700);
            SetRect(valueText.rectTransform, new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.95f));
            valueText.text = value;
            valueText.raycastTarget = false;

            if (!hasDetail)
                return;

            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            string capturedTitle = detailTitle;
            string capturedBody = detailBody;
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                CancelStatTooltip();
                int lines = Math.Max(1, capturedBody.Split('\n').Length);
                float height = Mathf.Clamp(120f + lines * 22f, 140f, 360f);
                ShowPlainTooltip(capturedTitle, capturedBody, new Vector2(360f, height), Input.mousePosition);
            });
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => HideStatTooltip());
        }

        private RectTransform AddRow(float height)
        {
            Transform parent = _uiListRoot != null ? _uiListRoot : _contentRoot;
            Image row = CreateUiObject<Image>("Row", parent);
            row.color = PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return row.rectTransform;
        }

        private void AddStatCard(RectTransform parent, float x0, float x1, string title, string value, Color valueColor)
        {
            Image card = CreateUiObject<Image>("Stat", parent);
            SetRect(card.rectTransform, new Vector2(x0 + 0.005f, 0.06f), new Vector2(x1 - 0.005f, 0.94f));
            UiLightChromeUtility.ApplySoftNavyPanel(card);

            Text titleText = CreateText("Title", card.transform, 12, TextAnchor.MiddleCenter, TextMuted);
            SetRect(titleText.rectTransform, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.92f));
            titleText.text = title;

            Text valueText = CreateText("Value", card.transform, 18, TextAnchor.MiddleCenter, valueColor);
            SetRect(valueText.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.58f));
            valueText.text = value;
        }

        private void AddRowLabel(
            RectTransform row, float x0, float x1, string content, Color color, int size,
            TextAnchor align = TextAnchor.MiddleLeft, float y0 = 0.08f, float y1 = 0.92f,
            bool wrap = false)
        {
            Text text = CreateText("Label", row, size, align, color);
            SetRect(text.rectTransform, new Vector2(x0, y0), new Vector2(x1, y1));
            if (wrap)
            {
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
            text.text = content;
        }

        private void AddRowButton(RectTransform row, float x0, float x1, string label, UnityEngine.Events.UnityAction action, int fontSize = 16)
        {
            CreateGlassCapsuleButton("Btn", label, row, new Vector2(x0, 0.10f), new Vector2(x1, 0.90f),
                ButtonEdge, ButtonFace, action, font: _uiFont700, fontSize: fontSize);
        }

        private Text CreateIconStat(
            string name, Transform parent, string iconResource, Vector2 min, Vector2 max, Color valueColor,
            string tooltipKey = null, UnityEngine.Events.UnityAction onClick = null)
        {
            Image root = CreateUiObject<Image>(name, parent);
            SetRect(root.rectTransform, min, max);
            root.color = Color.clear;
            bool interactive = onClick != null || !string.IsNullOrEmpty(tooltipKey);
            root.raycastTarget = interactive;

            Image icon = CreateUiObject<Image>("Icon", root.transform);
            SetRect(icon.rectTransform, new Vector2(0f, 0.12f), new Vector2(0.28f, 0.88f));
            icon.sprite = Resources.Load<Sprite>(iconResource);
            icon.preserveAspect = true;
            icon.color = CareerEditorialChrome.StageType;
            icon.raycastTarget = false;

            Text value = CreateText("Value", root.transform, 32, TextAnchor.MiddleLeft, valueColor, _uiFont700);
            SetRect(value.rectTransform, new Vector2(0.32f, 0.08f), new Vector2(1f, 0.92f));
            value.text = "—";
            value.fontStyle = FontStyle.Bold;
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 14;
            value.resizeTextMaxSize = 28;
            value.raycastTarget = false;

            if (onClick != null)
            {
                Button button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = root;
                ApplyButtonColors(button);
                button.onClick.AddListener(onClick);
            }

            if (!string.IsNullOrEmpty(tooltipKey))
            {
                EventTrigger trigger = root.gameObject.GetComponent<EventTrigger>() ??
                                       root.gameObject.AddComponent<EventTrigger>();
                string key = tooltipKey;
                AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => OnHeaderStatPointerEnter(key));
                AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnHeaderStatPointerExit());
            }

            return value;
        }

        private Text CreateLabeledIconStat(
            string name, Transform parent, string iconResource, string label,
            Vector2 min, Vector2 max, Color valueColor)
        {
            Image root = CreateUiObject<Image>(name, parent);
            SetRect(root.rectTransform, min, max);
            root.color = Color.clear;
            root.raycastTarget = false;

            Image icon = CreateUiObject<Image>("Icon", root.transform);
            SetRect(icon.rectTransform, new Vector2(0.02f, 0.16f), new Vector2(0.12f, 0.84f));
            icon.sprite = Resources.Load<Sprite>(iconResource);
            icon.preserveAspect = true;
            icon.color = CareerEditorialChrome.Ink;
            icon.raycastTarget = false;

            Text labelText = CreateText("Label", root.transform, 12, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(labelText.rectTransform, new Vector2(0.16f, 0.08f), new Vector2(0.48f, 0.92f));
            labelText.text = label;
            labelText.raycastTarget = false;

            Text value = CreateText("Value", root.transform, 13, TextAnchor.MiddleRight, valueColor, _uiFont700);
            SetRect(value.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.96f, 0.92f));
            value.text = "—";
            value.raycastTarget = false;
            return value;
        }

        private void CreateIconButton(
            string name, Transform parent, string iconResource, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction action)
        {
            Sprite shape = UiLightChromeUtility.ShapeSpriteForAnchors(min, max);
            Image edge = CreateUiObject<Image>(name, parent);
            SetRect(edge.rectTransform, min, max);
            edge.sprite = shape;
            edge.type = Image.Type.Sliced;
            edge.color = UiLightChromeUtility.CapsuleRim;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.sprite = shape;
            face.type = Image.Type.Sliced;
            face.color = UiLightChromeUtility.FlatFace;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);

            Image icon = CreateUiObject<Image>("Icon", face.transform);
            SetRect(icon.rectTransform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f));
            icon.sprite = Resources.Load<Sprite>(iconResource);
            icon.preserveAspect = true;
            icon.color = CareerEditorialChrome.Ink;
            icon.raycastTarget = false;

            UiLightChromeUtility.WireWhiteButtonHoverInvert(button, face, label: null);
        }

        private static Sprite LoadClubLogo(string clubId)
        {
            Sprite logo = LoadClubLogoOrNull(clubId);
            return logo != null ? logo : Resources.Load<Sprite>("UI/Logos/navi");
        }

        private void BuildSettingsPanel(Transform canvas)
        {
            var host = new SettingsPanelHost
            {
                CareerSaveLoadEnabled = true,
                CanLoad = () =>
                {
                    CareerDifficulty difficulty = _session?.State.difficulty ?? _newDifficulty;
                    return CareerDifficultyRules.AllowsManualLoad(difficulty);
                },
                CanSave = () =>
                {
                    if (!_careerStarted || _session == null) return false;
                    return CareerDifficultyRules.AllowsManualSave(_session.State.difficulty);
                },
                DifficultyHint = BuildSettingsDifficultyHint,
                GetSelectedSlot = () => _activeSaveSlot,
                OnSelectSlot = SetActiveSaveSlot,
                DescribeSlot = DescribeSaveSlot,
                SlotHasSave = slot => _persistence != null && _persistence.Exists(slot),
                OnLoadSlot = slot =>
                {
                    LoadCareer(slot);
                    HideSettingsPanel();
                },
                OnSaveSlot = slot =>
                {
                    SaveCareer(slot);
                    HideSettingsPanel();
                },
                OnDeleteSlot = DeleteCareerSlot,
                OnReturnToMainMenu = ReturnToMainMenuFromSettings,
                OnClose = OnSettingsClosed
            };

            _settingsPanel = new SettingsPanelPresentation(_uiFont700 ?? _uiFont, host);
            _settingsPanel.Build(canvas);
        }

        private string BuildSettingsDifficultyHint()
        {
            CareerDifficulty difficulty = _session?.State.difficulty ?? _newDifficulty;
            bool canLoad = CareerDifficultyRules.AllowsManualLoad(difficulty);
            if (_careerStarted)
            {
                return $"難度：{CareerDifficultyRules.DisplayName(difficulty)} · " +
                       (canLoad
                           ? $"可讀檔 · 目前槽位 {_activeSaveSlot}"
                           : "硬核：不可讀檔，僅自動存檔") +
                       (CareerDifficultyRules.AchievementsEligible(difficulty)
                           ? " · 成就可解"
                           : CareerDifficultyRules.UsesAttributeScouting(difficulty)
                               ? " · 他隊屬性需分析師"
                               : "");
            }

            return "開始生涯前請在標題畫面選擇難度（滑鼠停留 1 秒可看說明）";
        }

        private void ToggleSettingsPanel()
        {
            if (_settingsPanel == null) return;
            if (_settingsPanel.IsVisible)
                HideSettingsPanel();
            else
                ShowSettingsPanel();
        }

        private void ShowSettingsPanel()
        {
            if (_settingsPanel == null) return;
            CancelStatTooltip();
            HideStatTooltip();
            _pausedBeforeSettings = _timePaused;
            if (_careerStarted && _session != null && !_timePaused)
                PauseTime("設定開啟 · 時間已暫停");
            _settingsPanel.Show();
        }

        private void OnSettingsClosed()
        {
            if (_careerStarted && _session != null && !_pausedBeforeSettings && _timePaused)
                ResumeTime();
        }

        private void HideSettingsPanel()
        {
            if (_settingsPanel == null) return;
            bool wasVisible = _settingsPanel.IsVisible;
            _settingsPanel.Hide();
            if (wasVisible)
                OnSettingsClosed();
        }

        private void ReturnToMainMenuFromSettings()
        {
            OnSettingsClosed();
            if (_careerStarted && _session != null &&
                CareerDifficultyRules.AllowsManualSave(_session.State.difficulty))
                SaveCareer();
            ShowTitleScreen();
            _status = "已返回主菜單";
            if (_statusText != null) _statusText.text = _status;
        }

        private void TickOptionalAutoSave()
        {
            if (!_careerStarted || _session == null || _persistence == null) return;
            if (CareerDifficultyRules.UsesIronmanAutoSave(_session.State.difficulty))
                return;

            GameSettingsState settings = GameSettingsRuntime.Current;
            if (!settings.autoSaveEnabled)
            {
                _autoSaveTimer = 0f;
                return;
            }

            if (_timePaused) return;

            _autoSaveTimer += Time.unscaledDeltaTime;
            float intervalSeconds = (int)settings.autoSaveIntervalMinutes * 60f;
            if (_autoSaveTimer < intervalSeconds) return;

            _autoSaveTimer = 0f;
            if (!CareerDifficultyRules.AllowsManualSave(_session.State.difficulty))
                return;
            _persistence.Save(_session.State, _activeSaveSlot);
            _status = "已自動存檔";
            if (_statusText != null) _statusText.text = _status;
        }

        private void BuildWorldRankingPanel(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("RankingOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            _rankingRoot = dim.gameObject;
            _rankingRoot.SetActive(false);
            _rankingShowAllClubs = false;

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideWorldRankingPanel);

            // D3 light_chrome: Shadow > Frame > Face; opaque Panel fill; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "RankingPanel", dim.transform,
                new Vector2(0.14f, 0.04f), new Vector2(0.96f, 0.96f), Panel);
            UiChromeMotion.BindFace(_rankingRoot, panel);

            _rankingTitleText = CreateText("Title", panel.transform, 22, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(_rankingTitleText.rectTransform, new Vector2(0.04f, 0.905f), new Vector2(0.30f, 0.985f));
            _rankingTitleText.text = "世界排名 · Top 8";

            Button worldModeBtn = CreateGlassCapsuleButton("RankModeWorld", "世界排名", panel.transform,
                new Vector2(0.31f, 0.905f), new Vector2(0.44f, 0.975f), AccentDim, Accent,
                () => SetRankingPanelMode(rmr: false));
            _rankingModeWorldLabel = worldModeBtn.GetComponentInChildren<Text>();
            ApplyFilledButtonLabel(_rankingModeWorldLabel);

            Button rmrModeBtn = CreateGlassCapsuleButton("RankModeRmr", "地區 Major 排名", panel.transform,
                new Vector2(0.445f, 0.905f), new Vector2(0.62f, 0.975f), ButtonEdge, ButtonFace,
                () => SetRankingPanelMode(rmr: true));
            _rankingModeRmrLabel = rmrModeBtn.GetComponentInChildren<Text>();
            ApplyFilledButtonLabel(_rankingModeRmrLabel);

            Button allBtn = CreateGlassCapsuleButton("AllClubs", "全部戰隊 ›", panel.transform,
                new Vector2(0.625f, 0.905f), new Vector2(0.80f, 0.975f), AccentDim, Accent, ToggleRankingSecondaryView);
            _rankingAllButtonLabel = allBtn.GetComponentInChildren<Text>();

            CreateGlassCapsuleButton("CloseRanking", "關閉", panel.transform,
                new Vector2(0.81f, 0.905f), new Vector2(0.96f, 0.975f), ButtonEdge, ButtonFace, HideWorldRankingPanel);

            Image headerRow = CreateUiObject<Image>("HeaderRow", panel.transform);
            SetRect(headerRow.rectTransform, new Vector2(0.04f, 0.835f), new Vector2(0.96f, 0.895f));
            headerRow.color = PanelInner;
            _rankingHeaderRank = CreateText("HRank", headerRow.transform, 16, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
            SetRect(_rankingHeaderRank.rectTransform, new Vector2(0.01f, 0f), new Vector2(0.10f, 1f));
            _rankingHeaderRank.text = "排名";
            _rankingHeaderClub = CreateText("HClub", headerRow.transform, 16, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(_rankingHeaderClub.rectTransform, new Vector2(0.12f, 0f), new Vector2(0.62f, 1f));
            _rankingHeaderClub.text = "俱樂部";
            _rankingHeaderPts = CreateText("HPts", headerRow.transform, 16, TextAnchor.MiddleRight, TextMuted, _uiFont700);
            SetRect(_rankingHeaderPts.rectTransform, new Vector2(0.64f, 0f), new Vector2(0.98f, 1f));
            _rankingHeaderPts.text = "HLTV pts";

            Image scrollHost = CreateUiObject<Image>("RankingScrollHost", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.828f));
            scrollHost.color = Color.clear;
            scrollHost.raycastTarget = true;

            ScrollRect scroll = scrollHost.gameObject.AddComponent<ScrollRect>();
            _rankingScroll = scroll;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42f;

            Image viewport = CreateUiObject<Image>("Viewport", scrollHost.transform);
            Stretch(viewport.rectTransform);
            viewport.color = Color.clear;
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            _rankingListRoot = new GameObject("RankingList", typeof(RectTransform)).GetComponent<RectTransform>();
            _rankingListRoot.SetParent(viewport.transform, false);
            _rankingListRoot.anchorMin = new Vector2(0f, 1f);
            _rankingListRoot.anchorMax = new Vector2(1f, 1f);
            _rankingListRoot.pivot = new Vector2(0.5f, 1f);
            _rankingListRoot.anchoredPosition = Vector2.zero;
            _rankingListRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = _rankingListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 3f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var fitter = _rankingListRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _rankingListRoot;

            BuildRankingClubIntelOverlay(canvas);
        }

        private void OpenWorldRankingPanel()
        {
            if (!_careerStarted || _session == null)
            {
                _status = "請先開始生涯";
                if (_statusText != null) _statusText.text = _status;
                return;
            }

            HideSettingsPanel();
            CancelStatTooltip();
            HideStatTooltip();
            _rankingShowAllClubs = false;
            _rankingShowRmr = false;
            _rankingRmrRegionIndex = 0;
            RefreshWorldRankingPanel();
            if (_rankingRoot != null)
            {
                UiChromeMotion.Show(_rankingRoot);
                _rankingRoot.transform.SetAsLastSibling();
                RefreshCanvasFrost();
            }
        }

        private void HideWorldRankingPanel()
        {
            HideRankingClubIntel();
            UiChromeMotion.Hide(_rankingRoot);
            _rankingShowAllClubs = false;
            _rankingShowRmr = false;
        }

        private void SetRankingPanelMode(bool rmr)
        {
            _rankingShowRmr = rmr;
            _rankingShowAllClubs = false;
            if (rmr)
                EnsureRmrRankingsFresh();
            RefreshWorldRankingPanel();
        }

        private void ToggleRankingSecondaryView()
        {
            if (_rankingShowRmr)
            {
                _rankingRmrRegionIndex = (_rankingRmrRegionIndex + 1) % RmrRules.AllRegions.Length;
                RefreshWorldRankingPanel();
                return;
            }

            _rankingShowAllClubs = !_rankingShowAllClubs;
            RefreshWorldRankingPanel();
        }

        private void ToggleRankingAllClubsView()
        {
            ToggleRankingSecondaryView();
        }

        private void EnsureRmrRankingsFresh()
        {
            if (_session?.State == null || _content?.Clubs == null)
                return;
            CareerState state = _session.State;
            state.rmrBatches = state.rmrBatches ?? new List<RmrPointBatch>();
            if ((state.rmrRankings == null || state.rmrRankings.Count == 0) &&
                state.rmrBatches.Count == 0)
            {
                foreach (CareerClubDefinition club in _content.Clubs.Values)
                {
                    int circuit = RmrRules.CircuitSeedFromClub(club);
                    if (circuit <= 0)
                        continue;
                    if (state.rmrBatches.Any(x =>
                            x != null && x.seedSnapshot &&
                            string.Equals(x.clubId, club.id, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    state.rmrBatches.Add(RmrRules.CreateCircuitSeedBatch(club.id, circuit));
                }
            }

            state.rmrRankings = RmrRules.Build(state.rmrBatches, _content.Clubs.Values);
        }

        private void RefreshWorldRankingPanel()
        {
            if (_rankingListRoot == null || _session == null) return;

            for (int i = _rankingListRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_rankingListRoot.GetChild(i).gameObject);

            if (_rankingShowRmr)
            {
                RefreshRmrRankingPanel();
                return;
            }

            if (_rankingTitleText != null)
                _rankingTitleText.text = _rankingShowAllClubs ? "全部戰隊" : "世界排名 · Top 8";
            if (_rankingAllButtonLabel != null)
                _rankingAllButtonLabel.text = _rankingShowAllClubs ? "‹ 返回 Top 8" : "全部戰隊 ›";
            UpdateRankingModeButtonStyles();

            if (_rankingHeaderRank != null) _rankingHeaderRank.text = "排名";
            if (_rankingHeaderClub != null)
                _rankingHeaderClub.text = _rankingShowAllClubs ? "俱樂部／地區／陣容" : "俱樂部";
            if (_rankingHeaderPts != null)
                _rankingHeaderPts.text = "HLTV pts";

            if (_rankingScroll != null)
                _rankingScroll.vertical = _rankingShowAllClubs;

            CareerState state = _session.State;
            RankingEntry[] rankings = state.rankings
                .OrderBy(x => WorldRankLabel(x.clubId, x, state))
                .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (rankings.Length == 0 && (state.clubs == null || state.clubs.Count == 0))
            {
                AddRankingEmptyRow("尚無排名資料");
                return;
            }

            if (_rankingShowAllClubs)
                RefreshAllClubsDetailList(state, rankings);
            else
            {
                RefreshTopEightRankingList(state, rankings);
                FitTopEightRowsToViewport();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rankingListRoot);
        }

        private void UpdateRankingModeButtonStyles()
        {
            if (_rankingModeWorldLabel != null)
                _rankingModeWorldLabel.color = _rankingShowRmr
                    ? CareerEditorialChrome.InkMuted
                    : CareerEditorialChrome.Ink;
            if (_rankingModeRmrLabel != null)
                _rankingModeRmrLabel.color = _rankingShowRmr
                    ? CareerEditorialChrome.Ink
                    : CareerEditorialChrome.InkMuted;
        }

        private void RefreshRmrRankingPanel()
        {
            EnsureRmrRankingsFresh();
            string region = RmrRules.AllRegions[
                Math.Max(0, _rankingRmrRegionIndex) % RmrRules.AllRegions.Length];
            string regionLabel = RmrRules.RegionDisplayName(region);

            if (_rankingTitleText != null)
                _rankingTitleText.text = "地區 Major 排名 · " + regionLabel;
            if (_rankingAllButtonLabel != null)
                _rankingAllButtonLabel.text = "切換地區 ›";
            UpdateRankingModeButtonStyles();

            if (_rankingHeaderRank != null) _rankingHeaderRank.text = "區內";
            if (_rankingHeaderClub != null) _rankingHeaderClub.text = "俱樂部／資格";
            if (_rankingHeaderPts != null) _rankingHeaderPts.text = "RMR 積分";
            if (_rankingScroll != null)
                _rankingScroll.vertical = true;

            CareerState state = _session.State;
            RmrEntry[] entries = (state.rmrRankings ?? new List<RmrEntry>())
                .Where(x => x != null &&
                            string.Equals(x.rmrRegion, region, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.regionRank)
                .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (entries.Length == 0)
            {
                AddRankingEmptyRow(regionLabel + " 尚無地區 Major 排名資料");
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rankingListRoot);
                return;
            }

            foreach (RmrEntry entry in entries)
                AddRmrRankRow(state, entry);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rankingListRoot);
        }

        private void AddRmrRankRow(CareerState state, RmrEntry entry)
        {
            bool isPlayer = string.Equals(entry.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase);
            string clubName = _content.Clubs.TryGetValue(entry.clubId, out CareerClubDefinition club)
                ? club.displayName
                : entry.clubId;
            string stage = RmrRules.StageDisplayName(entry.stage);

            Image row = CreateUiObject<Image>("RmrRankRow", _rankingListRoot);
            row.color = isPlayer ? new Color(0.22f, 0.20f, 0.38f, 1f) : PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 78f;
            le.preferredHeight = 78f;
            le.flexibleWidth = 1f;

            Image logo = CreateUiObject<Image>("Logo", row.transform);
            SetRect(logo.rectTransform, new Vector2(0.02f, 0.14f), new Vector2(0.11f, 0.86f));
            logo.sprite = LoadClubLogoOrNull(entry.clubId);
            logo.preserveAspect = true;
            logo.color = logo.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            WireRankingClubLogoButton(logo, entry.clubId);

            Text rankText = CreateText("Rank", row.transform, 22, TextAnchor.MiddleCenter,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(rankText.rectTransform, new Vector2(0.11f, 0.08f), new Vector2(0.19f, 0.92f));
            rankText.text = $"#{entry.regionRank}";

            Text clubText = CreateText("Club", row.transform, 18, TextAnchor.MiddleLeft,
                isPlayer ? Accent : TextPrimary, _uiFont700);
            SetRect(clubText.rectTransform, new Vector2(0.20f, 0.42f), new Vector2(0.62f, 0.92f));
            clubText.text = isPlayer ? $"★ {clubName}" : clubName;

            Text stageText = CreateText("Stage", row.transform, 13, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(stageText.rectTransform, new Vector2(0.20f, 0.08f), new Vector2(0.62f, 0.42f));
            stageText.text = "資格預覽 " + stage;

            Text ptsText = CreateText("Pts", row.transform, 15, TextAnchor.MiddleRight,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(ptsText.rectTransform, new Vector2(0.60f, 0.08f), new Vector2(0.98f, 0.92f));
            ptsText.supportRichText = true;
            ptsText.lineSpacing = 0.9f;
            ptsText.text =
                $"{entry.totalPoints:0}\n" +
                $"<size=12><color=#8890A0>Circuit {entry.circuitPoints:0} · RMR1 {entry.rmr1Points:0} · " +
                $"RMR2 {entry.rmr2Points:0} · RMR3 {entry.rmr3Points:0}</color></size>";
            if (entry.deductionPoints < -0.5f)
            {
                ptsText.text +=
                    $"\n<size=12><color=#C07070>扣分 {entry.deductionPoints:0}</color></size>";
            }
        }

        private void FitTopEightRowsToViewport()
        {
            if (_rankingScroll == null || _rankingListRoot == null) return;

            Canvas.ForceUpdateCanvases();
            RectTransform viewport = _rankingScroll.viewport;
            if (viewport == null) return;

            int count = _rankingListRoot.childCount;
            if (count <= 0) return;

            VerticalLayoutGroup layout = _rankingListRoot.GetComponent<VerticalLayoutGroup>();
            float pad = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
            float spacing = layout != null ? layout.spacing : 3f;
            float viewHeight = viewport.rect.height;
            float available = viewHeight - pad - spacing * Mathf.Max(0, count - 1);
            float rowHeight = Mathf.Clamp(available / count, 62f, 96f);

            for (int i = 0; i < count; i++)
            {
                Transform row = _rankingListRoot.GetChild(i);
                LayoutElement le = row.GetComponent<LayoutElement>();
                if (le == null) continue;
                le.minHeight = rowHeight;
                le.preferredHeight = rowHeight;
                ApplyTopEightRowTypography(row, rowHeight);
            }

            float contentHeight = rowHeight * count + pad + spacing * Mathf.Max(0, count - 1);
            _rankingScroll.vertical = contentHeight > viewHeight + 2f;
            if (!_rankingScroll.vertical)
                _rankingScroll.verticalNormalizedPosition = 1f;
        }

        private static void ApplyTopEightRowTypography(Transform row, float rowHeight)
        {
            int rankSize = rowHeight >= 84f ? 26 : rowHeight >= 72f ? 23 : 20;
            int clubSize = rankSize - 1;
            int ptsSize = rankSize - 2;

            Text rankText = row.Find("Rank")?.GetComponent<Text>();
            if (rankText != null) rankText.fontSize = rankSize;

            Text clubText = row.Find("Club")?.GetComponent<Text>();
            if (clubText != null) clubText.fontSize = clubSize;

            Text ptsText = row.Find("Pts")?.GetComponent<Text>();
            if (ptsText != null) ptsText.fontSize = ptsSize;

            Image logo = row.Find("Logo")?.GetComponent<Image>();
            if (logo != null)
            {
                float logoPad = rowHeight >= 72f ? 0.10f : 0.14f;
                SetRect(logo.rectTransform, new Vector2(0.02f, logoPad), new Vector2(0.11f, 1f - logoPad));
            }
        }

        private void RefreshTopEightRankingList(CareerState state, RankingEntry[] rankings)
        {
            RankingEntry[] top = rankings.Take(8).ToArray();
            if (top.Length == 0)
            {
                AddRankingEmptyRow("尚無排名資料");
                return;
            }

            foreach (RankingEntry rank in top)
                AddTopEightRankRow(state, rank);
        }

        private void AddTopEightRankRow(CareerState state, RankingEntry rank)
        {
            bool isPlayer = string.Equals(rank.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase);
            string clubName = _content.Clubs.TryGetValue(rank.clubId, out CareerClubDefinition club)
                ? club.displayName
                : rank.clubId;

            Image row = CreateUiObject<Image>("RankRow", _rankingListRoot);
            row.color = isPlayer ? new Color(0.22f, 0.20f, 0.38f, 1f) : PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 68f;
            le.preferredHeight = 68f;
            le.flexibleWidth = 1f;

            Image logo = CreateUiObject<Image>("Logo", row.transform);
            SetRect(logo.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.11f, 0.88f));
            logo.sprite = LoadClubLogoOrNull(rank.clubId);
            logo.preserveAspect = true;
            logo.color = logo.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            WireRankingClubLogoButton(logo, rank.clubId);

            Text rankText = CreateText("Rank", row.transform, 23, TextAnchor.MiddleCenter,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(rankText.rectTransform, new Vector2(0.11f, 0.08f), new Vector2(0.19f, 0.92f));
            rankText.text = $"#{WorldRankLabel(rank.clubId, rank, state)}";

            Text clubText = CreateText("Club", row.transform, 22, TextAnchor.MiddleLeft,
                isPlayer ? Accent : TextPrimary, _uiFont700);
            SetRect(clubText.rectTransform, new Vector2(0.20f, 0.08f), new Vector2(0.70f, 0.92f));
            clubText.text = isPlayer ? $"★ {clubName}" : clubName;

            Text ptsText = CreateText("Pts", row.transform, 18, TextAnchor.MiddleRight,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(ptsText.rectTransform, new Vector2(0.62f, 0.08f), new Vector2(0.98f, 0.92f));
            float total = WorldPointsLabel(rank.clubId, rank, state);
            ptsText.text = $"{total:0}\n<size=14><color=#8890A0>近期分數 {RankingRules.ClampHltvPoints(rank.recentPoints):0}</color></size>";
            ptsText.supportRichText = true;
            ptsText.lineSpacing = 0.92f;
        }

        private void RefreshAllClubsDetailList(CareerState state, RankingEntry[] rankings)
        {
            Dictionary<string, RankingEntry> byClub = rankings.ToDictionary(
                x => x.clubId, x => x, StringComparer.OrdinalIgnoreCase);

            // Prefer ranking order; append any clubs missing from rankings.
            List<string> orderedIds = rankings.Select(x => x.clubId).ToList();
            foreach (ClubState clubState in state.clubs)
            {
                if (!orderedIds.Contains(clubState.clubId, StringComparer.OrdinalIgnoreCase))
                    orderedIds.Add(clubState.clubId);
            }

            foreach (CareerClubDefinition def in _content.Clubs.Values)
            {
                if (!orderedIds.Contains(def.id, StringComparer.OrdinalIgnoreCase))
                    orderedIds.Add(def.id);
            }

            if (orderedIds.Count == 0)
            {
                AddRankingEmptyRow("尚無戰隊資料");
                return;
            }

            foreach (string clubId in orderedIds)
                AddAllClubDetailRow(state, clubId, byClub);
        }

        private void AddAllClubDetailRow(
            CareerState state, string clubId, Dictionary<string, RankingEntry> byClub)
        {
            bool isPlayer = string.Equals(clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase);
            _content.Clubs.TryGetValue(clubId, out CareerClubDefinition def);
            ClubState clubState = state.clubs.FirstOrDefault(x =>
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
            byClub.TryGetValue(clubId, out RankingEntry rank);

            string clubName = def != null ? def.displayName : clubId;
            string region = def != null && !string.IsNullOrEmpty(def.region) ? def.region : "—";
            int fame = clubState != null ? clubState.fame : (def != null ? def.fame : 0);
            int cash = clubState != null ? clubState.cash : (def != null ? def.startingCash : 0);
            int fans = clubState != null ? clubState.fans : 0;
            string health = clubState != null ? HealthLabel(clubState.financialHealth) : "—";
            string rankLabel = rank != null ? $"#{WorldRankLabel(clubId, rank, state)}" : "—";
            string ptsLabel = rank != null ? $"{WorldPointsLabel(clubId, rank, state):0} HLTV pts" : "—";

            string[] rosterNames = BuildClubRosterNames(state, clubId, def);
            string rosterLine = rosterNames.Length > 0
                ? string.Join(" · ", rosterNames)
                : "（陣容資料尚未齊全）";

            Image row = CreateUiObject<Image>("ClubDetailRow", _rankingListRoot);
            row.color = isPlayer ? new Color(0.22f, 0.20f, 0.38f, 1f) : PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 96f;
            le.preferredHeight = 96f;
            le.flexibleWidth = 1f;

            Image logo = CreateUiObject<Image>("Logo", row.transform);
            SetRect(logo.rectTransform, new Vector2(0.015f, 0.22f), new Vector2(0.095f, 0.78f));
            logo.sprite = LoadClubLogoOrNull(clubId);
            logo.preserveAspect = true;
            logo.color = logo.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
            WireRankingClubLogoButton(logo, clubId);

            Text rankText = CreateText("Rank", row.transform, 18, TextAnchor.MiddleCenter,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(rankText.rectTransform, new Vector2(0.10f, 0.55f), new Vector2(0.18f, 0.92f));
            rankText.text = rankLabel;

            Text nameText = CreateText("Name", row.transform, 17, TextAnchor.MiddleLeft,
                isPlayer ? Accent : TextPrimary, _uiFont700);
            SetRect(nameText.rectTransform, new Vector2(0.19f, 0.58f), new Vector2(0.62f, 0.92f));
            nameText.text = isPlayer ? $"★ {clubName}" : clubName;

            Text metaText = CreateText("Meta", row.transform, 13, TextAnchor.MiddleLeft, TextMuted, _uiFont700);
            SetRect(metaText.rectTransform, new Vector2(0.19f, 0.32f), new Vector2(0.72f, 0.58f));
            metaText.text = $"{region} · 名氣 {fame} · 威望 {(clubState?.exposure ?? fame)} · 粉絲 {fans:N0} · 現金 ${cash:N0} · {health}";

            Text rosterText = CreateText("Roster", row.transform, 12, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(rosterText.rectTransform, new Vector2(0.19f, 0.06f), new Vector2(0.96f, 0.32f));
            rosterText.text = rosterLine;
            rosterText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rosterText.verticalOverflow = VerticalWrapMode.Truncate;

            Text ptsText = CreateText("Pts", row.transform, 14, TextAnchor.MiddleRight,
                isPlayer ? RankGold : TextMuted, _uiFont700);
            SetRect(ptsText.rectTransform, new Vector2(0.64f, 0.58f), new Vector2(0.97f, 0.92f));
            ptsText.text = ptsLabel;
        }

        private string[] BuildClubRosterNames(CareerState state, string clubId, CareerClubDefinition def)
        {
            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(string id)
            {
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    return;
                ordered.Add(id);
            }

            if (def?.playerIds != null)
            {
                for (int i = 0; i < def.playerIds.Length; i++)
                    TryAdd(def.playerIds[i]);
            }

            if (state?.players != null)
            {
                foreach (PlayerCareerState player in state.players)
                {
                    if (player == null) continue;
                    if (!string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    TryAdd(player.playerId);
                }
            }

            return ordered
                .Take(6)
                .Select(id =>
                {
                    if (_content.Players.TryGetValue(id, out CareerPlayerDefinition player) &&
                        !string.IsNullOrWhiteSpace(player.displayName))
                        return player.displayName;
                    return id;
                })
                .ToArray();
        }

        private void AddRankingEmptyRow(string message)
        {
            Image empty = CreateUiObject<Image>("Empty", _rankingListRoot);
            empty.color = PanelRow;
            var emptyLe = empty.gameObject.AddComponent<LayoutElement>();
            emptyLe.minHeight = 48f;
            emptyLe.preferredHeight = 48f;
            emptyLe.flexibleWidth = 1f;
            Text emptyText = CreateText("EmptyText", empty.transform, 14, TextAnchor.MiddleCenter, TextMuted, _uiFont700);
            Stretch(emptyText.rectTransform);
            emptyText.text = message;
        }

        private static Sprite LoadClubLogoOrNull(string clubId)
        {
            if (string.Equals(clubId, "gambit_2021", StringComparison.OrdinalIgnoreCase))
                return Resources.Load<Sprite>("UI/Logos/gambit");
            if (string.Equals(clubId, "navi_2021", StringComparison.OrdinalIgnoreCase))
                return Resources.Load<Sprite>("UI/Logos/navi");
            return null;
        }

        private void BindTitleMotion(RectTransform rect, int index)
        {
            if (rect == null || index < 0 || index >= _titleMotionBlocks.Length)
                return;
            _titleMotionBlocks[index] = rect;
            if (rect.GetComponent<UiSettleSlide>() == null)
                rect.gameObject.AddComponent<UiSettleSlide>();
        }

        private void ApplyFlatFrame(Image panel)
        {
            if (panel == null)
                return;
            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = UiLightChromeUtility.FlatBorder;
            Image fill = CreateUiObject<Image>("FlatFill", panel.transform);
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            fill.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            fill.sprite = rounded;
            fill.type = Image.Type.Sliced;
            fill.color = UiLightChromeUtility.FlatFace;
            fill.raycastTarget = false;
            fill.transform.SetAsFirstSibling();
        }

        private void BuildGoldDrift(Transform parent, string name, Vector2 min, Vector2 max, float alpha, float period)
        {
            Image disc = CreateUiObject<Image>(name, parent);
            SetRect(disc.rectTransform, min, max);
            disc.sprite = UiRoundedRectUtility.GetRadialGlowSprite();
            disc.type = Image.Type.Simple;
            disc.color = new Color(Accent.r, Accent.g, Accent.b, alpha);
            disc.raycastTarget = false;
            UiGoldDrift drift = disc.gameObject.AddComponent<UiGoldDrift>();
            drift.Period = period;
            drift.Amplitude = 48f;
        }

        private Button CreateTitleHairlineButton(
            string name, string label, Transform parent, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction action, int fontSize)
        {
            Image edge = CreateUiObject<Image>(name, parent);
            SetRect(edge.rectTransform, min, max);
            edge.sprite = UiRoundedRectUtility.GetRoundedRectSprite();
            edge.type = Image.Type.Sliced;
            edge.color = new Color(1f, 1f, 1f, 0.28f);
            edge.raycastTarget = true;

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.sprite = UiRoundedRectUtility.GetRoundedRectSprite();
            face.type = Image.Type.Sliced;
            face.color = UiLightChromeUtility.FlatFace;
            face.raycastTarget = true;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            if (action != null)
                button.onClick.AddListener(action);

            Text text = CreateText("Text", face.transform, fontSize, TextAnchor.MiddleCenter, TextPrimary, _uiFont);
            text.text = label;
            ApplyFilledButtonLabel(text, Mathf.Max(36, fontSize + 12));
            UiLightChromeUtility.WireWhiteButtonHoverInvert(button, face, text);
            return button;
        }

        /// <summary>E1 glass capsule for light_chrome overlays only. Shell keeps CreateActionButton.</summary>
        private Button CreateGlassCapsuleButton(
            string name, string label, Transform parent, Vector2 min, Vector2 max,
            Color edgeColor, Color faceColor, UnityEngine.Events.UnityAction action,
            Font font = null, int fontSize = 16)
        {
            return UiLightChromeUtility.CreateGlassCapsuleButton(
                name, label, parent, min, max, edgeColor, faceColor, action,
                font ?? _uiFont700, fontSize, fillLabel: true);
        }

        private Button CreateActionButton(
            string name, string label, Transform parent, Vector2 min, Vector2 max,
            Color edgeColor, Color faceColor, UnityEngine.Events.UnityAction action,
            Font font = null, int fontSize = 16)
        {
            Image edge = CreateUiObject<Image>(name, parent);
            edge.color = edgeColor;
            SetRect(edge.rectTransform, min, max);

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.color = faceColor;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ApplyButtonColors(button);
            button.onClick.AddListener(action);

            Text text = CreateText("Text", face.transform, fontSize, TextAnchor.MiddleCenter, TextPrimary, font);
            if (font == _playerIdFont)
            {
                text.fontStyle = FontStyle.Normal;
                text.text = label;
                SetRect(text.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            }
            else
            {
                text.text = label;
                ApplyFilledButtonLabel(text, Mathf.Max(36, fontSize + 12));
            }
            return button;
        }

        private static void ApplyButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.92f, 0.88f, 0.78f, 1f);
            colors.pressedColor = new Color(0.84f, 0.78f, 0.66f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.75f, 0.72f, 0.66f, 0.55f);
            button.colors = colors;
        }

        private Text CreateText(string name, Transform parent, int size, TextAnchor alignment, Color color, Font font = null)
        {
            Text text = CreateUiObject<Text>(name, parent);
            text.font = font != null ? font : _uiFont;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static T CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private static Font LoadProjectFont(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

#if UNITY_EDITOR
            string assetPath = "Assets/_Project/Fonts/" + fileName;
            Font fromAsset = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            if (fromAsset != null)
                return fromAsset;
#endif

            string resourceName = Path.GetFileNameWithoutExtension(fileName);
            Font fromResources = Resources.Load<Font>("Fonts/" + resourceName);
            return fromResources;
        }

        private static Font ResolveOsFallbackFont()
        {
            string[] candidates =
            {
                "Microsoft JhengHei UI",
                "Microsoft JhengHei",
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "Noto Sans CJK TC",
                "Source Han Sans TC",
                "Arial"
            };
            Font font = Font.CreateDynamicFontFromOSFont(candidates, 42);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
