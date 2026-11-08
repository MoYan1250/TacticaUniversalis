# -*- coding: utf-8 -*-
"""Battle map: zone labels show name only; bonuses on hover tooltip. UTF-8 safe."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "zone_label_hover"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text and "叫暫停" not in text:
        raise RuntimeError("refusing: file looks corrupted")

    text = must_replace(
        text,
        """        private readonly List<Text> _zoneLabels = new List<Text>();
        private readonly Dictionary<string, MapZoneDefinition> _zones = new Dictionary<string, MapZoneDefinition>();""",
        """        private readonly List<Text> _zoneLabels = new List<Text>();
        private readonly List<Image> _zoneHitPads = new List<Image>();
        private Image _zoneTooltip;
        private Text _zoneTooltipText;
        private string _hoveredZoneId;
        private readonly Dictionary<string, MapZoneDefinition> _zones = new Dictionary<string, MapZoneDefinition>();""",
    )

    text = must_replace(
        text,
        """            CreateZoneLabels();
            CreateMarkers();
            CreateBombMarker();
            CreateRoundWinBanner(canvas.transform);
            CreateProfilePopup(canvas.transform);""",
        """            CreateMarkers();
            CreateBombMarker();
            CreateZoneLabels();
            CreateZoneTooltip(canvas.transform);
            CreateRoundWinBanner(canvas.transform);
            CreateProfilePopup(canvas.transform);""",
    )

    text = must_replace(
        text,
        """        private void CreateZoneLabels()
        {
            _zoneLabels.Clear();
            var zoneLabelColor = new Color(0.92f, 0.93f, 0.94f, 0.78f);
            foreach (MapZoneDefinition zone in _content.Dust2.zones)
            {
                Text label = CreateText("Zone_" + zone.id, _mapRect, 12, TextAnchor.MiddleCenter, zoneLabelColor);
                label.text = DemoZh.ZoneName(zone.id) + "\\n" + FormatZoneBonusHint(zone);
                label.fontStyle = FontStyle.Bold;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(120f, 36f);
                Outline outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
                outline.effectDistance = new Vector2(1.1f, -1.1f);
                _zoneLabels.Add(label);
            }
        }

        private void RelayoutZoneLabels()
        {
            if (_zoneLabels.Count == 0 || _content?.Dust2?.zones == null)
                return;
            for (int i = 0; i < _zoneLabels.Count && i < _content.Dust2.zones.Length; i++)
            {
                MapZoneDefinition zone = _content.Dust2.zones[i];
                _zoneLabels[i].rectTransform.anchoredPosition =
                    ClampToMap(LocalZonePosition(zone.id) + new Vector2(0f, 22f));
            }
        }

        private static string FormatZoneBonusHint(MapZoneDefinition zone)
        {
            var parts = new List<string>(4);
            AppendBonusPart(parts, "攻瞄", zone.attackerAimBonus);
            AppendBonusPart(parts, "守反", zone.defenderReactionBonus);
            AppendBonusPart(parts, "攻協", zone.attackerTeamworkBonus);
            AppendBonusPart(parts, "守察", zone.defenderAwarenessBonus);
            return parts.Count == 0 ? "無加成" : string.Join(" ", parts);
        }

        private static void AppendBonusPart(List<string> parts, string label, float bonus)
        {
            if (Mathf.Abs(bonus) < 0.0001f)
                return;
            float pct = bonus * 100f;
            parts.Add(pct >= 0f ? $"{label}+{pct:0}" : $"{label}{pct:0}");
        }""",
        """        private void CreateZoneLabels()
        {
            _zoneLabels.Clear();
            _zoneHitPads.Clear();
            var zoneLabelColor = new Color(0.92f, 0.93f, 0.94f, 0.78f);
            foreach (MapZoneDefinition zone in _content.Dust2.zones)
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
            if (_content?.Dust2?.zones == null)
                return;
            for (int i = 0; i < _content.Dust2.zones.Length; i++)
            {
                MapZoneDefinition zone = _content.Dust2.zones[i];
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
            return string.Join("\\n", lines);
        }

        private static void AppendBonusDetailLine(List<string> lines, string label, float bonus)
        {
            if (Mathf.Abs(bonus) < 0.0001f)
                return;
            float pct = bonus * 100f;
            string value = pct >= 0f ? $"+{pct:0}%" : $"{pct:0}%";
            lines.Add($"{label}  {value}");
        }

        private static string FormatZoneBonusHint(MapZoneDefinition zone)
        {
            var parts = new List<string>(4);
            AppendBonusPart(parts, "攻瞄", zone.attackerAimBonus);
            AppendBonusPart(parts, "守反", zone.defenderReactionBonus);
            AppendBonusPart(parts, "攻協", zone.attackerTeamworkBonus);
            AppendBonusPart(parts, "守察", zone.defenderAwarenessBonus);
            return parts.Count == 0 ? "無加成" : string.Join(" ", parts);
        }

        private static void AppendBonusPart(List<string> parts, string label, float bonus)
        {
            if (Mathf.Abs(bonus) < 0.0001f)
                return;
            float pct = bonus * 100f;
            parts.Add(pct >= 0f ? $"{label}+{pct:0}" : $"{label}{pct:0}");
        }""",
    )

    # Zone labels are now children of hit pads; upright rotation should still apply to Text.
    # Hit pads rotate with the map which is correct.

    # Hide zone tooltip on ESC alongside profile if needed — optional.
    if "HideZoneTooltip(null)" not in text and "HideZoneTooltip(_hoveredZoneId)" not in text:
        text = must_replace(
            text,
            """            if (_profilePopup != null && _profilePopup.gameObject.activeSelf)
            {
                HideProfilePopup();
                CancelProfileHover();
            }
        }""",
            """            if (_profilePopup != null && _profilePopup.gameObject.activeSelf)
            {
                HideProfilePopup();
                CancelProfileHover();
            }

            if (_zoneTooltip != null && _zoneTooltip.gameObject.activeSelf)
                HideZoneTooltip(_hoveredZoneId);
        }""",
        )

    TARGET.write_text(text, encoding="utf-8")
    assert "ShowZoneTooltip" in text
    assert 'label.text = DemoZh.ZoneName(zone.id);' in text
    assert "FormatZoneBonusDetail" in text
    assert "擊殺廣播" in text or "叫暫停" in text
    # Ensure we didn't leave name+hint concatenation on labels
    assert 'DemoZh.ZoneName(zone.id) + "\\n" + FormatZoneBonusHint' not in text
    print("patched", TARGET)


if __name__ == "__main__":
    main()
