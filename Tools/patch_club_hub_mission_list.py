# -*- coding: utf-8 -*-
"""Club hub missions tab: node list + open CTA."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "Assets/_Project/Scripts/Presentation/CareerClubHubPresentation.cs"
text = PATH.read_text(encoding="utf-8")

old = '''        private void DrawClubHubMissions()
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
                Image row = CreateUiObject<Image>("MissionReopenRow", _clubHubContentRoot);
                LayoutElement rowLe = row.gameObject.AddComponent<LayoutElement>();
                rowLe.preferredHeight = 44f;
                rowLe.flexibleWidth = 1f;
                UiLightChromeUtility.ApplyFrostedListRow(row);
                CreateGlassCapsuleButton(
                    "ReopenMissionTree", "開啟任務樹", row.transform,
                    new Vector2(0.04f, 0.14f), new Vector2(0.42f, 0.86f),
                    ButtonEdge, ButtonFace, ReopenMissionTreeOverlay, _uiFont700);
                return;
            }

            AddClubHubContentMuted("全螢幕任務樹已開啟。游標停在節點上可查看背景與目標。");

            if (_missionTreePresentation == null || !_missionTreePresentation.IsOpen)
                OpenMissionTreeOverlay();
        }'''

new = '''        private void DrawClubHubMissions()
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
            UiLightChromeUtility.ApplyFrostedListRow(openRow);
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
            UiLightChromeUtility.ApplyFrostedListRow(row);

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
        }'''

if old not in text:
    raise SystemExit("DrawClubHubMissions not found")
text = text.replace(old, new, 1)
PATH.write_text(text, encoding="utf-8")
print("patched", PATH)
assert "DrawClubHubMissionNodeRow" in text
assert "開啟任務樹" in text
print("ok")
