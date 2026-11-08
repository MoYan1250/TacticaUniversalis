# -*- coding: utf-8 -*-
"""Thin red facing laser + orange traveling fire tracer on map markers."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
from backup_quickmatch import backup  # noqa: E402

SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError("missing: " + label)
    return text.replace(old, new, 1)


def main() -> int:
    backup("sight_ray_tracer")
    text = SRC.read_text(encoding="utf-8")
    if "ShowMuzzleRay" not in text or "ViewCone" not in text:
        raise RuntimeError("viewcone/muzzle anchors missing")
    if "擊殺廣播" not in text and "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 anchors missing")

    text = must_replace(
        text,
        """        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly List<MuzzleRay> _muzzleRays = new List<MuzzleRay>();
""",
        """        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _markerSightRays = new Dictionary<string, Image>();
        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly List<MuzzleRay> _muzzleRays = new List<MuzzleRay>();
""",
        "sight ray dictionary",
    )

    text = must_replace(
        text,
        """        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
        private const float MuzzleRayWidth = 3.2f;
        private const float MuzzleRaySeconds = 0.28f;
""",
        """        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
        private const float SightRayWidth = 1.15f;
        private const float SightRayLength = 108f;
        private static readonly Color SightRayColor = new Color(1f, 0.16f, 0.12f, 0.88f);
        private const float MuzzleRayWidth = 2.05f;
        private const float MuzzleRaySeconds = 0.14f;
        private const float MuzzleBoltLength = 42f;
        private static readonly Color MuzzleTracerColor = new Color(1f, 0.50f, 0.14f, 0.96f);
""",
        "sight/tracer constants",
    )

    text = must_replace(
        text,
        """        private sealed class MuzzleRay
        {
            public Image Image;
            public float ExpireAtUnscaled;
            public Color BaseColor;
        }
""",
        """        private sealed class MuzzleRay
        {
            public Image Image;
            public Vector2 From;
            public Vector2 To;
            public float ExpireAtUnscaled;
            public Color BaseColor;
        }
""",
        "MuzzleRay travel fields",
    )

    text = must_replace(
        text,
        """        private Sprite _circleSprite;
        private Sprite _ringSprite;
""",
        """        private Sprite _circleSprite;
        private Sprite _ringSprite;
        private Sprite _lineSprite;
""",
        "line sprite field",
    )

    text = must_replace(
        text,
        """            _circleSprite = CreateCircleSprite(true);
            _ringSprite = CreateCircleSprite(false);
""",
        """            _circleSprite = CreateCircleSprite(true);
            _ringSprite = CreateCircleSprite(false);
            _lineSprite = CreateLineSprite();
""",
        "create line sprite",
    )

    text = must_replace(
        text,
        """            if (_circleSprite != null)
                Destroy(_circleSprite.texture);
            if (_ringSprite != null)
                Destroy(_ringSprite.texture);
""",
        """            if (_circleSprite != null)
                Destroy(_circleSprite.texture);
            if (_ringSprite != null)
                Destroy(_ringSprite.texture);
            if (_lineSprite != null)
                Destroy(_lineSprite.texture);
""",
        "destroy line sprite",
    )

    text = must_replace(
        text,
        """                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                {
                    float coneLen = IsGeneralBattleMode() ? ViewConeLength : ViewConeLength * 1.15f;
                    cone.rectTransform.sizeDelta = new Vector2(coneLen * 2f, coneLen * 2f);
                }
""",
        """                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
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
""",
        "resize sight rays",
    )

    text = must_replace(
        text,
        """                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
""",
        """                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();

                    Image sight = CreateUiObject<Image>("SightRay", root.transform);
                    sight.sprite = _lineSprite != null ? _lineSprite : _circleSprite;
                    sight.color = SightRayColor;
                    sight.raycastTarget = false;
                    sight.rectTransform.anchorMin = sight.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    sight.rectTransform.pivot = new Vector2(0f, 0.5f);
                    sight.rectTransform.anchoredPosition = Vector2.zero;
                    sight.rectTransform.sizeDelta = new Vector2(SightRayLength, SightRayWidth);

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
""",
        "create sight ray",
    )

    text = must_replace(
        text,
        """                    _markerCones[id] = cone;
                    _markerFacingDeg[id] = 90f;
                    ApplyViewConeFacing(id, 90f);
""",
        """                    _markerCones[id] = cone;
                    _markerSightRays[id] = sight;
                    _markerFacingDeg[id] = 90f;
                    ApplyViewConeFacing(id, 90f);
""",
        "register sight ray",
    )

    text = must_replace(
        text,
        """        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            if (!_markerCones.TryGetValue(playerId, out Image cone) || cone == null)
                return;
            cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);
        }
""",
        """        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            if (_markerCones.TryGetValue(playerId, out Image cone) && cone != null)
                cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);
            if (_markerSightRays.TryGetValue(playerId, out Image sight) && sight != null)
            {
                float rad = facingDeg * Mathf.Deg2Rad;
                float muzzle = ActiveMarkerDotDiameter() * 0.48f;
                sight.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * muzzle;
                sight.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg);
            }
        }
""",
        "facing rotates sight ray",
    )

    text = must_replace(
        text,
        """        private void ShowMuzzleRay(string shooterId, string targetId)
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
                ray.sprite = _circleSprite;
                ray.raycastTarget = false;
                ray.rectTransform.anchorMin = ray.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ray.rectTransform.pivot = new Vector2(0f, 0.5f);
                slot = new MuzzleRay { Image = ray };
                _muzzleRays.Add(slot);
            }

            bool isT = PlayerIsTerrorist(shooterId, _currentRound);
            Color tint = isT ? TText : CtText;
            tint.a = 0.92f;
            ray.color = tint;
            ray.gameObject.SetActive(true);
            ray.rectTransform.SetAsLastSibling();
            ray.rectTransform.anchoredPosition = from;
            ray.rectTransform.sizeDelta = new Vector2(dist, MuzzleRayWidth);
            ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, FacingDegrees(delta));
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
                Color c = ray.BaseColor;
                c.a = ray.BaseColor.a * Mathf.Clamp01(left / MuzzleRaySeconds);
                ray.Image.color = c;
            }
        }
""",
        """        private void ShowMuzzleRay(string shooterId, string targetId)
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
""",
        "orange traveling tracer",
    )

    text = must_replace(
        text,
        """        private static Sprite CreateCircleSprite(bool filled)
""",
        """        private static Sprite CreateLineSprite()
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
""",
        "insert CreateLineSprite",
    )

    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    for need in (
        "SightRay",
        "_markerSightRays",
        "MuzzleTracerColor",
        "CreateLineSprite",
        "ShowMuzzleRay",
        "TickMuzzleRays",
        "SimulationEventType.Shot",
        "ApplyControlBarLayout(simulation)",
        "叫暫停",
    ):
        if need not in check:
            raise RuntimeError("missing after patch: " + need)
    if "ApplyControlBarLayout(!simulation)" in check:
        raise RuntimeError("control bar polarity broken")
    print("patched thin red sight ray + orange fire tracer")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
