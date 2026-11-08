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
using SimulationEventType = TacticaUniversalis.Domain.EventType;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticaUniversalis.Presentation
{
    public sealed class QuickMatchDemoController : MonoBehaviour
    {
        [SerializeField] private int seed = 2021;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private string myTeamId = "navi_2021";

        private string _opponentTeamId = "gambit_2021";
        private bool _careerOfficialMatch;
        private bool _skippingMatch;
        private float _skipBusyStartedAt;
        private Button _skipButton;
        private string _careerCompetitionTitle;
        private string _careerMapDisplayName;
        private string _careerMapId;
        private int _seriesScorePlayer;
        private int _seriesScoreOpponent;
        private int _seriesMapIndex;
        private int _seriesBestOf = 3;
        private Image[] _leftSeriesPips;
        private Image[] _rightSeriesPips;
        private Image[] _resultLeftSeriesPips;
        private Image[] _resultRightSeriesPips;
        private bool _officialResultUsesOuterSeriesPips;
        private Text _officialResultSeriesMeta;
        private static readonly Color SeriesPipOff = new Color(0f, 0f, 0f, 1f);
        private static readonly Color SeriesPipT = HexColor(0xE0A526);
        private static readonly Color SeriesPipCt = HexColor(0x1B99FF);
        private string _careerBracketStageLabel;

        private ContentDatabase _content;
        private MatchResult _result;
        private MapPathfinder _pathfinder;
        private readonly Dictionary<string, RectTransform> _markers = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Image> _markerDots = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> _markerLabels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, ViewConeMeshGraphic> _markerConeMeshes =
            new Dictionary<string, ViewConeMeshGraphic>();
        private readonly Dictionary<string, Image> _markerSightRays = new Dictionary<string, Image>();
        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();
        private string _overviewResource = "Maps/dust2_overview";
        private float _collisionHammerPerPixel;
        private Texture2D _collisionMask;
        private readonly Vector2[] _coneRimScratch = new Vector2[24];
        private Vector2 _collisionMapSize;
        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Vector2> _markerLosCachedPos =
            new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _markerLosCachedFacing =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly List<MuzzleRay> _muzzleRays = new List<MuzzleRay>();
        private readonly Dictionary<string, Image> _markerHpFills = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _markerArmorFills = new Dictionary<string, Image>();
        private WeaponCatalog _weaponCatalog;
        private BattleSimMode _activeBattleSimMode = BattleSimMode.Quick;
        private readonly List<Text> _zoneLabels = new List<Text>();
        private readonly List<Image> _zoneHitPads = new List<Image>();
        private Image _zoneTooltip;
        private Text _zoneTooltipText;
        private string _hoveredZoneId;
        private readonly Dictionary<string, MapZoneDefinition> _zones = new Dictionary<string, MapZoneDefinition>();
        private readonly Dictionary<string, string> _playerZones = new Dictionary<string, string>();
        private readonly HashSet<string> _deadMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Coroutine> _moveRoutines = new Dictionary<string, Coroutine>();
        private readonly Dictionary<string, string> _moveDestinations =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>General-mode marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
        private const int ViewConeRaySamples = 10;
        private const float MarkerLosPosEpsilon = 0.5f;
        private const float MarkerLosFacingEpsilon = 0.5f;
        private const float SightRayWidth = 1.15f;
        private const float SightRayLength = 108f;
        private static readonly Color SightRayColor = new Color(1f, 0.16f, 0.12f, 0.88f);
        private const float MuzzleRayWidth = 2.05f;
        private const float MuzzleRaySeconds = 0.14f;
        private const float MuzzleBoltLength = 42f;
        private static readonly Color MuzzleTracerColor = new Color(1f, 0.50f, 0.14f, 0.96f);
        /// <summary>Quick-mode marker dot diameter (unchanged classic size).</summary>
        private const float QuickMarkerDotDiameter = 28f;
        /// <summary>Used only when a map has no baked hammer scale.</summary>
        private const float GeneralCollisionRadiusFallback = MarkerDotDiameter * 0.10f;
        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>
        private const float GeneralChaseSpeed = 88f;
        private const float MapZoomMin = 0.75f;
        private const float MapZoomMax = 4.5f;
        private const float MapZoomDefault = 1.95f;
        private const float MapZoomScrollFactor = 0.14f;
        private const float MapEdgePanMargin = 0.09f;
        private const float MapEdgePanSpeed = 520f;
        private readonly Dictionary<string, GeneralChaseState> _generalChases =
            new Dictionary<string, GeneralChaseState>(StringComparer.OrdinalIgnoreCase);

        private sealed class GeneralChaseState
        {
            public string TargetZone;
            public List<Vector2> Points;
            public float[] SegmentLengths;
            public float TotalLength;
            public float Progress;
            public string[] PathZones;
        }

        private sealed class MuzzleRay
        {
            public Image Image;
            public Vector2 From;
            public Vector2 To;
            public float ExpireAtUnscaled;
            public Color BaseColor;
        }
        private readonly Dictionary<string, PlayerStat> _stats = new Dictionary<string, PlayerStat>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Text> _leftStatSlots = new List<Text>();
        private readonly List<Text> _rightStatSlots = new List<Text>();
        private readonly List<Text> _leftRoundKillSlots = new List<Text>();
        private readonly List<Text> _rightRoundKillSlots = new List<Text>();
        private readonly List<StatMetricSlot[]> _leftStatusRows = new List<StatMetricSlot[]>();
        private readonly List<StatMetricSlot[]> _rightStatusRows = new List<StatMetricSlot[]>();
        private readonly List<Image> _leftJerseyBadges = new List<Image>();
        private readonly List<Image> _rightJerseyBadges = new List<Image>();
        private readonly List<Text> _leftJerseyNumbers = new List<Text>();
        private readonly List<Text> _rightJerseyNumbers = new List<Text>();
        private readonly List<Image> _leftStatRows = new List<Image>();
        private readonly List<Image> _rightStatRows = new List<Image>();
        private readonly List<string> _leftStatPlayerIds = new List<string>();
        private readonly List<string> _rightStatPlayerIds = new List<string>();
        private readonly List<BattlePlayerCardView> _leftBattleCards = new List<BattlePlayerCardView>();
        private readonly List<BattlePlayerCardView> _rightBattleCards = new List<BattlePlayerCardView>();
        private RectTransform _leftClassicRoot;
        private RectTransform _rightClassicRoot;
        private RectTransform _leftGeneralRoot;
        private RectTransform _rightGeneralRoot;
        private Image _leftTeamHeaderBlock;
        private Image _rightTeamHeaderBlock;
        private readonly Dictionary<string, int> _playerNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Text> _statLines = new Dictionary<string, Text>(StringComparer.OrdinalIgnoreCase);
        private readonly List<KillFeedLine> _killFeed = new List<KillFeedLine>();
        private readonly List<KillFeedRow> _killFeedRows = new List<KillFeedRow>();
        private readonly List<string> _traitFeed = new List<string>();
        private RectTransform _mapFrame;
        private Image _mapFrameImage;
        private Image _mapFrameFace;
        private RectTransform _mapRotator;
        private RectTransform _mapRect;
        private float _mapZoom = 1f;
        private Vector2 _mapPan = Vector2.zero;
        private Image _mapImage;
        private Image _bgImage;
        private int _matchBackgroundIndex = -1;
        private Sprite _mapSprite;
        private string _currentMapFloor;
        private string _bombZoneId;
        private string _bombCarrierId;
        private string _droppedBombZoneId;
        private readonly List<UtilityMapMarker> _utilityMarkers = new List<UtilityMapMarker>();
        private Transform _utilityMarkerRoot;
        private readonly Dictionary<string, float> _blindedUntilByPlayer =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Image> _markerBlindGlows =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Round clock time when bomb was planted; negative = not planted.</summary>
        private float _bombPlantedAtRoundTime = -1f;
        private const float BombCountdownSeconds = 40f;
        private Image _bombMarkerGlow;
        private Image _bombMarkerCore;
        private RectTransform _bombMarker;
        private RectTransform _aliveCountRoot;
        private Image _leftAliveBadge;
        private Image _rightAliveBadge;
        private Text _leftAliveText;
        private Text _rightAliveText;
        private Text _aliveVsText;
        private bool _roundHasDeath;
        private Text _leftScoreText;
        private Text _rightScoreText;
        private Image _leftTeamLogo;
        private Image _rightTeamLogo;
        private Text _roundText;
        private Text _clockText;
        private Text _traitFeedText;
        private Text _speedText;
        private Image _pauseIcon;
        private Text _matchText;
        private int _matchNumber = 1;
        private Text _sideLabel;
        private Text _naviSideButton;
        private GameObject _naviSideButtonRoot;
        private GameObject _gambitSideButtonRoot;
        private GameObject _newSeedButtonRoot;
        private Text _controlBarTitle;
        private Text _careerMatchMetaText;
        private RectTransform _controlBarRect;
        private Image _controlBarImage;
        private Image _settingsButtonEdge;
        private Image _settingsButtonFace;
        private RectTransform _settingsButtonRect;
        private RectTransform _timeoutButtonRect;
        private Image _timeoutButtonEdge;
        private Image _timeoutButtonFace;
        private RectTransform _pauseButtonRect;
        private Image _pauseButtonEdge;
        private Image _pauseButtonFace;
        private RectTransform _speedButtonRect;
        private Image _speedButtonEdge;
        private Image _speedButtonFace;
        private RectTransform _skipButtonRect;
        private Image _skipButtonEdge;
        private Image _skipButtonFace;
        private RectTransform _floorButtonRect;
        private Text _floorButtonText;
        private GameObject _officialResultRoot;
        private Image _officialResultLeftLogo;
        private Image _officialResultRightLogo;
        private Text _officialResultLeftName;
        private Text _officialResultRightName;
        private Text _officialResultLeftScore;
        private Text _officialResultRightScore;
        private Text _officialResultOutcome;
        private bool _awaitingOfficialResultConfirm;
        private SettingsPanelPresentation _settingsPanel;
        private bool _pausedBeforeSettings;
        private Text _gambitSideButton;
        private Text _leftTeamHeader;
        private Text _rightTeamHeader;
        private Text _leftMoneyText;
        private Text _rightMoneyText;
        private Text _leftCpText;
        private Text _rightCpText;
        private Text _leftCardText;
        private Text _rightCardText;
        private Text _roundWinBanner;
        private CanvasGroup _roundWinGroup;
        private Image _leftStatsPanel;
        private Image _rightStatsPanel;
        private RectTransform _canvasRect;
        private Image _profilePopup;
        private Text _profilePopupBody;
        private Coroutine _hoverProfileRoutine;
        private string _hoverProfilePlayerId;
        private Image _cardPickerRoot;
        private Image _cardPickerBackdrop;
        private Image _cardPickerPanel;
        private Text _cardPickerTitle;
        private Text _cardPickerSubtitle;
        private readonly List<Button> _cardButtons = new List<Button>();
        private readonly List<Image> _cardButtonEdges = new List<Image>();
        private readonly List<Image> _cardButtonFaces = new List<Image>();
        private readonly List<Text> _cardLevelLabels = new List<Text>();
        private readonly List<Text> _cardTitleLabels = new List<Text>();
        private readonly List<Text> _cardCostLabels = new List<Text>();
        private readonly List<Text> _cardDetailLabels = new List<Text>();
        private readonly List<CanvasGroup> _cardGroups = new List<CanvasGroup>();
        private Image _tacticalPickerRoot;
        private Image _tacticalPickerPanel;
        private Text _tacticalPickerSubtitle;
        private Text _tacticalAnalysis;
        private readonly List<Button> _tacticalButtons = new List<Button>();
        private readonly List<Image> _tacticalCardEdges = new List<Image>();
        private readonly List<Image> _tacticalCardFaces = new List<Image>();
        private readonly List<Image> _tacticalCardStrips = new List<Image>();
        private readonly List<Text> _tacticalScopeLabels = new List<Text>();
        private readonly List<Text> _tacticalTitleLabels = new List<Text>();
        private readonly List<Text> _tacticalCostLabels = new List<Text>();
        private readonly List<Text> _tacticalStatLabels = new List<Text>();
        private readonly List<Text> _tacticalDetailLabels = new List<Text>();
        private readonly List<CanvasGroup> _tacticalCardGroups = new List<CanvasGroup>();
        private readonly List<string> _visibleTacticalCardIds = new List<string>();
        private bool _awaitingTacticalCards;
        private bool _awaitingTimeout;
        private bool _awaitingFocus;
        private bool _inCombatPlayback;
        private Text _timeoutCallLabel;
        private Image _timeoutPickerRoot;
        private Text _timeoutSubtitle;
        private Text _timeoutBody;
        private readonly List<Button> _coachButtons = new List<Button>();
        private readonly List<Image> _coachEdges = new List<Image>();
        private readonly List<Text> _coachTitles = new List<Text>();
        private readonly List<Text> _coachDetails = new List<Text>();
        private readonly List<string> _visibleCoachIds = new List<string>();
        private readonly HashSet<string> _selectedCoachIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _coachPickTargets =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Image _focusPickerRoot;
        private Text _focusSubtitle;
        private readonly List<Button> _focusButtons = new List<Button>();
        private readonly List<Image> _focusEdges = new List<Image>();
        private readonly List<Text> _focusTitles = new List<Text>();
        private readonly List<Text> _focusCostLabels = new List<Text>();
        private readonly List<Text> _focusStatLabels = new List<Text>();
        private readonly List<Text> _focusDetails = new List<Text>();
        private readonly List<string> _visibleFocusIds = new List<string>();
        private readonly List<Button> _focusTargetButtons = new List<Button>();
        private readonly List<Text> _focusTargetLabels = new List<Text>();
        private Text _focusTargetHint;
        private string _focusPickTargetId;
        private static readonly Color TraitNameUi = new Color(0.867f, 0.545f, 1f, 1f);
        private static readonly Color TraitEffectUi = new Color(0.451f, 0.741f, 0.518f, 1f);
        private static readonly Color UniversalCardEdge = new Color(0.42f, 0.46f, 0.54f, 1f);
        private static readonly Color UniversalCardStrip = new Color(0.34f, 0.42f, 0.54f, 1f);
        private static readonly Color ExclusiveCardEdge = new Color(0.72f, 0.48f, 0.28f, 1f);
        private static readonly Color ExclusiveCardStrip = new Color(0.78f, 0.42f, 0.22f, 1f);
        private static readonly Color TacticalPanelNormal = new Color(0.08f, 0.09f, 0.11f, 0.98f);
        private static readonly Color TacticalPanelOverflow = new Color(0.32f, 0.07f, 0.09f, 0.98f);
        private static readonly Color TacticalRootNormal = new Color(0.02f, 0.025f, 0.035f, 0.78f);
        private static readonly Color TacticalRootOverflow = new Color(0.28f, 0.03f, 0.05f, 0.88f);
        private static readonly Color OverflowWarn = new Color(1f, 0.45f, 0.42f, 1f);
        private Coroutine _playback;
        private Coroutine _orientRoutine;
        private Coroutine _roundWinRoutine;
        private InteractiveQuickMatchSession _session;
        private bool _paused;
        private bool _awaitingCard;
        private bool _awaitingHeroPick;
        private EquipmentLevel? _pendingEconomyCard;
        private readonly List<string> _heroPickIds = new List<string>();
        private readonly List<Button> _heroPickButtons = new List<Button>();
        private readonly List<Text> _heroPickLabels = new List<Text>();
        private Button _heroConfirmButton;
        private Text _heroPickHint;
        private GameObject _heroPickRow;
        private EquipmentLevel? _selectedCard;
        private readonly Dictionary<string, int> _teamMoney = new Dictionary<string, int>();
        private readonly Dictionary<string, EquipmentLevel> _teamCards = new Dictionary<string, EquipmentLevel>();
        private int _naviScore;
        private int _gambitScore;
        private int _currentRound = 1;
        private bool _mapFlipped;
        private Sprite _circleSprite;
        private Sprite _ringSprite;
        private Sprite _lineSprite;
        private Sprite[] _statIcons;
        private Sprite _killFeedIconSprite;
        private Sprite _killFeedSkullSprite;
        private Sprite _naviLogoSprite;
        private Sprite _gambitLogoSprite;
        private Sprite _playbackPauseSprite;
        private Sprite _playbackPlaySprite;
        private Sprite _playbackSkipSprite;
        private Font _uiFont;
        private Font _uiFont700;
        private Font _uiFontBlack;
        private Font _playerIdFont;
        private Font _scoreboardFont;
        private Text _profilePlayerName;

        private sealed class StatMetricSlot
        {
            public Image Icon;
            public Text Value;
        }

        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public string WeaponId;
            public bool Headshot;
            public float ExpireAtUnscaled;
        }

        private const float KillFeedVisibleSeconds = 5f;
        private static readonly Color KillFeedGray = new Color(0.62f, 0.64f, 0.67f, 1f);
        private const string KillFeedGrayHex = "9EA3AB";
        private static readonly Color KillFeedRowBg = new Color(0.06f, 0.07f, 0.08f, 0.72f);

        private sealed class KillFeedRow
        {
            public GameObject Root;
            public Text Left;
            public Image Icon;
            public Image HeadshotIcon;
            public Text Right;
        }

        private sealed class PlayerStat
        {
            public int Kills;
            public int Deaths;
            public int Assists;
            public int RoundKills;
            public int Health = 100;
            public bool Alive = true;
            public float Stamina = 100f;
            public float Mentality = 50f;
            public float Form = 50f;
        }

        // L→R: 擊殺、死亡、助攻 | 體力、心態、手感
        private static readonly string[] StatIconResourcePaths =
        {
            "UI/Icons/kill",
            "UI/Icons/death",
            "UI/Icons/assist",
            "UI/Icons/stamina",
            "UI/Icons/mentality",
            "UI/Icons/form",
        };

        private static readonly Color Bg = new Color(0.04f, 0.045f, 0.05f, 1f);
        private static readonly Color Panel = new Color(0.09f, 0.10f, 0.115f, 0.98f);
        private static readonly Color PanelInner = new Color(0.07f, 0.075f, 0.085f, 1f);
        private static readonly Color Accent = new Color(0.92f, 0.62f, 0.22f, 1f);
        private static readonly Color TextPrimary = new Color(0.93f, 0.94f, 0.95f, 1f);
        private static readonly Color TextMuted = new Color(0.68f, 0.70f, 0.73f, 1f);
        private static readonly Color ButtonFace = new Color(0.16f, 0.17f, 0.20f, 1f);
        private static readonly Color ButtonEdge = new Color(0.32f, 0.34f, 0.38f, 1f);
        private static readonly Color Danger = new Color(0.92f, 0.42f, 0.38f, 1f);
        private static readonly Color NaviColor = new Color(0.95f, 0.78f, 0.16f, 1f);
        private static readonly Color GambitColor = new Color(0.86f, 0.22f, 0.24f, 1f);
        private static readonly Color CtPanel = new Color(0.42f, 0.62f, 0.82f, 0.90f);
        private static readonly Color TPanel = new Color(0.86f, 0.48f, 0.18f, 0.90f);
        private static readonly Color CtText = new Color(0.45f, 0.72f, 0.95f, 1f);
        private static readonly Color TText = new Color(0.95f, 0.58f, 0.22f, 1f);
        private static readonly Color CtAliveBg = new Color(0.28f, 0.52f, 0.82f, 0.95f);
        private static readonly Color TAliveBg = new Color(0.90f, 0.48f, 0.16f, 0.95f);
        private static readonly Color DeadRowText = new Color(0.48f, 0.50f, 0.53f, 1f);
        private static readonly Color DeadRowMuted = new Color(0.40f, 0.42f, 0.45f, 1f);
        private static readonly Color DeadRowBadge = new Color(0.42f, 0.44f, 0.48f, 1f);
        private static readonly Color DeadRowBg = new Color(0.05f, 0.055f, 0.06f, 0.88f);
        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);
        private static readonly Color OfficialWinUi = new Color(0.45f, 0.78f, 0.52f, 1f);
        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);
        private static readonly Color BombClockRed = new Color(0.95f, 0.18f, 0.18f, 1f);
        private const string CtHex = "#73B8F2";
        private const string THex = "#F29438";
        private const string TraitNameColor = "#DD8BFF";
        private const string TraitEffectColor = "#73BD84";
        private const float ProfileHoverDelaySeconds = 1f;

        private void Awake()
        {
            GameSettingsRuntime.LoadAndApply();
            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("GenYoMin2.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont;
            _scoreboardFont = LoadProjectFont("Anton.ttf") ?? _uiFont;
            _content = RuntimeContentLoader.Load();
            DemoZh.ConfigurePlayerNames(_content.Players.Values);
            _circleSprite = CreateCircleSprite(true);
            _ringSprite = CreateCircleSprite(false);
            _lineSprite = CreateLineSprite();
            _statIcons = LoadStatIcons();
            BattleStatPanelPresentation.SetKitAndBombIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/defuse_kit"),
                Resources.Load<Sprite>("UI/Icons/Weapons/bomb"));
            BattleStatPanelPresentation.SetNadeIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/flashbang"),
                Resources.Load<Sprite>("UI/Icons/Weapons/smoke_grenade"),
                Resources.Load<Sprite>("UI/Icons/Weapons/he_grenade"),
                Resources.Load<Sprite>("UI/Icons/Weapons/molotov"),
                Resources.Load<Sprite>("UI/Icons/Weapons/incendiary"));
            _killFeedIconSprite = Resources.Load<Sprite>("UI/Icons/kill_feed");
            _killFeedSkullSprite = Resources.Load<Sprite>("UI/Icons/kill_skull") ?? _killFeedIconSprite;
            _naviLogoSprite = Resources.Load<Sprite>("UI/Logos/navi");
            _gambitLogoSprite = Resources.Load<Sprite>("UI/Logos/gambit");
            _playbackPauseSprite = Resources.Load<Sprite>("UI/Icons/playback_pause");
            _playbackPlaySprite = Resources.Load<Sprite>("UI/Icons/playback_play");
            _playbackSkipSprite = Resources.Load<Sprite>("UI/Icons/playback_skip");
            if (CareerMatchLaunchContext.TryTakeLaunch(out CareerMatchLaunchContext.LaunchRequest launch))
            {
                _careerOfficialMatch = true;
                myTeamId = launch.playerSimTeamId;
                _opponentTeamId = launch.opponentSimTeamId;
                seed = launch.seed;
                _careerCompetitionTitle = launch.competitionDisplayName;
                _careerMapDisplayName = string.IsNullOrEmpty(launch.mapDisplayName) ? "Dust II" : launch.mapDisplayName;
                _careerMapId = string.IsNullOrEmpty(launch.mapId) ? "dust2" : launch.mapId;
                _careerBracketStageLabel = launch.bracketStageLabel;
                _seriesBestOf = launch.bestOf > 0 ? launch.bestOf : 3;
                _seriesMapIndex = Mathf.Max(0, launch.seriesMapIndex);
                _seriesScorePlayer = Mathf.Max(0, launch.seriesScorePlayer);
                _seriesScoreOpponent = Mathf.Max(0, launch.seriesScoreOpponent);
            }
            else
            {
                _careerMapId = "dust2";
                _careerMapDisplayName = "Dust II";
            }

            BindActiveMap();
            BuildPlayerNumbers();
            BuildInterface();
            Canvas.ForceUpdateCanvases();
            RelayoutZoneLabels();
            ApplyMapOrientation(1, false);
            GenerateMatch();
            ApplyMatchModeUi();
            RefreshSeriesPips();
            StartCoroutine(DeferredInitialMapLayout());
        }

        private IEnumerator DeferredInitialMapLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (_session == null)
                yield break;
            SyncMatchMarkerVisibility();
            ResetMarkers(_currentRound > 0 ? _currentRound : 1, false);
            RelayoutZoneLabels();
        }

        private void OnApplicationFocus(bool hasFocus) =>
            GameSettingsRuntime.HandleFocus(hasFocus);

        private void ApplyMatchModeUi()
        {
            bool simulation = !_careerOfficialMatch;
            if (_naviSideButtonRoot != null)
                _naviSideButtonRoot.SetActive(simulation);
            if (_gambitSideButtonRoot != null)
                _gambitSideButtonRoot.SetActive(simulation);
            if (_newSeedButtonRoot != null)
                _newSeedButtonRoot.SetActive(simulation);
            if (_controlBarTitle != null)
                _controlBarTitle.gameObject.SetActive(simulation);
            if (_sideLabel != null)
                _sideLabel.gameObject.SetActive(simulation);
            if (_matchText != null)
                _matchText.gameObject.SetActive(simulation);
            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }
            if (_clockText != null)
                _clockText.transform.SetAsLastSibling();

            ApplyControlBarLayout(simulation);
            ApplyScoreboardModeChrome();
            ApplySettingsFrostedChrome(true);
            ApplyMapCameraForMode();
            bool floorSwitch = MapSupportsFloors();
            if (_skipButtonRect != null)
                _skipButtonRect.gameObject.SetActive(!floorSwitch && !IsGeneralBattleMode());
            if (_floorButtonRect != null)
                _floorButtonRect.gameObject.SetActive(floorSwitch);
            RefreshFloorButton();
            RefreshFloorVisibility();
            RefreshMarkerHpBarsVisible();
        }

        private void ApplyControlBarLayout(bool practiceLayout)
        {
            bool general = IsGeneralBattleMode();

            if (general)
            {
                // Merge settings into the bottom bar and keep the whole pack screen-centered.
                if (_settingsButtonRect != null && _controlBarRect != null)
                    _settingsButtonRect.SetParent(_controlBarRect, false);
                if (_controlBarRect != null)
                    SetRect(_controlBarRect, new Vector2(0.30f, 0.018f), new Vector2(0.70f, 0.095f));

                // L→R inside bar: 叫暫停 | 暫停 | 倍速 | 跳過/樓層 | 設定
                if (_matchText != null)
                    _matchText.gameObject.SetActive(false);
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.02f, 0.14f), new Vector2(0.28f, 0.86f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.30f, 0.14f), new Vector2(0.44f, 0.86f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.46f, 0.14f), new Vector2(0.60f, 0.86f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.62f, 0.14f), new Vector2(0.76f, 0.86f));
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.62f, 0.14f), new Vector2(0.76f, 0.86f));
                if (_settingsButtonRect != null)
                    SetRect(_settingsButtonRect, new Vector2(0.78f, 0.14f), new Vector2(0.98f, 0.86f));
                return;
            }

            // Quick / official: settings stays outside bottom-right on the canvas.
            if (_settingsButtonRect != null && _canvasRect != null)
                _settingsButtonRect.SetParent(_canvasRect, false);
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));

            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    // Official: four controls, screen-centered, tall enough to click.
                    SetRect(_controlBarRect, new Vector2(0.36f, 0.018f), new Vector2(0.64f, 0.095f));
            }

            if (practiceLayout)
            {
                if (_matchText != null)
                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;
            }

            // L→R centered pack: 叫暫停 | 暫停 | 倍速 | 跳過
            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.03f, 0.14f), new Vector2(0.35f, 0.86f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.37f, 0.14f), new Vector2(0.55f, 0.86f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.57f, 0.14f), new Vector2(0.75f, 0.86f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
            if (_floorButtonRect != null)
                SetRect(_floorButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }


        private void ApplyScoreboardModeChrome()
        {
            bool general = IsGeneralBattleMode();
            if (_leftClassicRoot != null)
                _leftClassicRoot.gameObject.SetActive(!general);
            if (_rightClassicRoot != null)
                _rightClassicRoot.gameObject.SetActive(!general);
            if (_leftGeneralRoot != null)
                _leftGeneralRoot.gameObject.SetActive(general);
            if (_rightGeneralRoot != null)
                _rightGeneralRoot.gameObject.SetActive(general);

            if (_leftCpText != null)
                _leftCpText.gameObject.SetActive(general);
            if (_rightCpText != null)
                _rightCpText.gameObject.SetActive(general);
            if (_leftCardText != null)
                _leftCardText.gameObject.SetActive(false);
            if (_rightCardText != null)
                _rightCardText.gameObject.SetActive(false);

            if (general)
            {
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _leftTeamHeader, _leftMoneyText, _leftCpText, leftSide: true);
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _rightTeamHeader, _rightMoneyText, _rightCpText, leftSide: false);

                BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                    true, out var leftHeaderMin, out var leftHeaderMax, out var leftPanelMin, out var leftPanelMax);
                BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                    false, out var rightHeaderMin, out var rightHeaderMax, out var rightPanelMin, out var rightPanelMax);

                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, leftHeaderMin, leftHeaderMax);
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, rightHeaderMin, rightHeaderMax);
                if (_leftStatsPanel != null)
                {
                    SetRect(_leftStatsPanel.rectTransform, leftPanelMin, leftPanelMax);
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, rightPanelMin, rightPanelMax);
                    _rightStatsPanel.color = Color.clear;
                }
            }
            else
            {
                // Quick mode: original monolithic side panels (figure 1).
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.720f), new Vector2(0.165f, 0.775f));
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.835f, 0.720f), new Vector2(0.992f, 0.775f));
                if (_leftStatsPanel != null)
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.165f, 0.775f));
                if (_rightStatsPanel != null)
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.835f, 0.112f), new Vector2(0.992f, 0.775f));
            }

            UpdateSidePanelColors();
            UpdateTeamHeaders();
        }

        private void RefreshOfficialMatchMeta()
        {
            if (_careerMatchMetaText == null)
                return;
            string mapName = string.IsNullOrEmpty(_careerMapDisplayName) ? "Dust II" : _careerMapDisplayName;
            string competition = _careerCompetitionTitle ?? string.Empty;
            string stage = _careerBracketStageLabel ?? string.Empty;
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(mapName))
                parts.Add(mapName);
            if (!string.IsNullOrEmpty(competition))
                parts.Add(competition);
            if (!string.IsNullOrEmpty(stage))
                parts.Add(stage);
            _careerMatchMetaText.text = parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }

        private void ApplySettingsFrostedChrome(bool enabled)
        {
            if (!enabled)
                return;

            Color bar = new Color(Panel.r, Panel.g, Panel.b, 0.38f);
            Color edge = new Color(Panel.r, Panel.g, Panel.b, 0.55f);
            Color face = new Color(Panel.r, Panel.g, Panel.b, 0.28f);
            Color highlight = new Color(0.55f, 0.58f, 0.64f, 0.42f);
            Color pressed = new Color(0.22f, 0.24f, 0.28f, 0.55f);

            if (_controlBarImage != null)
                _controlBarImage.color = bar;
            if (_mapFrameImage != null)
                _mapFrameImage.color = edge;
            if (_mapFrameFace != null)
                _mapFrameFace.color = face;

            ApplyFrostedButtonChrome(_settingsButtonEdge, _settingsButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_timeoutButtonEdge, _timeoutButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_pauseButtonEdge, _pauseButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_speedButtonEdge, _speedButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_skipButtonEdge, _skipButtonFace, edge, face, highlight, pressed);
        }

        private static void ApplyFrostedButtonChrome(
            Image edgeImage, Image faceImage, Color edge, Color face, Color highlight, Color pressed)
        {
            if (edgeImage != null)
                edgeImage.color = edge;
            if (faceImage != null)
                faceImage.color = face;

            Button button = edgeImage != null
                ? edgeImage.GetComponent<Button>()
                : faceImage != null ? faceImage.GetComponentInParent<Button>() : null;
            if (button == null)
                return;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = highlight;
            colors.pressedColor = pressed;
            colors.selectedColor = highlight;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
        }

        private static void CaptureButtonChrome(Button button, out Image edge, out Image face)
        {
            edge = button != null ? button.GetComponent<Image>() : null;
            face = null;
            if (button == null)
                return;
            Transform faceTransform = button.transform.Find("Face");
            if (faceTransform != null)
                face = faceTransform.GetComponent<Image>();
        }

        private void OnDestroy()
        {
            if (_circleSprite != null)
                Destroy(_circleSprite.texture);
            if (_ringSprite != null)
                Destroy(_ringSprite.texture);
            if (_lineSprite != null)
                Destroy(_lineSprite.texture);
        }

        private void GenerateMatch()
        {
            StopAllMovement();
            HideBomb();
            CancelProfileHover();
            HideProfilePopup();
            if (_playback != null)
                StopCoroutine(_playback);
            string activeMapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            string defaultZone = "mid";
            if (string.Equals(activeMapId, "train", StringComparison.OrdinalIgnoreCase))
                defaultZone = "connector";
            else if (string.Equals(activeMapId, "overpass", StringComparison.OrdinalIgnoreCase))
                defaultZone = "connector";
            else if (string.Equals(activeMapId, "vertigo", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";
            else if (string.Equals(activeMapId, "inferno", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";
            else if (string.Equals(activeMapId, "mirage", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";
            else if (string.Equals(activeMapId, "nuke", StringComparison.OrdinalIgnoreCase))
                defaultZone = "outside";
            _activeBattleSimMode = GameSettingsRuntime.EffectiveBattleSimMode();
            if (_weaponCatalog == null)
                _weaponCatalog = RuntimeContentLoader.LoadWeaponCatalog();
            IRoundSimulator roundSim = BattleSimFactory.CreateRoundSimulator(
                _activeBattleSimMode, _weaponCatalog);
            _session = new InteractiveQuickMatchSession(
                _content, seed, new RuleBasedTacticalPlanner(defaultZone), roundSim,
                myTeamId, _opponentTeamId, activeMapId,
                _activeBattleSimMode, _weaponCatalog);
            if (_mapCollision != null && _mapCollision.IsReady)
                _session.CombatLos = _mapCollision;
            if (IsGeneralBattleMode())
            {
                playbackSpeed = 1f;
                if (_speedText != null)
                    _speedText.text = "1.0×";
            }
            ApplyMatchBackground();
            _result = _session.Result;
            _selectedCard = null;
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _inCombatPlayback = false;
            _paused = false;
            RefreshPauseIcon();
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();
            UpdateScore();
            UpdateAliveCountHud();
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateSideButtons();
            UpdateTeamHeaders();
            UpdateEconomyHud();
            UpdateSidePanelColors();
            ApplyMapOrientation(1, false);
            SyncMatchMarkerVisibility();
            ResetMarkers(1, false);
            if (_matchText != null)
                _matchText.text = $"第 {_matchNumber} 場";
            SetRoundHud(_currentRound > 0 ? _currentRound : 1, "選擇經濟卡");
            HideRoundWinBanner();
            RefreshTimeoutCallButton();
            ApplyMatchModeUi();
            _playback = StartCoroutine(MatchLoop());
        }

        private IEnumerator MatchLoop()
        {
            while (_session != null && !_session.IsComplete)
            {
                int beforePrepare = _session.Result.events.Count;
                _session.PrepareRound();
                yield return PlayEventRange(beforePrepare, _session.Result.events.Count, true);
                SyncEconomyFromState();
                UpdateEconomyHud();
                RefreshTimeoutCallButton();

                if (_session.TryBeginPendingTimeoutWindow())
                {
                    _awaitingTimeout = true;
                    ShowTimeoutPicker();
                    RefreshStatPanels();
                    while (_awaitingTimeout)
                        yield return null;
                    HideTimeoutPicker();
                    _session.EndTimeoutWindow();
                    RefreshStatPanels();
                    RefreshTimeoutCallButton();
                }

                EquipmentLevel card = EquipmentLevel.Eco;
                string[] heroIds = Array.Empty<string>();
                bool pistolRound = MatchRules.IsPistolRound(_session.State.CompletedRounds);
                if (pistolRound)
                {
                    SetPhaseStatus("手槍局：裝備由選手自行決定");
                    HideCardPicker();
                }
                else
                {
                    _selectedCard = null;
                    _pendingEconomyCard = null;
                    _awaitingHeroPick = false;
                    _heroPickIds.Clear();
                    _awaitingCard = true;
                    ShowCardPicker();
                    while (_awaitingCard && _selectedCard == null)
                        yield return null;

                    card = _selectedCard ?? EquipmentLevel.Eco;
                    heroIds = _heroPickIds.Count > 0 ? _heroPickIds.ToArray() : Array.Empty<string>();
                    _selectedCard = null;
                    _pendingEconomyCard = null;
                    _awaitingHeroPick = false;
                    _awaitingCard = false;
                    HideCardPicker();
                    HideHeroPickRow();
                }

                _awaitingTacticalCards = true;
                ShowTacticalCardPicker();
                while (_awaitingTacticalCards)
                    yield return null;
                string[] tacticalCards = _session.TeamById(myTeamId).TacticalDeck.Selected.ToArray();
                HideTacticalCardPicker();

                _focusPickTargetId = string.Empty;
                _session.SetFocusSelection(myTeamId, string.Empty, string.Empty);
                _awaitingFocus = true;
                ShowFocusPicker();
                while (_awaitingFocus)
                    yield return null;
                TeamRuntime focusTeam = _session.TeamById(myTeamId);
                string focusId = focusTeam.SelectedFocusCardId;
                string focusTarget = focusTeam.SelectedFocusTargetId;
                HideFocusPicker();

                int beforeRound = _session.Result.events.Count;
                _session.PlayRound(myTeamId, card, tacticalCards, focusId, focusTarget, heroIds);
                _result = _session.Result;
                _inCombatPlayback = true;
                RefreshTimeoutCallButton();
                yield return PlayEventRange(beforeRound, _session.Result.events.Count, true);
                _inCombatPlayback = false;
                RefreshTimeoutCallButton();
                while (_roundWinRoutine != null)
                    yield return null;
            }

            if (_session != null && !_session.IsFinished)
                _session.FinishMatch();
            _result = _session?.Result;
            RefreshStatPanels();
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            _playback = null;

            if (_careerOfficialMatch && _result != null)
                yield return ShowOfficialResultFlow();
        }

        private IEnumerator ShowOfficialResultFlow()
        {
            EnsureOfficialResultOverlay();
            RefreshOfficialResultPresentation();
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(true);
            _awaitingOfficialResultConfirm = true;
            while (_awaitingOfficialResultConfirm)
                yield return null;
        }

        private void RefreshOfficialResultPresentation()
        {
            if (_result == null || _session == null)
                return;
            string teamAId = _session.State.TeamA.Definition.id;
            bool myTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);
            int myScore = myTeamA ? _result.teamAScore : _result.teamBScore;
            int oppScore = myTeamA ? _result.teamBScore : _result.teamAScore;
            bool won = string.Equals(_result.winnerTeamId, myTeamId, StringComparison.OrdinalIgnoreCase);

            BindTeamLogo(_officialResultLeftLogo, myTeamId);
            BindTeamLogo(_officialResultRightLogo, _opponentTeamId);
            if (_officialResultLeftName != null)
                _officialResultLeftName.text = ResolveBattleTeamDisplayName(myTeamId);
            if (_officialResultRightName != null)
                _officialResultRightName.text = ResolveBattleTeamDisplayName(_opponentTeamId);
            if (_officialResultLeftScore != null)
                _officialResultLeftScore.text = myScore.ToString("00");
            if (_officialResultRightScore != null)
                _officialResultRightScore.text = oppScore.ToString("00");
            if (_officialResultOutcome != null)
            {
                _officialResultOutcome.text = won ? "勝利" : "戰敗";
                _officialResultOutcome.color = won ? OfficialWinUi : OfficialLossUi;
            }

            if (_careerOfficialMatch)
            {
                int nextPlayer = _seriesScorePlayer + (won ? 1 : 0);
                int nextOpp = _seriesScoreOpponent + (won ? 0 : 1);
                ApplySeriesPipColors(_resultLeftSeriesPips, nextPlayer, MyTeamIsTerrorist(Mathf.Max(1, _currentRound)));
                ApplySeriesPipColors(_resultRightSeriesPips, nextOpp, !MyTeamIsTerrorist(Mathf.Max(1, _currentRound)));
                if (_officialResultSeriesMeta != null)
                {
                    _officialResultSeriesMeta.gameObject.SetActive(true);
                    _officialResultSeriesMeta.text =
                        "地圖 " + (_seriesMapIndex + 1) + " / " + _seriesBestOf +
                        " · 系列 " + nextPlayer + "-" + nextOpp;
                }
            }
            else if (_officialResultSeriesMeta != null)
                _officialResultSeriesMeta.gameObject.SetActive(false);
        }


        private string ResolveBattleTeamDisplayName(string teamId)
        {
            if (_content != null &&
                _content.Teams.TryGetValue(teamId, out TeamDefinition team) &&
                !string.IsNullOrWhiteSpace(team.displayName))
                return DemoZh.TeamName(team.displayName);
            return DemoZh.TeamName(teamId);
        }

        private void EnsureOfficialResultOverlay()
        {
            if (_canvasRect == null)
                return;
            if (_officialResultRoot != null &&
                (_officialResultLeftLogo == null || !_officialResultUsesOuterSeriesPips))
            {
                Destroy(_officialResultRoot);
                _officialResultRoot = null;
                _officialResultLeftLogo = null;
                _officialResultRightLogo = null;
                _resultLeftSeriesPips = null;
                _resultRightSeriesPips = null;
            }
            if (_officialResultRoot != null)
                return;

            Image dim = CreateUiObject<Image>("OfficialResultDim", _canvasRect);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;
            _officialResultRoot = dim.gameObject;

            Image panel = CreateUiObject<Image>("OfficialResultPanel", dim.transform);
            SetRect(panel.rectTransform, new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f));
            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);

            Text title = CreateText("Title", panel.transform, 22, TextAnchor.MiddleCenter, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f));
            title.text = "比賽結束";
            title.fontStyle = FontStyle.Bold;

            // Same order as TopHud: series | logo | score … score | logo | series
            _resultLeftSeriesPips = CreateSeriesPips("ResultLeftPips", panel.transform, 0.08f, true);
            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);
            _resultRightSeriesPips = CreateSeriesPips("ResultRightPips", panel.transform, 0.92f, false);
            _officialResultUsesOuterSeriesPips = true;

            _officialResultLeftName = CreateText("LeftName", panel.transform, 22, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.06f, 0.36f), new Vector2(0.30f, 0.50f));
            _officialResultLeftName.fontStyle = FontStyle.Bold;
            _officialResultLeftName.resizeTextForBestFit = true;
            _officialResultLeftName.resizeTextMinSize = 16;
            _officialResultLeftName.resizeTextMaxSize = 24;
            _officialResultLeftName.horizontalOverflow = HorizontalWrapMode.Overflow;

            _officialResultRightName = CreateText("RightName", panel.transform, 22, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_officialResultRightName.rectTransform, new Vector2(0.70f, 0.36f), new Vector2(0.94f, 0.50f));
            _officialResultRightName.fontStyle = FontStyle.Bold;
            _officialResultRightName.resizeTextForBestFit = true;
            _officialResultRightName.resizeTextMinSize = 16;
            _officialResultRightName.resizeTextMaxSize = 24;
            _officialResultRightName.horizontalOverflow = HorizontalWrapMode.Overflow;

            _officialResultLeftScore = CreateText("LeftScore", panel.transform, 48, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.30f, 0.52f), new Vector2(0.44f, 0.82f));
            _officialResultLeftScore.fontStyle = FontStyle.Bold;

            Text colon = CreateText("Colon", panel.transform, 36, TextAnchor.MiddleCenter, TextMuted);
            SetRect(colon.rectTransform, new Vector2(0.44f, 0.52f), new Vector2(0.56f, 0.82f));
            colon.text = ":";
            colon.fontStyle = FontStyle.Bold;

            _officialResultRightScore = CreateText("RightScore", panel.transform, 48, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.56f, 0.52f), new Vector2(0.70f, 0.82f));
            _officialResultRightScore.fontStyle = FontStyle.Bold;

            _officialResultOutcome = CreateText("Outcome", panel.transform, 26, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);
            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.34f));

            _officialResultSeriesMeta = CreateText("SeriesMeta", panel.transform, 14, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultSeriesMeta.rectTransform, new Vector2(0.20f, 0.12f), new Vector2(0.80f, 0.22f));
            _officialResultSeriesMeta.gameObject.SetActive(false);
            _officialResultOutcome.fontStyle = FontStyle.Bold;

            CreateButton("ConfirmOfficialResult", "確認", panel.transform,
                new Vector2(0.30f, 0.06f), new Vector2(0.70f, 0.20f), OnConfirmOfficialResult);
            _officialResultRoot.SetActive(false);
        }

        private Image CreateResultTeamLogo(string name, Transform parent, float anchorX)
        {
            Image logo = CreateUiObject<Image>(name, parent);
            RectTransform rt = logo.rectTransform;
            // Column-centered with team name (same anchorX). Height matches score row.
            rt.anchorMin = new Vector2(anchorX, 0.52f);
            rt.anchorMax = new Vector2(anchorX, 0.82f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var fitter = logo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            logo.preserveAspect = false;
            logo.raycastTarget = false;
            logo.color = Color.white;
            return logo;
        }

        private void OnConfirmOfficialResult()
        {
            _awaitingOfficialResultConfirm = false;
            if (_officialResultRoot != null)
                _officialResultRoot.SetActive(false);
            if (_result != null)
                CareerMatchLaunchContext.SetCompletedResult(_result);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
        }

        private IEnumerator PlayEventRange(int startInclusive, int endExclusive, bool animate)
        {
            if (_session == null || startInclusive >= endExclusive)
                yield break;

            List<SimulationEvent> slice = _session.Result.events
                .Skip(startInclusive)
                .Take(endExclusive - startInclusive)
                .OrderBy(x => x.roundNumber)
                .ThenBy(x => x.roundTime)
                .ThenBy(x => x.sequence)
                .ToList();

            int currentRound = slice.Count > 0 ? slice[0].roundNumber : 0;
            float previousTime = 0f;
            HashSet<int> silentMoves = null;
            Dictionary<int, string> coalesceDest = null;
            if (IsGeneralBattleMode())
                BuildGeneralMoveCoalesce(slice, out silentMoves, out coalesceDest);
            foreach (SimulationEvent evt in slice)
            {
                if (evt.roundNumber != currentRound)
                {
                    currentRound = evt.roundNumber;
                    previousTime = 0f;
                }

                if (IsGeneralBattleMode() &&
                    silentMoves != null &&
                    evt.type == SimulationEventType.Move &&
                    silentMoves.Contains(evt.sequence))
                {
                    previousTime = Mathf.Max(previousTime, evt.roundTime);
                    ApplyEvent(evt, false, silentMoves, coalesceDest);
                    continue;
                }

                if (animate)
                {
                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);

                    // General mode moves: set chase target immediately, never wait on travel duration.
                    if (IsGeneralBattleMode() && evt.type == SimulationEventType.Move)
                    {
                        float chainEnd = evt.roundTime;
                        if (coalesceDest != null &&
                            coalesceDest.ContainsKey(evt.sequence) &&
                            silentMoves != null)
                        {
                            for (int k = 0; k < slice.Count; k++)
                            {
                                SimulationEvent later = slice[k];
                                if (later.sequence <= evt.sequence) continue;
                                if (later.type == SimulationEventType.Move &&
                                    string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase) &&
                                    silentMoves.Contains(later.sequence))
                                    chainEnd = later.roundTime;
                                else if (later.type == SimulationEventType.Move &&
                                         string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))
                                    break;
                                else if (later.type != SimulationEventType.StatusUpdated &&
                                         later.type != SimulationEventType.Contact &&
                                         later.type != SimulationEventType.Shot &&
                                         later.type != SimulationEventType.TraitTriggered)
                                    break;
                            }
                        }
                        previousTime = chainEnd;
                        ApplyEvent(evt, true, silentMoves, coalesceDest);
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        // Let Update tick chase at least once; avoid same-frame retarget storms.
                        yield return null;
                        continue;
                    }

                    previousTime = evt.roundTime;
                    float idleWait = GeneralModePlaybackWait(gap);
                    float idleElapsed = 0f;
                    float idleShown = evt.roundTime - gap;
                    while (idleElapsed < idleWait)
                    {
                        if (!_paused)
                        {
                            idleElapsed += Time.unscaledDeltaTime;
                            if (IsGeneralBattleMode() && idleWait > 0.001f && gap > 0.001f)
                            {
                                float u = Mathf.Clamp01(idleElapsed / idleWait);
                                SetRoundHud(evt.roundNumber, FormatClock(idleShown + gap * u));
                            }
                        }
                        yield return null;
                    }
                    while (_paused)
                        yield return null;
                }
                else
                    previousTime = evt.roundTime;

                if (animate &&
                    IsGeneralBattleMode() &&
                    (evt.type == SimulationEventType.RoundEnded ||
                     evt.type == SimulationEventType.BombExploded ||
                     evt.type == SimulationEventType.BombDefused))
                {
                    float settleCap = 8f;
                    float settleElapsed = 0f;
                    while (_generalChases.Count > 0 && settleElapsed < settleCap)
                    {
                        if (!_paused)
                            settleElapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                ApplyEvent(evt, animate, silentMoves, coalesceDest);
            }
        }

        private static void BuildGeneralMoveCoalesce(
            List<SimulationEvent> slice,
            out HashSet<int> silentMoves,
            out Dictionary<int, string> coalesceDest)
        {
            silentMoves = new HashSet<int>();
            coalesceDest = new Dictionary<int, string>();
            for (int i = 0; i < slice.Count; i++)
            {
                SimulationEvent evt = slice[i];
                if (evt.type != SimulationEventType.Move || string.IsNullOrEmpty(evt.actorId))
                    continue;
                if (silentMoves.Contains(evt.sequence))
                    continue;

                string finalZone = evt.zoneId;
                var intermediates = new List<int>();
                for (int j = i + 1; j < slice.Count; j++)
                {
                    SimulationEvent later = slice[j];
                    if (later.type == SimulationEventType.Move &&
                        string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))
                    {
                        finalZone = later.zoneId;
                        intermediates.Add(later.sequence);
                        continue;
                    }
                    if (later.type == SimulationEventType.StatusUpdated ||
                        later.type == SimulationEventType.Contact ||
                        later.type == SimulationEventType.Shot ||
                        later.type == SimulationEventType.TraitTriggered)
                        continue;
                    break;
                }

                coalesceDest[evt.sequence] = finalZone;
                foreach (int seq in intermediates)
                    silentMoves.Add(seq);
            }
        }

        private void SyncEconomyFromState()
        {
            if (_session == null)
                return;
            _teamMoney[_session.State.TeamA.Definition.id] = _session.State.TeamA.Money;
            _teamMoney[_session.State.TeamB.Definition.id] = _session.State.TeamB.Money;
            _teamCards[_session.State.TeamA.Definition.id] = _session.State.TeamA.Equipment;
            _teamCards[_session.State.TeamB.Definition.id] = _session.State.TeamB.Equipment;
        }

        private void ApplyEvent(
            SimulationEvent evt,
            bool animate,
            HashSet<int> silentMoves = null,
            Dictionary<int, string> coalesceDest = null)
        {
            _currentRound = Mathf.Max(1, evt.roundNumber);
            if (evt.type == SimulationEventType.MatchEnded)
                SetRoundHud(_currentRound, "比賽結束");
            else
                SetRoundHud(evt.roundNumber, FormatClock(evt.roundTime));

            switch (evt.type)
            {
                case SimulationEventType.RoundStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    ResetMarkers(evt.roundNumber, false);
                    _bombCarrierId = null;
                    _droppedBombZoneId = null;
                    _bombPlantedAtRoundTime = -1f;
                    ApplyClockPlantedColor();
                    HideBomb();
                    ClearUtilityMarkers();
                    ClearBlindedGlows();
                    _roundHasDeath = false;
                    ResetRoundKills();
                    ResetRoundFeeds();
                    HideRoundWinBanner();
                    UpdateSidePanelColors();
                    UpdateAliveCountHud();
                    break;
                case SimulationEventType.Setup:
                    ApplyEconomyTags(evt);
                    break;
                case SimulationEventType.HalfTime:
                case SimulationEventType.OvertimeStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    UpdateSidePanelColors();
                    break;
                case SimulationEventType.Move:
                    ExpireUtilityMarkers(evt.roundTime);
                    ExpireBlindedGlows(evt.roundTime);
                    if (silentMoves != null && silentMoves.Contains(evt.sequence))
                    {
                        if (!string.IsNullOrEmpty(evt.actorId) && !string.IsNullOrEmpty(evt.zoneId))
                            _playerZones[evt.actorId] = evt.zoneId;
                        break;
                    }
                    {
                        string dest = evt.zoneId;
                        if (coalesceDest != null &&
                            coalesceDest.TryGetValue(evt.sequence, out string coalesced) &&
                            !string.IsNullOrEmpty(coalesced))
                            dest = coalesced;
                        MoveMarkerAlongPath(evt.actorId, dest, animate);
                    }
                    break;
                case SimulationEventType.BombPlanted:
                    _droppedBombZoneId = null;
                    _bombCarrierId = evt.actorId;
                    _bombPlantedAtRoundTime = evt.roundTime;
                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);
                    ShowBomb(evt.zoneId, planted: true);
                    SetRoundHud(evt.roundNumber, FormatClock(evt.roundTime));
                    RefreshStatPanels();
                    break;
                case SimulationEventType.BombDefused:
                case SimulationEventType.BombExploded:
                    _bombPlantedAtRoundTime = -1f;
                    ApplyClockPlantedColor();
                    HideBomb();
                    break;
                case SimulationEventType.Utility:
                    ExpireUtilityMarkers(evt.roundTime);
                    ShowUtilityEffect(evt);
                    RefreshStatPanels();
                    break;
                case SimulationEventType.Shot:
                    ShowMuzzleRay(evt.actorId, evt.targetId);
                    break;
                case SimulationEventType.Kill:
                    if (!IsGeneralBattleMode())
                        ShowMuzzleRay(evt.actorId, evt.targetId);
                    if (!string.IsNullOrEmpty(evt.targetId))
                    {
                        _deadMarkers.Add(evt.targetId);
                        SetMarkerVitality(evt.targetId, 0, 0);
                        _generalChases.Remove(evt.targetId);
                        _moveDestinations.Remove(evt.targetId);
                    }
                    if (!string.IsNullOrEmpty(evt.targetId) && _markers.TryGetValue(evt.targetId, out RectTransform victim))
                    {
                        victim.gameObject.SetActive(false);
                        if (!IsGeneralBattleMode())
                        {
                            string deathZone = _playerZones.TryGetValue(evt.targetId, out string z) ? z : evt.zoneId;
                            RelayoutZone(deathZone, animate, 0.18f);
                        }
                    }
                    ApplyKillStats(evt);
                    ApplyEconomyTags(evt);
                    AddKillFeed(evt);
                    _roundHasDeath = true;
                    UpdateAliveCountHud();
                    break;
                case SimulationEventType.TraitTriggered:
                    AddTraitFeed(evt);
                    break;
                case SimulationEventType.StatusUpdated:
                    ExpireUtilityMarkers(evt.roundTime);
                    ExpireBlindedGlows(evt.roundTime);
                    ApplyStatusUpdate(evt);
                    break;
                case SimulationEventType.RoundEnded:
                    HideBomb();
                    ClearUtilityMarkers();
                    ClearBlindedGlows();
                    if (_session != null)
                    {
                        string teamAId = _session.State.TeamA.Definition.id;
                        string teamBId = _session.State.TeamB.Definition.id;
                        if (evt.teamId == teamAId) _naviScore++;
                        else if (evt.teamId == teamBId) _gambitScore++;
                    }
                    ApplyEconomyTags(evt);
                    UpdateScore();
                    ShowRoundWinBanner(evt.teamId, evt.roundNumber, animate);
                    break;
                case SimulationEventType.MatchEnded:
                    _naviScore = _result.teamAScore;
                    _gambitScore = _result.teamBScore;
                    UpdateScore();
                    break;
            }
        }

        private void ResetMarkers(int roundNumber, bool animate)
        {
            StopAllMovement();
            _deadMarkers.Clear();
            ClearMuzzleRays();
            if (_session == null) return;
            string teamAId = _session.State.TeamA.Definition.id;
            string teamBId = _session.State.TeamB.Definition.id;
            bool naviT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));
            PositionTeam(_content.Teams[teamAId], naviT ? "t_spawn" : "ct_spawn", animate);
            PositionTeam(_content.Teams[teamBId], naviT ? "ct_spawn" : "t_spawn", animate);
        }

        private void PositionTeam(TeamDefinition team, string spawn, bool animate)
        {
            for (int i = 0; i < team.playerIds.Length; i++)
            {
                string id = team.playerIds[i];
                RectTransform marker = _markers[id];
                marker.gameObject.SetActive(true);
                _playerZones[id] = spawn;
                if (IsGeneralBattleMode())
                {
                    PlayerRuntime live = null;
                    foreach (PlayerRuntime p in _session.State.TeamA.Players.Concat(_session.State.TeamB.Players))
                    {
                        if (p.Definition.id == id) { live = p; break; }
                    }
                    if (live != null)
                        SetMarkerVitality(id, live.Alive ? live.Health : 0, live.ArmorHp);
                    else
                        SetMarkerVitality(id, 100, 0);
                }
            }

            if (IsGeneralBattleMode())
            {
                // Place on zone center then push apart via collision — no flower ring.
                Vector2 center = ClampToMapCollision(LocalZonePosition(spawn));
                for (int i = 0; i < team.playerIds.Length; i++)
                {
                    string id = team.playerIds[i];
                    if (!_markers.TryGetValue(id, out RectTransform marker))
                        continue;
                    // Tiny deterministic seed so identical starts do not stack perfectly.
                    float seed = (PlayerNumber(id) - 1) * 0.01f;
                    marker.anchoredPosition = center + new Vector2(seed, -seed);
                }
                SeparateGeneralAgentsNear(spawn);
            }
            else
                RelayoutZone(spawn, animate, 0.25f);
            InitTeamFacings(team, spawn);
            RefreshAllMarkerLos();
            RefreshFloorVisibility();
        }

        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)
        {
            if (IsGeneralBattleMode())
            {
                SetGeneralMoveTarget(playerId, zoneId, animate);
                return;
            }

            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            if (string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                return;

            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);
            _playerZones[playerId] = zoneId;

            Vector2 endOffset = ZoneOccupantOffset(playerId, zoneId);
            if (!animate || path.Count == 0)
            {
                Vector2 instantEnd = ClampToMap(LocalZonePosition(zoneId) + endOffset);
                FaceMarkerToward(playerId, marker.anchoredPosition, instantEnd);
                marker.anchoredPosition = instantEnd;
                _moveDestinations.Remove(playerId);
                if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                    RelayoutZone(from, false, 0f);
                RelayoutZone(zoneId, false, 0f);
                return;
            }

            float duration = Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);
            if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                RelayoutZone(from, true, 0.18f);
            _moveDestinations[playerId] = zoneId;
            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));
        }

        private void SetGeneralMoveTarget(string playerId, string zoneId, bool animate)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            if (_deadMarkers.Contains(playerId))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            _playerZones[playerId] = zoneId;
            _moveDestinations[playerId] = zoneId;
            // Never snap markers for general mode — always chase from current visual position.
            _ = animate;

            if (_generalChases.TryGetValue(playerId, out GeneralChaseState ongoing) &&
                string.Equals(ongoing.TargetZone, zoneId, StringComparison.OrdinalIgnoreCase) &&
                ongoing.Points != null && ongoing.Points.Count > 0)
                return;

            IReadOnlyList<string> path = _pathfinder != null
                ? _pathfinder.FindPath(from, zoneId)
                : Array.Empty<string>();
            if (path == null || path.Count == 0)
                path = new[] { zoneId };

            // Path starts at the marker's current visual position so retargeting never
            // teleports onto a logical zone center far from where the marker actually is.
            Vector2 visualStart = ClampToMapCollision(marker.anchoredPosition);
            var points = new List<Vector2>(path.Count + 1) { visualStart };
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = ClampToMapCollision(LocalZonePosition(path[i]));
                if (Vector2.Distance(points[points.Count - 1], p) < 2.5f)
                    continue;
                points.Add(p);
            }
            points = DensifyPathPoints(points, 18f);

            Vector2 destPos = ClampToMapCollision(LocalZonePosition(zoneId));
            if (points.Count <= 1)
            {
                if (Vector2.Distance(visualStart, destPos) < 3f)
                {
                    marker.anchoredPosition = ResolveGeneralPosition(playerId, destPos);
                    _generalChases.Remove(playerId);
                    _moveDestinations.Remove(playerId);
                    RefreshFloorVisibility();
                    return;
                }
                points = new List<Vector2> { visualStart, destPos };
            }
            else if (Vector2.Distance(points[points.Count - 1], destPos) > 2.5f)
            {
                points.Add(destPos);
            }

            float[] seg = new float[points.Count - 1];
            float total = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                seg[i] = Mathf.Max(0.001f, Vector2.Distance(points[i], points[i + 1]));
                total += seg[i];
            }

            // Always begin at the visual start we prepended (progress 0) — never project
            // onto a path that begins at a distant logical zone (that caused teleports).
            var pathZones = new string[path.Count];
            for (int i = 0; i < path.Count; i++)
                pathZones[i] = path[i];

            _generalChases[playerId] = new GeneralChaseState
            {
                TargetZone = zoneId,
                Points = points,
                SegmentLengths = seg,
                TotalLength = total,
                Progress = 0f,
                PathZones = pathZones
            };
        }

        private static float ProjectOntoPolyline(
            Vector2 pos, List<Vector2> points, float[] seg, float total)
        {
            float bestDist = float.MaxValue;
            float bestProgress = 0f;
            float walked = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 ab = b - a;
                float abLenSq = ab.sqrMagnitude;
                float t = abLenSq < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pos - a, ab) / abLenSq);
                Vector2 proj = a + ab * t;
                float d = Vector2.Distance(pos, proj);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestProgress = walked + seg[i] * t;
                }
                walked += seg[i];
            }
            return Mathf.Clamp(bestProgress, 0f, total);
        }

        private void TickGeneralChases()
        {
            if (!IsGeneralBattleMode() || _generalChases.Count == 0)
                return;
            if (_paused)
                return;

            float speedMul = Mathf.Clamp(playbackSpeed, 0.25f, 3f);
            float step = Mathf.Min(
                GeneralChaseSpeed * Time.unscaledDeltaTime * speedMul,
                48f);
            var arrived = new List<string>();
            foreach (KeyValuePair<string, GeneralChaseState> pair in _generalChases)
            {
                string playerId = pair.Key;
                GeneralChaseState chase = pair.Value;
                if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                {
                    arrived.Add(playerId);
                    continue;
                }
                if (_deadMarkers.Contains(playerId))
                {
                    arrived.Add(playerId);
                    continue;
                }

                chase.Progress = Mathf.Min(chase.TotalLength, chase.Progress + step);
                // Stay on the authored path — no mid-chase collision shove (was stuttering).
                Vector2 pos = ClampToMapCollision(
                    SamplePolyline(chase.Points, chase.SegmentLengths, chase.Progress));
                FaceMarkerToward(playerId, marker.anchoredPosition, pos);
                marker.anchoredPosition = pos;

                int zoneIndex = 0;
                float walked = 0f;
                for (int i = 0; i < chase.SegmentLengths.Length; i++)
                {
                    if (walked + chase.SegmentLengths[i] >= chase.Progress ||
                        i == chase.SegmentLengths.Length - 1)
                    {
                        zoneIndex = i;
                        break;
                    }
                    walked += chase.SegmentLengths[i];
                }
                if (chase.PathZones != null && chase.PathZones.Length > 0)
                {
                    string segmentFloor = ZoneFloor(
                        chase.PathZones[Mathf.Min(zoneIndex, chase.PathZones.Length - 1)]);
                    marker.gameObject.SetActive(
                        string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
                }

                if (chase.Progress >= chase.TotalLength - 0.5f)
                    arrived.Add(playerId);
            }

            for (int i = 0; i < arrived.Count; i++)
            {
                string id = arrived[i];
                if (_generalChases.TryGetValue(id, out GeneralChaseState done))
                {
                    SettlePlayerInZone(id, done.TargetZone);
                    _generalChases.Remove(id);
                }
            }
        }

        private static Vector2 SamplePolyline(List<Vector2> points, float[] seg, float distance)
        {
            float walked = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                float len = seg[i];
                if (walked + len >= distance || i == seg.Length - 1)
                {
                    float local = Mathf.Clamp01((distance - walked) / Mathf.Max(0.001f, len));
                    return Vector2.Lerp(points[i], points[i + 1], local);
                }
                walked += len;
            }
            return points[points.Count - 1];
        }

        private void StartMoveRoutine(string playerId, IEnumerator routine)
        {
            if (_moveRoutines.TryGetValue(playerId, out Coroutine existing) && existing != null)
                StopCoroutine(existing);
            _moveRoutines[playerId] = StartCoroutine(routine);
        }

        private IEnumerator FollowPathRoutine(string playerId, IReadOnlyList<string> path, Vector2 endOffset, float duration)
        {
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                yield break;

            string destination = path.Count > 0 ? path[path.Count - 1] : null;
            var points = new List<Vector2>();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = LocalZonePosition(path[i]);
                if (i == path.Count - 1)
                    p += endOffset;
                points.Add(IsGeneralBattleMode() ? ClampToMapCollision(p) : ClampToMap(p));
            }
            if (IsGeneralBattleMode())
                points = DensifyPathPoints(points, 18f);

            if (points.Count == 1)
            {
                marker.anchoredPosition = IsGeneralBattleMode()
                    ? ClampToMapCollision(points[0])
                    : points[0];
                SettlePlayerInZone(playerId, destination);
                _moveRoutines[playerId] = null;
                yield break;
            }

            float[] segmentLengths = new float[points.Count - 1];
            float total = 0f;
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                segmentLengths[i] = Vector2.Distance(points[i], points[i + 1]);
                total += Mathf.Max(0.001f, segmentLengths[i]);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!_paused)
                    elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float distance = t * total;
                float walked = 0f;
                for (int i = 0; i < segmentLengths.Length; i++)
                {
                    float seg = Mathf.Max(0.001f, segmentLengths[i]);
                    if (walked + seg >= distance || i == segmentLengths.Length - 1)
                    {
                        float local = Mathf.Clamp01((distance - walked) / seg);
                        string segmentFloor = ZoneFloor(path[Mathf.Min(i, path.Count - 1)]);
                        marker.gameObject.SetActive(
                            !_deadMarkers.Contains(playerId) &&
                            string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);
                        Vector2 nextPos = IsGeneralBattleMode()
                            ? ClampToMapCollision(lerped)
                            : lerped;
                        FaceMarkerToward(playerId, marker.anchoredPosition, nextPos);
                        marker.anchoredPosition = nextPos;
                        break;
                    }
                    walked += seg;
                }
                yield return null;
            }

            SettlePlayerInZone(playerId, destination);
            _moveRoutines[playerId] = null;
        }

        private void SettlePlayerInZone(string playerId, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            Vector2 desired = LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId);
            if (IsGeneralBattleMode())
            {
                // Soft land: keep path-end position; tiny overlap nudge only.
                Vector2 current = marker.anchoredPosition;
                if (Vector2.Distance(current, desired) < AgentCollisionRadius() * 4f)
                    desired = current;
                marker.anchoredPosition = SoftSeparateOnce(playerId, ClampToMapCollision(desired));
            }
            else
            {
                marker.anchoredPosition = ClampToMap(desired);
                RelayoutZone(zoneId, false, 0f);
            }
            _moveDestinations.Remove(playerId);
            RefreshFloorVisibility();
        }

        /// <summary>Insert samples so long nav segments glide instead of leaping.</summary>
        private static List<Vector2> DensifyPathPoints(List<Vector2> source, float maxSegment)
        {
            if (source == null || source.Count < 2 || maxSegment < 4f)
                return source;
            var dense = new List<Vector2>(source.Count * 2);
            dense.Add(source[0]);
            for (int i = 0; i < source.Count - 1; i++)
            {
                Vector2 a = source[i];
                Vector2 b = source[i + 1];
                float dist = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / maxSegment));
                for (int s = 1; s <= steps; s++)
                    dense.Add(Vector2.Lerp(a, b, s / (float)steps));
            }
            return dense;
        }

        /// <summary>
        /// Spread active markers around a shared zone center so they do not stack,
        /// while keeping the same logical zone for combat.
        /// </summary>
        private void RelayoutZone(string zoneId, bool animate, float duration)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            List<string> occupants = ActiveOccupants(zoneId);
            foreach (string id in occupants)
            {
                if (_moveRoutines.TryGetValue(id, out Coroutine moving) && moving != null)
                    continue;
                if (!_markers.TryGetValue(id, out RectTransform marker) || !marker.gameObject.activeSelf)
                    continue;

                Vector2 offset = ZoneOccupantOffset(id, zoneId);
                Vector2 target = ClampToMap(LocalZonePosition(zoneId) + offset);
                if (animate && duration > 0.01f)
                    StartMoveRoutine(id, FollowPathRoutine(id, new[] { zoneId }, offset, duration));
                else
                    marker.anchoredPosition = target;
            }
        }

        private List<string> ActiveOccupants(string zoneId)
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, string> pair in _playerZones)
            {
                if (!string.Equals(pair.Value, zoneId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!_markers.TryGetValue(pair.Key, out RectTransform marker) || !marker.gameObject.activeSelf)
                    continue;
                list.Add(pair.Key);
            }

            list.Sort((a, b) =>
            {
                int team = string.Compare(ResolvePlayerTeamId(a), ResolvePlayerTeamId(b), StringComparison.Ordinal);
                if (team != 0)
                    return team;
                return PlayerNumber(a).CompareTo(PlayerNumber(b));
            });
            return list;
        }

        private Vector2 ZoneOccupantOffset(string playerId, string zoneId)
        {
            // General mode uses physical collision instead of flower packing.
            if (IsGeneralBattleMode())
                return Vector2.zero;

            List<string> occupants = ActiveOccupants(zoneId);
            int index = occupants.IndexOf(playerId);
            if (index < 0)
                index = Mathf.Max(0, PlayerNumber(playerId) - 1);
            return SlotRingOffset(index, Mathf.Max(1, occupants.Count));
        }

        private Vector2 SlotRingOffset(int index, int count)
        {
            if (count <= 1)
                return Vector2.zero;

            GetMapContentSize(out float width, out float height);
            float dot = ActiveMarkerDotDiameter();
            float radius = Mathf.Clamp(
                Mathf.Min(width, height) * (IsGeneralBattleMode() ? 0.022f : 0.032f),
                dot * 0.55f,
                IsGeneralBattleMode() ? dot * 1.15f : 26f);
            if (count >= 6)
                radius *= 1.2f;
            else if (count == 2)
                radius *= 0.85f;

            // Even ring around zone center — same fight location, no stacked dots.
            float angle = -Mathf.PI * 0.5f + index * (Mathf.PI * 2f / count);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private void StopAllMovement()
        {
            foreach (KeyValuePair<string, Coroutine> pair in _moveRoutines.ToList())
            {
                if (pair.Value != null)
                    StopCoroutine(pair.Value);
            }
            _moveRoutines.Clear();
            _moveDestinations.Clear();
            _generalChases.Clear();
        }

        private Vector2 LocalZonePosition(string zoneId)
        {
            if (!_zones.TryGetValue(zoneId, out MapZoneDefinition zone))
            {
                if (!_zones.TryGetValue("connector", out zone) &&
                    !_zones.TryGetValue("mid", out zone) &&
                    !_zones.TryGetValue("a_site", out zone))
                    zone = _zones.Values.First();
            }
            GetMapContentSize(out float width, out float height);
            // zone.x/y are UV over the visible sprite (preserveAspect content), not the padded Rect.
            return new Vector2((zone.x - 0.5f) * width, (zone.y - 0.5f) * height);
        }

        private Vector2 ClampToMap(Vector2 local)
        {
            GetMapContentSize(out float width, out float height);
            float pad = Mathf.Min(28f, Mathf.Min(width, height) * 0.04f);
            return new Vector2(
                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),
                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));
        }

        /// <summary>
        /// Body radius from the shared hammer size. Dust II's mid door fits one body plus a little room.
        /// </summary>
        private float AgentCollisionRadius()
        {
            GetMapContentSize(out float width, out float _);
            float overviewWidth = _mapSprite != null ? _mapSprite.rect.width : 0f;
            if (_collisionHammerPerPixel > 0.001f && overviewWidth > 1f && width > 1f)
                return MapBodyClearance.LocalRadius(width, overviewWidth, _collisionHammerPerPixel);
            return GeneralCollisionRadiusFallback;
        }

        /// <summary>Map bounds inset by agent collision radius so bodies stay inside.</summary>
        private Vector2 ClampToMapCollision(Vector2 local)
        {
            GetMapContentSize(out float width, out float height);
            float pad = Mathf.Max(AgentCollisionRadius() + 1f,
                Mathf.Min(28f, Mathf.Min(width, height) * 0.04f));
            local = new Vector2(
                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),
                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));
            if (_mapCollision != null && _mapCollision.IsReady)
                local = _mapCollision.ResolveCircle(local, AgentCollisionRadius());
            return local;
        }

        private Vector2 ResolveGeneralPosition(string playerId, Vector2 desired)
        {
            desired = ClampToMapCollision(desired);
            float minDist = AgentCollisionRadius() * 2f;
            // Only separate against nearby agents — far teammates must not shove across the map.
            float consider = minDist * 3f;
            for (int iter = 0; iter < 5; iter++)
            {
                foreach (KeyValuePair<string, RectTransform> pair in _markers)
                {
                    if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (_deadMarkers.Contains(pair.Key))
                        continue;
                    RectTransform other = pair.Value;
                    if (other == null || !other.gameObject.activeSelf)
                        continue;

                    Vector2 delta = desired - other.anchoredPosition;
                    float dist = delta.magnitude;
                    if (dist >= minDist || dist > consider)
                        continue;
                    Vector2 dir;
                    if (dist < 0.001f)
                    {
                        float ang = PlayerNumber(playerId) * 1.2566f;
                        dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    }
                    else
                        dir = delta / dist;
                    // Never scale a unit vector by 1/dist (old bug teleported markers to corners).
                    desired += dir * (minDist - dist);
                }
                desired = ClampToMapCollision(desired);
            }
            return desired;
        }

        /// <summary>One light push for overlap at rest — weaker than ResolveGeneralPosition.</summary>
        private Vector2 SoftSeparateOnce(string playerId, Vector2 desired)
        {
            float minDist = AgentCollisionRadius() * 2f;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_deadMarkers.Contains(pair.Key) || _generalChases.ContainsKey(pair.Key))
                    continue;
                RectTransform other = pair.Value;
                if (other == null || !other.gameObject.activeSelf)
                    continue;
                Vector2 delta = desired - other.anchoredPosition;
                float dist = delta.magnitude;
                if (dist >= minDist || dist > minDist * 2f)
                    continue;
                Vector2 dir = dist < 0.001f
                    ? new Vector2(Mathf.Cos(PlayerNumber(playerId) * 1.2566f), Mathf.Sin(PlayerNumber(playerId) * 1.2566f))
                    : delta / dist;
                desired += dir * ((minDist - dist) * 0.35f);
            }
            return ClampToMapCollision(desired);
        }

        private void SeparateGeneralAgentsNear(string zoneId)
        {
            if (!IsGeneralBattleMode())
                return;
            // Relax nearby agents so stacks at a zone break apart without flower layout.
            for (int iter = 0; iter < 8; iter++)
            {
                foreach (KeyValuePair<string, RectTransform> pair in _markers)
                {
                    if (_deadMarkers.Contains(pair.Key))
                        continue;
                    RectTransform marker = pair.Value;
                    if (marker == null || !marker.gameObject.activeSelf)
                        continue;
                    if (!string.IsNullOrEmpty(zoneId) &&
                        _playerZones.TryGetValue(pair.Key, out string z) &&
                        !string.Equals(z, zoneId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (_moveRoutines.TryGetValue(pair.Key, out Coroutine moving) && moving != null)
                        continue;
                    marker.anchoredPosition = ResolveGeneralPosition(pair.Key, marker.anchoredPosition);
                }
            }
        }

        /// <summary>
        /// Shrinks the map frame to the sprite aspect ratio inside a max slot so left/right
        /// letterbox bars are cropped away instead of shown as empty padding.
        /// </summary>
        private void FitMapFrameToSprite(Vector2 slotMin, Vector2 slotMax)
        {
            const float refW = 1920f;
            const float refH = 1080f;
            float slotW = Mathf.Max(0.001f, slotMax.x - slotMin.x);
            float slotH = Mathf.Max(0.001f, slotMax.y - slotMin.y);
            float spriteAspect = _mapSprite != null
                ? _mapSprite.rect.width / Mathf.Max(0.0001f, _mapSprite.rect.height)
                : 1f;

            float slotAspect = (slotW * refW) / (slotH * refH);
            float useW = slotW;
            float useH = slotH;
            if (slotAspect > spriteAspect)
                useW = (slotH * refH * spriteAspect) / refW;
            else
                useH = (slotW * refW / spriteAspect) / refH;

            float cx = (slotMin.x + slotMax.x) * 0.5f;
            float cy = (slotMin.y + slotMax.y) * 0.5f;
            SetRect(_mapFrame,
                new Vector2(cx - useW * 0.5f, cy - useH * 0.5f),
                new Vector2(cx + useW * 0.5f, cy + useH * 0.5f));
        }

        /// <summary>
        /// Map image fills _mapRect exactly after FitMapFrameToSprite.
        /// </summary>

        private MapDefinition ActiveMapDefinition()
        {
            string id = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            return _content != null ? _content.GetMap(id) : null;
        }

        private MapZoneDefinition[] ActiveMapZones() =>
            ActiveMapDefinition()?.zones ?? Array.Empty<MapZoneDefinition>();

        private bool MapSupportsFloors()
        {
            MapDefinition map = ActiveMapDefinition();
            return map?.floors != null && map.floors.Length > 1;
        }

        private string ZoneFloor(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && _zones.TryGetValue(zoneId, out MapZoneDefinition zone) &&
                !string.IsNullOrEmpty(zone.floor))
                return zone.floor;
            return string.IsNullOrEmpty(_currentMapFloor) ? "main" : _currentMapFloor;
        }

        private void BindActiveMap()
        {
            _zones.Clear();
            MapDefinition map = ActiveMapDefinition();
            if (map == null)
                return;
            _pathfinder = new MapPathfinder(map);
            foreach (MapZoneDefinition zone in map.zones ?? Array.Empty<MapZoneDefinition>())
                _zones[zone.id] = zone;
            _currentMapFloor = !string.IsNullOrEmpty(map.defaultFloor)
                ? map.defaultFloor
                : map.floors != null && map.floors.Length > 0 ? map.floors[0].id : "main";
        }

        private Sprite LoadFloorOverviewSprite()
        {
            MapDefinition map = ActiveMapDefinition();
            MapFloorDefinition floor = map?.floors?.FirstOrDefault(x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            if (floor != null && !string.IsNullOrEmpty(floor.overviewResource))
            {
                Sprite sprite = Resources.Load<Sprite>(floor.overviewResource);
                if (sprite != null)
                {
                    BindOverviewResource(floor.overviewResource);
                    return sprite;
                }
            }
            return LoadOverviewSprite(string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId);
        }

        private void CycleMapFloor()
        {
            MapDefinition map = ActiveMapDefinition();
            if (map?.floors == null || map.floors.Length < 2)
                return;
            int current = Array.FindIndex(map.floors, x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            _currentMapFloor = map.floors[(current + 1 + map.floors.Length) % map.floors.Length].id;
            _mapSprite = LoadFloorOverviewSprite();
            if (_mapImage != null)
                _mapImage.sprite = _mapSprite;
            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            ApplyMapPanZoom();
            RebuildMapCollision();
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();
        }

        private void RefreshFloorButton()
        {
            if (_floorButtonText == null)
                return;
            MapDefinition map = ActiveMapDefinition();
            MapFloorDefinition floor = map?.floors?.FirstOrDefault(x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            _floorButtonText.text = floor != null && !string.IsNullOrEmpty(floor.displayName)
                ? floor.displayName
                : _currentMapFloor;
        }

        private bool IsCurrentMatchPlayer(string playerId)
        {
            if (_session == null || string.IsNullOrEmpty(playerId))
                return false;
            return _session.State.TeamA.Definition.playerIds.Contains(playerId) ||
                   _session.State.TeamB.Definition.playerIds.Contains(playerId);
        }

        private void RefreshFloorVisibility()
        {
            MapZoneDefinition[] zones = ActiveMapZones();
            for (int i = 0; i < zones.Length && i < _zoneHitPads.Count; i++)
            {
                MapZoneDefinition zone = zones[i];
                bool visible = !zone.navigationOnly &&
                    string.Equals(ZoneFloor(zone.id), _currentMapFloor, StringComparison.OrdinalIgnoreCase);
                if (_zoneHitPads[i] != null)
                    _zoneHitPads[i].gameObject.SetActive(visible);
            }
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (!IsCurrentMatchPlayer(pair.Key))
                    continue;
                string zoneId = _playerZones.TryGetValue(pair.Key, out string current) ? current : "t_spawn";
                bool visible = !_deadMarkers.Contains(pair.Key) &&
                    string.Equals(ZoneFloor(zoneId), _currentMapFloor, StringComparison.OrdinalIgnoreCase);
                pair.Value.gameObject.SetActive(visible);
            }
            if (_bombMarker != null && !string.IsNullOrEmpty(_bombZoneId))
                _bombMarker.gameObject.SetActive(
                    string.Equals(ZoneFloor(_bombZoneId), _currentMapFloor, StringComparison.OrdinalIgnoreCase));
        }

        private Sprite LoadOverviewSprite(string mapId)
        {
            if (!string.IsNullOrEmpty(mapId) &&
                !string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
            {
                string namedResource = "Maps/" + mapId + "_overview";
                Sprite named = Resources.Load<Sprite>(namedResource);
                if (named != null)
                {
                    BindOverviewResource(namedResource);
                    return named;
                }
            }
            BindOverviewResource("Maps/dust2_overview");
            return Resources.Load<Sprite>("Maps/dust2_overview");
        }

        private void BindOverviewResource(string resource)
        {
            _overviewResource = string.IsNullOrEmpty(resource) ? "Maps/dust2_overview" : resource;
            _collisionHammerPerPixel = MapCollisionScale.HammerPerPixel(_overviewResource);
            _collisionMask = Resources.Load<Texture2D>(_overviewResource + "_collision");
        }

        private static string BackgroundResourcePrefix(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) ||
                string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/dust2_bg_";
            return "UI/Backgrounds/" + mapId + "_bg_";
        }

        private float ActiveMarkerDotDiameter() =>
            IsGeneralBattleMode() ? MarkerDotDiameter : QuickMarkerDotDiameter;

        private void ApplyMapPanZoom()
        {
            if (_mapRotator == null)
                return;
            if (!IsGeneralBattleMode())
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }
            else
                _mapZoom = Mathf.Clamp(_mapZoom, MapZoomMin, MapZoomMax);
            _mapRotator.localScale = new Vector3(_mapZoom, _mapZoom, 1f);
            _mapRotator.anchoredPosition = _mapPan;
        }

        private void ApplyMapCameraForMode()
        {
            if (IsGeneralBattleMode())
            {
                _mapZoom = MapZoomDefault;
                _mapPan = Vector2.zero;
            }
            else
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }
            ApplyMapPanZoom();
            ApplyMarkerVisualSizes();
        }

        private void ApplyMarkerVisualSizes()
        {
            float d = ActiveMarkerDotDiameter();
            float rootSize = d + 6f;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                RectTransform root = pair.Value;
                if (root == null)
                    continue;
                root.sizeDelta = new Vector2(rootSize, rootSize);
                Transform ringT = root.Find("Ring");
                if (ringT is RectTransform ring)
                    ring.sizeDelta = new Vector2(d + 4f, d + 4f);
                if (_markerDots.TryGetValue(pair.Key, out Image dot) && dot != null)
                    dot.rectTransform.sizeDelta = new Vector2(d, d);
                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                {
                    float coneLen = IsGeneralBattleMode() ? ViewConeLength : ViewConeLength * 1.15f;
                    cone.rectTransform.sizeDelta = new Vector2(coneLen * 2f, coneLen * 2f);
                }
                if (_markerSightRays.TryGetValue(pair.Key, out Image sight) && sight != null)
                {
                    bool show = IsGeneralBattleMode();
                    sight.gameObject.SetActive(show);
                    float len = show ? SightRayLength : SightRayLength * 1.08f;
                    sight.rectTransform.sizeDelta = new Vector2(len, SightRayWidth);
                }
                if (_markerLabels.TryGetValue(pair.Key, out Text label) && label != null)
                {
                    label.fontSize = IsGeneralBattleMode()
                        ? Mathf.Clamp(Mathf.RoundToInt(d * 0.7f), 9, 14)
                        : 15;
                    label.rectTransform.sizeDelta = new Vector2(d, d);
                }
            }

            if (_bombMarker != null)
            {
                float bombSize = d + 8f;
                _bombMarker.sizeDelta = new Vector2(bombSize, bombSize);
                if (_bombMarkerGlow != null)
                    _bombMarkerGlow.rectTransform.sizeDelta = new Vector2(bombSize, bombSize);
                if (_bombMarkerCore != null)
                {
                    float core = IsGeneralBattleMode() ? d * 0.65f : 18f;
                    _bombMarkerCore.rectTransform.sizeDelta = new Vector2(core, core);
                }
                Text bombLabel = _bombMarker.GetComponentInChildren<Text>();
                if (bombLabel != null)
                    bombLabel.fontSize = IsGeneralBattleMode() ? 11 : 14;
            }
        }

        private bool TryMapPointerLocal(out Vector2 local)
        {
            local = Vector2.zero;
            if (_mapFrame == null)
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapFrame, Input.mousePosition, null, out local))
                return false;
            return _mapFrame.rect.Contains(local);
        }

        private void TickMapCamera()
        {
            if (!IsGeneralBattleMode())
                return;
            if (_mapFrame == null || _mapRotator == null)
                return;
            if (_settingsPanel != null && _settingsPanel.IsVisible)
                return;
            if (_officialResultRoot != null && _officialResultRoot.activeSelf)
                return;
            if (!TryMapPointerLocal(out Vector2 local))
                return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float factor = 1f + MapZoomScrollFactor;
                _mapZoom = scroll > 0f
                    ? _mapZoom * factor
                    : _mapZoom / factor;
                ApplyMapPanZoom();
            }

            Rect r = _mapFrame.rect;
            float nx = (local.x - r.xMin) / Mathf.Max(1f, r.width);
            float ny = (local.y - r.yMin) / Mathf.Max(1f, r.height);
            Vector2 dir = Vector2.zero;
            if (nx <= MapEdgePanMargin)
                dir.x -= 1f;
            if (nx >= 1f - MapEdgePanMargin)
                dir.x += 1f;
            if (ny <= MapEdgePanMargin)
                dir.y -= 1f;
            if (ny >= 1f - MapEdgePanMargin)
                dir.y += 1f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();
            // Mouse at an edge pans the view that way (map content slides opposite).
            float speed = MapEdgePanSpeed * Time.unscaledDeltaTime;
            _mapPan -= dir * speed;
            // Soft overscroll — allow leaving the map art, but not endlessly.
            GetMapContentSize(out float mapW, out float mapH);
            float viewW = Mathf.Max(1f, r.width);
            float viewH = Mathf.Max(1f, r.height);
            float halfX = (mapW * _mapZoom + viewW) * 0.55f;
            float halfY = (mapH * _mapZoom + viewH) * 0.55f;
            _mapPan.x = Mathf.Clamp(_mapPan.x, -halfX, halfX);
            _mapPan.y = Mathf.Clamp(_mapPan.y, -halfY, halfY);
            ApplyMapPanZoom();
        }

        private void GetMapContentSize(out float width, out float height)
        {
            Rect rect = _mapRect != null ? _mapRect.rect : new Rect(0f, 0f, 1f, 1f);
            width = rect.width;
            height = rect.height;
        }

        private void ShowBomb(string zoneId, bool planted = false)
        {
            if (_bombMarker == null)
                return;
            _bombZoneId = zoneId;
            _bombMarker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            SetBombMarkerPlantedStyle(planted);
            _bombMarker.gameObject.SetActive(true);
            _bombMarker.SetAsLastSibling();
            RefreshFloorVisibility();
        }

        private void SetBombMarkerPlantedStyle(bool planted)
        {
            if (_bombMarkerGlow != null)
                _bombMarkerGlow.color = planted
                    ? new Color(0.95f, 0.12f, 0.12f, 0.40f)
                    : new Color(0.95f, 0.55f, 0.12f, 0.35f);
            if (_bombMarkerCore != null)
                _bombMarkerCore.color = planted
                    ? new Color(0.92f, 0.12f, 0.12f, 1f)
                    : new Color(0.98f, 0.72f, 0.18f, 1f);
        }

        private void HideBomb()
        {
            _bombZoneId = null;
            if (_bombMarker != null)
                _bombMarker.gameObject.SetActive(false);
        }

        private sealed class UtilityMapMarker
        {
            public RectTransform Root;
            public Image Fill;
            public string ZoneId;
            public string Kind;
            public float ExpireAtRoundTime;
        }

        private void ClearUtilityMarkers()
        {
            for (int i = 0; i < _utilityMarkers.Count; i++)
            {
                UtilityMapMarker m = _utilityMarkers[i];
                if (m?.Root != null)
                    Destroy(m.Root.gameObject);
            }
            _utilityMarkers.Clear();
        }

        private void ExpireUtilityMarkers(float roundTime)
        {
            for (int i = _utilityMarkers.Count - 1; i >= 0; i--)
            {
                UtilityMapMarker m = _utilityMarkers[i];
                if (m == null || roundTime + 0.001f < m.ExpireAtRoundTime)
                    continue;
                if (m.Root != null)
                    Destroy(m.Root.gameObject);
                _utilityMarkers.RemoveAt(i);
            }
        }

        private void ShowUtilityEffect(SimulationEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.zoneId))
                return;
            string kind = (evt.weaponId ?? string.Empty).ToLowerInvariant();
            if (kind == "he" || kind == "hegrenade")
            {
                SpawnHeExplosionBurst(evt.zoneId);
                return;
            }
            if (kind == "flash" || kind == "flashbang")
            {
                SpawnUtilityBurst(evt.zoneId, new Color(1f, 1f, 1f, 0.72f), 36f);
                return;
            }

            bool smoke = kind == "smoke" || kind == "smoke_grenade";
            bool fire = kind == "fire" || kind == "molotov" || kind == "incendiary";
            if (!smoke && !fire)
                smoke = true;

            float duration = smoke ? 15f : 7f;
            Color fill = smoke
                ? new Color(0.72f, 0.74f, 0.78f, 0.42f)
                : new Color(0.95f, 0.28f, 0.08f, 0.38f);
            EnsureUtilityMarkerRoot();
            Image disc = CreateUiObject<Image>("Util_" + kind + "_" + evt.zoneId, _utilityMarkerRoot);
            disc.raycastTarget = false;
            disc.color = fill;
            RectTransform rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float diameter = smoke ? 54f : 42f;
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = ClampToMap(LocalZonePosition(evt.zoneId));
            disc.transform.SetAsFirstSibling();
            _utilityMarkers.Add(new UtilityMapMarker
            {
                Root = rt,
                Fill = disc,
                ZoneId = evt.zoneId,
                Kind = smoke ? "smoke" : "fire",
                ExpireAtRoundTime = evt.roundTime + duration
            });
            RefreshFloorVisibility();
        }

        private void SpawnUtilityBurst(string zoneId, Color color, float size)
        {
            SpawnUtilityBurst(zoneId, color, size, 0.45f);
        }

        private void SpawnUtilityBurst(string zoneId, Color color, float size, float lifetimeSeconds)
        {
            EnsureUtilityMarkerRoot();
            Image disc = CreateUiObject<Image>("UtilBurst", _utilityMarkerRoot);
            disc.raycastTarget = false;
            disc.color = color;
            RectTransform rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            StartCoroutine(DestroyUtilityBurstAfter(rt, lifetimeSeconds));
        }

        private void SpawnHeExplosionBurst(string zoneId)
        {
            SpawnUtilityBurst(zoneId, new Color(1f, 0.92f, 0.35f, 0.75f), 18f, 0.28f);
            SpawnUtilityBurst(zoneId, new Color(0.98f, 0.45f, 0.08f, 0.55f), 34f, 0.45f);
            SpawnUtilityBurst(zoneId, new Color(0.85f, 0.22f, 0.05f, 0.32f), 52f, 0.62f);
        }

        private System.Collections.IEnumerator DestroyUtilityBurstNextFrame(RectTransform rt)
        {
            yield return DestroyUtilityBurstAfter(rt, 0.45f);
        }

        private System.Collections.IEnumerator DestroyUtilityBurstAfter(RectTransform rt, float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, seconds));
            if (rt != null)
                Destroy(rt.gameObject);
        }

        private void EnsureUtilityMarkerRoot()
        {
            if (_utilityMarkerRoot != null || _mapRect == null)
                return;
            GameObject go = new GameObject("UtilityMarkers", typeof(RectTransform));
            _utilityMarkerRoot = go.transform;
            _utilityMarkerRoot.SetParent(_mapRect, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
        }

        private static Color HexColor(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private bool MyTeamIsTerrorist(int roundNumber)
        {
            bool teamAIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));
            if (_session != null)
            {
                string teamAId = _session.State.TeamA.Definition.id;
                bool myIsTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);
                return myIsTeamA ? teamAIsT : !teamAIsT;
            }

            bool naviIsT = teamAIsT;
            bool myIsNavi = myTeamId == "navi_2021";
            return myIsNavi ? naviIsT : !naviIsT;
        }

        private void ApplyMatchBackground()
        {
            if (_bgImage == null)
                return;
            string mapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            int idx = Mathf.Abs(seed) % 4;
            Sprite sprite = LoadMatchBackgroundSprite(mapId, idx);
            if (sprite == null && !string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                sprite = LoadMatchBackgroundSprite("dust2", idx);
            _matchBackgroundIndex = idx;
            _bgImage.sprite = sprite;
            _bgImage.enabled = sprite != null;
            _bgImage.color = sprite != null ? Color.white : Bg;
        }

        private static Sprite LoadMatchBackgroundSprite(string mapId, int idx)
        {
            string path = BackgroundResourcePrefix(mapId) + idx;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                return sprite;
            Sprite[] all = Resources.LoadAll<Sprite>(path);
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private void SyncMatchMarkerVisibility()
        {
            var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_session != null)
            {
                foreach (string id in _session.State.TeamA.Definition.playerIds)
                    active.Add(id);
                foreach (string id in _session.State.TeamB.Definition.playerIds)
                    active.Add(id);
            }
            else
            {
                if (_content.Teams.TryGetValue(myTeamId, out TeamDefinition mine))
                {
                    foreach (string id in mine.playerIds)
                        active.Add(id);
                }

                if (_content.Teams.TryGetValue(_opponentTeamId, out TeamDefinition opponent))
                {
                    foreach (string id in opponent.playerIds)
                        active.Add(id);
                }
            }

            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                pair.Value.gameObject.SetActive(active.Contains(pair.Key));
            RefreshFloorVisibility();
        }

        private void ApplyMapOrientation(int roundNumber, bool animate)
        {
            // Default art has T spawn at bottom. Flip 180 when my team is CT so my side stays bottom.
            bool shouldFlip = !MyTeamIsTerrorist(roundNumber);
            RefreshSeriesPips();
            if (shouldFlip == _mapFlipped && _mapRotator != null)
            {
                RefreshLabelUpright();
                return;
            }

            _mapFlipped = shouldFlip;
            float target = shouldFlip ? 180f : 0f;
            if (_orientRoutine != null)
                StopCoroutine(_orientRoutine);
            if (animate)
                _orientRoutine = StartCoroutine(RotateMap(target));
            else
            {
                _mapRotator.localEulerAngles = new Vector3(0f, 0f, target);
                RefreshLabelUpright();
            }
        }

        private IEnumerator RotateMap(float targetZ)
        {
            float start = _mapRotator.localEulerAngles.z;
            if (start > 180f) start -= 360f;
            if (targetZ > 180f) targetZ -= 360f;
            float elapsed = 0f;
            const float duration = 0.7f;
            while (elapsed < duration)
            {
                if (!_paused)
                    elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                float z = Mathf.LerpAngle(start, targetZ, t);
                _mapRotator.localEulerAngles = new Vector3(0f, 0f, z);
                RefreshLabelUpright();
                yield return null;
            }
            _mapRotator.localEulerAngles = new Vector3(0f, 0f, targetZ);
            RefreshLabelUpright();
            _orientRoutine = null;
        }

        private void RefreshLabelUpright()
        {
            float inverse = -_mapRotator.localEulerAngles.z;
            foreach (Text label in _markerLabels.Values)
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, inverse);
            foreach (Text label in _zoneLabels)
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, inverse);
            if (_bombMarker != null)
            {
                Text bombLabel = _bombMarker.GetComponentInChildren<Text>();
                if (bombLabel != null)
                    bombLabel.rectTransform.localEulerAngles = new Vector3(0f, 0f, inverse);
            }
        }

        private void SelectSide(string teamId)
        {
            if (_careerOfficialMatch)
                return;
            if (myTeamId == teamId)
                return;
            myTeamId = teamId;
            UpdateSideButtons();
            UpdateTeamHeaders();
            UpdateSidePanelColors();
            RefreshStatPanels();
            UpdateScore();
            UpdateAliveCountHud();
            ApplyMapOrientation(_currentRound, true);
            if (_awaitingCard)
                RefreshCardPicker();
            if (_awaitingTacticalCards)
                RefreshTacticalCardPicker();
        }

        private void UpdateSideButtons()
        {
            if (_sideLabel != null)
                _sideLabel.text = $"我方：{DemoZh.TeamName(myTeamId)}";
            if (_naviSideButton != null)
                _naviSideButton.color = myTeamId == "navi_2021" ? Accent : TextPrimary;
            if (_gambitSideButton != null)
                _gambitSideButton.color = myTeamId == "gambit_2021" ? Accent : TextPrimary;
        }

        private void Skip()
        {
            if (IsGeneralBattleMode())
                return;
            if (_skippingMatch)
                return;

            _skippingMatch = true;
            if (_skipButton != null)
                _skipButton.interactable = false;

            StopAllMovement();
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _selectedCard = null;
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            HideRoundWinBanner();

            if (_session != null)
            {
                if (_session.TimeoutActive)
                    _session.EndTimeoutWindow();
                if (_session.TimeoutPending)
                {
                    _session.TryBeginPendingTimeoutWindow();
                    _session.EndTimeoutWindow();
                }
            }

            _playback = StartCoroutine(SkipMatchRoutine());
        }

        private IEnumerator SkipMatchRoutine()
        {
            _skipBusyStartedAt = Time.unscaledTime;
            RefreshSkipBusyHud();
            try
            {
                // Yield each round so the elapsed clock can update.
                yield return null;

                if (_session != null)
                {
                    while (!_session.IsFinished)
                    {
                        if (!_session.AutoPlayOneRound(myTeamId))
                            break;
                        RefreshSkipBusyHud();
                        yield return null;
                    }

                    if (_session.IsComplete && !_session.IsFinished)
                        _session.FinishMatch();
                }

                _result = _session?.Result;
                ApplySkippedMatchPresentation();

                if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)
                    yield return ShowOfficialResultFlow();
            }
            finally
            {
                _skippingMatch = false;
                _playback = null;
                if (_skipButton != null)
                    _skipButton.interactable = _session == null || !_session.IsFinished;
            }
        }

        private void TickSkipBusyHud()
        {
            if (!_skippingMatch)
                return;
            RefreshSkipBusyHud();
        }

        private void RefreshSkipBusyHud()
        {
            float elapsed = Mathf.Max(0f, Time.unscaledTime - _skipBusyStartedAt);
            int totalSec = Mathf.FloorToInt(elapsed);
            int mm = totalSec / 60;
            int ss = totalSec % 60;
            int round = _session != null
                ? Mathf.Max(1, _session.NextRoundNumber)
                : Mathf.Max(1, _currentRound);
            SetRoundHud(round, $"跳過結算中 {mm}:{ss:00}");
        }

        private void ApplySkippedMatchPresentation()
        {
            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();

            if (_result == null)
            {
                UpdateKillFeed();
                UpdateTraitFeed();
                UpdateAliveCountHud();
                UpdateEconomyHud();
                return;
            }

            _naviScore = _result.teamAScore;
            _gambitScore = _result.teamBScore;

            if (_result.playerSummaries != null)
            {
                foreach (PlayerMatchSummary summary in _result.playerSummaries)
                {
                    if (!_stats.TryGetValue(summary.playerId, out PlayerStat stat))
                        continue;
                    stat.Kills = summary.kills;
                    stat.Deaths = summary.deaths;
                    stat.Assists = summary.assists;
                    stat.Alive = true;
                    stat.RoundKills = 0;
                }
            }

            SyncEconomyFromState();
            if (_session != null)
            {
                _teamCards[_session.State.TeamA.Definition.id] = _session.State.TeamA.Equipment;
                _teamCards[_session.State.TeamB.Definition.id] = _session.State.TeamB.Equipment;
            }

            int lastRound = Mathf.Max(
                1,
                _session != null ? _session.State.CompletedRounds : 1);
            _currentRound = lastRound;
            SetRoundHud(lastRound, "比賽結束");
            ApplyMapOrientation(lastRound, false);
            ResetMarkers(lastRound, false);
            HideBomb();
            HideRoundWinBanner();
            UpdateScore();
            RefreshStatPanels();
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateAliveCountHud();
            UpdateEconomyHud();
            UpdateSidePanelColors();
            UpdateTeamHeaders();
        }

        private void TogglePause()
        {
            _paused = !_paused;
            RefreshPauseIcon();
        }

        private void RefreshPauseIcon()
        {
            if (_pauseIcon == null)
                return;
            _pauseIcon.sprite = _paused ? _playbackPlaySprite : _playbackPauseSprite;
            _pauseIcon.color = TextPrimary;
        }

        private void CycleSpeed()
        {
            if (IsGeneralBattleMode())
            {
                CycleGeneralSpeed();
                return;
            }
            playbackSpeed = playbackSpeed < 4f ? 4f : playbackSpeed < 16f ? 16f : playbackSpeed < 64f ? 64f : 1f;
            _speedText.text = $"{playbackSpeed:0}×";
        }

        private bool IsGeneralBattleMode() =>
            _activeBattleSimMode == BattleSimMode.BehaviorTree ||
            (_session != null && _session.BattleSimMode == BattleSimMode.BehaviorTree);

        /// <summary>
        /// General mode often has long idle gaps before timeout; cap waits so playback
        /// does not freeze the UI for ~100s with no events.
        /// </summary>
        private float GeneralModePlaybackWait(float roundTimeGap)
        {
            float speed = Mathf.Max(0.25f, playbackSpeed);
            float gap = Mathf.Max(0f, roundTimeGap);
            if (!IsGeneralBattleMode())
                return gap / speed;

            // Only squash long idle/timeout gaps. Moves chase independently at GeneralChaseSpeed.
            if (gap >= 4f)
                gap = 0.35f;
            else if (gap >= 1.5f)
                gap = Mathf.Lerp(0.85f, 0.35f, Mathf.InverseLerp(1.5f, 4f, gap));
            return gap / speed;
        }

        private void CycleGeneralSpeed()
        {
            if (playbackSpeed < 0.75f)
                playbackSpeed = 1f;
            else if (playbackSpeed < 1.25f)
                playbackSpeed = 1.5f;
            else if (playbackSpeed < 1.75f)
                playbackSpeed = 2f;
            else
                playbackSpeed = 0.5f;
            if (_speedText != null)
                _speedText.text = $"{playbackSpeed:0.0}×";
        }

        private void NewSeed()
        {
            if (_careerOfficialMatch)
                return;
            seed++;
            _matchNumber++;
            GenerateMatch();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            Canvas canvas = CreateUiObject<Canvas>("Canvas", transform);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasRect = canvas.GetComponent<RectTransform>();
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            Image background = CreateUiObject<Image>("Background", canvas.transform);
            Stretch(background.rectTransform);
            background.color = Bg;

            Image bgPhoto = CreateUiObject<Image>("MatchBackgroundPhoto", canvas.transform);
            _bgImage = bgPhoto;
            Stretch(bgPhoto.rectTransform);
            bgPhoto.preserveAspect = false;
            bgPhoto.raycastTarget = false;
            bgPhoto.color = Bg;

            Image bgVignette = CreateUiObject<Image>("BackgroundVignette", canvas.transform);
            Stretch(bgVignette.rectTransform);
            bgVignette.raycastTarget = false;
            bgVignette.color = new Color(0.02f, 0.025f, 0.04f, 0.52f);

            // Map slot — frosted chrome matching control buttons (transparent map letterbox shows through)
            Image mapFrame = CreateUiObject<Image>("MapFrame", canvas.transform);
            _mapFrame = mapFrame.rectTransform;
            _mapFrameImage = mapFrame;
            mapFrame.color = new Color(Panel.r, Panel.g, Panel.b, 0.55f);
            mapFrame.raycastTarget = false;

            Image mapFace = CreateUiObject<Image>("MapFace", mapFrame.transform);
            Stretch(mapFace.rectTransform);
            mapFace.rectTransform.offsetMin = new Vector2(3f, 3f);
            mapFace.rectTransform.offsetMax = new Vector2(-3f, -3f);
            mapFace.color = new Color(Panel.r, Panel.g, Panel.b, 0.28f);
            mapFace.raycastTarget = false;
            if (mapFace.GetComponent<RectMask2D>() == null)
                mapFace.gameObject.AddComponent<RectMask2D>();
            _mapFrameFace = mapFace;

            // L→R: series | logo | round score | ROUND/clock/meta | round score | logo | series
            Image topHud = CreateUiObject<Image>("TopHud", canvas.transform);
            SetRect(topHud.rectTransform, new Vector2(0.28f, 0.886f), new Vector2(0.72f, 0.992f));
            topHud.color = new Color(Panel.r, Panel.g, Panel.b, 0.72f);

            _leftSeriesPips = CreateSeriesPips("LeftSeriesPips", topHud.transform, 0.045f, true);
            _leftTeamLogo = CreateTeamLogo("LeftLogo", topHud.transform, 0.13f);
            _leftScoreText = CreateText("LeftScore", topHud.transform, 42, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftScoreText.rectTransform, new Vector2(0.20f, 0.28f), new Vector2(0.32f, 0.95f));
            _leftScoreText.fontStyle = FontStyle.Bold;
            _leftScoreText.resizeTextForBestFit = true;
            _leftScoreText.resizeTextMinSize = 22;
            _leftScoreText.resizeTextMaxSize = 54;

            _roundText = CreateText("Round", topHud.transform, 17, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.34f, 0.66f), new Vector2(0.66f, 0.96f));
            _roundText.fontStyle = FontStyle.Bold;
            _roundText.verticalOverflow = VerticalWrapMode.Truncate;
            _roundText.text = "ROUND 01";

            // Countdown sits directly under ROUND; keep a dedicated band so meta cannot cover it.
            _clockText = CreateText("Clock", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.64f));
            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockText.verticalOverflow = VerticalWrapMode.Truncate;
            _clockText.fontStyle = FontStyle.Bold;
            _clockText.raycastTarget = false;
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 13, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.32f, 0.02f), new Vector2(0.68f, 0.34f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.verticalOverflow = VerticalWrapMode.Truncate;
            _careerMatchMetaText.resizeTextForBestFit = true;
            _careerMatchMetaText.resizeTextMinSize = 11;
            _careerMatchMetaText.resizeTextMaxSize = 14;
            _careerMatchMetaText.raycastTarget = false;
            _careerMatchMetaText.gameObject.SetActive(false);

            _rightScoreText = CreateText("RightScore", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightScoreText.rectTransform, new Vector2(0.68f, 0.28f), new Vector2(0.80f, 0.95f));
            _rightScoreText.fontStyle = FontStyle.Bold;
            _rightScoreText.resizeTextForBestFit = true;
            _rightScoreText.resizeTextMinSize = 22;
            _rightScoreText.resizeTextMaxSize = 54;
            _rightTeamLogo = CreateTeamLogo("RightLogo", topHud.transform, 0.87f);
            _rightSeriesPips = CreateSeriesPips("RightSeriesPips", topHud.transform, 0.955f, false);

            // Place under TopHud on the canvas so logos / scores cannot cover it.
            CreateAliveCountHud(canvas.transform);

            // Compact top-right kill feed (above equal-height side stats)
            Image killBlock = CreateUiObject<Image>("KillFeed", canvas.transform);
            SetRect(killBlock.rectTransform, new Vector2(0.835f, 0.795f), new Vector2(0.992f, 0.992f));
            killBlock.color = Color.clear;
            killBlock.raycastTarget = false;

            CreateKillFeedRows(killBlock.transform);

            // Top-left trait trigger feed
            Image traitBlock = CreateUiObject<Image>("TraitFeed", canvas.transform);
            SetRect(traitBlock.rectTransform, new Vector2(0.008f, 0.795f), new Vector2(0.165f, 0.992f));
            traitBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);

            Text traitLabel = CreateText("TraitLabel", traitBlock.transform, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(traitLabel.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.96f));
            traitLabel.text = "特性觸發";

            _traitFeedText = CreateText("TraitLines", traitBlock.transform, 12, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_traitFeedText.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.74f));
            _traitFeedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _traitFeedText.verticalOverflow = VerticalWrapMode.Truncate;
            _traitFeedText.lineSpacing = 1.05f;
            _traitFeedText.supportRichText = true;

            // Team headers one row — L: club/money/CP/strategy; R mirrored.
            Font antonFont = _scoreboardFont ?? _uiFont;
            Font tacticsFont = _uiFont ?? _uiFont700;
            Color headerInk = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.CtDark);
            BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                true, out var createLeftHeaderMin, out var createLeftHeaderMax, out var createLeftPanelMin, out var createLeftPanelMax);
            BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                false, out var createRightHeaderMin, out var createRightHeaderMax, out var createRightPanelMin, out var createRightPanelMax);

            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, createLeftHeaderMin, createLeftHeaderMax);
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.CtLight);
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCpText = CreateText("LeftCp", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCardText = CreateText("LeftCard", _leftTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);
            _leftCardText.gameObject.SetActive(false);

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, createRightHeaderMin, createRightHeaderMax);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.TLight);
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightCardText = CreateText("RightCard", _rightTeamHeaderBlock.transform, 12, TextAnchor.MiddleLeft, headerInk, tacticsFont);
            _rightCardText.gameObject.SetActive(false);
            _rightCpText = CreateText("RightCp", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, headerInk, antonFont);

            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _leftTeamHeader, _leftMoneyText, _leftCpText, leftSide: true);
            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _rightTeamHeader, _rightMoneyText, _rightCpText, leftSide: false);

            // Transparent columns so gaps between player cards show the match background.
            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, createLeftPanelMin, createLeftPanelMax);
            leftPanel.color = Color.clear;
            leftPanel.raycastTarget = false;
            _leftStatsPanel = leftPanel;

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, createRightPanelMin, createRightPanelMax);
            rightPanel.color = Color.clear;
            rightPanel.raycastTarget = false;
            _rightStatsPanel = rightPanel;

            CreateStatSlots(leftPanel.transform, true);
            CreateStatSlots(rightPanel.transform, false);

            var rotatorGo = new GameObject("MapRotator", typeof(RectTransform));
            rotatorGo.transform.SetParent(_mapFrameFace != null ? _mapFrameFace.transform : _mapFrame, false);
            _mapRotator = rotatorGo.GetComponent<RectTransform>();
            Stretch(_mapRotator);
            _mapRotator.pivot = new Vector2(0.5f, 0.5f);

            Image map = CreateUiObject<Image>("ActiveMap", _mapRotator);
            _mapImage = map;
            _mapRect = map.rectTransform;
            Stretch(_mapRect);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapSprite = LoadFloorOverviewSprite();
            map.sprite = _mapSprite;
            map.preserveAspect = false;
            map.color = _mapSprite == null
                ? new Color(0.14f, 0.15f, 0.17f, 1f)
                : new Color(0.88f, 0.88f, 0.90f, 1f);

            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();
            RebuildMapCollision();

            // Bottom controls — wider than map so “叫暫停(n)” has room
            Image controlBar = CreateUiObject<Image>("Controls", canvas.transform);
            SetRect(controlBar.rectTransform, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.38f);
            _controlBarRect = controlBar.rectTransform;
            _controlBarImage = controlBar;

            Text title = CreateText("Title", controlBar.transform, 14, TextAnchor.MiddleLeft, TextMuted);
            SetRect(title.rectTransform, new Vector2(0.01f, 0.15f), new Vector2(0.155f, 0.85f));
            title.text = "戰術控制室 · " + (string.IsNullOrEmpty(_careerMapDisplayName) ? "Dust II" : _careerMapDisplayName);
            _controlBarTitle = title;

            _sideLabel = CreateText("SideLabel", controlBar.transform, 13, TextAnchor.MiddleLeft, TextMuted);
            SetRect(_sideLabel.rectTransform, new Vector2(0.155f, 0.15f), new Vector2(0.255f, 0.85f));
            _sideLabel.text = "我方：NAVI";

            Button naviBtn = CreateButton("SideNavi", "NAVI", controlBar.transform,
                new Vector2(0.26f, 0.18f), new Vector2(0.33f, 0.82f), () => SelectSide("navi_2021"));
            _naviSideButtonRoot = naviBtn.gameObject;
            _naviSideButton = naviBtn.GetComponentInChildren<Text>();
            Button gambitBtn = CreateButton("SideGambit", "Gambit", controlBar.transform,
                new Vector2(0.335f, 0.18f), new Vector2(0.425f, 0.82f), () => SelectSide("gambit_2021"));
            _gambitSideButtonRoot = gambitBtn.gameObject;
            _gambitSideButton = gambitBtn.GetComponentInChildren<Text>();

            _matchText = CreateText("Match", controlBar.transform, 13, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
            _matchText.text = $"第 {_matchNumber} 場";

            Button callTimeout = CreateButton("CallTimeout", "叫暫停", controlBar.transform,
                new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f), OnRequestTimeoutClicked);
            _timeoutButtonRect = callTimeout.GetComponent<RectTransform>();
            CaptureButtonChrome(callTimeout, out _timeoutButtonEdge, out _timeoutButtonFace);
            _timeoutCallLabel = callTimeout.GetComponentInChildren<Text>();
            if (_timeoutCallLabel != null)
            {
                _timeoutCallLabel.resizeTextForBestFit = true;
                _timeoutCallLabel.resizeTextMinSize = 11;
                _timeoutCallLabel.resizeTextMaxSize = 15;
            }

            Button pauseBtn = CreateIconButton("Pause", controlBar.transform,
                new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f),
                _playbackPauseSprite, TogglePause, out _pauseIcon);
            _pauseButtonRect = pauseBtn.GetComponent<RectTransform>();
            CaptureButtonChrome(pauseBtn, out _pauseButtonEdge, out _pauseButtonFace);
            RefreshPauseIcon();

            Button speed = CreateButton("Speed", "1×", controlBar.transform,
                new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f), CycleSpeed);
            _speedButtonRect = speed.GetComponent<RectTransform>();
            CaptureButtonChrome(speed, out _speedButtonEdge, out _speedButtonFace);
            _speedText = speed.GetComponentInChildren<Text>();
            if (_speedText != null)
                _speedText.text = $"{playbackSpeed:0}×";

            Button skipBtn = CreateIconButton("Skip", controlBar.transform,
                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),
                _playbackSkipSprite, Skip, out _);
            _skipButton = skipBtn;
            _skipButtonRect = skipBtn.GetComponent<RectTransform>();
            CaptureButtonChrome(skipBtn, out _skipButtonEdge, out _skipButtonFace);
            Button floorBtn = CreateButton("Floor", "", controlBar.transform,
                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f), CycleMapFloor);
            _floorButtonRect = floorBtn.GetComponent<RectTransform>();
            _floorButtonText = floorBtn.GetComponentInChildren<Text>();
            _floorButtonRect.gameObject.SetActive(false);
            Button newSeedBtn = CreateButton("NewSeed", "下一場", controlBar.transform,
                new Vector2(0.86f, 0.15f), new Vector2(0.985f, 0.85f), NewSeed);
            _newSeedButtonRoot = newSeedBtn.gameObject;

            Sprite settingsSprite = Resources.Load<Sprite>("UI/Icons/settings");
            Button settingsBtn = CreateIconButton(
                "Settings",
                canvas.transform,
                new Vector2(0.905f, 0.015f),
                new Vector2(0.985f, 0.100f),
                settingsSprite,
                ToggleSettingsPanel,
                out _);
            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();
            _settingsButtonEdge = settingsBtn.GetComponent<Image>();
            Transform settingsFaceTransform = settingsBtn.transform.Find("Face");
            if (settingsFaceTransform != null)
                _settingsButtonFace = settingsFaceTransform.GetComponent<Image>();
            ApplySettingsFrostedChrome(true);

            CreateMarkers();
            CreateBombMarker();
            CreateZoneLabels();
            CreateZoneTooltip(canvas.transform);
            CreateRoundWinBanner(canvas.transform);
            CreateProfilePopup(canvas.transform);
            ResetStats();
            UpdateSideButtons();
            UpdateTeamHeaders();
            UpdateSidePanelColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
            UpdateKillFeed();
            UpdateTraitFeed();
            CreateCardPicker(canvas.transform);
            CreateTacticalCardPicker(canvas.transform);
            CreateTimeoutPicker(canvas.transform);
            CreateFocusPicker(canvas.transform);
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            BuildSettingsPanel(canvas.transform);
            EnsureOfficialResultOverlay();
            ApplyMatchModeUi();
        }

        private void BuildSettingsPanel(Transform canvas)
        {
            var host = new SettingsPanelHost
            {
                CareerSaveLoadEnabled = false,
                OnReturnToMainMenu = ReturnToCareerFromSettings,
                OnClose = OnSettingsClosed
            };
            _settingsPanel = new SettingsPanelPresentation(_uiFont700 ?? _uiFont, host);
            _settingsPanel.Build(canvas);
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
            _pausedBeforeSettings = _paused;
            if (!_paused)
                TogglePause();
            _settingsPanel.Show();
        }

        private void OnSettingsClosed()
        {
            if (!_pausedBeforeSettings && _paused)
                TogglePause();
        }

        private void HideSettingsPanel()
        {
            if (_settingsPanel == null) return;
            bool wasVisible = _settingsPanel.IsVisible;
            _settingsPanel.Hide();
            if (wasVisible)
                OnSettingsClosed();
        }

        private void ReturnToCareerFromSettings()
        {
            // Abandon in-progress battle without writing an official result.
            OnSettingsClosed();
            StopAllCoroutines();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo_Career");
        }

        private void Update()
        {
            TickSkipBusyHud();
            TickMapCamera();
            TickGeneralChases();
            TickMarkerLos();
            TickMuzzleRays();
            PruneExpiredKillFeed();

            if (Input.GetKeyDown(KeyCode.Space) &&
                (_settingsPanel == null || !_settingsPanel.IsVisible))
            {
                TogglePause();
            }

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanel != null && _settingsPanel.IsVisible)
            {
                HideSettingsPanel();
                return;
            }

            if (_profilePopup != null && _profilePopup.gameObject.activeSelf)
            {
                HideProfilePopup();
                CancelProfileHover();
            }

            if (_zoneTooltip != null && _zoneTooltip.gameObject.activeSelf)
                HideZoneTooltip(_hoveredZoneId);
        }

        private void CreateRoundWinBanner(Transform canvas)
        {
            _roundWinBanner = CreateText("RoundWinBanner", canvas, 110, TextAnchor.MiddleCenter, TText, _uiFontBlack);
            SetRect(_roundWinBanner.rectTransform, new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.64f));
            _roundWinBanner.fontStyle = FontStyle.Normal;
            _roundWinBanner.raycastTarget = false;
            _roundWinBanner.text = string.Empty;
            Outline outline = _roundWinBanner.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.88f);
            outline.effectDistance = new Vector2(3f, -3f);
            _roundWinGroup = _roundWinBanner.gameObject.AddComponent<CanvasGroup>();
            _roundWinGroup.alpha = 0f;
            _roundWinGroup.blocksRaycasts = false;
            _roundWinGroup.interactable = false;
        }

        private void CreateProfilePopup(Transform canvas)
        {
            _profilePopup = CreateUiObject<Image>("PlayerProfilePopup", canvas);
            _profilePopup.color = new Color(Panel.r, Panel.g, Panel.b, 0.97f);
            _profilePopup.raycastTarget = false;
            RectTransform rect = _profilePopup.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(340f, 420f);

            Image edge = CreateUiObject<Image>("Edge", _profilePopup.transform);
            Stretch(edge.rectTransform);
            edge.rectTransform.offsetMin = new Vector2(1f, 1f);
            edge.rectTransform.offsetMax = new Vector2(-1f, -1f);
            edge.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 1f);
            edge.raycastTarget = false;

            Text title = CreateText("Title", edge.transform, 13, TextAnchor.MiddleLeft, Accent);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.995f));
            title.raycastTarget = false;
            title.text = "選手資料";

            _profilePlayerName = CreateText("PlayerId", edge.transform, 22, TextAnchor.MiddleLeft, TextPrimary, _playerIdFont);
            SetRect(_profilePlayerName.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.93f));
            _profilePlayerName.raycastTarget = false;
            _profilePlayerName.supportRichText = false;
            _profilePlayerName.text = string.Empty;

            _profilePopupBody = CreateText("Body", edge.transform, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_profilePopupBody.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.83f));
            _profilePopupBody.raycastTarget = false;
            _profilePopupBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _profilePopupBody.verticalOverflow = VerticalWrapMode.Truncate;
            NarrativeUiText.ApplyBodyStyle(_profilePopupBody);
            _profilePopupBody.supportRichText = true;
            _profilePopupBody.text = string.Empty;

            HideProfilePopup();
        }

        private static void AddPointerTrigger(
            EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void OnStatRowPointerEnter(bool leftSide, int slotIndex)
        {
            string playerId = GetStatRowPlayerId(leftSide, slotIndex);
            if (string.IsNullOrEmpty(playerId))
                return;

            CancelProfileHover();
            HideProfilePopup();
            _hoverProfilePlayerId = playerId;
            _hoverProfileRoutine = StartCoroutine(ShowProfileAfterDelay(playerId));
        }

        private void OnStatRowPointerExit(bool leftSide, int slotIndex)
        {
            CancelProfileHover();
            HideProfilePopup();
        }

        private string GetStatRowPlayerId(bool leftSide, int slotIndex)
        {
            List<string> ids = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            if (slotIndex < 0 || slotIndex >= ids.Count)
                return string.Empty;
            return ids[slotIndex];
        }

        private IEnumerator ShowProfileAfterDelay(string playerId)
        {
            yield return new WaitForSecondsRealtime(ProfileHoverDelaySeconds);
            if (!string.Equals(_hoverProfilePlayerId, playerId, StringComparison.OrdinalIgnoreCase))
                yield break;
            ShowProfilePopup(playerId, Input.mousePosition);
            _hoverProfileRoutine = null;
        }

        private void CancelProfileHover()
        {
            if (_hoverProfileRoutine != null)
            {
                StopCoroutine(_hoverProfileRoutine);
                _hoverProfileRoutine = null;
            }
            _hoverProfilePlayerId = null;
        }

        private void ShowProfilePopup(string playerId, Vector2 screenPosition)
        {
            if (_profilePopup == null || _canvasRect == null || _content == null)
                return;
            if (!_content.Players.TryGetValue(playerId, out PlayerDefinition player) || player == null)
                return;

            if (_profilePlayerName != null)
                _profilePlayerName.text = string.IsNullOrWhiteSpace(player.displayName)
                    ? player.id
                    : player.displayName;
            NarrativeUiText.Set(_profilePopupBody, BuildProfilePopupText(player));
            _profilePopup.gameObject.SetActive(true);
            _profilePopup.transform.SetAsLastSibling();
            PositionProfilePopup(screenPosition);
        }

        private void HideProfilePopup()
        {
            if (_profilePopup != null)
                _profilePopup.gameObject.SetActive(false);
        }

        private void PositionProfilePopup(Vector2 screenPosition)
        {
            RectTransform rect = _profilePopup.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, null, out Vector2 localPoint);

            Vector2 size = rect.sizeDelta;
            Vector2 canvasSize = _canvasRect.rect.size;
            float minX = -canvasSize.x * 0.5f + 8f;
            float maxX = canvasSize.x * 0.5f - size.x - 8f;
            float minY = -canvasSize.y * 0.5f + size.y + 8f;
            float maxY = canvasSize.y * 0.5f - 8f;

            // Pivot is top-left; offset slightly so the cursor isn't covering the title.
            localPoint.x = Mathf.Clamp(localPoint.x + 14f, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y - 10f, minY, maxY);
            rect.anchoredPosition = localPoint;
        }

        private string BuildProfilePopupText(PlayerDefinition player)
        {
            var sb = new StringBuilder(512);
            CombatAttributes a = player.attributes ?? new CombatAttributes();
            int combatTotal = DevelopmentRules.SumCombatAttributes(a);
            sb.AppendLine(combatTotal > 0
                ? $"<color=#EBB438>戰鬥屬性 {combatTotal}</color>"
                : "<color=#EBB438>戰鬥屬性</color>");
            sb.AppendLine($"瞄準  {a.aim}    反應  {a.reaction}");
            sb.AppendLine($"意識  {a.awareness}    道具  {a.utility}");
            sb.AppendLine($"配合  {a.teamwork}    溝通  {a.communication}");
            sb.AppendLine($"首殺  {a.opening}    殘局  {a.clutch}");
            sb.AppendLine();
            sb.AppendLine("<color=#EBB438>特性</color>");
            string[] traitIds = player.traitIds ?? Array.Empty<string>();
            if (traitIds.Length == 0)
            {
                sb.AppendLine("無");
            }
            else
            {
                foreach (string traitId in traitIds)
                {
                    string name = DemoZh.TraitName(traitId);
                    string effect = DemoZh.FormatTraitEffect(traitId);
                    // 特性描述暫未撰寫，先留空欄位。
                    const string description = "";
                    sb.AppendLine($"· {ColoredTraitName(name)}");
                    sb.AppendLine($"  效果：{ColoredTraitEffect(effect)}");
                    sb.AppendLine($"  描述：{description}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("<color=#EBB438>當前 Buff</color>");
            List<string> buffs = CollectActiveBuffs(player.id);
            if (buffs.Count == 0)
                sb.AppendLine("無");
            else
            {
                foreach (string buff in buffs)
                    sb.AppendLine($"· {buff}");
            }

            return sb.ToString().TrimEnd();
        }

        private static string ColoredTraitName(string traitName) =>
            $"<color={TraitNameColor}>{traitName}</color>";

        private static string ColoredTraitEffect(string effect) =>
            $"<color={TraitEffectColor}>{effect}</color>";

        private static string FormatColoredTraitBuff(string traitId, float bonus = 0f, string extra = null)
        {
            string name = ColoredTraitName(DemoZh.TraitName(traitId));
            string effect = ColoredTraitEffect(DemoZh.FormatTraitEffect(traitId, bonus, extra));
            return $"{name}：{effect}";
        }

        private List<string> CollectActiveBuffs(string playerId)
        {
            var buffs = new List<string>();
            PlayerRuntime runtime = FindPlayerRuntime(playerId);
            if (runtime == null)
                return buffs;

            if (runtime.SpecialState)
                buffs.Add(FormatColoredTraitBuff("pyramid_solution"));

            foreach (string state in runtime.States)
            {
                if (string.IsNullOrEmpty(state))
                    continue;
                // Internal one-shot bookkeeping, not a displayable status.
                if (string.Equals(state, "forest_intimidation_team_debuff", StringComparison.OrdinalIgnoreCase))
                    continue;
                buffs.Add(FormatColoredTraitBuff(state));
            }

            if (runtime.TraitStacks > 0)
                buffs.Add(FormatColoredTraitBuff("red_star_child", runtime.TraitStacks * 2f));

            string[] traits = runtime.Definition?.traitIds ?? Array.Empty<string>();
            if (traits.Contains("electronic_tyrant") && runtime.ConsecutiveKillRounds >= 2)
                buffs.Add(FormatColoredTraitBuff("electronic_tyrant"));

            if (traits.Contains("human_emperor") && runtime.RoundKills > 0)
                buffs.Add(FormatColoredTraitBuff("human_emperor"));

            return buffs;
        }

        private PlayerRuntime FindPlayerRuntime(string playerId)
        {
            if (_session?.State == null || string.IsNullOrEmpty(playerId))
                return null;

            foreach (PlayerRuntime player in _session.State.TeamA.Players.Concat(_session.State.TeamB.Players))
            {
                if (string.Equals(player.Definition.id, playerId, StringComparison.OrdinalIgnoreCase))
                    return player;
            }

            return null;
        }

        private void CreateCardPicker(Transform canvas)
        {
            _cardPickerRoot = CreateUiObject<Image>("CardPickerRoot", canvas);
            Stretch(_cardPickerRoot.rectTransform);
            _cardPickerRoot.color = Color.clear;
            _cardPickerRoot.raycastTarget = true;
            _cardPickerRoot.gameObject.SetActive(false);

            _cardPickerBackdrop = CreateUiObject<Image>("Backdrop", _cardPickerRoot.transform);
            Stretch(_cardPickerBackdrop.rectTransform);
            // Soft dim / 虛化：深色半透明遮罩壓暗周圍
            _cardPickerBackdrop.color = new Color(0.02f, 0.025f, 0.035f, 0.72f);
            _cardPickerBackdrop.raycastTarget = true;

            // Extra soft vignette rings for blur-like falloff without a blur shader
            Image vignette = CreateUiObject<Image>("Vignette", _cardPickerRoot.transform);
            Stretch(vignette.rectTransform);
            vignette.color = new Color(0.01f, 0.01f, 0.02f, 0.28f);
            vignette.raycastTarget = false;

            _cardPickerPanel = CreateUiObject<Image>("CardPicker", _cardPickerRoot.transform);
            SetRect(_cardPickerPanel.rectTransform, new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.78f));
            _cardPickerPanel.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
            _cardPickerPanel.raycastTarget = true;

            Image panelEdge = CreateUiObject<Image>("PanelEdge", _cardPickerPanel.transform);
            Stretch(panelEdge.rectTransform);
            panelEdge.rectTransform.offsetMin = new Vector2(-3f, -3f);
            panelEdge.rectTransform.offsetMax = new Vector2(3f, 3f);
            panelEdge.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            panelEdge.raycastTarget = false;
            panelEdge.transform.SetAsFirstSibling();

            _cardPickerTitle = CreateText("CardPickerTitle", _cardPickerPanel.transform, 28, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_cardPickerTitle.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));
            _cardPickerTitle.fontStyle = FontStyle.Bold;
            _cardPickerTitle.text = "選擇經濟卡";

            _cardPickerSubtitle = CreateText("CardPickerSubtitle", _cardPickerPanel.transform, 16, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_cardPickerSubtitle.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.88f));
            _cardPickerSubtitle.text = string.Empty;

            _cardButtons.Clear();
            _cardButtonEdges.Clear();
            _cardButtonFaces.Clear();
            _cardLevelLabels.Clear();
            _cardTitleLabels.Clear();
            _cardCostLabels.Clear();
            _cardDetailLabels.Clear();
            _cardGroups.Clear();

            EquipmentLevel[] levels =
            {
                EquipmentLevel.Eco, EquipmentLevel.HalfBuy, EquipmentLevel.ForceBuy,
                EquipmentLevel.FullBuy, EquipmentLevel.Hero
            };

            const float gap = 0.015f;
            const float totalWidth = 0.90f;
            float cardWidth = (totalWidth - gap * (levels.Length - 1)) / levels.Length;
            float startX = (1f - totalWidth) * 0.5f;

            for (int i = 0; i < levels.Length; i++)
            {
                EquipmentLevel level = levels[i];
                float x0 = startX + i * (cardWidth + gap);
                float x1 = x0 + cardWidth;

                Image edge = CreateUiObject<Image>("EcoCard" + i, _cardPickerPanel.transform);
                SetRect(edge.rectTransform, new Vector2(x0, 0.22f), new Vector2(x1, 0.74f));
                edge.color = new Color(0.55f, 0.48f, 0.32f, 1f);

                CanvasGroup group = edge.gameObject.AddComponent<CanvasGroup>();
                _cardGroups.Add(group);

                Image face = CreateUiObject<Image>("Face", edge.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(3f, 3f);
                face.rectTransform.offsetMax = new Vector2(-3f, -3f);
                face.color = new Color(0.14f, 0.145f, 0.17f, 1f);

                // Inner top accent strip
                Image strip = CreateUiObject<Image>("Strip", face.transform);
                SetRect(strip.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f));
                strip.color = CardAccentColor(level);
                strip.raycastTarget = false;

                Text levelLabel = CreateText("Level", face.transform, 14, TextAnchor.MiddleCenter, TextPrimary);
                SetRect(levelLabel.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f));
                levelLabel.fontStyle = FontStyle.Bold;
                levelLabel.text = "Lv." + (int)level;

                Text title = CreateText("Title", face.transform, 18, TextAnchor.MiddleCenter, TextPrimary);
                SetRect(title.rectTransform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.80f));
                title.fontStyle = FontStyle.Bold;
                title.text = EconomyRules.CardName(level);

                Text cost = CreateText("Cost", face.transform, 20, TextAnchor.MiddleCenter, Accent);
                SetRect(cost.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.58f));
                cost.fontStyle = FontStyle.Bold;
                cost.text = "$0";

                Text detail = CreateText("Detail", face.transform, 15, TextAnchor.UpperCenter, TextMuted);
                SetRect(detail.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.42f));
                detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                detail.verticalOverflow = VerticalWrapMode.Truncate;
                NarrativeUiText.ApplyBodyStyle(detail);
                NarrativeUiText.Set(detail, CardDescription(level));

                Button button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.05f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.82f, 1f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
                button.onClick.AddListener(() => OnEconomyCardChosen(level));

                _cardButtons.Add(button);
                _cardButtonEdges.Add(edge);
                _cardButtonFaces.Add(face);
                _cardLevelLabels.Add(levelLabel);
                _cardTitleLabels.Add(title);
                _cardCostLabels.Add(cost);
                _cardDetailLabels.Add(detail);
            }
        }

        private static Color CardAccentColor(EquipmentLevel level)
        {
            switch (level)
            {
                case EquipmentLevel.Eco: return new Color(0.35f, 0.40f, 0.45f, 1f);
                case EquipmentLevel.HalfBuy: return new Color(0.28f, 0.48f, 0.42f, 1f);
                case EquipmentLevel.ForceBuy: return new Color(0.30f, 0.45f, 0.62f, 1f);
                case EquipmentLevel.FullBuy: return new Color(0.62f, 0.42f, 0.22f, 1f);
                case EquipmentLevel.Hero: return new Color(0.55f, 0.28f, 0.32f, 1f);
                default: return new Color(0.35f, 0.35f, 0.38f, 1f);
            }
        }

        private string CardDescription(EquipmentLevel level)
        {
            if (IsGeneralBattleMode())
            {
                switch (level)
                {
                    case EquipmentLevel.Eco:
                        return "純經濟局\n幾乎不花錢，積蓄下回合全起";
                    case EquipmentLevel.HalfBuy:
                        return "半經濟局\n護甲與強手槍，保住下回合全起";
                    case EquipmentLevel.ForceBuy:
                        return "強起局\n孤注一擲，槍械由選手決定";
                    case EquipmentLevel.FullBuy:
                        return "全起局\n長槍全甲滿道具，槍械由選手決定";
                    case EquipmentLevel.Hero:
                        return "英雄槍局\n指定1至2名選手起關鍵武器";
                    default:
                        return "全隊經濟意圖";
                }
            }
            float mult = EconomyRules.EquipmentMultiplier(level);
            switch (level)
            {
                case EquipmentLevel.Eco:
                    return $"火力 ×{mult:0.00}\n幾乎不購裝";
                case EquipmentLevel.HalfBuy:
                    return $"火力 ×{mult:0.00}\n護甲 + 強手槍";
                case EquipmentLevel.ForceBuy:
                    return $"火力 ×{mult:0.00}\n強起火力";
                case EquipmentLevel.FullBuy:
                    return $"火力 ×{mult:0.00}\n全起滿配";
                case EquipmentLevel.Hero:
                    return $"火力 ×{mult:0.00}\n集中資源起英雄槍";
                default:
                    return $"火力 ×{mult:0.00}";
            }
        }

        private void ShowCardPicker()
        {
            if (_cardPickerRoot == null)
                return;
            RefreshCardPicker();
            _cardPickerRoot.transform.SetAsLastSibling();
            _cardPickerRoot.gameObject.SetActive(true);
            if (_roundText != null && _session != null)
                SetPhaseStatus("選擇經濟卡");
        }

        private void HideCardPicker()
        {
            if (_cardPickerRoot != null)
                _cardPickerRoot.gameObject.SetActive(false);
            else if (_cardPickerPanel != null)
                _cardPickerPanel.gameObject.SetActive(false);
        }

        private void RefreshCardPicker()
        {
            if (_session == null || _cardButtons.Count == 0)
                return;

            TeamRuntime player = _session.TeamById(myTeamId);
            int money = player.Money;
            bool hasAwper = player.Players.Any(x => x.Definition.primaryRole == "Awper");
            if (_cardPickerTitle != null)
                _cardPickerTitle.text = "選擇經濟卡";
            if (_cardPickerSubtitle != null)
                _cardPickerSubtitle.text = $"{DemoZh.TeamName(myTeamId)} 可用 ${money:N0}";

            EquipmentLevel[] levels =
            {
                EquipmentLevel.Eco, EquipmentLevel.HalfBuy, EquipmentLevel.ForceBuy,
                EquipmentLevel.FullBuy, EquipmentLevel.Hero
            };
            for (int i = 0; i < levels.Length && i < _cardButtons.Count; i++)
            {
                EquipmentLevel level = levels[i];
                bool heroNeedsPick = level == EquipmentLevel.Hero && IsGeneralBattleMode();
                int cost;
                bool affordable;
                if (IsGeneralBattleMode() && _weaponCatalog != null)
                {
                    if (level == EquipmentLevel.Hero && player.HeroBuyPlayerIds.Count == 0)
                        BehaviorTreeBuyRules.SetHeroTargets(
                            player, BehaviorTreeBuyRules.SuggestHeroTargets(player));
                    cost = EconomyCardLoadoutRules.EstimateTeamCardCost(
                        player, level, _weaponCatalog);
                    affordable = EconomyCardLoadoutRules.CanAffordCard(
                        player, level, _weaponCatalog);
                }
                else
                {
                    cost = EconomyRules.EquipmentCost(level);
                    affordable = EconomyRules.CanAfford(money, level);
                }
                _cardButtons[i].interactable = affordable;

                if (i < _cardCostLabels.Count && _cardCostLabels[i] != null)
                {
                    _cardCostLabels[i].text = cost == 0 ? "$0" : $"${cost:N0}";
                    _cardCostLabels[i].color = affordable ? Accent : TextMuted;
                }
                if (i < _cardTitleLabels.Count && _cardTitleLabels[i] != null)
                    _cardTitleLabels[i].color = affordable ? TextPrimary : TextMuted;
                if (i < _cardDetailLabels.Count && _cardDetailLabels[i] != null)
                {
                    string status = !affordable ? "金錢不足" : heroNeedsPick ? "需指定選手" : string.Empty;
                    string body = CardDescription(level);
                    NarrativeUiText.Set(_cardDetailLabels[i], string.IsNullOrEmpty(status) ? body : $"{body}\n{status}");
                    _cardDetailLabels[i].color = affordable ? TextMuted : new Color(0.55f, 0.42f, 0.42f, 1f);
                }
                if (i < _cardButtonEdges.Count && _cardButtonEdges[i] != null)
                    _cardButtonEdges[i].color = affordable
                        ? new Color(0.72f, 0.58f, 0.28f, 1f)
                        : new Color(0.28f, 0.30f, 0.33f, 1f);
                if (i < _cardButtonFaces.Count && _cardButtonFaces[i] != null)
                    _cardButtonFaces[i].color = affordable
                        ? new Color(0.14f, 0.145f, 0.17f, 1f)
                        : new Color(0.09f, 0.095f, 0.11f, 1f);
                if (i < _cardGroups.Count && _cardGroups[i] != null)
                    _cardGroups[i].alpha = affordable ? 1f : 0.48f;
            }
        }

        private void OnEconomyCardChosen(EquipmentLevel level)
        {
            if (!_awaitingCard || _session == null)
                return;
            TeamRuntime player = _session.TeamById(myTeamId);
            if (IsGeneralBattleMode() && _weaponCatalog != null)
            {
                if (level == EquipmentLevel.Hero && player.HeroBuyPlayerIds.Count == 0)
                    BehaviorTreeBuyRules.SetHeroTargets(
                        player, BehaviorTreeBuyRules.SuggestHeroTargets(player));
                if (!EconomyCardLoadoutRules.CanAffordCard(player, level, _weaponCatalog))
                    return;
            }
            else if (!EconomyRules.CanAfford(player.Money, level))
                return;

            if (IsGeneralBattleMode() && level == EquipmentLevel.Hero)
            {
                _pendingEconomyCard = level;
                _awaitingHeroPick = true;
                if (_heroPickIds.Count == 0)
                {
                    _heroPickIds.Clear();
                    _heroPickIds.AddRange(BehaviorTreeBuyRules.SuggestHeroTargets(player));
                }
                EnsureHeroPickRow();
                RefreshHeroPickRow();
                if (_cardPickerSubtitle != null)
                    _cardPickerSubtitle.text = "點選 1 至 2 名選手後按確認";
                return;
            }

            _heroPickIds.Clear();
            HideHeroPickRow();
            _pendingEconomyCard = null;
            _awaitingHeroPick = false;
            _selectedCard = level;
        }

        private void EnsureHeroPickRow()
        {
            if (_heroPickRow != null || _cardPickerPanel == null)
            {
                if (_heroPickRow != null)
                    _heroPickRow.SetActive(true);
                return;
            }

            _heroPickRow = new GameObject("HeroPickRow", typeof(RectTransform));
            _heroPickRow.transform.SetParent(_cardPickerPanel.transform, false);
            var rowRt = _heroPickRow.GetComponent<RectTransform>();
            SetRect(rowRt, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.20f));

            _heroPickHint = CreateUiObject<Text>("HeroHint", _heroPickRow.transform);
            SetRect(_heroPickHint.rectTransform, new Vector2(0f, 0.62f), new Vector2(0.70f, 1f));
            _heroPickHint.font = _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            _heroPickHint.fontSize = 16;
            _heroPickHint.alignment = TextAnchor.MiddleLeft;
            _heroPickHint.color = TextMuted;
            _heroPickHint.text = "英雄槍選手";

            TeamRuntime team = _session != null ? _session.TeamById(myTeamId) : null;
            int n = team != null ? team.Players.Count : 5;
            for (int i = 0; i < n; i++)
            {
                Image edge = CreateUiObject<Image>("HeroPick" + i, _heroPickRow.transform);
                float x0 = 0.02f + i * 0.14f;
                SetRect(edge.rectTransform, new Vector2(x0, 0.06f), new Vector2(x0 + 0.13f, 0.56f));
                edge.color = new Color(0.72f, 0.58f, 0.28f, 1f);
                Image face = CreateUiObject<Image>("Face", edge.transform);
                SetRect(face.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
                face.color = new Color(0.14f, 0.145f, 0.17f, 1f);
                Text label = CreateUiObject<Text>("Label", face.transform);
                Stretch(label.rectTransform);
                label.font = _heroPickHint.font;
                label.fontSize = 14;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = TextPrimary;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                Button btn = edge.gameObject.AddComponent<Button>();
                btn.targetGraphic = face;
                int idx = i;
                btn.onClick.AddListener(() => ToggleHeroPick(idx));
                _heroPickButtons.Add(btn);
                _heroPickLabels.Add(label);
            }

            Image confEdge = CreateUiObject<Image>("HeroConfirm", _heroPickRow.transform);
            SetRect(confEdge.rectTransform, new Vector2(0.74f, 0.06f), new Vector2(0.88f, 0.56f));
            confEdge.color = new Color(0.72f, 0.58f, 0.28f, 1f);
            Image confFace = CreateUiObject<Image>("Face", confEdge.transform);
            SetRect(confFace.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            confFace.color = new Color(0.18f, 0.22f, 0.16f, 1f);
            Text confLabel = CreateUiObject<Text>("Label", confFace.transform);
            Stretch(confLabel.rectTransform);
            confLabel.font = _heroPickHint.font;
            confLabel.fontSize = 16;
            confLabel.alignment = TextAnchor.MiddleCenter;
            confLabel.color = TextPrimary;
            confLabel.text = "確認";
            _heroConfirmButton = confEdge.gameObject.AddComponent<Button>();
            _heroConfirmButton.targetGraphic = confFace;
            _heroConfirmButton.onClick.AddListener(ConfirmHeroPick);
        }

        private void HideHeroPickRow()
        {
            if (_heroPickRow != null)
                _heroPickRow.SetActive(false);
        }

        private void RefreshHeroPickRow()
        {
            if (_session == null)
                return;
            EnsureHeroPickRow();
            TeamRuntime team = _session.TeamById(myTeamId);
            for (int i = 0; i < _heroPickLabels.Count && i < team.Players.Count; i++)
            {
                PlayerRuntime p = team.Players[i];
                string id = p.Definition.id;
                bool on = _heroPickIds.Any(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
                _heroPickLabels[i].text = DemoZh.PlayerName(id);
                _heroPickButtons[i].targetGraphic.color = on
                    ? new Color(0.28f, 0.32f, 0.20f, 1f)
                    : new Color(0.14f, 0.145f, 0.17f, 1f);
            }
            if (_heroConfirmButton != null)
                _heroConfirmButton.interactable = _heroPickIds.Count >= 1 && _heroPickIds.Count <= 2;
        }

        private void ToggleHeroPick(int index)
        {
            if (!_awaitingHeroPick || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (index < 0 || index >= team.Players.Count)
                return;
            string id = team.Players[index].Definition.id;
            int at = _heroPickIds.FindIndex(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
            if (at >= 0)
                _heroPickIds.RemoveAt(at);
            else if (_heroPickIds.Count < 2)
                _heroPickIds.Add(id);
            RefreshHeroPickRow();
        }

        private void ConfirmHeroPick()
        {
            if (!_awaitingHeroPick || _pendingEconomyCard == null || _heroPickIds.Count < 1)
                return;
            TeamRuntime player = _session.TeamById(myTeamId);
            BehaviorTreeBuyRules.SetHeroTargets(player, _heroPickIds);
            if (_weaponCatalog != null &&
                !EconomyCardLoadoutRules.CanAffordCard(player, _pendingEconomyCard.Value, _weaponCatalog))
                return;
            _selectedCard = _pendingEconomyCard;
            _pendingEconomyCard = null;
            _awaitingHeroPick = false;
        }


        private void CreateTacticalCardPicker(Transform canvas)
        {
            _tacticalPickerRoot = CreateUiObject<Image>("TacticalPickerRoot", canvas);
            Stretch(_tacticalPickerRoot.rectTransform);
            _tacticalPickerRoot.color = TacticalRootNormal;
            _tacticalPickerRoot.raycastTarget = true;

            _tacticalPickerPanel = CreateUiObject<Image>("TacticalPicker", _tacticalPickerRoot.transform);
            SetRect(_tacticalPickerPanel.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            _tacticalPickerPanel.color = TacticalPanelNormal;

            Text title = CreateText("Title", _tacticalPickerPanel.transform, 30, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.99f));
            title.fontStyle = FontStyle.Bold;
            title.text = "選擇戰術卡";

            _tacticalPickerSubtitle = CreateText(
                "Subtitle", _tacticalPickerPanel.transform, 18, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_tacticalPickerSubtitle.rectTransform, new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.92f));

            _tacticalAnalysis = CreateText(
                "Analysis", _tacticalPickerPanel.transform, 15, TextAnchor.MiddleLeft, TextMuted);
            SetRect(_tacticalAnalysis.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.72f, 0.10f));

            _tacticalButtons.Clear();
            _tacticalCardEdges.Clear();
            _tacticalCardFaces.Clear();
            _tacticalCardStrips.Clear();
            _tacticalScopeLabels.Clear();
            _tacticalTitleLabels.Clear();
            _tacticalCostLabels.Clear();
            _tacticalStatLabels.Clear();
            _tacticalDetailLabels.Clear();
            _tacticalCardGroups.Clear();

            // 6×2 portrait slots so cards read as paper cards, not wide buttons.
            const int columns = 6;
            const int rows = 2;
            const float left = 0.03f;
            const float right = 0.97f;
            const float bottom = 0.125f;
            const float top = 0.83f;
            const float gapX = 0.012f;
            const float gapY = 0.024f;
            float width = (right - left - gapX * (columns - 1)) / columns;
            float height = (top - bottom - gapY * (rows - 1)) / rows;
            for (int i = 0; i < 12; i++)
            {
                int slot = i;
                int row = i / columns;
                int column = i % columns;
                float x0 = left + column * (width + gapX);
                float y1 = top - row * (height + gapY);
                float y0 = y1 - height;

                Image shadow = CreateUiObject<Image>("Shadow" + i, _tacticalPickerPanel.transform);
                SetRect(shadow.rectTransform,
                    new Vector2(x0 + 0.003f, y0 - 0.012f),
                    new Vector2(x0 + width + 0.003f, y1 - 0.012f));
                shadow.color = new Color(0f, 0f, 0f, 0.40f);
                shadow.raycastTarget = false;

                Image frame = CreateUiObject<Image>("TacticalCard" + i, _tacticalPickerPanel.transform);
                SetRect(frame.rectTransform, new Vector2(x0, y0), new Vector2(x0 + width, y1));
                frame.color = UniversalCardEdge;

                CanvasGroup group = frame.gameObject.AddComponent<CanvasGroup>();
                _tacticalCardGroups.Add(group);

                Image face = CreateUiObject<Image>("Face", frame.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(3f, 3f);
                face.rectTransform.offsetMax = new Vector2(-3f, -3f);
                face.color = new Color(0.12f, 0.13f, 0.155f, 1f);

                Image matte = CreateUiObject<Image>("Matte", face.transform);
                Stretch(matte.rectTransform);
                matte.rectTransform.offsetMin = new Vector2(4f, 4f);
                matte.rectTransform.offsetMax = new Vector2(-4f, -4f);
                matte.color = new Color(0.095f, 0.10f, 0.12f, 1f);
                matte.raycastTarget = false;

                Image strip = CreateUiObject<Image>("Strip", matte.transform);
                SetRect(strip.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f));
                strip.color = UniversalCardStrip;
                strip.raycastTarget = false;

                Text scope = CreateText("Scope", strip.transform, 14, TextAnchor.MiddleCenter, TextPrimary);
                Stretch(scope.rectTransform);
                scope.fontStyle = FontStyle.Bold;
                scope.raycastTarget = false;
                scope.text = "通用";

                Text name = CreateText("Title", matte.transform, 18, TextAnchor.MiddleCenter, TraitNameUi);
                SetRect(name.rectTransform, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.82f));
                name.fontStyle = FontStyle.Bold;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.supportRichText = true;
                name.raycastTarget = false;
                name.text = "戰術卡";

                Text cost = CreateText("Cost", matte.transform, 15, TextAnchor.MiddleCenter, Accent);
                SetRect(cost.rectTransform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.68f));
                cost.fontStyle = FontStyle.Bold;
                cost.supportRichText = true;
                cost.raycastTarget = false;
                cost.text = "0 CP";

                Image divider = CreateUiObject<Image>("Divider", matte.transform);
                SetRect(divider.rectTransform, new Vector2(0.16f, 0.565f), new Vector2(0.84f, 0.575f));
                divider.color = new Color(1f, 1f, 1f, 0.10f);
                divider.raycastTarget = false;

                Text stats = CreateText("Stats", matte.transform, 12, TextAnchor.UpperCenter, TraitEffectUi);
                SetRect(stats.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.56f));
                stats.fontStyle = FontStyle.Bold;
                stats.horizontalOverflow = HorizontalWrapMode.Wrap;
                stats.verticalOverflow = VerticalWrapMode.Truncate;
                stats.lineSpacing = 1.02f;
                stats.supportRichText = true;
                stats.raycastTarget = false;
                stats.text = "風險 0";

                Text detail = CreateText("Detail", matte.transform, 12, TextAnchor.UpperCenter, TextMuted);
                SetRect(detail.rectTransform, new Vector2(0.07f, 0.03f), new Vector2(0.93f, 0.21f));
                detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                detail.verticalOverflow = VerticalWrapMode.Truncate;
                NarrativeUiText.ApplyBodyStyle(detail);
                detail.raycastTarget = false;
                detail.text = string.Empty;

                // Keep clickability without ColorTint so presses never tint like UI buttons.
                Button button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnTacticalCardClicked(slot));

                _tacticalButtons.Add(button);
                _tacticalCardEdges.Add(frame);
                _tacticalCardFaces.Add(face);
                _tacticalCardStrips.Add(strip);
                _tacticalScopeLabels.Add(scope);
                _tacticalTitleLabels.Add(name);
                _tacticalCostLabels.Add(cost);
                _tacticalStatLabels.Add(stats);
                _tacticalDetailLabels.Add(detail);
            }

            CreateButton(
                "ConfirmTactics", "鎖定戰術", _tacticalPickerPanel.transform,
                new Vector2(0.76f, 0.025f), new Vector2(0.96f, 0.10f),
                ConfirmTacticalCards);
        }

        private void ShowTacticalCardPicker()
        {
            if (_tacticalPickerRoot == null)
                return;
            RefreshTacticalCardPicker();
            _tacticalPickerRoot.transform.SetAsLastSibling();
            _tacticalPickerRoot.gameObject.SetActive(true);
            if (_roundText != null && _session != null)
                SetPhaseStatus("選擇戰術卡");
        }

        private void HideTacticalCardPicker()
        {
            if (_tacticalPickerRoot != null)
                _tacticalPickerRoot.gameObject.SetActive(false);
        }

        private void CreateTimeoutPicker(Transform canvas)
        {
            _timeoutPickerRoot = CreateUiObject<Image>("TimeoutPickerRoot", canvas);
            Stretch(_timeoutPickerRoot.rectTransform);
            _timeoutPickerRoot.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
            _timeoutPickerRoot.raycastTarget = true;

            Image panel = CreateUiObject<Image>("TimeoutPanel", _timeoutPickerRoot.transform);
            SetRect(panel.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            panel.color = new Color(0.08f, 0.09f, 0.11f, 0.98f);

            Text title = CreateText("Title", panel.transform, 28, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.fontStyle = FontStyle.Bold;
            title.raycastTarget = false;
            title.text = "教練暫停";

            _timeoutSubtitle = CreateText("Subtitle", panel.transform, 16, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_timeoutSubtitle.rectTransform, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.90f));
            _timeoutSubtitle.raycastTarget = false;

            _timeoutBody = CreateText("Body", panel.transform, 14, TextAnchor.UpperLeft, TextMuted);
            SetRect(_timeoutBody.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.83f));
            _timeoutBody.raycastTarget = false;
            _timeoutBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            NarrativeUiText.ApplyBodyStyle(_timeoutBody);
            NarrativeUiText.Set(_timeoutBody, "全體手感 −5。點擊教練卡選擇／取消；基礎卡可多選，付費卡限一張。按「結束暫停」才套用。");

            _coachButtons.Clear();
            _coachEdges.Clear();
            _coachTitles.Clear();
            _coachDetails.Clear();
            const int columns = 3;
            const int rows = 3;
            for (int i = 0; i < columns * rows; i++)
            {
                int slot = i;
                int row = i / columns;
                int col = i % columns;
                float x0 = 0.05f + col * 0.31f;
                float y1 = 0.72f - row * 0.18f;
                float y0 = y1 - 0.16f;
                Image edge = CreateUiObject<Image>("Coach" + i, panel.transform);
                SetRect(edge.rectTransform, new Vector2(x0, y0), new Vector2(x0 + 0.29f, y1));
                edge.color = UniversalCardEdge;
                edge.raycastTarget = true;
                Image face = CreateUiObject<Image>("Face", edge.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(2f, 2f);
                face.rectTransform.offsetMax = new Vector2(-2f, -2f);
                face.color = new Color(0.12f, 0.13f, 0.155f, 1f);
                face.raycastTarget = true;
                Text name = CreateText("Title", face.transform, 15, TextAnchor.MiddleCenter, TraitNameUi);
                SetRect(name.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f));
                name.fontStyle = FontStyle.Bold;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.raycastTarget = false;
                Text detail = CreateText("Detail", face.transform, 12, TextAnchor.UpperCenter, TextMuted);
                SetRect(detail.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.55f));
                detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                detail.verticalOverflow = VerticalWrapMode.Truncate;
                detail.raycastTarget = false;
                NarrativeUiText.ApplyBodyStyle(detail);
                Button button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
                button.onClick.AddListener(() => OnCoachCardClicked(slot));
                _coachButtons.Add(button);
                _coachEdges.Add(edge);
                _coachTitles.Add(name);
                _coachDetails.Add(detail);
            }

            CreateButton("SkipTimeout", "跳過", panel.transform,
                new Vector2(0.05f, 0.03f), new Vector2(0.32f, 0.11f), OnSkipTimeoutClicked);
            CreateButton("ConfirmTimeout", "結束暫停", panel.transform,
                new Vector2(0.68f, 0.03f), new Vector2(0.95f, 0.11f), OnConfirmTimeoutClicked);
        }

        private void CreateFocusPicker(Transform canvas)
        {
            _focusPickerRoot = CreateUiObject<Image>("FocusPickerRoot", canvas);
            Stretch(_focusPickerRoot.rectTransform);
            _focusPickerRoot.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
            _focusPickerRoot.raycastTarget = true;

            Image panel = CreateUiObject<Image>("FocusPanel", _focusPickerRoot.transform);
            SetRect(panel.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
            panel.color = new Color(0.08f, 0.09f, 0.11f, 0.98f);

            Text title = CreateText("Title", panel.transform, 30, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.96f, 0.99f));
            title.fontStyle = FontStyle.Bold;
            title.text = "選擇聚焦卡";

            _focusSubtitle = CreateText("Subtitle", panel.transform, 18, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_focusSubtitle.rectTransform, new Vector2(0.04f, 0.845f), new Vector2(0.96f, 0.915f));

            _focusButtons.Clear();
            _focusEdges.Clear();
            _focusTitles.Clear();
            _focusCostLabels.Clear();
            _focusStatLabels.Clear();
            _focusDetails.Clear();
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                float x0 = 0.03f + i * 0.242f;
                Image edge = CreateUiObject<Image>("Focus" + i, panel.transform);
                SetRect(edge.rectTransform, new Vector2(x0, 0.28f), new Vector2(x0 + 0.225f, 0.835f));
                edge.color = ExclusiveCardEdge;

                Image face = CreateUiObject<Image>("Face", edge.transform);
                Stretch(face.rectTransform);
                face.rectTransform.offsetMin = new Vector2(3f, 3f);
                face.rectTransform.offsetMax = new Vector2(-3f, -3f);
                face.color = new Color(0.14f, 0.11f, 0.09f, 1f);

                Image matte = CreateUiObject<Image>("Matte", face.transform);
                Stretch(matte.rectTransform);
                matte.rectTransform.offsetMin = new Vector2(4f, 4f);
                matte.rectTransform.offsetMax = new Vector2(-4f, -4f);
                matte.color = new Color(0.10f, 0.085f, 0.07f, 1f);
                matte.raycastTarget = false;

                Image strip = CreateUiObject<Image>("Strip", matte.transform);
                SetRect(strip.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f));
                strip.color = ExclusiveCardStrip;
                strip.raycastTarget = false;
                Text scope = CreateText("Scope", strip.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
                Stretch(scope.rectTransform);
                scope.fontStyle = FontStyle.Bold;
                scope.raycastTarget = false;
                scope.text = "專屬";

                Text name = CreateText("Title", matte.transform, 20, TextAnchor.MiddleCenter, TraitNameUi);
                SetRect(name.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.87f));
                name.fontStyle = FontStyle.Bold;
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.supportRichText = true;
                name.raycastTarget = false;

                Text cost = CreateText("Cost", matte.transform, 17, TextAnchor.MiddleCenter, Accent);
                SetRect(cost.rectTransform, new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.72f));
                cost.fontStyle = FontStyle.Bold;
                cost.raycastTarget = false;

                Image divider = CreateUiObject<Image>("Divider", matte.transform);
                SetRect(divider.rectTransform, new Vector2(0.14f, 0.615f), new Vector2(0.86f, 0.622f));
                divider.color = new Color(1f, 1f, 1f, 0.10f);
                divider.raycastTarget = false;

                Text stats = CreateText("Stats", matte.transform, 15, TextAnchor.UpperCenter, TraitEffectUi);
                SetRect(stats.rectTransform, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.61f));
                stats.fontStyle = FontStyle.Bold;
                stats.horizontalOverflow = HorizontalWrapMode.Wrap;
                stats.verticalOverflow = VerticalWrapMode.Truncate;
                stats.lineSpacing = 1.06f;
                stats.supportRichText = true;
                stats.raycastTarget = false;

                Text detail = CreateText("Detail", matte.transform, 14, TextAnchor.UpperCenter, TextMuted);
                SetRect(detail.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.27f));
                detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                detail.verticalOverflow = VerticalWrapMode.Truncate;
                NarrativeUiText.ApplyBodyStyle(detail);
                detail.raycastTarget = false;

                Button button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnFocusCardClicked(slot));
                _focusButtons.Add(button);
                _focusEdges.Add(edge);
                _focusTitles.Add(name);
                _focusCostLabels.Add(cost);
                _focusStatLabels.Add(stats);
                _focusDetails.Add(detail);
            }

            _focusTargetButtons.Clear();
            _focusTargetLabels.Clear();
            _focusTargetHint = CreateText("TargetHint", panel.transform, 15, TextAnchor.MiddleLeft, TextMuted);
            SetRect(_focusTargetHint.rectTransform, new Vector2(0.04f, 0.195f), new Vector2(0.22f, 0.265f));
            _focusTargetHint.text = "指定對象";
            _focusTargetHint.gameObject.SetActive(false);
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                float x0 = 0.22f + i * 0.145f;
                Image edge = CreateUiObject<Image>("FocusTarget" + i, panel.transform);
                SetRect(edge.rectTransform, new Vector2(x0, 0.18f), new Vector2(x0 + 0.13f, 0.27f));
                edge.color = ButtonEdge;
                Text label = CreateText("Label", edge.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
                Stretch(label.rectTransform);
                label.fontStyle = FontStyle.Bold;
                Button button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = edge;
                button.onClick.AddListener(() => OnFocusTargetClicked(slot));
                _focusTargetButtons.Add(button);
                _focusTargetLabels.Add(label);
                edge.gameObject.SetActive(false);
            }

            CreateButton("ConfirmFocus", "鎖定聚焦", panel.transform,
                new Vector2(0.72f, 0.035f), new Vector2(0.96f, 0.14f), OnConfirmFocusClicked);
        }

        private static string FormatFocusStats(FocusCardDefinition card)
        {
            var lines = new List<string>(8);
            void AddPct(string label, float value)
            {
                if (value > 0.0001f)
                    lines.Add($"{label} +{value:P0}");
            }
            void AddVal(string label, float value)
            {
                if (Mathf.Abs(value) > 0.0001f)
                    lines.Add($"{label} {(value >= 0f ? "+" : string.Empty)}{value:0}");
            }

            AddPct("全屬性", card.allAttributeBonus);
            AddPct("瞄準", card.aimBonus);
            AddPct("反應", card.reactionBonus);
            AddPct("意識", card.awarenessBonus);
            AddPct("道具", card.utilityBonus);
            AddPct("配合", card.teamworkBonus);
            AddPct("溝通", card.communicationBonus);
            AddPct("首殺", card.openingBonus);
            AddPct("殘局", card.clutchBonus);
            AddVal("心態恢復", card.mentalityRestore);
            AddPct("同區配合", card.duoTeamworkBonus);

            if (!string.IsNullOrEmpty(card.specialRuleKey))
            {
                string special = card.specialRuleKey switch
                {
                    "s1mple_focus_death_penalty" => "陣亡：全隊心態-12 手感-10",
                    "gemini_same_zone_link" => "雙核同區觸發連線",
                    "b1t_growth_diminishing" => "可重複，效果遞減",
                    "perfecto_cleanse_clutch" => "清負面·殘局強化",
                    "system_team_boost" => "全隊體系強化",
                    "sh1ro_fog_of_war" => "位置隱蔽·首殺提升",
                    "hobbit_legacy_transfer" => "指定年輕選手接傳承",
                    "interz_support_aura" => "同區隊友失誤↓",
                    _ => card.specialRuleKey
                };
                lines.Add(special);
            }

            if (lines.Count == 0)
                return "無額外數值";

            // Pack into ~2 items per line for readability.
            var packed = new List<string>();
            for (int i = 0; i < lines.Count; i += 2)
            {
                if (i + 1 < lines.Count && lines[i].Length + lines[i + 1].Length < 22)
                    packed.Add(lines[i] + "  " + lines[i + 1]);
                else
                {
                    packed.Add(lines[i]);
                    if (i + 1 < lines.Count)
                        packed.Add(lines[i + 1]);
                }
            }
            return string.Join("\n", packed.Select(l => $"<color={TraitEffectColor}>{l}</color>"));
        }

        private void ShowTimeoutPicker()
        {
            if (_timeoutPickerRoot == null)
                return;
            CancelProfileHover();
            HideProfilePopup();
            _selectedCoachIds.Clear();
            _coachPickTargets.Clear();
            RefreshTimeoutPicker();
            _timeoutPickerRoot.gameObject.SetActive(true);
            _timeoutPickerRoot.transform.SetAsLastSibling();
            if (_roundText != null && _session != null)
                SetPhaseStatus("教練暫停");
        }

        private void HideTimeoutPicker()
        {
            if (_timeoutPickerRoot != null)
                _timeoutPickerRoot.gameObject.SetActive(false);
            _selectedCoachIds.Clear();
            _coachPickTargets.Clear();
        }

        private void RefreshTimeoutPicker()
        {
            if (_session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            int selectedCost = SelectedCoachCommandCost();
            _timeoutSubtitle.text =
                $"剩餘暫停 {team.TimeoutsRemaining} · CP {team.CommandPoints}（已選佔 {selectedCost}） · " +
                $"已選 {_selectedCoachIds.Count}/{TimeoutCoachRules.MaximumCoachCardsPerTimeout} · 全體手感 −5";
            _visibleCoachIds.Clear();
            _visibleCoachIds.AddRange(_content.CoachCards.Values
                .Where(c => c.teamScope == "universal" ||
                            string.Equals(c.teamScope, team.Definition.id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.isBaseCoach ? 0 : 1)
                .ThenBy(c => c.commandCost)
                .Select(c => c.id));
            for (int i = 0; i < _coachButtons.Count; i++)
            {
                bool visible = i < _visibleCoachIds.Count;
                _coachButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                string id = _visibleCoachIds[i];
                CoachCardDefinition card = _content.CoachCards[id];
                bool selected = _selectedCoachIds.Contains(id);
                bool canSelect = selected || CanSelectCoachCard(team, card);
                bool exclusive = card.teamScope != "universal";
                Image face = _coachButtons[i].targetGraphic as Image;

                if (selected)
                {
                    _coachEdges[i].color = Accent;
                    if (face != null)
                        face.color = new Color(0.16f, 0.145f, 0.11f, 1f);
                    _coachTitles[i].text = card.displayName + "（已選）";
                    _coachTitles[i].color = Accent;
                    NarrativeUiText.Set(_coachDetails[i], "再點一次取消選擇");
                    _coachDetails[i].color = Accent;
                    _coachButtons[i].interactable = true;
                }
                else if (canSelect)
                {
                    _coachEdges[i].color = exclusive ? ExclusiveCardEdge : UniversalCardEdge;
                    if (face != null)
                        face.color = new Color(0.12f, 0.13f, 0.155f, 1f);
                    _coachTitles[i].text = card.displayName + (card.isBaseCoach ? "（0）" : $"（{card.commandCost}）");
                    _coachTitles[i].color = TraitNameUi;
                    NarrativeUiText.Set(_coachDetails[i], card.description);
                    _coachDetails[i].color = TextMuted;
                    _coachButtons[i].interactable = true;
                }
                else
                {
                    _coachEdges[i].color = new Color(0.35f, 0.35f, 0.38f, 1f);
                    if (face != null)
                        face.color = new Color(0.10f, 0.10f, 0.12f, 1f);
                    _coachTitles[i].text = card.displayName + (card.isBaseCoach ? "（0）" : $"（{card.commandCost}）");
                    _coachTitles[i].color = TextMuted;
                    NarrativeUiText.Set(_coachDetails[i], CoachUnavailableReason(team, card));
                    _coachDetails[i].color = OverflowWarn;
                    _coachButtons[i].interactable = false;
                }
            }
        }

        private int SelectedCoachCommandCost()
        {
            int cost = 0;
            foreach (string id in _selectedCoachIds)
            {
                if (_content.CoachCards.TryGetValue(id, out CoachCardDefinition card))
                    cost += card.commandCost;
            }
            return cost;
        }

        private bool CanSelectCoachCard(TeamRuntime team, CoachCardDefinition card)
        {
            if (_selectedCoachIds.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
                return false;
            if (!TimeoutCoachRules.CanUseCoach(team, card))
                return false;

            int otherPaidCost = 0;
            foreach (string id in _selectedCoachIds)
            {
                if (!_content.CoachCards.TryGetValue(id, out CoachCardDefinition selected))
                    continue;
                if (!selected.isBaseCoach)
                    otherPaidCost += selected.commandCost;
            }
            if (card.isBaseCoach)
                return true;
            return team.CommandPoints - otherPaidCost >= card.commandCost;
        }

        private static string CoachUnavailableReason(TeamRuntime team, CoachCardDefinition card)
        {
            if (card == null || team == null)
                return "無法使用";
            if (team.CoachUsedThisTimeout.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
                return $"本暫停最多 {TimeoutCoachRules.MaximumCoachCardsPerTimeout} 張教練卡";
            if (card.isBaseCoach && team.CoachUsedThisTimeout.Contains(card.id))
                return "本暫停已套用";
            if (card.maxUsesPerMatch > 0 &&
                team.SpecialUses.TryGetValue("coach:" + card.id, out int uses) &&
                uses >= card.maxUsesPerMatch)
                return "已達本場次使用上限";
            if (team.CommandPoints < card.commandCost)
                return $"指揮點不足（需 {card.commandCost}）";
            return card.description;
        }

        private void OnRequestTimeoutClicked()
        {
            if (_session == null || !_inCombatPlayback)
                return;
            if (!_session.TryRequestTimeoutForNextRound(myTeamId))
                return;
            RefreshTimeoutCallButton();
            if (_roundText != null)
                SetPhaseStatus("已申請下回合暫停");
        }

        private void RefreshTimeoutCallButton()
        {
            if (_timeoutCallLabel == null || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (_session.TimeoutPending)
                _timeoutCallLabel.text = "已申請";
            else
                _timeoutCallLabel.text = $"叫暫停({team.TimeoutsRemaining})";
            Button button = _timeoutCallLabel.GetComponentInParent<Button>();
            if (button != null)
                button.interactable = _inCombatPlayback &&
                                     !_session.TimeoutPending &&
                                     team.TimeoutsRemaining > 0 &&
                                     !_session.IsComplete;
        }

        private void OnSkipTimeoutClicked()
        {
            if (!_awaitingTimeout)
                return;
            _selectedCoachIds.Clear();
            _coachPickTargets.Clear();
            _awaitingTimeout = false;
        }

        private void OnConfirmTimeoutClicked()
        {
            if (!_awaitingTimeout || _session == null)
                return;

            List<string> ordered = _selectedCoachIds
                .Where(id => _content.CoachCards.ContainsKey(id))
                .OrderBy(id => _content.CoachCards[id].isBaseCoach ? 0 : 1)
                .ThenBy(id => _content.CoachCards[id].commandCost)
                .ToList();

            foreach (string id in ordered)
            {
                _coachPickTargets.TryGetValue(id, out string targetId);
                _session.TryApplyCoachCard(myTeamId, id, targetId);
            }

            _selectedCoachIds.Clear();
            _coachPickTargets.Clear();
            RefreshStatPanels();
            _awaitingTimeout = false;
        }

        private void OnCoachCardClicked(int slot)
        {
            if (!_awaitingTimeout || _session == null)
                return;
            if (slot < 0 || slot >= _visibleCoachIds.Count)
                return;
            string id = _visibleCoachIds[slot];
            if (!_content.CoachCards.TryGetValue(id, out CoachCardDefinition card))
                return;

            if (_selectedCoachIds.Contains(id))
            {
                _selectedCoachIds.Remove(id);
                _coachPickTargets.Remove(id);
                RefreshTimeoutPicker();
                return;
            }

            TeamRuntime team = _session.TeamById(myTeamId);
            if (!CanSelectCoachCard(team, card))
            {
                RefreshTimeoutPicker();
                return;
            }

            if (_selectedCoachIds.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
            {
                RefreshTimeoutPicker();
                return;
            }

            _selectedCoachIds.Add(id);
            if (string.Equals(card.targetMode, "manual", StringComparison.OrdinalIgnoreCase))
            {
                PlayerRuntime pick = team.Players
                    .OrderBy(p => card.effectType == "stamina" ? p.Stamina : p.Mentality)
                    .FirstOrDefault();
                if (pick != null)
                    _coachPickTargets[id] = pick.Definition.id;
            }

            RefreshTimeoutPicker();
        }

        private void ShowFocusPicker()
        {
            if (_focusPickerRoot == null)
                return;
            RefreshFocusPicker();
            _focusPickerRoot.transform.SetAsLastSibling();
            _focusPickerRoot.gameObject.SetActive(true);
            if (_roundText != null && _session != null)
                SetPhaseStatus("選擇聚焦卡");
        }

        private void HideFocusPicker()
        {
            if (_focusPickerRoot != null)
                _focusPickerRoot.gameObject.SetActive(false);
        }

        private void RefreshFocusPicker()
        {
            if (_session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            int tacticalCost = FocusCardRules.SelectedTacticalCost(team, _content.TacticalCards);
            string selectedName = "無（直接鎖定即可跳過）";
            if (!string.IsNullOrEmpty(team.SelectedFocusCardId) &&
                _content.FocusCards.TryGetValue(team.SelectedFocusCardId, out FocusCardDefinition selectedDef))
                selectedName = selectedDef.displayName;
            _focusSubtitle.text =
                $"剩餘 CP {team.CommandPoints - tacticalCost}（戰術已佔 {tacticalCost}） · 已選 {selectedName}";
            _visibleFocusIds.Clear();
            _visibleFocusIds.AddRange(FocusCardRules.CatalogFor(team, _content.FocusCards).Select(c => c.id));
            for (int i = 0; i < _focusButtons.Count; i++)
            {
                bool visible = i < _visibleFocusIds.Count;
                _focusButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                FocusCardDefinition card = _content.FocusCards[_visibleFocusIds[i]];
                bool can = FocusCardRules.CanSelect(team, card, _content.TacticalCards);
                bool selected = string.Equals(team.SelectedFocusCardId, card.id, StringComparison.OrdinalIgnoreCase);
                _focusEdges[i].color = selected ? Accent : ExclusiveCardEdge;
                _focusTitles[i].text = card.displayName;
                _focusTitles[i].fontSize = card.displayName.Length > 6 ? 17 : 20;
                _focusTitles[i].color = can || selected ? TraitNameUi : TextMuted;
                _focusCostLabels[i].text = $"{card.commandCost} CP · {TargetModeLabel(card)}";
                _focusCostLabels[i].color = Accent;
                _focusStatLabels[i].text = FormatFocusStats(card);
                _focusStatLabels[i].color = TraitEffectUi;
                NarrativeUiText.Set(_focusDetails[i], !can && !selected ? "指揮點不足\n" + card.description : card.description);
                _focusDetails[i].color = can || selected ? TextMuted : new Color(0.55f, 0.42f, 0.42f, 1f);
                _focusButtons[i].interactable = can || selected;
            }

            FocusCardDefinition selectedCard = null;
            if (!string.IsNullOrEmpty(team.SelectedFocusCardId))
                _content.FocusCards.TryGetValue(team.SelectedFocusCardId, out selectedCard);
            bool showTargets = selectedCard != null &&
                               string.Equals(selectedCard.targetMode, "manual", StringComparison.OrdinalIgnoreCase);
            if (_focusTargetHint != null)
                _focusTargetHint.gameObject.SetActive(showTargets);
            string[] candidates = selectedCard?.targetPlayerIds ?? Array.Empty<string>();
            for (int i = 0; i < _focusTargetButtons.Count; i++)
            {
                bool visible = showTargets && i < candidates.Length;
                _focusTargetButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                string pid = candidates[i];
                _focusTargetLabels[i].text = DemoZh.PlayerName(pid);
                bool chosen = string.Equals(_focusPickTargetId, pid, StringComparison.OrdinalIgnoreCase);
                _focusTargetButtons[i].image.color = chosen ? Accent : ButtonEdge;
            }
        }

        private static string TargetModeLabel(FocusCardDefinition card)
        {
            if (card.targetMode == "team")
                return "全隊";
            if (card.targetMode == "duo")
                return "雙人";
            if (card.targetMode == "manual")
                return "手動指定";
            if (card.targetPlayerIds != null && card.targetPlayerIds.Length > 0)
                return DemoZh.PlayerName(card.targetPlayerIds[0]);
            return "自動";
        }

        private void OnFocusCardClicked(int slot)
        {
            if (!_awaitingFocus || _session == null || slot < 0 || slot >= _visibleFocusIds.Count)
                return;
            string id = _visibleFocusIds[slot];
            TeamRuntime team = _session.TeamById(myTeamId);
            if (string.Equals(team.SelectedFocusCardId, id, StringComparison.OrdinalIgnoreCase))
            {
                _session.SetFocusSelection(myTeamId, string.Empty, string.Empty);
                _focusPickTargetId = string.Empty;
            }
            else
            {
                FocusCardDefinition card = _content.FocusCards[id];
                if (!FocusCardRules.CanSelect(team, card, _content.TacticalCards))
                    return;
                string autoTarget = card.targetPlayerIds != null && card.targetPlayerIds.Length == 1
                    ? card.targetPlayerIds[0]
                    : string.Empty;
                _focusPickTargetId = autoTarget;
                _session.SetFocusSelection(myTeamId, id, _focusPickTargetId);
            }
            RefreshFocusPicker();
        }

        private void OnFocusTargetClicked(int slot)
        {
            if (!_awaitingFocus || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (string.IsNullOrEmpty(team.SelectedFocusCardId) ||
                !_content.FocusCards.TryGetValue(team.SelectedFocusCardId, out FocusCardDefinition card) ||
                card.targetPlayerIds == null || slot >= card.targetPlayerIds.Length)
                return;
            _focusPickTargetId = card.targetPlayerIds[slot];
            _session.SetFocusSelection(myTeamId, card.id, _focusPickTargetId);
            RefreshFocusPicker();
        }

        private void OnConfirmFocusClicked()
        {
            if (!_awaitingFocus || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (!string.IsNullOrEmpty(team.SelectedFocusCardId) &&
                _content.FocusCards.TryGetValue(team.SelectedFocusCardId, out FocusCardDefinition card) &&
                string.Equals(card.targetMode, "manual", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(team.SelectedFocusTargetId))
                return;
            _awaitingFocus = false;
        }

        private void RefreshTacticalCardPicker()
        {
            if (_session == null || _tacticalButtons.Count == 0)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            _visibleTacticalCardIds.Clear();
            _visibleTacticalCardIds.AddRange(team.TacticalDeck.Hand);
            int selectedCost = team.TacticalDeck.Selected
                .Where(_content.TacticalCards.ContainsKey)
                .Sum(id => _content.TacticalCards[id].commandCost);
            bool overflow = team.TacticalDeck.Hand.Count > TacticalDeckRules.MaximumHandSize;
            if (_tacticalPickerRoot != null)
                _tacticalPickerRoot.color = overflow ? TacticalRootOverflow : TacticalRootNormal;
            if (_tacticalPickerPanel != null)
                _tacticalPickerPanel.color = overflow ? TacticalPanelOverflow : TacticalPanelNormal;

            if (overflow)
            {
                _tacticalPickerSubtitle.color = OverflowWarn;
                _tacticalPickerSubtitle.text =
                    $"手牌超出上限！{team.TacticalDeck.Hand.Count}/{TacticalDeckRules.MaximumHandSize}　·　" +
                    $"請點擊未選中的卡棄置，否則無法鎖定";
            }
            else
            {
                _tacticalPickerSubtitle.color = TextMuted;
                _tacticalPickerSubtitle.text =
                    $"指揮點 {team.CommandPoints} · 已選 {team.TacticalDeck.Selected.Count}/3 · " +
                    $"費用 {selectedCost} · 手牌 {team.TacticalDeck.Hand.Count}/{TacticalDeckRules.MaximumHandSize}";
            }

            bool analysis = team.TacticalDeck.Selected.Any(id =>
                _content.TacticalCards.TryGetValue(id, out TacticalCardDefinition card) &&
                card.specialRuleKey == "swap_and_analysis");
            _tacticalAnalysis.color = overflow ? OverflowWarn : TextMuted;
            _tacticalAnalysis.text = overflow
                ? "背景轉紅＝手牌過多。點選多餘的未選卡棄掉，回到 7 張以內即可鎖定。"
                : analysis
                    ? $"局勢分析：{_naviScore}:{_gambitScore} · 己方 ${team.Money:N0} · " +
                      $"連敗 {team.LossStreak} · 可取消一張已選卡後改選"
                    : "點擊卡牌選取／取消；可不出牌保留指揮點。";

            for (int i = 0; i < _tacticalButtons.Count; i++)
            {
                bool visible = i < _visibleTacticalCardIds.Count;
                _tacticalButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                string id = _visibleTacticalCardIds[i];
                TacticalCardDefinition card = _content.TacticalCards[id];
                bool selected = team.TacticalDeck.Selected.Contains(id);
                bool isUniversal = string.Equals(card.teamScope, "universal", StringComparison.OrdinalIgnoreCase);
                bool affordable = selected || selectedCost + card.commandCost <= team.CommandPoints;
                bool interactable = overflow
                    ? !selected
                    : affordable && (selected || team.TacticalDeck.Selected.Count < TacticalDeckRules.MaximumCardsPerRound);

                Color edgeColor = isUniversal ? UniversalCardEdge : ExclusiveCardEdge;
                Color stripColor = isUniversal ? UniversalCardStrip : ExclusiveCardStrip;
                Color faceColor = isUniversal
                    ? new Color(0.12f, 0.135f, 0.165f, 1f)
                    : new Color(0.165f, 0.125f, 0.11f, 1f);
                if (selected)
                {
                    edgeColor = Accent;
                    stripColor = new Color(Accent.r * 0.85f, Accent.g * 0.55f, Accent.b * 0.20f, 1f);
                    faceColor = new Color(0.16f, 0.145f, 0.11f, 1f);
                }
                else if (overflow)
                {
                    edgeColor = OverflowWarn;
                    stripColor = new Color(0.75f, 0.22f, 0.22f, 1f);
                    faceColor = new Color(0.22f, 0.10f, 0.10f, 1f);
                }
                else if (!interactable)
                {
                    edgeColor = new Color(edgeColor.r * 0.55f, edgeColor.g * 0.55f, edgeColor.b * 0.55f, 1f);
                    stripColor = new Color(stripColor.r * 0.55f, stripColor.g * 0.55f, stripColor.b * 0.55f, 1f);
                    faceColor = new Color(faceColor.r * 0.7f, faceColor.g * 0.7f, faceColor.b * 0.7f, 1f);
                }

                _tacticalCardEdges[i].color = edgeColor;
                _tacticalCardStrips[i].color = stripColor;
                _tacticalCardFaces[i].color = faceColor;
                _tacticalCardGroups[i].alpha = interactable || selected ? 1f : 0.52f;
                // Slight scale-up when selected so it reads as a picked card, not a pressed button.
                _tacticalCardEdges[i].rectTransform.localScale =
                    selected ? new Vector3(1.035f, 1.035f, 1f) : Vector3.one;

                string scopeLabel = isUniversal ? "通用" : "專屬";
                _tacticalScopeLabels[i].text = selected ? "已選 · " + scopeLabel : scopeLabel;
                _tacticalScopeLabels[i].color = selected ? Accent : TextPrimary;

                _tacticalTitleLabels[i].text = card.displayName;
                _tacticalTitleLabels[i].color = TraitNameUi;
                _tacticalTitleLabels[i].fontSize = card.displayName.Length > 6 ? 16 : 18;

                _tacticalCostLabels[i].text = $"{card.commandCost} CP · {DemoZh.ZoneName(card.primaryZoneId)}";
                _tacticalCostLabels[i].color = Accent;

                _tacticalStatLabels[i].text = FormatTacticalStats(card);
                _tacticalStatLabels[i].color = TraitEffectUi;

                string status = overflow && !selected
                    ? "點擊棄置"
                    : !affordable && !selected
                        ? "指揮點不足"
                        : card.description;
                NarrativeUiText.Set(_tacticalDetailLabels[i], status);
                _tacticalDetailLabels[i].color = overflow && !selected
                    ? OverflowWarn
                    : interactable || selected
                        ? TextMuted
                        : new Color(0.55f, 0.42f, 0.42f, 1f);

                _tacticalButtons[i].interactable = interactable;
            }
        }

        private static string FormatTacticalStats(TacticalCardDefinition card)
        {
            return
                $"<color={TraitEffectColor}>風險 {card.risk:0.00}</color>\n" +
                $"<color={TraitEffectColor}>首殺 {FormatSignedPercent(card.openingBonus)}</color>\n" +
                $"<color={TraitEffectColor}>火力 {FormatSignedPercent(card.firepowerBonus)}</color>\n" +
                $"<color={TraitEffectColor}>配合 {FormatSignedPercent(card.teamworkBonus)}</color>\n" +
                $"<color={TraitEffectColor}>殘局 {FormatSignedPercent(card.clutchBonus)}</color>";
        }

        private static string FormatSignedPercent(float value)
        {
            if (Mathf.Abs(value) < 0.0001f)
                return "+0%";
            return value >= 0f ? $"+{value:P0}" : value.ToString("P0");
        }

        private void OnTacticalCardClicked(int slot)
        {
            if (!_awaitingTacticalCards || _session == null ||
                slot < 0 || slot >= _visibleTacticalCardIds.Count)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            string id = _visibleTacticalCardIds[slot];
            if (team.TacticalDeck.Hand.Count > TacticalDeckRules.MaximumHandSize)
                _session.DiscardTacticalCard(myTeamId, id);
            else
                _session.ToggleTacticalCard(myTeamId, id);
            RefreshTacticalCardPicker();
        }

        private void ConfirmTacticalCards()
        {
            if (!_awaitingTacticalCards || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (team.TacticalDeck.Hand.Count > TacticalDeckRules.MaximumHandSize)
            {
                RefreshTacticalCardPicker();
                return;
            }
            _awaitingTacticalCards = false;
        }

        private void BuildPlayerNumbers()
        {
            _playerNumbers.Clear();
            foreach (TeamDefinition team in _content.Teams.Values)
            {
                for (int i = 0; i < team.playerIds.Length; i++)
                    _playerNumbers[team.playerIds[i]] = i + 1;
            }
        }

        private int PlayerNumber(string playerId) =>
            _playerNumbers.TryGetValue(playerId, out int n) ? n : 0;

        private void CreateStatSlots(Transform parent, bool leftSide)
        {
            Image classicHost = CreateUiObject<Image>("ClassicRoot", parent);
            Stretch(classicHost.rectTransform);
            classicHost.color = Color.clear;
            classicHost.raycastTarget = false;
            Image generalHost = CreateUiObject<Image>("GeneralRoot", parent);
            Stretch(generalHost.rectTransform);
            generalHost.color = Color.clear;
            generalHost.raycastTarget = false;
            if (leftSide)
            {
                _leftClassicRoot = classicHost.rectTransform;
                _leftGeneralRoot = generalHost.rectTransform;
            }
            else
            {
                _rightClassicRoot = classicHost.rectTransform;
                _rightGeneralRoot = generalHost.rectTransform;
            }

            CreateClassicStatSlots(classicHost.transform, leftSide);
            CreateGeneralStatSlots(generalHost.transform, leftSide);
            classicHost.gameObject.SetActive(true);
            generalHost.gameObject.SetActive(false);
        }

        private void CreateClassicStatSlots(Transform parent, bool leftSide)
        {
            List<Text> slots = leftSide ? _leftStatSlots : _rightStatSlots;
            List<Text> roundKills = leftSide ? _leftRoundKillSlots : _rightRoundKillSlots;
            List<StatMetricSlot[]> statuses = leftSide ? _leftStatusRows : _rightStatusRows;
            List<Image> jerseys = leftSide ? _leftJerseyBadges : _rightJerseyBadges;
            List<Text> jerseyNums = leftSide ? _leftJerseyNumbers : _rightJerseyNumbers;
            List<Image> rows = leftSide ? _leftStatRows : _rightStatRows;
            List<string> playerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            const float topStart = 0.91f;
            const float bottomPad = 0.02f;
            const float rowGap = 0.01f;
            float rowHeight = (topStart - bottomPad - 4f * rowGap) / 5f;
            float top = topStart;
            for (int i = 0; i < 5; i++)
            {
                float bottom = top - rowHeight;
                Image row = CreateUiObject<Image>("StatRow" + i, parent);
                SetRect(row.rectTransform, new Vector2(0.05f, bottom), new Vector2(0.95f, top));
                row.color = AliveRowBg;
                row.raycastTarget = true;
                rows.Add(row);
                playerIds.Add(string.Empty);

                int slotIndex = i;
                bool sideIsLeft = leftSide;
                EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
                AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => OnStatRowPointerEnter(sideIsLeft, slotIndex));
                AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnStatRowPointerExit(sideIsLeft, slotIndex));

                Image badge = CreateUiObject<Image>("Jersey", row.transform);
                badge.sprite = _circleSprite;
                badge.color = CtText;
                badge.raycastTarget = false;
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax =
                    leftSide ? new Vector2(0.10f, 0.70f) : new Vector2(0.90f, 0.70f);
                badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                badge.rectTransform.sizeDelta = new Vector2(18f, 18f);
                badge.rectTransform.anchoredPosition = Vector2.zero;
                jerseys.Add(badge);

                Text jerseyNum = CreateText("JerseyNum", badge.transform, 11, TextAnchor.MiddleCenter, TextPrimary);
                Stretch(jerseyNum.rectTransform);
                jerseyNum.fontStyle = FontStyle.Bold;
                jerseyNum.raycastTarget = false;
                jerseyNum.text = (i + 1).ToString();
                jerseyNums.Add(jerseyNum);

                Text line = CreateText("Line", row.transform, 16, leftSide ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, TextPrimary);
                SetRect(line.rectTransform,
                    leftSide ? new Vector2(0.18f, 0.34f) : new Vector2(0.28f, 0.34f),
                    leftSide ? new Vector2(0.68f, 1f) : new Vector2(0.80f, 1f));
                line.rectTransform.offsetMin = new Vector2(leftSide ? 2f : 4f, 0f);
                line.rectTransform.offsetMax = new Vector2(leftSide ? -4f : -2f, -2f);
                line.raycastTarget = false;
                line.text = "—";
                slots.Add(line);

                Text rk = CreateText("RoundKills", row.transform, 20, TextAnchor.MiddleCenter, CtText);
                SetRect(rk.rectTransform,
                    leftSide ? new Vector2(0.70f, 0.42f) : new Vector2(0.02f, 0.42f),
                    leftSide ? new Vector2(0.98f, 0.95f) : new Vector2(0.26f, 0.95f));
                rk.fontStyle = FontStyle.Bold;
                rk.raycastTarget = false;
                rk.text = string.Empty;
                roundKills.Add(rk);

                statuses.Add(CreateStatusMetricRow(row.transform));
                top = bottom - rowGap;
            }
        }

        private void CreateGeneralStatSlots(Transform parent, bool leftSide)
        {
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            Sprite killIcon = _statIcons != null && _statIcons.Length > 0 ? _statIcons[0] : null;
            Sprite deathIcon = _statIcons != null && _statIcons.Length > 1 ? _statIcons[1] : null;
            Sprite assistIcon = _statIcons != null && _statIcons.Length > 2 ? _statIcons[2] : null;
            Sprite staminaIcon = _statIcons != null && _statIcons.Length > 3 ? _statIcons[3] : null;
            Sprite mentalityIcon = _statIcons != null && _statIcons.Length > 4 ? _statIcons[4] : null;
            Sprite formIcon = _statIcons != null && _statIcons.Length > 5 ? _statIcons[5] : null;

            for (int i = 0; i < 5; i++)
            {
                BattlePlayerCardView card = BattleStatPanelPresentation.CreateCard(
                    parent,
                    leftSide,
                    i,
                    _scoreboardFont ?? _uiFont,
                    killIcon,
                    deathIcon,
                    assistIcon,
                    mentalityIcon,
                    formIcon,
                    staminaIcon,
                    OnStatRowPointerEnter,
                    OnStatRowPointerExit);
                BattleStatPanelPresentation.LayoutCardInColumn(card, i);
                cards.Add(card);
            }
        }

        private StatMetricSlot[] CreateStatusMetricRow(Transform row)
        {
            Image root = CreateUiObject<Image>("StatusRow", row);
            root.color = Color.clear;
            root.raycastTarget = false;
            SetRect(root.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.34f));

            const int frontCount = 3;
            const int backCount = 3;
            const float frontEnd = 0.48f;
            const float backStart = 0.52f;
            var metrics = new StatMetricSlot[frontCount + backCount];

            for (int m = 0; m < frontCount; m++)
            {
                float x0 = (m / (float)frontCount) * frontEnd;
                float x1 = ((m + 1) / (float)frontCount) * frontEnd;
                metrics[m] = CreateMetricCell(root.transform, m, x0, x1, TextPrimary);
            }

            Image divider = CreateUiObject<Image>("Divider", root.transform);
            divider.color = new Color(TextMuted.r, TextMuted.g, TextMuted.b, 0.35f);
            divider.raycastTarget = false;
            SetRect(divider.rectTransform, new Vector2(0.495f, 0.20f), new Vector2(0.505f, 0.80f));

            for (int m = 0; m < backCount; m++)
            {
                int index = frontCount + m;
                float span = 1f - backStart;
                float x0 = backStart + (m / (float)backCount) * span;
                float x1 = backStart + ((m + 1) / (float)backCount) * span;
                metrics[index] = CreateMetricCell(root.transform, index, x0, x1, TextMuted);
            }

            return metrics;
        }

        private StatMetricSlot CreateMetricCell(
            Transform parent, int index, float x0, float x1, Color tint)
        {
            Image cell = CreateUiObject<Image>("Metric" + index, parent);
            cell.color = Color.clear;
            cell.raycastTarget = false;
            SetRect(cell.rectTransform, new Vector2(x0, 0f), new Vector2(x1, 1f));

            Image icon = CreateUiObject<Image>("Icon", cell.transform);
            icon.sprite = _statIcons != null && index < _statIcons.Length ? _statIcons[index] : null;
            icon.preserveAspect = true;
            icon.color = tint;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.46f, 0.82f));

            Text value = CreateText("Value", cell.transform, 10, TextAnchor.MiddleLeft, tint);
            SetRect(value.rectTransform, new Vector2(0.48f, 0f), new Vector2(1f, 1f));
            value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.text = "--";

            return new StatMetricSlot { Icon = icon, Value = value };
        }

        private static Sprite[] LoadStatIcons()
        {
            var icons = new Sprite[StatIconResourcePaths.Length];
            for (int i = 0; i < StatIconResourcePaths.Length; i++)
                icons[i] = Resources.Load<Sprite>(StatIconResourcePaths[i]);
            return icons;
        }

        private void CreateZoneLabels()
        {
            _zoneLabels.Clear();
            _zoneHitPads.Clear();
            var zoneLabelColor = new Color(0.92f, 0.93f, 0.94f, 0.78f);
            foreach (MapZoneDefinition zone in ActiveMapZones())
            {
                Image hit = CreateUiObject<Image>("ZoneHit_" + zone.id, _mapRect);
                hit.color = Color.clear;
                hit.raycastTarget = true;
                hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hit.rectTransform.sizeDelta = new Vector2(78f, 78f);
                hit.rectTransform.anchoredPosition = ClampToMap(LocalZonePosition(zone.id));

                Text label = CreateText("Zone_" + zone.id, hit.transform, 12, TextAnchor.MiddleCenter, zoneLabelColor);
                label.text = DemoZh.ZoneName(zone.id);
                label.fontStyle = FontStyle.Bold;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 18f);
                label.rectTransform.sizeDelta = new Vector2(110f, 24f);
                Outline outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
                outline.effectDistance = new Vector2(1.1f, -1.1f);

                string zoneId = zone.id;
                EventTrigger trigger = hit.gameObject.AddComponent<EventTrigger>();
                AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowZoneTooltip(zoneId));
                AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => HideZoneTooltip(zoneId));

                _zoneHitPads.Add(hit);
                _zoneLabels.Add(label);
            }
            RefreshFloorVisibility();
        }

        private void CreateZoneTooltip(Transform canvas)
        {
            _zoneTooltip = CreateUiObject<Image>("ZoneTooltip", canvas);
            _zoneTooltip.color = new Color(0.06f, 0.07f, 0.09f, 0.94f);
            _zoneTooltip.raycastTarget = false;
            RectTransform rt = _zoneTooltip.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(220f, 132f);
            _zoneTooltip.gameObject.SetActive(false);

            _zoneTooltipText = CreateText("Body", _zoneTooltip.transform, 13, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            Stretch(_zoneTooltipText.rectTransform);
            _zoneTooltipText.rectTransform.offsetMin = new Vector2(10f, 8f);
            _zoneTooltipText.rectTransform.offsetMax = new Vector2(-10f, -8f);
            _zoneTooltipText.alignment = TextAnchor.UpperLeft;
            _zoneTooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _zoneTooltipText.verticalOverflow = VerticalWrapMode.Overflow;
            _zoneTooltipText.supportRichText = true;
            _zoneTooltipText.raycastTarget = false;
        }

        private void RelayoutZoneLabels()
        {
            MapZoneDefinition[] zones = ActiveMapZones();
            if (zones == null)
                return;
            for (int i = 0; i < zones.Length; i++)
            {
                MapZoneDefinition zone = zones[i];
                Vector2 center = ClampToMap(LocalZonePosition(zone.id));
                if (i < _zoneHitPads.Count && _zoneHitPads[i] != null)
                    _zoneHitPads[i].rectTransform.anchoredPosition = center;
                if (i < _zoneLabels.Count && _zoneLabels[i] != null)
                    _zoneLabels[i].rectTransform.anchoredPosition = new Vector2(0f, 18f);
            }
        }

        private void ShowZoneTooltip(string zoneId)
        {
            if (_zoneTooltip == null || _zoneTooltipText == null || string.IsNullOrEmpty(zoneId))
                return;
            if (!_zones.TryGetValue(zoneId, out MapZoneDefinition zone) || zone == null)
                return;

            _hoveredZoneId = zoneId;
            _zoneTooltipText.text = FormatZoneBonusDetail(zone);
            _zoneTooltip.gameObject.SetActive(true);
            _zoneTooltip.transform.SetAsLastSibling();
            PositionZoneTooltip(Input.mousePosition);
        }

        private void HideZoneTooltip(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) &&
                !string.Equals(_hoveredZoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                return;
            _hoveredZoneId = null;
            if (_zoneTooltip != null)
                _zoneTooltip.gameObject.SetActive(false);
        }

        private void PositionZoneTooltip(Vector2 screenPosition)
        {
            if (_zoneTooltip == null || _canvasRect == null)
                return;
            RectTransform rect = _zoneTooltip.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, null, out Vector2 localPoint);

            Vector2 size = rect.sizeDelta;
            Vector2 canvasSize = _canvasRect.rect.size;
            float minX = -canvasSize.x * 0.5f + 8f;
            float maxX = canvasSize.x * 0.5f - size.x - 8f;
            float minY = -canvasSize.y * 0.5f + size.y + 8f;
            float maxY = canvasSize.y * 0.5f - 8f;

            localPoint.x = Mathf.Clamp(localPoint.x + 14f, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y - 10f, minY, maxY);
            rect.anchoredPosition = localPoint;
        }

        private static string FormatZoneBonusDetail(MapZoneDefinition zone)
        {
            string name = DemoZh.ZoneName(zone.id);
            var lines = new List<string>(6)
            {
                $"<color=#EBB438>{name}</color>",
                string.Empty
            };
            AppendBonusDetailLine(lines, "進攻瞄準", zone.attackerAimBonus);
            AppendBonusDetailLine(lines, "防守反應", zone.defenderReactionBonus);
            AppendBonusDetailLine(lines, "進攻配合", zone.attackerTeamworkBonus);
            AppendBonusDetailLine(lines, "防守意識", zone.defenderAwarenessBonus);
            if (lines.Count <= 2)
                lines.Add("此區域無能力增減");
            return string.Join("\n", lines);
        }

        private static void AppendBonusDetailLine(List<string> lines, string label, float bonus)
        {
            if (Mathf.Abs(bonus) < 0.0001f)
                return;
            float pct = bonus * 100f;
            string value = pct >= 0f ? $"+{pct:0}%" : $"{pct:0}%";
            lines.Add($"{label}  {value}");
        }

        private void CreateBombMarker()
        {
            var root = new GameObject("Bomb", typeof(RectTransform));
            root.transform.SetParent(_mapRect, false);
            _bombMarker = root.GetComponent<RectTransform>();
            _bombMarker.anchorMin = _bombMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _bombMarker.sizeDelta = new Vector2(36f, 36f);
            _bombMarker.gameObject.SetActive(false);

            Image glow = CreateUiObject<Image>("Glow", root.transform);
            glow.sprite = _circleSprite;
            glow.color = new Color(0.95f, 0.55f, 0.12f, 0.35f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(36f, 36f);
            _bombMarkerGlow = glow;

            Image core = CreateUiObject<Image>("Core", root.transform);
            core.sprite = _circleSprite;
            core.color = new Color(0.98f, 0.72f, 0.18f, 1f);
            core.rectTransform.anchorMin = core.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            core.rectTransform.sizeDelta = new Vector2(18f, 18f);
            _bombMarkerCore = core;

            Text label = CreateText("Label", root.transform, 14, TextAnchor.MiddleCenter, TextPrimary);
            label.text = "C4";
            label.fontStyle = FontStyle.Bold;
            label.rectTransform.anchorMin = new Vector2(0f, -0.55f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.05f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private void CreateMarkers()
        {
            foreach (TeamDefinition team in _content.Teams.Values)
            {
                for (int i = 0; i < team.playerIds.Length; i++)
                {
                    string id = team.playerIds[i];
                    int number = i + 1;
                    var root = new GameObject(id, typeof(RectTransform));
                    root.transform.SetParent(_mapRect, false);
                    RectTransform rootRect = root.GetComponent<RectTransform>();
                    rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    float d = QuickMarkerDotDiameter;
                    float rootSize = d + 6f;
                    rootRect.sizeDelta = new Vector2(rootSize, rootSize);

                    Image cone = CreateUiObject<Image>("ViewCone", root.transform);
                    cone.sprite = _circleSprite;
                    cone.type = Image.Type.Filled;
                    cone.fillMethod = Image.FillMethod.Radial360;
                    cone.fillOrigin = (int)Image.Origin360.Right;
                    cone.fillClockwise = false;
                    cone.fillAmount = ViewConeDegrees / 360f;
                    cone.color = ViewConeColor(false);
                    cone.raycastTarget = false;
                    cone.rectTransform.anchorMin = cone.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    cone.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();
                    cone.enabled = false;
                    cone.raycastTarget = false;

                    GameObject coneMeshRoot = new GameObject("ViewConeMesh", typeof(RectTransform), typeof(CanvasRenderer));
                    coneMeshRoot.transform.SetParent(root.transform, false);
                    RectTransform coneMeshRect = coneMeshRoot.GetComponent<RectTransform>();
                    coneMeshRect.anchorMin = coneMeshRect.anchorMax = new Vector2(0.5f, 0.5f);
                    coneMeshRect.pivot = new Vector2(0.5f, 0.5f);
                    coneMeshRect.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    coneMeshRect.SetAsFirstSibling();
                    ViewConeMeshGraphic coneMesh = coneMeshRoot.AddComponent<ViewConeMeshGraphic>();
                    coneMesh.raycastTarget = false;
                    coneMesh.color = ViewConeColor(false);

                    Image sight = CreateUiObject<Image>("SightRay", root.transform);
                    sight.sprite = _lineSprite != null ? _lineSprite : _circleSprite;
                    sight.color = SightRayColor;
                    sight.raycastTarget = false;
                    sight.rectTransform.anchorMin = sight.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    sight.rectTransform.pivot = new Vector2(0f, 0.5f);
                    sight.rectTransform.anchoredPosition = Vector2.zero;
                    sight.rectTransform.sizeDelta = new Vector2(SightRayLength, SightRayWidth);

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(d + 4f, d + 4f);

                    Image marker = CreateUiObject<Image>("Dot", root.transform);
                    marker.sprite = _circleSprite;
                    marker.color = CtText;
                    marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    marker.rectTransform.sizeDelta = new Vector2(d, d);

                    Text label = CreateText("Label", root.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
                    label.text = number.ToString();
                    label.fontStyle = FontStyle.Bold;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    label.rectTransform.sizeDelta = new Vector2(d, d);
                    label.rectTransform.anchoredPosition = Vector2.zero;
                    Outline outline = label.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    outline.effectDistance = new Vector2(1.1f, -1.1f);

                    // Map markers are number dots only; HP/armor live in the side scoreboard.
                    _markers[id] = rootRect;
                    _markerDots[id] = marker;
                    _markerLabels[id] = label;
                    _markerCones[id] = cone;
                    _markerConeMeshes[id] = coneMesh;
                    _markerSightRays[id] = sight;
                    _markerFacingDeg[id] = 90f;
                    ApplyViewConeFacing(id, 90f);
                    _playerZones[id] = "t_spawn";
                }
            }

            RefreshMarkerSideColors();
        }

        private void RefreshMarkerSideColors()
        {
            foreach (KeyValuePair<string, Image> pair in _markerDots)
            {
                bool isT = PlayerIsTerrorist(pair.Key, _currentRound);
                pair.Value.color = isT ? TText : CtText;
                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                    cone.color = ViewConeColor(isT);
                if (_markerConeMeshes.TryGetValue(pair.Key, out ViewConeMeshGraphic mesh) && mesh != null)
                    mesh.color = ViewConeColor(isT);
            }
        }

        private static Color SideBadgeColor(bool isTerrorist) => isTerrorist ? TText : CtText;

        private void AddKillFeed(SimulationEvent evt)
        {
            int round = Mathf.Max(1, evt.roundNumber);
            string killer = ColoredPlayerName(evt.actorId, round);
            string victim = ColoredPlayerName(evt.targetId, round);
            var assists = new List<string>();
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag == null || !tag.StartsWith("assist:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string assistId = tag.Substring("assist:".Length);
                    if (!string.IsNullOrEmpty(assistId))
                        assists.Add(ColoredPlayerName(assistId, round));
                }
            }

            string left = killer;
            if (assists.Count > 0)
                left += $" <color=#{KillFeedGrayHex}>+</color>" + string.Join(
                    $"<color=#{KillFeedGrayHex}>+</color>", assists);

            string right = victim;
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag != null && tag.StartsWith("kill_reward:", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(tag.Substring("kill_reward:".Length), out int reward) && reward > 0)
                    {
                        right += $"  <color=#C9A227>+${reward}</color>";
                        break;
                    }
                }
            }

            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!headshot && evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag != null && tag.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        headshot = true;
                        break;
                    }
                }
            }
            _killFeed.Add(new KillFeedLine
            {
                Left = left,
                Right = right,
                WeaponId = evt.weaponId,
                Headshot = headshot,
                ExpireAtUnscaled = Time.unscaledTime + KillFeedVisibleSeconds
            });
            while (_killFeed.Count > 5)
                _killFeed.RemoveAt(0);
            UpdateKillFeed();
        }

        private void PruneExpiredKillFeed()
        {
            if (_killFeed.Count == 0)
                return;
            float now = Time.unscaledTime;
            int removed = _killFeed.RemoveAll(x => x == null || now >= x.ExpireAtUnscaled);
            if (removed > 0)
                UpdateKillFeed();
        }

        private string ColoredPlayerName(string playerId, int roundNumber)
        {
            if (string.IsNullOrEmpty(playerId))
                return string.Empty;
            string hex = PlayerIsTerrorist(playerId, roundNumber) ? THex : CtHex;
            return $"<color={hex}>{DemoZh.PlayerName(playerId)}</color>";
        }

        private bool PlayerIsTerrorist(string playerId, int roundNumber)
        {
            string teamId = ResolvePlayerTeamId(playerId);
            return TeamIsTerrorist(teamId, roundNumber);
        }

        private string ResolvePlayerTeamId(string playerId)
        {
            if (_content == null || string.IsNullOrEmpty(playerId))
                return string.Empty;
            foreach (TeamDefinition team in _content.Teams.Values)
            {
                foreach (string id in team.playerIds)
                {
                    if (string.Equals(id, playerId, StringComparison.OrdinalIgnoreCase))
                        return team.id;
                }
            }
            return string.Empty;
        }

        private bool TeamIsTerrorist(string teamId, int roundNumber)
        {
            bool teamAIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, roundNumber - 1));
            if (_session != null)
            {
                string teamAId = _session.State.TeamA.Definition.id;
                if (string.Equals(teamId, teamAId, StringComparison.OrdinalIgnoreCase))
                    return teamAIsT;
                string teamBId = _session.State.TeamB.Definition.id;
                if (string.Equals(teamId, teamBId, StringComparison.OrdinalIgnoreCase))
                    return !teamAIsT;
                return false;
            }

            if (teamId == "navi_2021")
                return teamAIsT;
            if (teamId == "gambit_2021")
                return !teamAIsT;
            return false;
        }

        private void UpdateKillFeed()
        {
            for (int i = 0; i < _killFeedRows.Count; i++)
            {
                KillFeedRow row = _killFeedRows[i];
                if (row?.Root == null)
                    continue;
                if (i < _killFeed.Count)
                {
                    KillFeedLine line = _killFeed[i];
                    row.Root.SetActive(true);
                    if (row.Left != null)
                    {
                        row.Left.text = line.Left ?? string.Empty;
                        // White base so rich-text CT/T colors are not gray-tinted.
                        row.Left.color = Color.white;
                    }
                    if (row.Right != null)
                    {
                        row.Right.text = line.Right ?? string.Empty;
                        row.Right.color = Color.white;
                    }
                    Image rowBg = row.Root.GetComponent<Image>();
                    if (rowBg != null)
                        rowBg.color = KillFeedRowBg;
                    if (row.Icon != null)
                    {
                        Sprite weaponSprite = WeaponIconAtlas.Get(line.WeaponId);
                        row.Icon.enabled = true;
                        row.Icon.sprite = weaponSprite != null ? weaponSprite : _killFeedSkullSprite;
                        row.Icon.color = Color.white;
                        row.Icon.preserveAspect = true;
                        float iconW = weaponSprite != null ? 42f * BattleStatPanelPresentation.WeaponIconScale : 16f;
                        float iconH = weaponSprite != null ? 18f * BattleStatPanelPresentation.WeaponIconScale : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconW, iconH);
                    }
                    if (row.HeadshotIcon != null)
                    {
                        bool headshot = line.Headshot;
                        row.HeadshotIcon.enabled = headshot;
                        if (headshot)
                        {
                            row.HeadshotIcon.sprite = _killFeedIconSprite ?? _killFeedSkullSprite;
                            row.HeadshotIcon.color = Color.white;
                            row.HeadshotIcon.rectTransform.sizeDelta = new Vector2(18f, 18f);
                        }
                    }
                }
                else
                {
                    row.Root.SetActive(false);
                }
            }
        }

        private void CreateKillFeedRows(Transform parent)
        {
            _killFeedRows.Clear();
            const int maxLines = 5;
            const float top = 0.98f;
            const float bottom = 0.02f;
            const float gap = 0.018f;
            float rowH = (top - bottom - gap * (maxLines - 1)) / maxLines;
            for (int i = 0; i < maxLines; i++)
            {
                float y1 = top - i * (rowH + gap);
                float y0 = y1 - rowH;
                Image rowRoot = CreateUiObject<Image>("KillRow" + i, parent);
                rowRoot.color = KillFeedRowBg;
                rowRoot.raycastTarget = false;
                SetRect(rowRoot.rectTransform, new Vector2(0.03f, y0), new Vector2(0.97f, y1));

                Text left = CreateText("Left", rowRoot.transform, 12, TextAnchor.MiddleRight, KillFeedGray);
                // Sit against the kill icon; keep left pad so MiddleRight overflow stays inside the panel.
                SetRect(left.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.42f, 1f));
                left.supportRichText = true;
                left.horizontalOverflow = HorizontalWrapMode.Overflow;
                left.verticalOverflow = VerticalWrapMode.Truncate;
                left.resizeTextForBestFit = true;
                left.resizeTextMinSize = 9;
                left.resizeTextMaxSize = 12;
                left.raycastTarget = false;
                left.text = string.Empty;

                Image icon = CreateUiObject<Image>("Icon", rowRoot.transform);
                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.50f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(
                    42f * BattleStatPanelPresentation.WeaponIconScale,
                    18f * BattleStatPanelPresentation.WeaponIconScale);
                icon.rectTransform.anchoredPosition = Vector2.zero;

                Image headshotIcon = CreateUiObject<Image>("HeadshotIcon", rowRoot.transform);
                headshotIcon.sprite = _killFeedIconSprite ?? _killFeedSkullSprite;
                headshotIcon.preserveAspect = true;
                headshotIcon.color = Color.white;
                headshotIcon.raycastTarget = false;
                headshotIcon.enabled = false;
                headshotIcon.rectTransform.anchorMin = headshotIcon.rectTransform.anchorMax = new Vector2(0.62f, 0.5f);
                headshotIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                headshotIcon.rectTransform.sizeDelta = new Vector2(18f, 18f);
                headshotIcon.rectTransform.anchoredPosition = Vector2.zero;

                Text right = CreateText("Right", rowRoot.transform, 12, TextAnchor.MiddleLeft, KillFeedGray);
                SetRect(right.rectTransform, new Vector2(0.68f, 0f), new Vector2(0.98f, 1f));
                right.supportRichText = true;
                right.horizontalOverflow = HorizontalWrapMode.Overflow;
                right.verticalOverflow = VerticalWrapMode.Truncate;
                right.resizeTextForBestFit = true;
                right.resizeTextMinSize = 9;
                right.resizeTextMaxSize = 12;
                right.raycastTarget = false;
                right.text = string.Empty;

                rowRoot.gameObject.SetActive(false);
                _killFeedRows.Add(new KillFeedRow
                {
                    Root = rowRoot.gameObject,
                    Left = left,
                    Icon = icon,
                    HeadshotIcon = headshotIcon,
                    Right = right,
                });
            }
        }

        private Image[] CreateSeriesPips(string name, Transform parent, float anchorX, bool growRight)
        {
            int winsNeeded = Mathf.Max(1, _seriesBestOf / 2 + 1);
            _ = growRight;
            const float pipSize = 13f;
            const float gap = 4f;
            float totalH = winsNeeded * pipSize + (winsNeeded - 1) * gap;

            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, 0.5f);
            rt.anchorMax = new Vector2(anchorX, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(pipSize, totalH);
            rt.anchoredPosition = Vector2.zero;

            Image[] pips = new Image[winsNeeded];
            for (int i = 0; i < winsNeeded; i++)
            {
                Image pip = CreateUiObject<Image>("Pip" + i, root.transform);
                RectTransform pr = pip.rectTransform;
                pr.anchorMin = new Vector2(0.5f, 1f);
                pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.sizeDelta = new Vector2(pipSize, pipSize);
                pr.anchoredPosition = new Vector2(0f, -(i * (pipSize + gap)));
                pip.sprite = _circleSprite;
                pip.type = Image.Type.Simple;
                pip.preserveAspect = true;
                pip.color = SeriesPipOff;
                pip.raycastTarget = false;
                pips[i] = pip;
            }

            return pips;
        }

        private void RefreshSeriesPips()
        {
            bool leftIsT = false;
            bool rightIsT = true;
            if (_session != null)
            {
                int round = Mathf.Max(1, _currentRound);
                bool myT = MyTeamIsTerrorist(round);
                // Official HUD: left = my team, right = opponent.
                leftIsT = myT;
                rightIsT = !myT;
            }

            ApplySeriesPipColors(_leftSeriesPips, _seriesScorePlayer, leftIsT);
            ApplySeriesPipColors(_rightSeriesPips, _seriesScoreOpponent, rightIsT);
            ApplySeriesPipColors(_resultLeftSeriesPips, _seriesScorePlayer, leftIsT);
            ApplySeriesPipColors(_resultRightSeriesPips, _seriesScoreOpponent, rightIsT);
        }

        private static void ApplySeriesPipColors(Image[] pips, int wins, bool isTerrorist)
        {
            if (pips == null)
                return;
            Color on = isTerrorist ? SeriesPipT : SeriesPipCt;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null)
                    continue;
                pips[i].color = i < wins ? on : SeriesPipOff;
            }
        }

        private Image CreateTeamLogo(string name, Transform parent, float anchorX)
        {
            Image logo = CreateUiObject<Image>(name, parent);
            RectTransform rt = logo.rectTransform;
            // ~90% of TopHud height; width follows height for a square mark.
            rt.anchorMin = new Vector2(anchorX, 0.05f);
            rt.anchorMax = new Vector2(anchorX, 0.95f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var fitter = logo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = 1f;
            logo.preserveAspect = false;
            logo.raycastTarget = false;
            logo.color = Color.white;
            return logo;
        }

        private void BindTeamLogo(Image target, string teamId)
        {
            if (target == null)
                return;
            Sprite sprite = ResolveTeamLogoSprite(teamId);
            target.sprite = sprite;
            target.enabled = true;
            target.color = sprite != null ? Color.white : Color.clear;
        }

        private Sprite ResolveTeamLogoSprite(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
                return null;
            if (string.Equals(teamId, "navi_2021", StringComparison.OrdinalIgnoreCase))
                return _naviLogoSprite;
            if (string.Equals(teamId, "gambit_2021", StringComparison.OrdinalIgnoreCase))
                return _gambitLogoSprite;
            int suffix = teamId.IndexOf("_2021", StringComparison.OrdinalIgnoreCase);
            string slug = suffix > 0 ? teamId.Substring(0, suffix) : teamId;
            return Resources.Load<Sprite>("UI/Logos/" + slug);
        }

        private void AddTraitFeed(SimulationEvent evt)
        {
            if (string.IsNullOrEmpty(evt.actorId) || string.IsNullOrEmpty(evt.message))
                return;

            string[] parts = evt.message.Split('|');
            string traitId = parts[0];
            string traitName = DemoZh.TraitName(traitId);
            float bonus = 0f;
            string extra = null;

            // Formats:
            // traitId
            // traitId|+5
            // traitId|extra effect text
            // traitId|extra|ignored
            if (parts.Length >= 2)
            {
                string second = parts[1];
                if (second.StartsWith("+") || second.StartsWith("-"))
                    float.TryParse(second, out bonus);
                else if (!(second.Length > 0 && char.IsDigit(second[0])))
                    extra = second;
                else
                    float.TryParse(second, out bonus);
            }
            if (parts.Length >= 3 && string.IsNullOrEmpty(extra))
                extra = parts[1];

            string effect = DemoZh.FormatTraitEffect(traitId, bonus, extra);
            string player = ColoredPlayerName(evt.actorId, Mathf.Max(1, evt.roundNumber));
            string coloredTrait = ColoredTraitName(traitName);
            string coloredEffect = ColoredTraitEffect(effect);
            string line;

            if (string.Equals(traitId, "forest_intimidation", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(evt.targetId))
            {
                string target = ColoredPlayerName(evt.targetId, Mathf.Max(1, evt.roundNumber));
                line = $"{player} 觸發了 {coloredTrait}，與對位的對手 {target} {coloredEffect}";
            }
            else
            {
                line = $"{player} 觸發了 {coloredTrait}，{coloredEffect}";
            }

            _traitFeed.Add(line);
            while (_traitFeed.Count > 6)
                _traitFeed.RemoveAt(0);
            UpdateTraitFeed();
        }

        private void ResetRoundFeeds()
        {
            _killFeed.Clear();
            _traitFeed.Clear();
            UpdateKillFeed();
            UpdateTraitFeed();
        }

        private void UpdateTraitFeed()
        {
            if (_traitFeedText == null)
                return;
            _traitFeedText.text = _traitFeed.Count == 0
                ? string.Empty
                : string.Join("\n", _traitFeed);
        }

        private bool ApplyBombLootStatus(SimulationEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.message))
                return false;

            string msg = evt.message;
            if (msg.StartsWith("bomb_drop:", StringComparison.OrdinalIgnoreCase) ||
                (evt.reasonTags != null && Array.Exists(evt.reasonTags, t =>
                    string.Equals(t, "bomb_dropped", StringComparison.OrdinalIgnoreCase))))
            {
                string zone = msg.StartsWith("bomb_drop:", StringComparison.OrdinalIgnoreCase)
                    ? msg.Substring("bomb_drop:".Length)
                    : evt.zoneId;
                _bombCarrierId = null;
                _droppedBombZoneId = zone;
                if (!string.IsNullOrEmpty(zone))
                    ShowBomb(zone);
                RefreshStatPanels();
                return true;
            }

            if (string.Equals(msg, "bomb:1", StringComparison.OrdinalIgnoreCase) ||
                (evt.reasonTags != null && (
                    Array.Exists(evt.reasonTags, t => string.Equals(t, "bomb_carrier", StringComparison.OrdinalIgnoreCase)) ||
                    Array.Exists(evt.reasonTags, t => string.Equals(t, "bomb_pickup", StringComparison.OrdinalIgnoreCase)))))
            {
                _bombCarrierId = evt.actorId;
                _droppedBombZoneId = null;
                if (!IsBombPlantedVisible())
                    HideBomb();
                RefreshStatPanels();
                return true;
            }

            if (msg.StartsWith("kit:", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("gun:", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("gun_drop:", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(msg, "kit_drop", StringComparison.OrdinalIgnoreCase))
            {
                RefreshStatPanels();
                return true;
            }

            return false;
        }

        private bool IsBombPlantedVisible() =>
            !string.IsNullOrEmpty(_bombZoneId) &&
            string.IsNullOrEmpty(_droppedBombZoneId);

        private void ApplyStatusUpdate(SimulationEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.message) &&
                evt.message.StartsWith("clock:", StringComparison.OrdinalIgnoreCase))
                return;

            if (ApplyBombLootStatus(evt))
                return;

            if (ApplyBlindedStatus(evt))
                return;

            if (string.IsNullOrEmpty(evt.actorId))
                return;
            if (string.IsNullOrEmpty(evt.message))
                return;
            if (evt.message.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
            {
                int hp = 0;
                int armor = 0;
                string[] parts = evt.message.Split('|');
                foreach (string part in parts)
                {
                    if (part.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(part.Substring(3), out hp);
                    else if (part.StartsWith("armor:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(part.Substring(6), out armor);
                }
                SetMarkerVitality(evt.actorId, hp, armor);
                if (_stats.TryGetValue(evt.actorId, out PlayerStat hpStat))
                {
                    hpStat.Health = Mathf.Clamp(hp, 0, 100);
                    if (hp <= 0)
                        hpStat.Alive = false;
                    RefreshStatPanels();
                }
                return;
            }
            if (!_stats.TryGetValue(evt.actorId, out PlayerStat stat))
                return;
            string[] legacy = evt.message.Split('|');
            if (legacy.Length < 3)
                return;
            if (float.TryParse(legacy[0], out float stamina))
                stat.Stamina = stamina;
            if (float.TryParse(legacy[1], out float mentality))
                stat.Mentality = mentality;
            if (float.TryParse(legacy[2], out float form))
                stat.Form = form;
            RefreshStatPanels();
        }

        private void SetMarkerVitality(string playerId, int hp, int armor)
        {
            if (_markerHpFills.TryGetValue(playerId, out Image hpFill) && hpFill != null)
                hpFill.fillAmount = Mathf.Clamp01(hp / 100f);
            if (_markerArmorFills.TryGetValue(playerId, out Image armorFill) && armorFill != null)
                armorFill.fillAmount = Mathf.Clamp01(armor / 100f);
        }

        private bool ApplyBlindedStatus(SimulationEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.actorId) || string.IsNullOrEmpty(evt.message))
                return false;
            bool tagged = evt.reasonTags != null &&
                Array.Exists(evt.reasonTags, t =>
                    string.Equals(t, "utility_blinded", StringComparison.OrdinalIgnoreCase));
            if (!evt.message.StartsWith("blinded:", StringComparison.OrdinalIgnoreCase) && !tagged)
                return false;
            string payload = evt.message;
            int colon = payload.IndexOf(':');
            if (colon < 0 ||
                !float.TryParse(payload.Substring(colon + 1),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float until))
                return tagged;
            _blindedUntilByPlayer[evt.actorId] = until;
            EnsureBlindGlow(evt.actorId, true);
            RefreshStatPanels();
            return true;
        }

        private void EnsureBlindGlow(string playerId, bool on)
        {
            if (string.IsNullOrEmpty(playerId) || !_markers.TryGetValue(playerId, out RectTransform root) || root == null)
                return;
            if (!on)
            {
                if (_markerBlindGlows.TryGetValue(playerId, out Image existing) && existing != null)
                    existing.enabled = false;
                return;
            }
            if (!_markerBlindGlows.TryGetValue(playerId, out Image glow) || glow == null)
            {
                glow = CreateUiObject<Image>("BlindGlow", root);
                glow.sprite = _circleSprite;
                glow.raycastTarget = false;
                glow.color = new Color(1f, 1f, 1f, 0.55f);
                RectTransform rt = glow.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float d = QuickMarkerDotDiameter + 14f;
                rt.sizeDelta = new Vector2(d, d);
                rt.SetAsFirstSibling();
                _markerBlindGlows[playerId] = glow;
            }
            glow.enabled = true;
            glow.color = new Color(1f, 1f, 1f, 0.55f);
        }

        private void ExpireBlindedGlows(float roundTime)
        {
            if (_blindedUntilByPlayer.Count == 0)
                return;
            var expired = new List<string>();
            foreach (KeyValuePair<string, float> pair in _blindedUntilByPlayer)
            {
                if (roundTime + 0.001f >= pair.Value)
                    expired.Add(pair.Key);
            }
            for (int i = 0; i < expired.Count; i++)
            {
                string id = expired[i];
                _blindedUntilByPlayer.Remove(id);
                EnsureBlindGlow(id, false);
            }
        }

        private void ClearBlindedGlows()
        {
            foreach (KeyValuePair<string, Image> pair in _markerBlindGlows)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }
            _markerBlindGlows.Clear();
            _blindedUntilByPlayer.Clear();
        }

        private void RefreshMarkerHpBarsVisible()
        {
            // Intentionally unused: vitality bars were removed from map markers.
        }

        private void ApplyKillStats(SimulationEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.actorId) && _stats.TryGetValue(evt.actorId, out PlayerStat killer))
            {
                killer.Kills++;
                killer.RoundKills++;
            }
            if (!string.IsNullOrEmpty(evt.targetId) && _stats.TryGetValue(evt.targetId, out PlayerStat victim))
            {
                victim.Deaths++;
                victim.Alive = false;
                victim.Health = 0;
            }
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag == null || !tag.StartsWith("assist:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string assistId = tag.Substring("assist:".Length);
                    if (_stats.TryGetValue(assistId, out PlayerStat assist))
                        assist.Assists++;
                }
            }
            RefreshStatPanels();
        }

        private void ResetRoundKills()
        {
            foreach (PlayerStat stat in _stats.Values)
            {
                stat.RoundKills = 0;
                stat.Alive = true;
                stat.Health = 100;
            }
            RefreshStatPanels();
        }

        private void ResetStats()
        {
            _stats.Clear();
            foreach (TeamDefinition team in _content.Teams.Values)
            {
                foreach (string id in team.playerIds)
                    _stats[id] = new PlayerStat();
            }
            RefreshStatPanels();
        }

        private void ResetEconomyDisplay()
        {
            string teamA = _session?.State.TeamA.Definition.id ?? myTeamId;
            string teamB = _session?.State.TeamB.Definition.id ?? _opponentTeamId;
            foreach (string teamId in new[] { teamA, teamB })
            {
                if (string.IsNullOrEmpty(teamId))
                    continue;
                _teamMoney[teamId] = IsGeneralBattleMode()
                    ? ItemizedEconomyRules.StartingPlayerMoney * 5
                    : EconomyRules.StartingTeamMoney;
                _teamCards[teamId] = EquipmentLevel.Eco;
            }
            UpdateEconomyHud();
        }

        private void ApplyEconomyTags(SimulationEvent evt)
        {
            if (evt?.reasonTags == null)
                return;

            bool changed = false;
            foreach (string tag in evt.reasonTags)
            {
                if (tag == null)
                    continue;
                if (tag.StartsWith("money:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = tag.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int money))
                    {
                        _teamMoney[parts[1]] = money;
                        changed = true;
                    }
                }
                else if (tag.StartsWith("card:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = tag.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int level))
                    {
                        _teamCards[parts[1]] = (EquipmentLevel)level;
                        changed = true;
                    }
                }
                else if (tag.StartsWith("team_money:", StringComparison.OrdinalIgnoreCase) &&
                         !string.IsNullOrEmpty(evt.teamId) &&
                         int.TryParse(tag.Substring("team_money:".Length), out int teamMoney))
                {
                    _teamMoney[evt.teamId] = teamMoney;
                    changed = true;
                }
            }

            if (changed)
                UpdateEconomyHud();
        }

        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(leftIsT));
            Color rightInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(rightIsT));
            if (IsGeneralBattleMode())
            {
                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText);
                BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText);
            }
            else
            {
                if (_leftMoneyText != null)
                {
                    _leftMoneyText.color = leftInk;
                    _leftMoneyText.text = FormatEconomyMoney(leftId) + " · " + FormatEconomyCp(leftId);
                }
                if (_rightMoneyText != null)
                {
                    _rightMoneyText.color = rightInk;
                    _rightMoneyText.text = FormatEconomyMoney(rightId) + " · " + FormatEconomyCp(rightId);
                }
            }
        }

        private void BindTeamEconomyTexts(
            string teamId, Color ink, Text moneyText, Text cpText)
        {
            if (moneyText != null)
            {
                moneyText.color = ink;
                moneyText.text = FormatEconomyMoney(teamId);
            }
            if (cpText != null)
            {
                cpText.color = ink;
                cpText.text = FormatEconomyCp(teamId);
            }
        }

        private void HideRoundWinBanner()
        {
            if (_roundWinRoutine != null)
            {
                StopCoroutine(_roundWinRoutine);
                _roundWinRoutine = null;
            }
            if (_roundWinGroup != null)
                _roundWinGroup.alpha = 0f;
            if (_roundWinBanner != null)
                _roundWinBanner.text = string.Empty;
        }

        private void ShowRoundWinBanner(string winnerTeamId, int roundNumber, bool animate)
        {
            if (_roundWinBanner == null || string.IsNullOrEmpty(winnerTeamId))
                return;

            bool winnerIsT = TeamIsTerrorist(winnerTeamId, roundNumber);
            _roundWinBanner.text = winnerIsT ? "T陣營勝利" : "CT陣營勝利";
            _roundWinBanner.color = winnerIsT ? TText : CtText;
            _roundWinBanner.transform.SetAsLastSibling();

            if (_roundWinRoutine != null)
                StopCoroutine(_roundWinRoutine);

            if (!animate)
            {
                if (_roundWinGroup != null)
                    _roundWinGroup.alpha = 0f;
                _roundWinBanner.text = string.Empty;
                return;
            }

            _roundWinRoutine = StartCoroutine(RoundWinBannerRoutine());
        }

        private IEnumerator RoundWinBannerRoutine()
        {
            if (_roundWinGroup == null)
                yield break;

            _roundWinGroup.alpha = 0f;
            float fadeIn = 0.25f;
            float hold = 1.15f;
            float fadeOut = 0.45f;
            float t = 0f;
            while (t < fadeIn)
            {
                if (!_paused)
                    t += Time.unscaledDeltaTime;
                _roundWinGroup.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _roundWinGroup.alpha = 1f;

            t = 0f;
            while (t < hold)
            {
                if (!_paused)
                    t += Time.unscaledDeltaTime;
                yield return null;
            }

            t = 0f;
            while (t < fadeOut)
            {
                if (!_paused)
                    t += Time.unscaledDeltaTime;
                _roundWinGroup.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            _roundWinGroup.alpha = 0f;
            _roundWinBanner.text = string.Empty;
            _roundWinRoutine = null;
        }

        private string FormatEconomyMoney(string teamId)
        {
            int money = _teamMoney.TryGetValue(teamId, out int value) ? value : EconomyRules.StartingTeamMoney;
            return "$" + money.ToString("N0");
        }

        private string FormatEconomyCp(string teamId)
        {
            int cp = _session != null ? _session.TeamById(teamId).CommandPoints : CommandPointRules.StartingPoints;
            return "CP " + cp;
        }

        private string FormatEconomyCardName(string teamId)
        {
            EquipmentLevel card = _teamCards.TryGetValue(teamId, out EquipmentLevel level) ? level : EquipmentLevel.Eco;
            return EconomyRules.CardName(card);
        }

        private void UpdateTeamHeaders()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(leftIsT));
            Color rightInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(rightIsT));
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = leftInk;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = rightInk;
            }
            UpdateEconomyHud();
        }

        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            // Header bar: opaque faction light so dark team/economy/CP ink stays readable.
            if (_leftTeamHeaderBlock != null)
            {
                BattleStatPanelPresentation.ApplyOpaqueFill(
                    _leftTeamHeaderBlock,
                    BattleStatPanelPresentation.AccentColor(leftIsT));
            }
            if (_rightTeamHeaderBlock != null)
            {
                BattleStatPanelPresentation.ApplyOpaqueFill(
                    _rightTeamHeaderBlock,
                    BattleStatPanelPresentation.AccentColor(rightIsT));
            }
            if (IsGeneralBattleMode())
            {
                _leftStatsPanel.color = Color.clear;
                _rightStatsPanel.color = Color.clear;
            }
            else
            {
                Color leftPanel = leftIsT ? TPanel : CtPanel;
                Color rightPanel = rightIsT ? TPanel : CtPanel;
                _leftStatsPanel.color = leftPanel;
                _rightStatsPanel.color = rightPanel;
            }
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }

        private void RefreshStatPanels()
        {
            if (_leftStatSlots.Count == 0 || _content == null)
                return;

            _statLines.Clear();
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            BindStatSlots(_leftStatSlots, _leftRoundKillSlots, _leftStatusRows, _content.Teams[leftId]);
            BindStatSlots(_rightStatSlots, _rightRoundKillSlots, _rightStatusRows, _content.Teams[rightId]);
        }

        private void BindStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            if (IsGeneralBattleMode())
                BindGeneralStatSlots(slots, team);
            else
                BindClassicStatSlots(slots, roundKillSlots, statusRows, team);
        }

        private void BindClassicStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<Image> jerseys = leftSide ? _leftJerseyBadges : _rightJerseyBadges;
            List<Text> jerseyNums = leftSide ? _leftJerseyNumbers : _rightJerseyNumbers;
            List<Image> rows = leftSide ? _leftStatRows : _rightStatRows;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color roundKillColor = teamIsT ? TText : CtText;
            Color sideColor = SideBadgeColor(teamIsT);
            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= team.playerIds.Length)
                {
                    if (slots[i] != null)
                        slots[i].text = string.Empty;
                    if (i < roundKillSlots.Count && roundKillSlots[i] != null)
                        roundKillSlots[i].text = string.Empty;
                    if (i < statusRows.Count)
                        ClearStatusMetrics(statusRows[i]);
                    if (i < jerseys.Count && jerseys[i] != null)
                        jerseys[i].gameObject.SetActive(false);
                    if (i < rows.Count && rows[i] != null)
                        rows[i].color = AliveRowBg;
                    if (i < rowPlayerIds.Count)
                        rowPlayerIds[i] = string.Empty;
                    continue;
                }

                string id = team.playerIds[i];
                int number = PlayerNumber(id);
                PlayerStat stat = _stats.TryGetValue(id, out PlayerStat found) ? found : new PlayerStat();
                bool alive = stat.Alive;
                Color nameColor = alive ? TextPrimary : DeadRowText;
                Color jerseyColor = alive ? sideColor : DeadRowBadge;
                Color jerseyNumColor = alive ? TextPrimary : DeadRowMuted;
                Color rkColor = alive ? roundKillColor : DeadRowMuted;
                slots[i].text = DemoZh.PlayerName(id);
                slots[i].color = nameColor;
                _statLines[id] = slots[i];
                if (i < rowPlayerIds.Count)
                    rowPlayerIds[i] = id;
                if (i < rows.Count && rows[i] != null)
                    rows[i].color = alive ? AliveRowBg : DeadRowBg;
                if (i < jerseys.Count && jerseys[i] != null)
                {
                    jerseys[i].gameObject.SetActive(true);
                    jerseys[i].color = jerseyColor;
                }
                if (i < jerseyNums.Count && jerseyNums[i] != null)
                {
                    jerseyNums[i].text = number > 0 ? number.ToString() : string.Empty;
                    jerseyNums[i].color = jerseyNumColor;
                }
                if (i < roundKillSlots.Count && roundKillSlots[i] != null)
                {
                    roundKillSlots[i].color = rkColor;
                    roundKillSlots[i].text = stat.RoundKills > 0 ? stat.RoundKills.ToString() : string.Empty;
                }
                if (i < statusRows.Count)
                    BindStatusMetrics(statusRows[i], stat, alive);
            }
        }

        private void BindGeneralStatSlots(List<Text> slots, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;

            for (int i = 0; i < cards.Count; i++)
            {
                BattlePlayerCardView card = cards[i];
                if (i >= team.playerIds.Length)
                {
                    BattleStatPanelPresentation.Clear(card);
                    if (i < rowPlayerIds.Count)
                        rowPlayerIds[i] = string.Empty;
                    continue;
                }

                string id = team.playerIds[i];
                PlayerStat stat = _stats.TryGetValue(id, out PlayerStat found) ? found : new PlayerStat();
                PlayerRuntime runtime = FindPlayerRuntime(id);
                int health = stat.Health;
                int money = teamMoney;
                string primaryName = string.Empty;
                string secondaryName = string.Empty;
                string primaryWeaponId = string.Empty;
                string secondaryWeaponId = string.Empty;
                bool hasKit = false;
                bool hasBomb = false;
                bool hasFlash = false, hasSmoke = false, hasHe = false, hasFire = false;
                bool fireIsIncendiary = !teamIsT;
                BattleArmorHudState armor = BattleArmorHudState.None;
                if (runtime != null)
                {
                    if (runtime.Health > 0 || !stat.Alive)
                        health = runtime.Health;
                    money = runtime.Money;
                    if (runtime.Loadout != null)
                    {
                        primaryWeaponId = runtime.Loadout.PrimaryWeaponId ?? string.Empty;
                        secondaryWeaponId = runtime.Loadout.PistolWeaponId ?? string.Empty;
                        primaryName = ResolveWeaponDisplayName(primaryWeaponId);
                        secondaryName = ResolveWeaponDisplayName(secondaryWeaponId);
                        hasKit = !teamIsT && runtime.Loadout.HasDefuseKit;
                        hasFlash = runtime.Loadout.HasFlash;
                        hasSmoke = runtime.Loadout.HasSmoke;
                        hasHe = runtime.Loadout.HasHe;
                        hasFire = runtime.Loadout.HasFire;
                        fireIsIncendiary = runtime.Loadout.ResolvedFireKind(
                            teamIsT ? Side.Terrorist : Side.CounterTerrorist) == FireNadeKind.Incendiary;
                        if (runtime.Loadout.HasArmor && runtime.Loadout.HasHelmet)
                            armor = BattleArmorHudState.Full;
                        else if (runtime.Loadout.HasArmor)
                            armor = BattleArmorHudState.Half;
                    }
                    if (!stat.Alive)
                        health = 0;
                }
                hasBomb = teamIsT && stat.Alive &&
                    !string.IsNullOrEmpty(_bombCarrierId) &&
                    string.Equals(_bombCarrierId, id, StringComparison.OrdinalIgnoreCase);
                if (runtime == null && !stat.Alive)
                    health = 0;

                var data = new BattlePlayerCardData
                {
                    PlayerId = id,
                    DisplayName = DemoZh.PlayerName(id),
                    JerseyNumber = PlayerNumber(id),
                    Health = health,
                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    RoundKills = stat.RoundKills,
                    Money = money,
                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    PrimaryWeaponId = primaryWeaponId,
                    SecondaryWeaponId = secondaryWeaponId,
                    HasFlash = hasFlash,
                    HasSmoke = hasSmoke,
                    HasHe = hasHe,
                    HasFire = hasFire,
                    FireIsIncendiary = fireIsIncendiary,
                    HasDefuseKit = hasKit,
                    HasBomb = hasBomb,
                    Armor = armor,
                    Mentality = stat.Mentality,
                    Form = stat.Form,
                    Stamina = stat.Stamina,
                    Alive = stat.Alive
                };
                BattleStatPanelPresentation.Bind(card, data, panelDark, accentLight);
                if (i < rowPlayerIds.Count)
                    rowPlayerIds[i] = id;
            }
        }

        private string ResolveWeaponDisplayName(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || _weaponCatalog == null)
                return string.Empty;
            return _weaponCatalog.TryGet(weaponId, out WeaponDefinition def) && def != null
                ? def.displayName
                : weaponId;
        }

        private static void ClearStatusMetrics(StatMetricSlot[] metrics)
        {
            if (metrics == null)
                return;
            for (int i = 0; i < metrics.Length; i++)
            {
                if (metrics[i]?.Value != null)
                    metrics[i].Value.text = string.Empty;
            }
        }

        private static void BindStatusMetrics(StatMetricSlot[] metrics, PlayerStat stat, bool alive = true)
        {
            if (metrics == null || metrics.Length < 6)
                return;
            Color frontTint = alive ? TextPrimary : DeadRowText;
            Color backTint = alive ? TextMuted : DeadRowMuted;
            SetMetric(metrics[0], stat.Kills.ToString(), frontTint);
            SetMetric(metrics[1], stat.Deaths.ToString(), frontTint);
            SetMetric(metrics[2], stat.Assists.ToString(), frontTint);
            SetMetric(metrics[3], $"{stat.Stamina:0}", backTint);
            SetMetric(metrics[4], $"{stat.Mentality:0}", backTint);
            SetMetric(metrics[5], $"{stat.Form:0}", backTint);
        }

        private static void SetMetric(StatMetricSlot slot, string value, Color tint)
        {
            if (slot?.Value != null)
            {
                slot.Value.text = value;
                slot.Value.color = tint;
            }
            if (slot?.Icon != null)
                slot.Icon.color = tint;
        }

        private void CreateAliveCountHud(Transform parent)
        {
            Image root = CreateUiObject<Image>("AliveCount", parent);
            // Sit just under the top score bar (TopHud is ~0.900–0.992).
            SetRect(root.rectTransform, new Vector2(0.40f, 0.848f), new Vector2(0.60f, 0.882f));
            root.color = new Color(Panel.r, Panel.g, Panel.b, 0.88f);
            root.raycastTarget = false;
            _aliveCountRoot = root.rectTransform;

            _leftAliveBadge = CreateUiObject<Image>("LeftAlive", root.transform);
            SetRect(_leftAliveBadge.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.32f, 0.88f));
            _leftAliveBadge.color = CtAliveBg;
            _leftAliveBadge.raycastTarget = false;
            _leftAliveText = CreateText("Value", _leftAliveBadge.transform, 18, TextAnchor.MiddleCenter, TextPrimary);
            Stretch(_leftAliveText.rectTransform);
            _leftAliveText.fontStyle = FontStyle.Bold;
            _leftAliveText.raycastTarget = false;
            _leftAliveText.text = "5";

            _aliveVsText = CreateText("Vs", root.transform, 14, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_aliveVsText.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.68f, 1f));
            _aliveVsText.text = "VS";
            _aliveVsText.fontStyle = FontStyle.Bold;
            _aliveVsText.raycastTarget = false;

            _rightAliveBadge = CreateUiObject<Image>("RightAlive", root.transform);
            SetRect(_rightAliveBadge.rectTransform, new Vector2(0.68f, 0.12f), new Vector2(0.96f, 0.88f));
            _rightAliveBadge.color = TAliveBg;
            _rightAliveBadge.raycastTarget = false;
            _rightAliveText = CreateText("Value", _rightAliveBadge.transform, 18, TextAnchor.MiddleCenter, TextPrimary);
            Stretch(_rightAliveText.rectTransform);
            _rightAliveText.fontStyle = FontStyle.Bold;
            _rightAliveText.raycastTarget = false;
            _rightAliveText.text = "5";

            root.gameObject.SetActive(false);
        }

        private void UpdateAliveCountHud()
        {
            if (_aliveCountRoot == null || _content == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int leftAlive = CountAliveOnTeam(leftId);
            int rightAlive = CountAliveOnTeam(rightId);
            int leftRoster = RosterSize(leftId);
            int rightRoster = RosterSize(rightId);
            bool show = _roundHasDeath || leftAlive < leftRoster || rightAlive < rightRoster;
            _aliveCountRoot.gameObject.SetActive(show);
            if (!show)
                return;

            _aliveCountRoot.SetAsLastSibling();
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            if (_leftAliveBadge != null)
                _leftAliveBadge.color = leftIsT ? TAliveBg : CtAliveBg;
            if (_rightAliveBadge != null)
                _rightAliveBadge.color = rightIsT ? TAliveBg : CtAliveBg;
            if (_leftAliveText != null)
                _leftAliveText.text = leftAlive.ToString();
            if (_rightAliveText != null)
                _rightAliveText.text = rightAlive.ToString();
            if (_aliveVsText != null)
                _aliveVsText.text = "VS";
        }

        private int CountAliveOnTeam(string teamId)
        {
            if (_content == null || !_content.Teams.TryGetValue(teamId, out TeamDefinition team))
                return 0;
            int count = 0;
            foreach (string id in team.playerIds)
            {
                bool alive = true;
                if (_stats.TryGetValue(id, out PlayerStat stat))
                    alive = stat.Alive;
                // Marker hide is the presentation source of truth mid-playback.
                if (_markers.TryGetValue(id, out RectTransform marker) && marker != null && !marker.gameObject.activeSelf)
                    alive = false;
                if (alive)
                    count++;
            }
            return count;
        }

        private int RosterSize(string teamId)
        {
            if (_content == null || !_content.Teams.TryGetValue(teamId, out TeamDefinition team))
                return 5;
            return Mathf.Max(1, team.playerIds.Length);
        }

        private void UpdateScore()
        {
            if (_leftScoreText == null || _rightScoreText == null)
                return;

            string teamAId = _session?.State.TeamA.Definition.id ?? myTeamId;
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int teamAScore = _naviScore;
            int teamBScore = _gambitScore;
            int leftScore = string.Equals(leftId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;
            int rightScore = string.Equals(rightId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;
            _leftScoreText.text = $"{leftScore:00}";
            _rightScoreText.text = $"{rightScore:00}";
            BindTeamLogo(_leftTeamLogo, leftId);
            BindTeamLogo(_rightTeamLogo, rightId);
        }

        private void SetRoundHud(int roundNumber, string clockOrStatus)
        {
            if (_roundText != null)
                _roundText.text = $"ROUND {Mathf.Max(1, roundNumber):00}";
            if (_clockText != null)
            {
                _clockText.text = string.IsNullOrEmpty(clockOrStatus) ? string.Empty : clockOrStatus;
                ApplyClockPlantedColor();
            }
        }

        private void ApplyClockPlantedColor()
        {
            if (_clockText == null)
                return;
            _clockText.color = _bombPlantedAtRoundTime >= 0f ? BombClockRed : TextPrimary;
        }

        private void SetPhaseStatus(string status)
        {
            int round = _session != null ? Mathf.Max(1, _session.NextRoundNumber) : Mathf.Max(1, _currentRound);
            SetRoundHud(round, status);
        }

        private string FormatClock(float seconds)
        {
            // After plant, HUD switches to the 40s bomb countdown.
            if (_bombPlantedAtRoundTime >= 0f)
            {
                float elapsed = Mathf.Max(0f, seconds - _bombPlantedAtRoundTime);
                int remaining = Mathf.Max(0, Mathf.CeilToInt(BombCountdownSeconds - elapsed));
                return $"{remaining / 60}:{remaining % 60:00}";
            }

            int roundLeft = Mathf.Max(0, 115 - Mathf.FloorToInt(seconds));
            return $"{roundLeft / 60}:{roundLeft % 60:00}";
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

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private static T CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private Text CreateText(
            string name, Transform parent, int size, TextAnchor alignment, Color color, Font font = null)
        {
            Text text = CreateUiObject<Text>(name, parent);
            text.font = font != null ? font : _uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            return text;
        }

        private Button CreateButton(
            string name, string label, Transform parent, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            Image edge = CreateUiObject<Image>(name, parent);
            edge.color = ButtonEdge;
            SetRect(edge.rectTransform, min, max);

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.color = ButtonFace;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.25f, 0.29f, 1f);
            colors.pressedColor = new Color(0.12f, 0.13f, 0.15f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);

            Text text = CreateText("Text", face.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            text.text = label;
            Stretch(text.rectTransform);
            return button;
        }

        private Button CreateIconButton(
            string name,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Sprite icon,
            UnityEngine.Events.UnityAction action,
            out Image iconImage)
        {
            Image edge = CreateUiObject<Image>(name, parent);
            edge.color = ButtonEdge;
            SetRect(edge.rectTransform, min, max);

            Image face = CreateUiObject<Image>("Face", edge.transform);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            face.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            face.color = ButtonFace;

            Button button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.25f, 0.29f, 1f);
            colors.pressedColor = new Color(0.12f, 0.13f, 0.15f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);

            iconImage = CreateUiObject<Image>("Icon", face.transform);
            Stretch(iconImage.rectTransform);
            iconImage.rectTransform.offsetMin = new Vector2(8f, 6f);
            iconImage.rectTransform.offsetMax = new Vector2(-8f, -6f);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = TextPrimary;
            iconImage.raycastTarget = false;
            return button;
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


        private static Color ViewConeColor(bool isTerrorist)
        {
            Color c = isTerrorist ? TText : CtText;
            c.a = 0.26f;
            return c;
        }

        private void InitTeamFacings(TeamDefinition team, string spawn)
        {
            if (team == null)
                return;
            Vector2 from = LocalZonePosition(spawn);
            string other = string.Equals(spawn, "t_spawn", StringComparison.OrdinalIgnoreCase)
                ? "ct_spawn"
                : "t_spawn";
            Vector2 to = LocalZonePosition(other);
            float facing = FacingDegrees(to - from);
            for (int i = 0; i < team.playerIds.Length; i++)
                ApplyViewConeFacing(team.playerIds[i], facing);
        }

        private static float FacingDegrees(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.01f)
                return 90f;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        private void FaceMarkerToward(string playerId, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 4f)
                return;
            ApplyViewConeFacing(playerId, FacingDegrees(delta));
        }

        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            RefreshMarkerLos(playerId);
        }

        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            if (width < 8f || height < 8f)
                return;
            _collisionMapSize = new Vector2(width, height);
            _mapCollision.Rebuild(_mapSprite, width, height, _collisionMask);
            if (_session != null)
                _session.CombatLos = _mapCollision.IsReady ? _mapCollision : null;
            RefreshAllMarkerLos();
        }

        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode())
                return;
            GetMapContentSize(out float width, out float height);
            if (!_mapCollision.IsReady ||
                Mathf.Abs(width - _collisionMapSize.x) > 1f ||
                Mathf.Abs(height - _collisionMapSize.y) > 1f)
                RebuildMapCollision();
            if (!_mapCollision.IsReady)
                return;

            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
                    continue;
                // Pure pan/zoom keeps map-local pose unchanged — skip LOS entirely.
                if (!MarkerLosIsDirty(pair.Key, pair.Value))
                    continue;
                RefreshMarkerLos(pair.Key, force: true);
            }
        }

        private bool MarkerLosIsDirty(string playerId, RectTransform marker)
        {
            if (marker == null)
                return true;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;
            Vector2 pos = marker.anchoredPosition;
            if (!_markerLosCachedPos.TryGetValue(playerId, out Vector2 cachedPos) ||
                !_markerLosCachedFacing.TryGetValue(playerId, out float cachedFacing))
                return true;
            if ((pos - cachedPos).sqrMagnitude > MarkerLosPosEpsilon * MarkerLosPosEpsilon)
                return true;
            if (Mathf.Abs(Mathf.DeltaAngle(cachedFacing, facingDeg)) > MarkerLosFacingEpsilon)
                return true;
            return false;
        }

        private void RefreshAllMarkerLos()
        {
            _markerLosCachedPos.Clear();
            _markerLosCachedFacing.Clear();
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                RefreshMarkerLos(pair.Key, force: true);
        }

        private void RefreshMarkerLos(string playerId, bool force = false)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                return;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;
            if (!force && !MarkerLosIsDirty(playerId, marker))
                return;

            bool general = IsGeneralBattleMode();
            float coneLen = general ? ViewConeLength : ViewConeLength * 1.15f;
            int samples = ViewConeRaySamples;
            Vector2 origin = marker.anchoredPosition;
            float half = ViewConeDegrees * 0.5f;
            for (int i = 0; i < samples; i++)
            {
                float t = samples <= 1 ? 0.5f : i / (float)(samples - 1);
                float ang = (facingDeg - half + ViewConeDegrees * t) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float dist = coneLen;
                if (general && _mapCollision.IsReady)
                    dist = _mapCollision.Raycast(origin, dir, coneLen);
                _coneRimScratch[i] = dir * dist;
            }

            if (_markerConeMeshes.TryGetValue(playerId, out ViewConeMeshGraphic mesh) && mesh != null)
            {
                bool isT = PlayerIsTerrorist(playerId, _currentRound);
                mesh.rectTransform.localEulerAngles = Vector3.zero;
                mesh.SetFan(ViewConeColor(isT), _coneRimScratch, samples);
            }
            else if (_markerCones.TryGetValue(playerId, out Image cone) && cone != null)
                cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);

            if (_markerSightRays.TryGetValue(playerId, out Image sight) && sight != null)
            {
                float rad = facingDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float muzzle = ActiveMarkerDotDiameter() * 0.48f;
                sight.rectTransform.anchoredPosition = dir * muzzle;
                sight.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg);
                float maxLen = general ? SightRayLength : SightRayLength * 1.08f;
                float hit = maxLen;
                if (general && _mapCollision.IsReady)
                    hit = _mapCollision.Raycast(origin + dir * muzzle, dir, maxLen);
                sight.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, hit), SightRayWidth);
                sight.gameObject.SetActive(general && marker.gameObject.activeSelf);
            }

            _markerLosCachedPos[playerId] = origin;
            _markerLosCachedFacing[playerId] = facingDeg;
        }

        private void ShowMuzzleRay(string shooterId, string targetId)
        {
            if (string.IsNullOrEmpty(shooterId) || string.IsNullOrEmpty(targetId))
                return;
            if (!_markers.TryGetValue(shooterId, out RectTransform fromRt) || fromRt == null)
                return;
            if (!_markers.TryGetValue(targetId, out RectTransform toRt) || toRt == null)
                return;
            if (!fromRt.gameObject.activeInHierarchy)
                return;

            Vector2 from = fromRt.anchoredPosition;
            Vector2 to = toRt.anchoredPosition;
            FaceMarkerToward(shooterId, from, to);
            if (IsGeneralBattleMode() && _mapCollision.IsReady)
                _mapCollision.TryClipSegment(from, to, out to);
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 4f)
                return;

            Image ray = null;
            MuzzleRay slot = null;
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                if (_muzzleRays[i].Image != null && !_muzzleRays[i].Image.gameObject.activeSelf)
                {
                    slot = _muzzleRays[i];
                    ray = slot.Image;
                    break;
                }
            }
            if (ray == null)
            {
                ray = CreateUiObject<Image>("MuzzleRay", _mapRect);
                ray.sprite = _lineSprite != null ? _lineSprite : _circleSprite;
                ray.raycastTarget = false;
                ray.rectTransform.anchorMin = ray.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ray.rectTransform.pivot = new Vector2(0f, 0.5f);
                slot = new MuzzleRay { Image = ray };
                _muzzleRays.Add(slot);
            }

            Color tint = MuzzleTracerColor;
            ray.color = tint;
            ray.gameObject.SetActive(true);
            ray.rectTransform.SetAsLastSibling();
            ray.rectTransform.anchoredPosition = from;
            ray.rectTransform.sizeDelta = new Vector2(Mathf.Min(MuzzleBoltLength, dist), MuzzleRayWidth);
            ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, FacingDegrees(delta));
            slot.From = from;
            slot.To = to;
            slot.BaseColor = tint;
            slot.ExpireAtUnscaled = Time.unscaledTime + MuzzleRaySeconds;
        }

        private void TickMuzzleRays()
        {
            if (_muzzleRays.Count == 0)
                return;
            float now = Time.unscaledTime;
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                MuzzleRay ray = _muzzleRays[i];
                if (ray.Image == null || !ray.Image.gameObject.activeSelf)
                    continue;
                float left = ray.ExpireAtUnscaled - now;
                if (left <= 0f)
                {
                    ray.Image.gameObject.SetActive(false);
                    continue;
                }
                float u = 1f - Mathf.Clamp01(left / MuzzleRaySeconds);
                Vector2 delta = ray.To - ray.From;
                float dist = delta.magnitude;
                if (dist < 1f)
                {
                    ray.Image.gameObject.SetActive(false);
                    continue;
                }
                Vector2 dir = delta / dist;
                float bolt = Mathf.Min(MuzzleBoltLength, dist * 0.45f);
                float head = dist * Mathf.SmoothStep(0f, 1f, u);
                float tail = Mathf.Max(0f, head - bolt);
                float len = Mathf.Max(2f, head - tail);
                ray.Image.rectTransform.anchoredPosition = ray.From + dir * tail;
                ray.Image.rectTransform.sizeDelta = new Vector2(len, MuzzleRayWidth);
                ray.Image.rectTransform.localEulerAngles = new Vector3(0f, 0f, FacingDegrees(delta));
                Color c = ray.BaseColor;
                c.a = ray.BaseColor.a * (u < 0.72f ? 1f : Mathf.Clamp01((1f - u) / 0.28f));
                ray.Image.color = c;
            }
        }

        private void ClearMuzzleRays()
        {
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                if (_muzzleRays[i].Image != null)
                    _muzzleRays[i].Image.gameObject.SetActive(false);
            }
        }

        private static Sprite CreateLineSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite CreateCircleSprite(bool filled)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float outer = size * 0.47f;
            float inner = size * 0.34f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool on = filled ? distance <= outer : distance <= outer && distance >= inner;
                pixels[y * size + x] = on ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
