using System;
using System.Collections.Generic;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticaUniversalis.Presentation
{
    public sealed class SettingsPanelHost
    {
        public bool CareerSaveLoadEnabled;
        public Func<bool> CanLoad;
        public Func<bool> CanSave;
        public Func<string> DifficultyHint;
        public Func<int> GetSelectedSlot;
        public Action<int> OnSelectSlot;
        public Func<int, string> DescribeSlot;
        public Func<int, bool> SlotHasSave;
        public Action<int> OnLoadSlot;
        public Action<int> OnSaveSlot;
        public Action<int> OnDeleteSlot;
        public Action OnReturnToMainMenu;
        public Action OnClose;
        public Action OnChanged;
    }

    public sealed class SettingsPanelPresentation
    {
        private static readonly Color Panel = CareerEditorialChrome.Paper;
        private static readonly Color PanelInner = CareerEditorialChrome.Paper;
        private static readonly Color PanelRow = CareerEditorialChrome.Rule;
        private static readonly Color Accent = CareerEditorialChrome.Mustard;
        private static readonly Color AccentDim = CareerEditorialChrome.Mustard;
        private static readonly Color TextPrimary = CareerEditorialChrome.Ink;
        private static readonly Color TextMuted = CareerEditorialChrome.InkMuted;
        private static readonly Color ButtonFace = CareerEditorialChrome.Paper;
        private static readonly Color ButtonEdge = CareerEditorialChrome.Ink;
        private static readonly Color Danger = CareerEditorialChrome.Red;
        private static readonly Color DangerFace = CareerEditorialChrome.Red;
        private static readonly Color SelectedFace = CareerEditorialChrome.Mustard;

        private readonly SettingsPanelHost _host;
        private readonly Font _font;
        private GameObject _root;
        private RectTransform _listRoot;
        private Text _difficultyHint;
        private Text _saveLoadHint;
        private readonly List<SaveSlotRow> _saveSlotRows = new List<SaveSlotRow>();
        private GameObject _saveBrowserRoot;
        private GameObject _deleteConfirmRoot;
        private Text _deleteConfirmBody;
        private int _pendingDeleteSlot = -1;
        private readonly List<ExpandableChoice> _choices = new List<ExpandableChoice>();
        private readonly List<CollapsibleSection> _sections = new List<CollapsibleSection>();
        private Slider _masterSlider;
        private Slider _sfxSlider;
        private Slider _musicSlider;
        private Text _masterValue;
        private Text _sfxValue;
        private Text _musicValue;
        private Button _muteUnfocusedButton;
        private Text _muteUnfocusedLabel;
        private Button _autoSaveButton;
        private Text _autoSaveLabel;
        private Button _pauseOnMatchEndButton;
        private Text _pauseOnMatchEndLabel;
        private Button _pauseOnEventSButton;
        private Text _pauseOnEventSLabel;
        private Button _pauseOnEventAButton;
        private Text _pauseOnEventALabel;
        private Button _pauseOnEventBButton;
        private Text _pauseOnEventBLabel;
        private Button _pauseOnEventCButton;
        private Text _pauseOnEventCLabel;
        private bool _suppressSliderEvents;
        private Image _optionHoverTipRoot;
        private Text _optionHoverTipBody;
        private UiFrostedGlassHost _frostHost;

        private sealed class SaveSlotRow
        {
            public int Slot;
            public Image Face;
            public Text MetaLabel;
            public Button LoadButton;
            public Button SaveButton;
            public Button DeleteButton;
        }

        private sealed class ChoiceButton
        {
            public Button Button;
            public Image Face;
            public Text Label;
            public object Value;
        }

        private sealed class ExpandableChoice
        {
            public string Title;
            public Func<object> GetSelected;
            public Func<object, string> FormatSelected;
            public Text HeaderValueLabel;
            public Text ChevronLabel;
            public GameObject OptionsRoot;
            public LayoutElement OptionsLayout;
            public readonly List<ChoiceButton> Buttons = new List<ChoiceButton>();
            public bool IsOpen;
        }

        private sealed class CollapsibleSection
        {
            public Text ChevronLabel;
            public GameObject BodyRoot;
            public LayoutElement BodyLayout;
            public bool IsOpen = true;
        }

        public GameObject Root => _root;
        public bool IsVisible => _root != null && _root.activeSelf;

        public SettingsPanelPresentation(Font font, SettingsPanelHost host)
        {
            _font = font;
            _host = host ?? new SettingsPanelHost();
        }

        public void Build(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("SettingsOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            _root = dim.gameObject;
            _root.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(RequestClose);

            // D1 light_chrome via shared utility; E1 glass capsule buttons.
            Image panel = UiLightChromeUtility.CreateCard(
                "SettingsPanel", dim.transform,
                new Vector2(0.18f, 0.04f), new Vector2(0.86f, 0.96f), Panel);
            UiChromeMotion.BindFace(_root, panel);

            Text title = CreateText("Title", panel.transform, 20, TextAnchor.MiddleLeft, Accent);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.92f), new Vector2(0.62f, 0.985f));
            title.text = "設定";

            UiLightChromeUtility.CreateSquareCloseButton(
                "Close", panel.transform,
                new Vector2(0.935f, 0.925f), new Vector2(0.985f, 0.985f),
                RequestClose, _font, 18);

            Image scrollHost = CreateUiObject<Image>("SettingsScrollHost", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.91f));
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

            _listRoot = new GameObject("SettingsList", typeof(RectTransform)).GetComponent<RectTransform>();
            _listRoot.SetParent(viewport.transform, false);
            _listRoot.anchorMin = new Vector2(0f, 1f);
            _listRoot.anchorMax = new Vector2(1f, 1f);
            _listRoot.pivot = new Vector2(0.5f, 1f);
            _listRoot.anchoredPosition = Vector2.zero;
            _listRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = _listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(2, 2, 4, 12);

            var fitter = _listRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _listRoot;

            BuildDisplaySection();
            BuildAudioSection();
            BuildGameSection();
            BuildFixedActionRows();
            BuildOptionHoverTip(panel.transform);
            BuildSaveBrowser(canvas);
            BuildDeleteConfirm(canvas);
        }

        private void BuildOptionHoverTip(Transform panel)
        {
            if (_root != null)
            {
                _frostHost = _root.GetComponent<UiFrostedGlassHost>();
                if (_frostHost == null)
                    _frostHost = _root.AddComponent<UiFrostedGlassHost>();
            }

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Image tipFrame = CreateUiObject<Image>("OptionHoverTip", panel);
            SetRect(tipFrame.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.22f));
            tipFrame.sprite = rounded;
            tipFrame.type = Image.Type.Sliced;
            tipFrame.color = UiLightChromeUtility.Border;
            tipFrame.raycastTarget = false;
            tipFrame.gameObject.SetActive(false);
            _optionHoverTipRoot = tipFrame;

            // Nested over the settings frosted card: soft navy film only — no second blur slice
            // (stacked RawImage reads as an opaque white box).
            Image tip = CreateUiObject<Image>("Face", tipFrame.transform);
            Stretch(tip.rectTransform);
            tip.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
            tip.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
            tip.raycastTarget = false;
            UiLightChromeUtility.ApplySoftNavyPanel(tip);
            tip.color = new Color(
                UiLightChromeUtility.SoftNavyFace.r,
                UiLightChromeUtility.SoftNavyFace.g,
                UiLightChromeUtility.SoftNavyFace.b,
                0.55f);

            _optionHoverTipBody = CreateText(
                "Body", tip.transform, 12, TextAnchor.UpperLeft, TextPrimary);
            Stretch(_optionHoverTipBody.rectTransform);
            _optionHoverTipBody.rectTransform.offsetMin = new Vector2(10f, 8f);
            _optionHoverTipBody.rectTransform.offsetMax = new Vector2(-10f, -8f);
            _optionHoverTipBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _optionHoverTipBody.verticalOverflow = VerticalWrapMode.Overflow;
            _optionHoverTipBody.color = TextPrimary;
            _optionHoverTipBody.transform.SetAsLastSibling();
        }

        /// <summary>Opaque light_chrome card via shared utility. No blur.</summary>
        private static Image CreateChromeCard(
            string name, Transform parent, Vector2 min, Vector2 max, Color fill, Sprite rounded)
        {
            return UiLightChromeUtility.CreateCard(name, parent, min, max, fill);
        }

        private void ShowOptionHoverTip(string description)
        {
            if (_optionHoverTipRoot == null || _optionHoverTipBody == null) return;
            if (string.IsNullOrEmpty(description))
            {
                HideOptionHoverTip();
                return;
            }

            _optionHoverTipBody.text = description;
            _optionHoverTipRoot.gameObject.SetActive(true);
            _optionHoverTipRoot.transform.SetAsLastSibling();
        }

        private void HideOptionHoverTip()
        {
            if (_optionHoverTipRoot != null)
                _optionHoverTipRoot.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (_root == null) return;
            CollapseAllChoices();
            Refresh();
            UiChromeMotion.Show(_root);
            _root.transform.SetAsLastSibling();
            if (_frostHost == null)
                _frostHost = _root.GetComponent<UiFrostedGlassHost>()
                             ?? _root.AddComponent<UiFrostedGlassHost>();
            // Hide settings chrome during capture so blur samples atmosphere behind the panel.
            _frostHost.RefreshBlur(new List<GameObject> { _root });
        }

        public void Hide()
        {
            HideOptionHoverTip();
            HideDeleteConfirm();
            HideSaveBrowser();
            UiChromeMotion.Hide(_root);
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        public void Refresh()
        {
            GameSettingsState state = GameSettingsRuntime.Current;
            for (int i = 0; i < _choices.Count; i++)
                RefreshExpandable(_choices[i]);

            _suppressSliderEvents = true;
            if (_masterSlider != null) _masterSlider.value = state.masterVolume * 100f;
            if (_sfxSlider != null) _sfxSlider.value = state.sfxVolume * 100f;
            if (_musicSlider != null) _musicSlider.value = state.musicVolume * 100f;
            _suppressSliderEvents = false;
            UpdateVolumeLabels(state);

            if (_muteUnfocusedLabel != null)
                _muteUnfocusedLabel.text = state.muteWhenUnfocused
                    ? "離開視窗靜音：開"
                    : "離開視窗靜音：關";
            if (_autoSaveLabel != null)
                _autoSaveLabel.text = state.autoSaveEnabled ? "自動存檔：開" : "自動存檔：關";
            if (_pauseOnMatchEndLabel != null)
                _pauseOnMatchEndLabel.text = state.pauseOnMatchEnd
                    ? "比賽結束時暫停：是"
                    : "比賽結束時暫停：否";
            if (_pauseOnEventSLabel != null)
                _pauseOnEventSLabel.text = state.pauseOnEventS
                    ? "S 變革事件暫停：是"
                    : "S 變革事件暫停：否";
            if (_pauseOnEventALabel != null)
                _pauseOnEventALabel.text = state.pauseOnEventA
                    ? "A 歷史事件暫停：是"
                    : "A 歷史事件暫停：否";
            if (_pauseOnEventBLabel != null)
                _pauseOnEventBLabel.text = state.pauseOnEventB
                    ? "B 隊伍事件暫停：是"
                    : "B 隊伍事件暫停：否";
            if (_pauseOnEventCLabel != null)
                _pauseOnEventCLabel.text = state.pauseOnEventC
                    ? "C 場外事件暫停：是"
                    : "C 場外事件暫停：否";

            bool career = _host.CareerSaveLoadEnabled;
            bool canLoad = career && (_host.CanLoad == null || _host.CanLoad());
            bool canSave = career && (_host.CanSave == null || _host.CanSave());
            RefreshSaveSlotRows(canLoad, canSave);

            if (_saveLoadHint != null)
            {
                _saveLoadHint.text = career
                    ? (_host.DifficultyHint != null ? _host.DifficultyHint() : string.Empty)
                    : "存檔功能僅在生涯模式可用";
            }

            if (_difficultyHint != null)
                _difficultyHint.text = career && _host.DifficultyHint != null
                    ? _host.DifficultyHint()
                    : string.Empty;
        }

        private void RefreshSaveSlotRows(bool? canLoadOverride = null, bool? canSaveOverride = null)
        {
            bool career = _host.CareerSaveLoadEnabled;
            bool canLoad = canLoadOverride ?? (career && (_host.CanLoad == null || _host.CanLoad()));
            bool canSave = canSaveOverride ?? (career && (_host.CanSave == null || _host.CanSave()));
            int selected = _host.GetSelectedSlot != null
                ? _host.GetSelectedSlot()
                : CareerPersistence.FirstManualSlot;

            for (int i = 0; i < _saveSlotRows.Count; i++)
            {
                SaveSlotRow row = _saveSlotRows[i];
                bool selectedSlot = row.Slot == selected;
                if (row.Face != null)
                    row.Face.color = selectedSlot
                        ? UiLightChromeUtility.CapsuleFaceSelected
                        : UiLightChromeUtility.CapsuleFace;
                if (row.MetaLabel != null)
                {
                    string desc = _host.DescribeSlot != null
                        ? _host.DescribeSlot(row.Slot)
                        : $"槽位 {row.Slot}";
                    row.MetaLabel.text = desc;
                    // Dark PanelRow: keep light ink so empty-slot copy stays readable.
                    row.MetaLabel.color = selectedSlot ? Accent : TextPrimary;
                }

                bool hasSave = _host.SlotHasSave == null || _host.SlotHasSave(row.Slot);
                if (row.LoadButton != null)
                    row.LoadButton.interactable = canLoad && hasSave;
                if (row.SaveButton != null)
                    row.SaveButton.interactable = canSave;
                if (row.DeleteButton != null)
                    row.DeleteButton.interactable = career && hasSave;
            }
        }

        /// <summary>非收合：存檔/讀檔、返回主菜單、退出遊戲。</summary>
        private void BuildFixedActionRows()
        {
            RectTransform openRow = AddRow(_listRoot, 48f);
            CreateActionButton(
                "OpenSaveBrowser", "存檔/讀檔", openRow,
                new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f),
                AccentDim, Accent, ShowSaveBrowser);

            RectTransform hintRow = AddRow(_listRoot, 40f);
            _saveLoadHint = CreateText("SaveLoadHint", hintRow, 11, TextAnchor.MiddleCenter, TextMuted);
            Stretch(_saveLoadHint.rectTransform);
            _saveLoadHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            _saveLoadHint.verticalOverflow = VerticalWrapMode.Overflow;
            _difficultyHint = _saveLoadHint;

            RectTransform menuRow = AddRow(_listRoot, 48f);
            CreateActionButton("MainMenu", "返回主菜單", menuRow,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                ButtonEdge, ButtonFace, () =>
                {
                    Hide();
                    _host.OnReturnToMainMenu?.Invoke();
                });

            RectTransform quitRow = AddRow(_listRoot, 48f);
            CreateActionButton("Quit", "退出遊戲", quitRow,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                Danger, DangerFace, QuitGame);
        }

        private void BuildSaveBrowser(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("SaveBrowserOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;
            _saveBrowserRoot = dim.gameObject;
            _saveBrowserRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideSaveBrowser);

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Image panel = CreateChromeCard(
                "SaveBrowserPanel", dim.transform,
                new Vector2(0.24f, 0.18f), new Vector2(0.76f, 0.82f),
                Panel, rounded);
            UiChromeMotion.BindFace(_saveBrowserRoot, panel);

            Text title = CreateText("Title", panel.transform, 18, TextAnchor.MiddleLeft, Accent);
            SetRect(title.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.62f, 0.97f));
            title.text = "存檔/讀檔";

            UiLightChromeUtility.CreateSquareCloseButton(
                "CloseSaveBrowser", panel.transform,
                new Vector2(0.935f, 0.885f), new Vector2(0.985f, 0.965f),
                HideSaveBrowser, fontSize: 18);

            _saveSlotRows.Clear();
            float rowTop = 0.82f;
            float rowHeight = 0.15f;
            float gap = 0.02f;
            for (int slot = CareerPersistence.FirstManualSlot; slot <= CareerPersistence.LastManualSlot; slot++)
            {
                int captured = slot;
                float y1 = rowTop - (slot - CareerPersistence.FirstManualSlot) * (rowHeight + gap);
                float y0 = y1 - rowHeight;

                Image row = CreateUiObject<Image>("SlotRow_" + slot, panel.transform);
                SetRect(row.rectTransform, new Vector2(0.04f, y0), new Vector2(0.96f, y1));
                row.color = PanelRow;

                Button selectBtn = CreateActionButton(
                    "SlotSelect_" + slot, $"槽位 {slot}", row.transform,
                    new Vector2(0.02f, 0.14f), new Vector2(0.16f, 0.86f),
                    ButtonEdge, ButtonFace, () =>
                    {
                        _host.OnSelectSlot?.Invoke(captured);
                        RefreshSaveSlotRows();
                    });

                Text meta = CreateText("SlotMeta_" + slot, row.transform, 14, TextAnchor.MiddleLeft, TextPrimary);
                SetRect(meta.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.48f, 0.88f));
                meta.horizontalOverflow = HorizontalWrapMode.Wrap;
                meta.verticalOverflow = VerticalWrapMode.Overflow;
                meta.resizeTextForBestFit = true;
                meta.resizeTextMinSize = 10;
                meta.resizeTextMaxSize = 28;

                Button loadBtn = CreateActionButton(
                    "SlotLoad_" + slot, "讀取", row.transform,
                    new Vector2(0.50f, 0.14f), new Vector2(0.64f, 0.86f),
                    ButtonEdge, ButtonFace, () =>
                    {
                        _host.OnSelectSlot?.Invoke(captured);
                        _host.OnLoadSlot?.Invoke(captured);
                        RefreshSaveSlotRows();
                    });

                Button saveBtn = CreateActionButton(
                    "SlotSave_" + slot, "存檔", row.transform,
                    new Vector2(0.66f, 0.14f), new Vector2(0.80f, 0.86f),
                    AccentDim, Accent, () =>
                    {
                        _host.OnSelectSlot?.Invoke(captured);
                        _host.OnSaveSlot?.Invoke(captured);
                        RefreshSaveSlotRows();
                    });

                Button deleteBtn = CreateActionButton(
                    "SlotDelete_" + slot, "刪除", row.transform,
                    new Vector2(0.82f, 0.14f), new Vector2(0.98f, 0.86f),
                    Danger, DangerFace, () => ShowDeleteConfirm(captured));

                Image face = selectBtn != null
                    ? selectBtn.transform.Find("Face")?.GetComponent<Image>()
                    : null;
                _saveSlotRows.Add(new SaveSlotRow
                {
                    Slot = slot,
                    Face = face,
                    MetaLabel = meta,
                    LoadButton = loadBtn,
                    SaveButton = saveBtn,
                    DeleteButton = deleteBtn
                });
            }
        }

        private void BuildDeleteConfirm(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("DeleteSaveConfirmOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.7f);
            dim.raycastTarget = true;
            _deleteConfirmRoot = dim.gameObject;
            _deleteConfirmRoot.SetActive(false);

            Button closeDim = dim.gameObject.AddComponent<Button>();
            closeDim.targetGraphic = dim;
            closeDim.transition = Selectable.Transition.None;
            closeDim.onClick.AddListener(HideDeleteConfirm);

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            Image panel = CreateChromeCard(
                "DeleteSaveConfirmPanel", dim.transform,
                new Vector2(0.32f, 0.36f), new Vector2(0.68f, 0.64f),
                Panel, rounded);
            UiChromeMotion.BindFace(_deleteConfirmRoot, panel);

            Text title = CreateText("Title", panel.transform, 16, TextAnchor.MiddleCenter, Accent);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f));
            title.text = "刪除存檔";

            _deleteConfirmBody = CreateText("Body", panel.transform, 14, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_deleteConfirmBody.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.70f));
            _deleteConfirmBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _deleteConfirmBody.verticalOverflow = VerticalWrapMode.Overflow;
            _deleteConfirmBody.text = "確定要刪除嗎？";

            CreateActionButton("CancelDelete", "取消", panel.transform,
                new Vector2(0.08f, 0.10f), new Vector2(0.46f, 0.30f),
                ButtonEdge, ButtonFace, HideDeleteConfirm, fontSize: 13, fillLabel: true);

            CreateActionButton("ConfirmDelete", "確定刪除", panel.transform,
                new Vector2(0.54f, 0.10f), new Vector2(0.92f, 0.30f),
                Danger, DangerFace, ConfirmDeleteSlot, fontSize: 13, fillLabel: true);
        }

        private void ShowSaveBrowser()
        {
            if (_saveBrowserRoot == null) return;
            HideDeleteConfirm();
            RefreshSaveSlotRows();
            UiChromeMotion.Show(_saveBrowserRoot);
            _saveBrowserRoot.transform.SetAsLastSibling();
        }

        private void HideSaveBrowser()
        {
            HideDeleteConfirm();
            UiChromeMotion.Hide(_saveBrowserRoot);
        }

        private void ShowDeleteConfirm(int slot)
        {
            if (_deleteConfirmRoot == null) return;
            if (!_host.CareerSaveLoadEnabled) return;
            bool hasSave = _host.SlotHasSave == null || _host.SlotHasSave(slot);
            if (!hasSave) return;

            _pendingDeleteSlot = slot;
            if (_deleteConfirmBody != null)
                _deleteConfirmBody.text = $"確定要刪除槽位 {slot} 的存檔嗎？";
            UiChromeMotion.Show(_deleteConfirmRoot);
            _deleteConfirmRoot.transform.SetAsLastSibling();
        }

        private void HideDeleteConfirm()
        {
            _pendingDeleteSlot = -1;
            UiChromeMotion.Hide(_deleteConfirmRoot);
        }

        private void ConfirmDeleteSlot()
        {
            int slot = _pendingDeleteSlot;
            HideDeleteConfirm();
            if (!CareerPersistence.IsManualSlot(slot))
                return;
            _host.OnDeleteSlot?.Invoke(slot);
            RefreshSaveSlotRows();
        }

        private void BuildDisplaySection()
        {
            RectTransform body = BeginCollapsibleSection("畫面設定");
            AddExpandableChoice(
                body,
                "解析度",
                () => GameSettingsRuntime.Current.resolution,
                v => GameSettingsRules.ResolutionLabel((DisplayResolutionPreset)v),
                GameSettingsRules.Resolutions,
                GameSettingsRules.ResolutionLabel,
                preset =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.resolution = preset);
                    NotifyChanged();
                },
                _ => true);

            AddExpandableChoice(
                body,
                "視窗模式",
                () => GameSettingsRuntime.Current.windowMode,
                v => GameSettingsRules.WindowModeLabel((DisplayWindowMode)v),
                GameSettingsRules.WindowModes,
                GameSettingsRules.WindowModeLabel,
                mode =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.windowMode = mode);
                    NotifyChanged();
                },
                _ => true);

            AddExpandableChoice(
                body,
                "幀數",
                () => GameSettingsRuntime.Current.frameRate,
                v => GameSettingsRules.FrameRateLabel((DisplayFrameRate)v),
                GameSettingsRules.FrameRates,
                GameSettingsRules.FrameRateLabel,
                rate =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.frameRate = rate);
                    NotifyChanged();
                },
                _ => true);
        }

        private void BuildAudioSection()
        {
            RectTransform body = BeginCollapsibleSection("聲音設定");
            _masterSlider = AddVolumeSlider(body, "主音量", v =>
            {
                GameSettingsRuntime.UpdateAndApply(s => s.masterVolume = v);
                NotifyChanged();
            }, out _masterValue);
            _sfxSlider = AddVolumeSlider(body, "音效", v =>
            {
                GameSettingsRuntime.UpdateAndApply(s => s.sfxVolume = v);
                NotifyChanged();
            }, out _sfxValue);
            _musicSlider = AddVolumeSlider(body, "音樂", v =>
            {
                GameSettingsRuntime.UpdateAndApply(s => s.musicVolume = v);
                NotifyChanged();
            }, out _musicValue);

            RectTransform muteRow = AddRow(body, 44f);
            _muteUnfocusedButton = CreateActionButton("MuteUnfocused", "離開視窗靜音：開", muteRow,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                ButtonEdge, ButtonFace, () =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.muteWhenUnfocused = !s.muteWhenUnfocused);
                    NotifyChanged();
                }, fontSize: 12);
            _muteUnfocusedLabel = _muteUnfocusedButton.GetComponentInChildren<Text>();
        }

        private void BuildGameSection()
        {
            RectTransform body = BeginCollapsibleSection("遊戲設定");
            AddExpandableChoice(
                body,
                "對戰形式",
                () => GameSettingsRuntime.Current.battleSimMode,
                v => GameSettingsRules.BattleModeLabel((BattleSimMode)v),
                GameSettingsRules.BattleModes,
                GameSettingsRules.BattleModeButtonLabel,
                mode =>
                {
                    // Placeholder modes stay visible for hover copy; only Quick applies for now.
                    if (!GameSettingsRules.IsBattleModeImplemented(mode))
                        return;
                    GameSettingsRuntime.UpdateAndApply(s => s.battleSimMode = mode);
                    NotifyChanged();
                },
                _ => true,
                optionsHeight: 56f,
                hoverDescription: mode => GameSettingsRules.BattleModeHoverDescription(mode));

            RectTransform pauseMatchRow = AddRow(body, 44f);
            _pauseOnMatchEndButton = CreateActionButton(
                "PauseOnMatchEnd", "比賽結束時暫停：是", pauseMatchRow,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                ButtonEdge, ButtonFace, () =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.pauseOnMatchEnd = !s.pauseOnMatchEnd);
                    NotifyChanged();
                }, fontSize: 12);
            _pauseOnMatchEndLabel = _pauseOnMatchEndButton.GetComponentInChildren<Text>();

            AddEventPauseToggle(body, "PauseOnEventS", "S 變革事件暫停：是",
                () => GameSettingsRuntime.UpdateAndApply(s => s.pauseOnEventS = !s.pauseOnEventS),
                out _pauseOnEventSButton, out _pauseOnEventSLabel);
            AddEventPauseToggle(body, "PauseOnEventA", "A 歷史事件暫停：是",
                () => GameSettingsRuntime.UpdateAndApply(s => s.pauseOnEventA = !s.pauseOnEventA),
                out _pauseOnEventAButton, out _pauseOnEventALabel);
            AddEventPauseToggle(body, "PauseOnEventB", "B 隊伍事件暫停：是",
                () => GameSettingsRuntime.UpdateAndApply(s => s.pauseOnEventB = !s.pauseOnEventB),
                out _pauseOnEventBButton, out _pauseOnEventBLabel);
            AddEventPauseToggle(body, "PauseOnEventC", "C 場外事件暫停：否",
                () => GameSettingsRuntime.UpdateAndApply(s => s.pauseOnEventC = !s.pauseOnEventC),
                out _pauseOnEventCButton, out _pauseOnEventCLabel);

            RectTransform autoRow = AddRow(body, 44f);
            _autoSaveButton = CreateActionButton("AutoSave", "自動存檔：關", autoRow,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                ButtonEdge, ButtonFace, () =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.autoSaveEnabled = !s.autoSaveEnabled);
                    NotifyChanged();
                }, fontSize: 12);
            _autoSaveLabel = _autoSaveButton.GetComponentInChildren<Text>();

            AddExpandableChoice(
                body,
                "自動存檔間隔",
                () => GameSettingsRuntime.Current.autoSaveIntervalMinutes,
                v => GameSettingsRules.AutoSaveIntervalLabel((AutoSaveIntervalMinutes)v),
                GameSettingsRules.AutoSaveIntervals,
                GameSettingsRules.AutoSaveIntervalLabel,
                interval =>
                {
                    GameSettingsRuntime.UpdateAndApply(s => s.autoSaveIntervalMinutes = interval);
                    NotifyChanged();
                },
                _ => true);
        }

        private void AddEventPauseToggle(
            RectTransform body,
            string name,
            string initialLabel,
            Action onClick,
            out Button button,
            out Text label)
        {
            RectTransform row = AddRow(body, 44f);
            button = CreateActionButton(name, initialLabel, row,
                new Vector2(0.02f, 0.12f), new Vector2(0.98f, 0.88f),
                ButtonEdge, ButtonFace, () =>
                {
                    onClick?.Invoke();
                    NotifyChanged();
                }, fontSize: 12);
            label = button.GetComponentInChildren<Text>();
        }

        private RectTransform BeginCollapsibleSection(string title, bool startOpen = true)
        {
            RectTransform header = AddRow(_listRoot, 34f);
            Image headerBg = header.GetComponent<Image>();
            if (headerBg != null) headerBg.color = PanelInner;

            Text label = CreateText("Section_" + title, header, 14, TextAnchor.MiddleLeft, Accent);
            SetRect(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.82f, 1f));
            label.text = title;

            Text chevron = CreateText("Chevron", header, 14, TextAnchor.MiddleRight, TextMuted);
            SetRect(chevron.rectTransform, new Vector2(0.82f, 0f), new Vector2(0.96f, 1f));
            chevron.text = startOpen ? "▾" : "▸";

            Image bodyHost = CreateUiObject<Image>("SectionBody_" + title, _listRoot);
            bodyHost.color = Color.clear;
            var bodyLe = bodyHost.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleWidth = 1f;
            bodyLe.minHeight = 0f;

            var bodyLayout = bodyHost.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.childAlignment = TextAnchor.UpperCenter;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;
            bodyLayout.spacing = 4f;
            bodyLayout.padding = new RectOffset(0, 0, 0, 4);

            var bodyFitter = bodyHost.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var section = new CollapsibleSection
            {
                ChevronLabel = chevron,
                BodyRoot = bodyHost.gameObject,
                BodyLayout = bodyLe,
                IsOpen = startOpen
            };
            _sections.Add(section);
            ApplySectionOpen(section);

            Button toggle = header.gameObject.AddComponent<Button>();
            toggle.targetGraphic = headerBg;
            toggle.transition = Selectable.Transition.None;
            toggle.onClick.AddListener(() =>
            {
                section.IsOpen = !section.IsOpen;
                ApplySectionOpen(section);
            });

            return bodyHost.rectTransform;
        }

        private static void ApplySectionOpen(CollapsibleSection section)
        {
            if (section.BodyRoot != null)
                section.BodyRoot.SetActive(section.IsOpen);
            if (section.ChevronLabel != null)
                section.ChevronLabel.text = section.IsOpen ? "▾" : "▸";
            if (section.BodyLayout != null)
                section.BodyLayout.ignoreLayout = !section.IsOpen;
        }

        private void AddExpandableChoice<T>(
            Transform parent,
            string title,
            Func<object> getSelected,
            Func<object, string> formatSelected,
            T[] values,
            Func<T, string> optionLabeler,
            Action<T> onPick,
            Func<T, bool> isEnabled,
            float optionsHeight = 44f,
            Func<T, string> hoverDescription = null)
        {
            var choice = new ExpandableChoice
            {
                Title = title,
                GetSelected = getSelected,
                FormatSelected = formatSelected,
                IsOpen = false
            };

            RectTransform header = AddRow(parent, 42f);
            Image headerBg = header.GetComponent<Image>();
            Button headerBtn = header.gameObject.AddComponent<Button>();
            headerBtn.targetGraphic = headerBg;
            ApplyButtonColors(headerBtn);
            headerBtn.onClick.AddListener(() => ToggleChoice(choice));

            Text titleLabel = CreateText("Title", header, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(titleLabel.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f));
            titleLabel.text = title;

            Text valueLabel = CreateText("Value", header, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(valueLabel.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.88f, 0.92f));
            valueLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            choice.HeaderValueLabel = valueLabel;

            Text chevron = CreateText("Chevron", header, 13, TextAnchor.MiddleCenter, Accent);
            SetRect(chevron.rectTransform, new Vector2(0.88f, 0.08f), new Vector2(0.98f, 0.92f));
            chevron.text = "▸";
            choice.ChevronLabel = chevron;

            Image options = CreateUiObject<Image>("Options_" + title, parent);
            options.color = PanelInner;
            var optionsLe = options.gameObject.AddComponent<LayoutElement>();
            optionsLe.minHeight = optionsHeight;
            optionsLe.preferredHeight = optionsHeight;
            optionsLe.flexibleWidth = 1f;
            choice.OptionsRoot = options.gameObject;
            choice.OptionsLayout = optionsLe;

            int count = values.Length;
            for (int i = 0; i < count; i++)
            {
                T value = values[i];
                float x0 = 0.02f + i * (0.96f / count);
                float x1 = 0.02f + (i + 1) * (0.96f / count) - 0.01f;
                bool enabled = isEnabled(value);
                Button btn = CreateActionButton(
                    "Opt_" + i,
                    optionLabeler(value),
                    options.rectTransform,
                    new Vector2(x0, 0.12f),
                    new Vector2(x1, 0.88f),
                    ButtonEdge,
                    ButtonFace,
                    () =>
                    {
                        onPick(value);
                        choice.IsOpen = false;
                        ApplyChoiceOpen(choice);
                        HideOptionHoverTip();
                        NotifyChanged();
                    },
                    fontSize: count >= 3 ? 10 : 11);
                btn.interactable = enabled;
                Image face = btn.transform.Find("Face")?.GetComponent<Image>();
                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 8;
                    label.resizeTextMaxSize = 12;
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }

                if (hoverDescription != null)
                {
                    string tip = hoverDescription(value);
                    EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>()
                                          ?? btn.gameObject.AddComponent<EventTrigger>();
                    AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowOptionHoverTip(tip));
                    AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => HideOptionHoverTip());
                }

                choice.Buttons.Add(new ChoiceButton
                {
                    Button = btn,
                    Face = face,
                    Label = label,
                    Value = value
                });
            }

            ApplyChoiceOpen(choice);
            _choices.Add(choice);
        }

        private static void AddPointerTrigger(
            EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }
        private void ToggleChoice(ExpandableChoice choice)
        {
            bool open = !choice.IsOpen;
            CollapseAllChoices();
            choice.IsOpen = open;
            ApplyChoiceOpen(choice);
        }

        private void CollapseAllChoices()
        {
            for (int i = 0; i < _choices.Count; i++)
            {
                _choices[i].IsOpen = false;
                ApplyChoiceOpen(_choices[i]);
            }
        }

        private void ApplyChoiceOpen(ExpandableChoice choice)
        {
            if (choice.OptionsRoot != null)
                choice.OptionsRoot.SetActive(choice.IsOpen);
            if (choice.OptionsLayout != null)
                choice.OptionsLayout.ignoreLayout = !choice.IsOpen;
            if (choice.ChevronLabel != null)
                choice.ChevronLabel.text = choice.IsOpen ? "▾" : "▸";
            if (!choice.IsOpen)
                HideOptionHoverTip();
        }

        private void RefreshExpandable(ExpandableChoice choice)
        {
            object selected = choice.GetSelected != null ? choice.GetSelected() : null;
            if (choice.HeaderValueLabel != null && choice.FormatSelected != null)
                choice.HeaderValueLabel.text = choice.FormatSelected(selected);

            for (int i = 0; i < choice.Buttons.Count; i++)
            {
                ChoiceButton item = choice.Buttons[i];
                bool isSelected = Equals(item.Value, selected);
                bool implemented = !(item.Value is BattleSimMode mode) ||
                                   GameSettingsRules.IsBattleModeImplemented(mode);
                if (item.Face != null)
                {
                    if (!implemented)
                        item.Face.color = new Color(0.85f, 0.85f, 0.88f, 0.55f);
                    else
                        item.Face.color = isSelected
                            ? CareerEditorialChrome.Mustard
                            : CareerEditorialChrome.Paper;
                }

                if (item.Label != null)
                {
                    if (!implemented)
                        item.Label.color = TextMuted;
                    else
                        item.Label.color = CareerEditorialChrome.Ink;
                }
            }
        }

        private Slider AddVolumeSlider(Transform parent, string title, Action<float> onChanged, out Text valueLabel)
        {
            RectTransform row = AddRow(parent, 52f);
            Text label = CreateText(title + "Label", row, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(label.rectTransform, new Vector2(0.04f, 0.55f), new Vector2(0.45f, 0.95f));
            label.text = title;

            valueLabel = CreateText(title + "Value", row, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(valueLabel.rectTransform, new Vector2(0.70f, 0.55f), new Vector2(0.96f, 0.95f));
            valueLabel.text = "100";

            Image track = CreateUiObject<Image>(title + "Track", row);
            SetRect(track.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.48f));
            track.color = PanelInner;

            Image fillArea = CreateUiObject<Image>("Fill Area", track.transform);
            Stretch(fillArea.rectTransform);
            fillArea.color = Color.clear;
            fillArea.raycastTarget = false;

            Image fill = CreateUiObject<Image>("Fill", fillArea.transform);
            Stretch(fill.rectTransform);
            fill.color = AccentDim;

            Image handleArea = CreateUiObject<Image>("Handle Slide Area", track.transform);
            Stretch(handleArea.rectTransform);
            handleArea.color = Color.clear;
            handleArea.raycastTarget = false;

            Image handle = CreateUiObject<Image>("Handle", handleArea.transform);
            handle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(16f, 20f);
            handle.color = Accent;

            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.value = 100f;
            Text capturedLabel = valueLabel;
            slider.onValueChanged.AddListener(v =>
            {
                if (capturedLabel != null)
                    capturedLabel.text = Mathf.RoundToInt(v).ToString();
                if (_suppressSliderEvents) return;
                onChanged(Mathf.Clamp01(v / 100f));
            });
            return slider;
        }

        private void UpdateVolumeLabels(GameSettingsState state)
        {
            if (_masterValue != null) _masterValue.text = Mathf.RoundToInt(state.masterVolume * 100f).ToString();
            if (_sfxValue != null) _sfxValue.text = Mathf.RoundToInt(state.sfxVolume * 100f).ToString();
            if (_musicValue != null) _musicValue.text = Mathf.RoundToInt(state.musicVolume * 100f).ToString();
        }

        private RectTransform AddRow(Transform parent, float height)
        {
            Image row = CreateUiObject<Image>("Row", parent);
            row.color = PanelRow;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return row.rectTransform;
        }

        private void RequestClose()
        {
            Hide();
            _host.OnClose?.Invoke();
        }

        private void NotifyChanged()
        {
            Refresh();
            _host.OnChanged?.Invoke();
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private Button CreateActionButton(
            string name, string label, Transform parent, Vector2 min, Vector2 max,
            Color edgeColor, Color faceColor, UnityAction action, int fontSize = 16,
            bool fillLabel = true)
        {
            Button button = UiLightChromeUtility.CreateGlassCapsuleButton(
                name, label, parent, min, max, edgeColor, faceColor, action,
                _font, fontSize, fillLabel);
            if (fillLabel && button != null)
            {
                Text labelText = button.GetComponentInChildren<Text>();
                ApplyFilledButtonLabel(labelText, 48);
            }
            return button;
        }

        /// <summary>Make label occupy ~90% of the button face and scale up to fill it.</summary>
        private static void ApplyFilledButtonLabel(Text text, int maxSize = 48)
        {
            if (text == null) return;
            text.fontStyle = FontStyle.Bold;
            SetRect(text.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = Mathf.Max(maxSize, text.fontSize);
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

        private Text CreateText(string name, Transform parent, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
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
