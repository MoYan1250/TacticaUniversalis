# -*- coding: utf-8 -*-
"""Map ban/pick UI: Black/Anton fonts, layout, start battle CTA."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "Assets/_Project/Scripts/Presentation/MapBanPickPresentation.cs"
CAREER = ROOT / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + repr(old[:280]))
    return text.replace(old, new, 1)


def patch_map(text: str) -> str:
    text = must_replace(
        text,
        "        private readonly Font _font;\n"
        "        private readonly Action _onVetoComplete;\n",
        "        private readonly Font _font;\n"
        "        private readonly Font _fontBlack;\n"
        "        private readonly Font _fontAnton;\n"
        "        private readonly Action _onVetoComplete;\n",
    )

    text = must_replace(
        text,
        "            Font font,\n"
        "            Action onVetoComplete,\n"
        "            Action<string> onStatus)\n"
        "        {\n"
        "            _session = session;\n"
        "            _font = font;\n"
        "            _onVetoComplete = onVetoComplete;\n"
        "            _onStatus = onStatus;\n"
        "        }\n",
        "            Font font,\n"
        "            Action onVetoComplete,\n"
        "            Action<string> onStatus,\n"
        "            Font fontBlack = null,\n"
        "            Font fontAnton = null)\n"
        "        {\n"
        "            _session = session;\n"
        "            _font = font;\n"
        "            _fontBlack = fontBlack != null ? fontBlack : font;\n"
        "            _fontAnton = fontAnton != null ? fontAnton : font;\n"
        "            _onVetoComplete = onVetoComplete;\n"
        "            _onStatus = onStatus;\n"
        "        }\n",
    )

    # Title anchors + Black font + shadow
    text = must_replace(
        text,
        'CreateText("Title", face.transform, 26, TextAnchor.MiddleCenter, TextPrimary);\n'
        "            SetRect(title.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.88f, 0.985f));\n",
        'CreateText("Title", face.transform, 28, TextAnchor.MiddleCenter, TextPrimary, _fontBlack);\n'
        "            SetRect(title.rectTransform, new Vector2(0.12f, 0.90f), new Vector2(0.88f, 0.985f));\n",
    )
    text = must_replace(
        text,
        "            title.fontStyle = FontStyle.Bold;\n"
        "            EnableBestFit(title, 42);\n",
        "            title.fontStyle = FontStyle.Bold;\n"
        "            EnableBestFit(title, 46);\n"
        "            ApplyTextShadow(title);\n",
    )

    # Step status larger + Black + shadow
    text = must_replace(
        text,
        '_stepText = CreateText("Step", face.transform, 22, TextAnchor.MiddleCenter, Accent);\n'
        "            SetRect(_stepText.rectTransform, new Vector2(0.06f, 0.835f), new Vector2(0.94f, 0.90f));\n"
        "            EnableBestFit(_stepText, 34);\n",
        '_stepText = CreateText("Step", face.transform, 28, TextAnchor.MiddleCenter, Accent, _fontBlack);\n'
        "            SetRect(_stepText.rectTransform, new Vector2(0.06f, 0.835f), new Vector2(0.94f, 0.90f));\n"
        "            EnableBestFit(_stepText, 44);\n"
        "            ApplyTextShadow(_stepText);\n",
    )

    # Summary white + Anton + shadow
    text = must_replace(
        text,
        '_summaryText = CreateText("Summary", face.transform, 18, TextAnchor.MiddleCenter, TextMuted);\n'
        "            SetRect(_summaryText.rectTransform, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.835f));\n"
        "            EnableBestFit(_summaryText, 28);\n",
        '_summaryText = CreateText("Summary", face.transform, 22, TextAnchor.MiddleCenter, Color.white, _fontAnton);\n'
        "            SetRect(_summaryText.rectTransform, new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.835f));\n"
        "            EnableBestFit(_summaryText, 36);\n"
        "            ApplyTextShadow(_summaryText);\n",
    )

    # Start button: label, width, Black font, 90% fill
    # Locate by StartMaps id and surrounding ASCII
    start_marker = 'CreateGlassCapsuleButton(\n                "StartMaps", "'
    i = text.find(start_marker)
    if i < 0:
        raise RuntimeError("StartMaps button not found")
    j = text.find('", face.transform,', i)
    if j < 0:
        raise RuntimeError("StartMaps label end not found")
    # Replace label only
    text = text[: i + len(start_marker)] + "開始戰鬥" + text[j:]

    text = must_replace(
        text,
        'CreateGlassCapsuleButton(\n'
        '                "StartMaps", "開始戰鬥", face.transform,\n'
        "                new Vector2(0.40f, 0.035f), new Vector2(0.60f, 0.105f),\n"
        "                () =>\n"
        "                {\n"
        "                    Close();\n"
        "                    _onVetoComplete?.Invoke();\n"
        "                },\n"
        "                _font, 16, fillLabel: true, primary: true);\n"
        "            _startMatchButton.gameObject.SetActive(false);\n",
        'CreateGlassCapsuleButton(\n'
        '                "StartMaps", "開始戰鬥", face.transform,\n'
        "                new Vector2(0.42f, 0.035f), new Vector2(0.58f, 0.105f),\n"
        "                () =>\n"
        "                {\n"
        "                    Close();\n"
        "                    _onVetoComplete?.Invoke();\n"
        "                },\n"
        "                _fontBlack, 18, fillLabel: true, primary: true);\n"
        "            _startMatchButton.gameObject.SetActive(false);\n"
        "            Text startLabel = _startMatchButton.GetComponentInChildren<Text>();\n"
        "            if (startLabel != null)\n"
        "            {\n"
        "                startLabel.font = _fontBlack;\n"
        "                ApplyFilledButtonLabel(startLabel, 48, 0.9f);\n"
        "            }\n",
    )

    # Remove rank creation block
    text = must_replace(
        text,
        'Text rankText = CreateText("Rank", column.transform, 20, TextAnchor.MiddleCenter, TextMuted);\n'
        "            SetRect(rankText.rectTransform, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 1f));\n"
        "            rankText.text = rank.ToString();\n"
        "            rankText.fontStyle = FontStyle.Bold;\n"
        "            EnableBestFit(rankText, 36);\n"
        "            _mapRanks[entry.id] = rankText;\n"
        "\n"
        '            Image edge = CreateUiObject<Image>("Card", column.transform);\n'
        "            SetRect(edge.rectTransform, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.84f));\n",
        'Image edge = CreateUiObject<Image>("Card", column.transform);\n'
        "            SetRect(edge.rectTransform, new Vector2(0.03f, 0.18f), new Vector2(0.97f, 0.92f));\n",
    )

    # Wider gaps between slots
    text = must_replace(
        text,
        'FormatSeriesSlot(series, 0) + "    " +\n'
        '                FormatSeriesSlot(series, 1) + "    " +\n'
        "                FormatSeriesSlot(series, 2);",
        'FormatSeriesSlot(series, 0) + "        " +\n'
        '                FormatSeriesSlot(series, 1) + "        " +\n'
        "                FormatSeriesSlot(series, 2);",
    )

    # Drop rank color updates
    text = must_replace(
        text,
        "                if (_mapRanks.TryGetValue(id, out Text rank) && rank != null)\n"
        "                    rank.color = status;\n",
        "",
    )

    # FormatSeriesSlot: mapN -> 圖一/二/三
    text = must_replace(
        text,
        '            string prefix = "map" + (index + 1);\n',
        '            string prefix = index == 0 ? "圖一" : index == 1 ? "圖二" : "圖三";\n',
    )

    # CreateText overload with font
    text = must_replace(
        text,
        "        private Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)\n"
        "        {\n"
        "            Text text = CreateUiObject<Text>(name, parent);\n"
        '            text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("Arial.ttf");\n',
        "        private static void ApplyTextShadow(Text text)\n"
        "        {\n"
        "            if (text == null)\n"
        "                return;\n"
        "            Shadow shadow = text.GetComponent<Shadow>();\n"
        "            if (shadow == null)\n"
        "                shadow = text.gameObject.AddComponent<Shadow>();\n"
        "            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);\n"
        "            shadow.effectDistance = new Vector2(2f, -2f);\n"
        "            shadow.useGraphicAlpha = true;\n"
        "        }\n"
        "\n"
        "        private Text CreateText(\n"
        "            string name, Transform parent, int size, TextAnchor anchor, Color color, Font font = null)\n"
        "        {\n"
        "            Text text = CreateUiObject<Text>(name, parent);\n"
        "            Font resolved = font != null ? font : _font;\n"
        '            text.font = resolved != null ? resolved : Resources.GetBuiltinResource<Font>("Arial.ttf");\n',
    )

    return text


def patch_career(text: str) -> str:
    if "private Font _uiFontBlack;" not in text:
        text = must_replace(
            text,
            "        private Font _uiFont;\n"
            "        private Font _uiFont700;\n",
            "        private Font _uiFont;\n"
            "        private Font _uiFont700;\n"
            "        private Font _uiFontBlack;\n"
            "        private Font _uiFontAnton;\n",
        )

    # Assign Anton after Black load — match whichever Black load line exists
    if "_uiFontAnton =" not in text:
        if 'LoadProjectFont("NotoSansTC-Black.ttf")' in text:
            text = must_replace(
                text,
                '_uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont700;\n',
                '_uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont700;\n'
                "            _uiFontAnton = LoadProjectFont(\"Anton.ttf\") ?? _uiFontBlack;\n",
            )
        elif "LoadProjectFont(\"NotoSansTC-Black.ttf\")" in text.replace("'", '"'):
            pass
        else:
            # Try alternate quote style / method name from file
            for needle in (
                '_uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont700;\n',
                "_uiFontBlack = LoadProjectFont(\"NotoSansTC-Black.ttf\") ?? _uiFont700;\n",
            ):
                if needle in text:
                    text = must_replace(
                        text,
                        needle,
                        needle + '            _uiFontAnton = LoadProjectFont("Anton.ttf") ?? _uiFontBlack;\n',
                    )
                    break
            else:
                # Find any _uiFontBlack = line
                idx = text.find("_uiFontBlack =")
                if idx < 0:
                    raise RuntimeError("no _uiFontBlack assignment")
                end = text.find("\n", idx) + 1
                text = text[:end] + '            _uiFontAnton = LoadProjectFont("Anton.ttf") ?? _uiFontBlack;\n' + text[end:]

    # Wire constructor call — find MapBanPickPresentation( block
    marker = "_mapBanPick = new MapBanPickPresentation("
    i = text.find(marker)
    if i < 0:
        raise RuntimeError("MapBanPickPresentation ctor call missing")
    # Find closing }); of that call
    j = text.find("});", i)
    if j < 0:
        raise RuntimeError("ctor call end missing")
    block = text[i:j]
    if "_uiFontAnton" in block:
        return text
    # Insert fonts before closing paren of constructor
    # block ends before });
    # typically ends with "});" after lambda
    close = text.rfind(")", i, j)
    text = text[:close] + ",\n                        _uiFontBlack,\n                        _uiFontAnton" + text[close:]
    return text


def main() -> None:
    map_text = patch_map(MAP.read_text(encoding="utf-8"))
    for s in ("開始戰鬥", "圖一", "ApplyTextShadow", "_fontBlack", "_fontAnton"):
        if s not in map_text:
            raise RuntimeError("missing: " + s)
    if 'CreateText("Rank"' in map_text:
        raise RuntimeError("Rank text still present")
    MAP.write_text(map_text, encoding="utf-8")
    print("patched", MAP)

    career = patch_career(CAREER.read_text(encoding="utf-8"))
    if "_uiFontAnton" not in career:
        raise RuntimeError("career anton missing")
    CAREER.write_text(career, encoding="utf-8")
    print("patched", CAREER)


if __name__ == "__main__":
    main()
