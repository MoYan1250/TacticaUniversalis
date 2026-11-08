# -*- coding: utf-8 -*-
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/CareerWorldMapPresentation.cs"
text = path.read_text(encoding="utf-8")

old_host = """            BuildMapTooltip(host.transform);
            BuildMapSidePanel(host.transform);
            BuildMapSchedulePanel(host.transform);
            _mapHost.SetActive(true);
        }"""

new_host = """            BuildMapTooltip(host.transform);
            BuildMapSidePanel(host.transform);
            BuildMapSchedulePanel(host.transform);
            BuildMapEventsNewsButton(host.transform);
            _mapHost.SetActive(true);
        }

        private void BuildMapEventsNewsButton(Transform mapHost)
        {
            Image dock = CreateUiObject<Image>("MapEventsNewsDock", mapHost);
            SetRect(dock.rectTransform, new Vector2(0.012f, 0.015f), new Vector2(0.30f, 0.105f));
            dock.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);
            dock.raycastTarget = true;

            CreateActionButton(
                "MapEventsNewsBtn", "事件與新聞", dock.transform,
                new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.86f),
                ButtonEdge, ButtonFace, ShowMapEventLog, _uiFont700);
        }"""

if old_host not in text:
    raise SystemExit("BuildWorldMapHost anchor not found")
text = text.replace(old_host, new_host, 1)

old_panel = """            Text title = CreateText("ScheduleTitle", panel.transform, 17, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.text = "賽事與事件";

            CreateActionButton(
                "MapRankBtn", "排名", panel.transform,
                new Vector2(0.05f, 0.82f), new Vector2(0.24f, 0.89f),
                ButtonEdge, ButtonFace, OpenWorldRankingPanel, _uiFont700);
            CreateActionButton(
                "MapEventsBtn", "事件", panel.transform,
                new Vector2(0.26f, 0.82f), new Vector2(0.45f, 0.89f),
                ButtonEdge, ButtonFace, ShowMapEventLog, _uiFont700);

            Image scrollHost = CreateUiObject<Image>("ScheduleScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.80f));"""

new_panel = """            Text title = CreateText("ScheduleTitle", panel.transform, 19, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.text = "賽事行程 · 報名";

            Image scrollHost = CreateUiObject<Image>("ScheduleScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.88f));"""

if old_panel not in text:
    raise SystemExit("BuildMapSchedulePanel anchor not found")
text = text.replace(old_panel, new_panel, 1)

path.write_text(text, encoding="utf-8", newline="\n")
print("Patched map events button layout")
