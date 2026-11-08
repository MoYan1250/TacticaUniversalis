using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TacticaUniversalis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public sealed partial class CareerDemoController
    {
        private GameObject _registerConfirmRoot;
        private Text _registerConfirmBody;
        private string _registerConfirmCompetitionId;

        private GameObject _registerChooseTwoRoot;
        private Text _registerChooseTwoBody;
        private Transform _registerChooseTwoList;
        private string _registerChooseTwoCompetitionId;
        private readonly List<string> _registerChooseTwoConflictIds = new List<string>();
        private readonly HashSet<string> _registerChooseTwoSelected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Button> _registerChooseTwoToggleButtons = new List<Button>();

        private void BuildCompetitionRegisterOverlays(Transform canvas)
        {
            BuildRegisterConfirmOverlay(canvas);
            BuildRegisterChooseTwoOverlay(canvas);
        }

        private void BuildRegisterConfirmOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("RegisterConfirmOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            dim.raycastTarget = true;
            _registerConfirmRoot = dim.gameObject;
            _registerConfirmRoot.SetActive(false);

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "Panel", dim.transform,
                new Vector2(0.28f, 0.32f), new Vector2(0.72f, 0.68f), Panel);
            UiChromeMotion.BindFace(_registerConfirmRoot, panel);
            Image face = UiLightChromeUtility.CreateInsetFace(panel.transform, PanelInner, 3f);

            Text title = CreateText("Title", face.transform, 20, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f));
            title.text = "賽事日期重疊";

            _registerConfirmBody = CreateText("Body", face.transform, 15, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_registerConfirmBody.rectTransform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.76f));
            _registerConfirmBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _registerConfirmBody.verticalOverflow = VerticalWrapMode.Overflow;

            CreateGlassCapsuleButton(
                "No", "否", face.transform,
                new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.24f),
                ButtonEdge, ButtonFace, HideRegisterConfirm, _uiFont700);
            CreateGlassCapsuleButton(
                "Yes", "是", face.transform,
                new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.24f),
                AccentDim, Accent, ConfirmOnlineOverlapRegister, _uiFont700);
        }

        private void BuildRegisterChooseTwoOverlay(Transform canvas)
        {
            Image dim = CreateUiObject<Image>("RegisterChooseTwoOverlay", canvas);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            dim.raycastTarget = true;
            _registerChooseTwoRoot = dim.gameObject;
            _registerChooseTwoRoot.SetActive(false);

            // D3 light_chrome: Shadow > Frame > Face + inset well; no blur.
            Image panel = UiLightChromeUtility.CreateCard(
                "Panel", dim.transform,
                new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f), Panel);
            UiChromeMotion.BindFace(_registerChooseTwoRoot, panel);
            Image face = UiLightChromeUtility.CreateInsetFace(panel.transform, PanelInner, 3f);

            Text title = CreateText("Title", face.transform, 20, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));
            title.text = "選擇保留的兩場賽事";

            _registerChooseTwoBody = CreateText("Body", face.transform, 14, TextAnchor.UpperLeft, TextPrimary, _uiFont700);
            SetRect(_registerChooseTwoBody.rectTransform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.87f));
            _registerChooseTwoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _registerChooseTwoBody.verticalOverflow = VerticalWrapMode.Overflow;

            Image listHost = CreateUiObject<Image>("List", face.transform);
            SetRect(listHost.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.72f));
            UiLightChromeUtility.ApplySoftNavyPanel(listHost);
            _registerChooseTwoList = listHost.transform;

            CreateGlassCapsuleButton(
                "Cancel", "否", face.transform,
                new Vector2(0.08f, 0.05f), new Vector2(0.46f, 0.16f),
                ButtonEdge, ButtonFace, HideRegisterChooseTwo, _uiFont700);
            CreateGlassCapsuleButton(
                "Confirm", "是", face.transform,
                new Vector2(0.54f, 0.05f), new Vector2(0.92f, 0.16f),
                AccentDim, Accent, ConfirmChooseTwoRegister, _uiFont700);
        }

        private void AttemptRegisterCompetition(string competitionId)
        {
            if (_session == null || string.IsNullOrWhiteSpace(competitionId))
                return;

            bool ok = _session.TryRegisterCompetition(
                competitionId, null, out RegisterCompetitionResult result);
            HandleRegisterCompetitionResult(competitionId, ok, result);
        }

        private void HandleRegisterCompetitionResult(
            string competitionId, bool ok, RegisterCompetitionResult result)
        {
            result = result ?? new RegisterCompetitionResult();
            if (ok)
            {
                string name = ResolveCompetitionDisplayName(competitionId);
                SetStatus(
                    string.IsNullOrWhiteSpace(result.failureReason)
                        ? $"已報名 {name}"
                        : result.failureReason,
                    new CareerUiNavLink(CareerUiDestination.MapCompetition, competitionId));
                RefreshAll();
                return;
            }

            switch (result.scheduleVerdict)
            {
                case CompetitionScheduleVerdict.NeedsOnlineOverlapConfirm:
                    ShowRegisterConfirm(competitionId, result);
                    return;
                case CompetitionScheduleVerdict.NeedsChooseTwo:
                    ShowRegisterChooseTwo(competitionId, result);
                    return;
                default:
                    SetStatus(
                        result.failureReason ?? "無法報名此賽事",
                        CareerUiNavLink.None);
                    RefreshAll();
                    return;
            }
        }

        private void ShowRegisterConfirm(string competitionId, RegisterCompetitionResult result)
        {
            if (_registerConfirmRoot == null)
                return;
            _registerConfirmCompetitionId = competitionId;
            var sb = new StringBuilder();
            sb.AppendLine(result.failureReason ??
                          "此線上賽事與已報名賽事日期重疊，出賽會額外消耗選手精力。確定要報名嗎？");
            if (result.conflictDisplayNames != null && result.conflictDisplayNames.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("重疊賽事：");
                foreach (string name in result.conflictDisplayNames)
                    sb.AppendLine("· " + name);
            }

            _registerConfirmBody.text = sb.ToString();
            UiChromeMotion.Show(_registerConfirmRoot);
            _registerConfirmRoot.transform.SetAsLastSibling();
        }

        private void HideRegisterConfirm()
        {
            UiChromeMotion.Hide(_registerConfirmRoot);
            _registerConfirmCompetitionId = null;
        }

        private void ConfirmOnlineOverlapRegister()
        {
            string competitionId = _registerConfirmCompetitionId;
            HideRegisterConfirm();
            if (_session == null || string.IsNullOrWhiteSpace(competitionId))
                return;

            var options = new CompetitionRegisterOptions { confirmOnlineOverlap = true };
            bool ok = _session.TryRegisterCompetition(competitionId, options, out RegisterCompetitionResult result);
            HandleRegisterCompetitionResult(competitionId, ok, result);
        }

        private void ShowRegisterChooseTwo(string competitionId, RegisterCompetitionResult result)
        {
            if (_registerChooseTwoRoot == null || _registerChooseTwoList == null)
                return;

            _registerChooseTwoCompetitionId = competitionId;
            _registerChooseTwoConflictIds.Clear();
            _registerChooseTwoSelected.Clear();
            _registerChooseTwoToggleButtons.Clear();

            for (int i = _registerChooseTwoList.childCount - 1; i >= 0; i--)
                DestroyImmediate(_registerChooseTwoList.GetChild(i).gameObject);

            List<string> ids = result.conflictCompetitionIds ?? new List<string>();
            if (!ids.Any(x => string.Equals(x, competitionId, StringComparison.OrdinalIgnoreCase)))
                ids = new List<string>(ids) { competitionId };

            _registerChooseTwoConflictIds.AddRange(ids);
            _registerChooseTwoBody.text = result.failureReason ??
                "同時間最多只能參加兩場賽事，請點選要保留的兩場後按是。";

            float y = 0.98f;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                string label = ResolveCompetitionDisplayName(id);
                float y1 = y;
                float y0 = y - 0.18f;
                y = y0 - 0.02f;
                string captured = id;
                Button btn = CreateGlassCapsuleButton(
                    "Pick_" + i, label, _registerChooseTwoList,
                    new Vector2(0.04f, y0), new Vector2(0.96f, y1),
                    ButtonEdge, ButtonFace,
                    () => ToggleChooseTwoSelection(captured),
                    _uiFont700);
                _registerChooseTwoToggleButtons.Add(btn);
            }

            RefreshChooseTwoToggleStyles();
            UiChromeMotion.Show(_registerChooseTwoRoot);
            _registerChooseTwoRoot.transform.SetAsLastSibling();
        }

        private void HideRegisterChooseTwo()
        {
            UiChromeMotion.Hide(_registerChooseTwoRoot);
            _registerChooseTwoCompetitionId = null;
            _registerChooseTwoConflictIds.Clear();
            _registerChooseTwoSelected.Clear();
            _registerChooseTwoToggleButtons.Clear();
        }

        private void ToggleChooseTwoSelection(string competitionId)
        {
            if (_registerChooseTwoSelected.Contains(competitionId))
                _registerChooseTwoSelected.Remove(competitionId);
            else
            {
                if (_registerChooseTwoSelected.Count >= CompetitionScheduleRules.MaxConcurrentCommitments)
                    return;
                _registerChooseTwoSelected.Add(competitionId);
            }

            RefreshChooseTwoToggleStyles();
        }

        private void RefreshChooseTwoToggleStyles()
        {
            for (int i = 0; i < _registerChooseTwoToggleButtons.Count && i < _registerChooseTwoConflictIds.Count; i++)
            {
                Button btn = _registerChooseTwoToggleButtons[i];
                if (btn == null)
                    continue;
                Image face = btn.transform.Find("Face")?.GetComponent<Image>();
                if (face == null)
                    continue;
                bool on = _registerChooseTwoSelected.Contains(_registerChooseTwoConflictIds[i]);
                face.color = on
                    ? UiLightChromeUtility.CapsuleFacePrimary
                    : UiLightChromeUtility.CapsuleFace;
                Image edge = btn.targetGraphic != null
                    ? btn.GetComponent<Image>()
                    : null;
                if (edge != null)
                {
                    edge.color = on
                        ? UiLightChromeUtility.CapsuleRimPrimary
                        : UiLightChromeUtility.CapsuleRim;
                }
            }
        }

        private void ConfirmChooseTwoRegister()
        {
            string competitionId = _registerChooseTwoCompetitionId;
            if (_registerChooseTwoSelected.Count != CompetitionScheduleRules.MaxConcurrentCommitments)
            {
                SetStatus("請正好選出兩場要保留的賽事", CareerUiNavLink.None);
                return;
            }

            string[] keep = _registerChooseTwoSelected.ToArray();
            HideRegisterChooseTwo();
            if (_session == null || string.IsNullOrWhiteSpace(competitionId))
                return;

            var options = new CompetitionRegisterOptions
            {
                confirmOnlineOverlap = true,
                keepCompetitionIds = keep
            };
            bool ok = _session.TryRegisterCompetition(competitionId, options, out RegisterCompetitionResult result);
            HandleRegisterCompetitionResult(competitionId, ok, result);
        }

        private string ResolveCompetitionDisplayName(string competitionId)
        {
            if (_content?.Competitions != null &&
                _content.Competitions.TryGetValue(competitionId, out CompetitionDefinition def) &&
                def != null &&
                !string.IsNullOrWhiteSpace(def.displayName))
                return def.displayName;
            return competitionId ?? string.Empty;
        }
    }
}
