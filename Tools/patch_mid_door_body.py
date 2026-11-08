# -*- coding: utf-8 -*-
"""Size general-mode bodies from the Dust II mid door, and read baked map masks."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing anchor:\n" + old[:180])
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "mid_door_body"])
    text = SRC.read_text(encoding="utf-8")
    if "AgentCollisionRadius()" in text and "BindOverviewResource" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();\n",
        "        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();\n"
        "        private string _overviewResource = \"Maps/dust2_overview\";\n"
        "        private float _collisionHammerPerPixel;\n"
        "        private Texture2D _collisionMask;\n",
    )
    text = must_replace(
        text,
        "        /// <summary>Collision diameter is 20% of the visual circle. The drawn dot is unchanged.</summary>\n"
        "        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.10f;\n",
        "        /// <summary>Used only when a map has no baked hammer scale.</summary>\n"
        "        private const float GeneralCollisionRadiusFallback = MarkerDotDiameter * 0.10f;\n",
    )
    text = text.replace("GeneralCollisionRadiusFallback", "TEMP_COLLISION_FALLBACK")
    text = text.replace("GeneralCollisionRadius", "AgentCollisionRadius()")
    text = text.replace("TEMP_COLLISION_FALLBACK", "GeneralCollisionRadiusFallback")
    text = must_replace(
        text,
        "        /// <summary>Map bounds inset by agent collision radius so bodies stay inside.</summary>\n"
        "        private Vector2 ClampToMapCollision(Vector2 local)\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float height);\n"
        "            float pad = Mathf.Max(AgentCollisionRadius() + 1f,\n"
        "                Mathf.Min(28f, Mathf.Min(width, height) * 0.04f));\n"
        "            return new Vector2(\n"
        "                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),\n"
        "                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));\n"
        "        }\n",
        "        /// <summary>\n"
        "        /// Body radius from the shared hammer size. Dust II's mid door fits one body plus a little room.\n"
        "        /// </summary>\n"
        "        private float AgentCollisionRadius()\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float _);\n"
        "            float overviewWidth = _mapSprite != null ? _mapSprite.rect.width : 0f;\n"
        "            if (_collisionHammerPerPixel > 0.001f && overviewWidth > 1f && width > 1f)\n"
        "                return MapBodyClearance.LocalRadius(width, overviewWidth, _collisionHammerPerPixel);\n"
        "            return GeneralCollisionRadiusFallback;\n"
        "        }\n"
        "\n"
        "        /// <summary>Map bounds inset by agent collision radius so bodies stay inside.</summary>\n"
        "        private Vector2 ClampToMapCollision(Vector2 local)\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float height);\n"
        "            float pad = Mathf.Max(AgentCollisionRadius() + 1f,\n"
        "                Mathf.Min(28f, Mathf.Min(width, height) * 0.04f));\n"
        "            local = new Vector2(\n"
        "                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),\n"
        "                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));\n"
        "            if (_mapCollision != null && _mapCollision.IsReady)\n"
        "                local = _mapCollision.ResolveCircle(local, AgentCollisionRadius());\n"
        "            return local;\n"
        "        }\n",
    )
    text = must_replace(
        text,
        "            if (floor != null && !string.IsNullOrEmpty(floor.overviewResource))\n"
        "            {\n"
        "                Sprite sprite = Resources.Load<Sprite>(floor.overviewResource);\n"
        "                if (sprite != null)\n"
        "                    return sprite;\n"
        "            }\n",
        "            if (floor != null && !string.IsNullOrEmpty(floor.overviewResource))\n"
        "            {\n"
        "                Sprite sprite = Resources.Load<Sprite>(floor.overviewResource);\n"
        "                if (sprite != null)\n"
        "                {\n"
        "                    BindOverviewResource(floor.overviewResource);\n"
        "                    return sprite;\n"
        "                }\n"
        "            }\n",
    )
    text = must_replace(
        text,
        "        private static Sprite LoadOverviewSprite(string mapId)\n"
        "        {\n"
        "            if (!string.IsNullOrEmpty(mapId) &&\n"
        "                !string.Equals(mapId, \"dust2\", StringComparison.OrdinalIgnoreCase))\n"
        "            {\n"
        "                Sprite named = Resources.Load<Sprite>(\"Maps/\" + mapId + \"_overview\");\n"
        "                if (named != null)\n"
        "                    return named;\n"
        "            }\n"
        "            return Resources.Load<Sprite>(\"Maps/dust2_overview\");\n"
        "        }\n",
        "        private Sprite LoadOverviewSprite(string mapId)\n"
        "        {\n"
        "            if (!string.IsNullOrEmpty(mapId) &&\n"
        "                !string.Equals(mapId, \"dust2\", StringComparison.OrdinalIgnoreCase))\n"
        "            {\n"
        "                string namedResource = \"Maps/\" + mapId + \"_overview\";\n"
        "                Sprite named = Resources.Load<Sprite>(namedResource);\n"
        "                if (named != null)\n"
        "                {\n"
        "                    BindOverviewResource(namedResource);\n"
        "                    return named;\n"
        "                }\n"
        "            }\n"
        "            BindOverviewResource(\"Maps/dust2_overview\");\n"
        "            return Resources.Load<Sprite>(\"Maps/dust2_overview\");\n"
        "        }\n"
        "\n"
        "        private void BindOverviewResource(string resource)\n"
        "        {\n"
        "            _overviewResource = string.IsNullOrEmpty(resource) ? \"Maps/dust2_overview\" : resource;\n"
        "            _collisionHammerPerPixel = MapCollisionScale.HammerPerPixel(_overviewResource);\n"
        "            _collisionMask = Resources.Load<Texture2D>(_overviewResource + \"_collision\");\n"
        "        }\n",
    )
    text = must_replace(
        text,
        "            _mapCollision.Rebuild(_mapSprite, width, height);\n",
        "            _mapCollision.Rebuild(_mapSprite, width, height, _collisionMask);\n",
    )
    if "叫暫停" not in text or "特性觸發" not in text:
        raise RuntimeError("UTF-8 anchors missing after patch")
    if "ApplyControlBarLayout(simulation)" not in text or "ApplyControlBarLayout(!simulation)" in text:
        raise RuntimeError("control bar polarity changed")
    if "private const float GeneralCollisionRadius =" in text:
        raise RuntimeError("old collision constant still present")
    SRC.write_text(text, encoding="utf-8")
    print("mid-door body collision patched")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
