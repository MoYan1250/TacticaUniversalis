using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Rounded masked RawImage that samples a shared blurred backdrop texture by screen UV.
    /// Without a blur texture, shows a soft navy glass fallback (never solid white RawImage).
    /// With blur, applies FrostBlurTint + FrostBlurVeil so frost stays readable, not washed-out.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiFrostedGlassSlice : MonoBehaviour
    {
        private static readonly Color FallbackGlass = UiLightChromeUtility.GlassFace;

        private Image _panel;
        private Mask _mask;
        private RawImage _raw;
        private RectTransform _rawRect;
        private Image _veil;
        private UiFrostedGlassHost _host;
        private bool _registered;

        public static UiFrostedGlassSlice Attach(Image panel)
        {
            if (panel == null)
                return null;

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            panel.sprite = rounded;
            panel.type = Image.Type.Sliced;
            panel.color = FallbackGlass;

            Mask mask = panel.GetComponent<Mask>();
            if (mask == null)
                mask = panel.gameObject.AddComponent<Mask>();
            // Show rounded fallback until blur texture exists.
            mask.showMaskGraphic = true;

            UiFrostedGlassSlice slice = panel.GetComponent<UiFrostedGlassSlice>();
            if (slice == null)
                slice = panel.gameObject.AddComponent<UiFrostedGlassSlice>();

            slice._panel = panel;
            slice._mask = mask;
            slice.EnsureRaw();
            slice.EnsureVeil();
            slice.ApplyTexture(null);
            slice.TryRegister();
            return slice;
        }

        private void EnsureRaw()
        {
            if (_raw != null)
                return;

            GameObject go = new GameObject("FrostBlur", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            _rawRect = go.GetComponent<RectTransform>();
            _rawRect.anchorMin = Vector2.zero;
            _rawRect.anchorMax = Vector2.one;
            _rawRect.offsetMin = Vector2.zero;
            _rawRect.offsetMax = Vector2.zero;

            _raw = go.AddComponent<RawImage>();
            _raw.color = UiLightChromeUtility.FrostBlurTint;
            _raw.raycastTarget = false;
            _raw.texture = null;
            _raw.enabled = false;
            _raw.maskable = true;
        }

        private void EnsureVeil()
        {
            if (_veil != null)
                return;

            EnsureRaw();

            GameObject go = new GameObject("FrostVeil", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);
            // Sit directly above FrostBlur so content siblings stay on top.
            go.transform.SetSiblingIndex(_raw != null ? _raw.transform.GetSiblingIndex() + 1 : 0);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Sprite rounded = UiRoundedRectUtility.GetRoundedRectSprite();
            _veil = go.AddComponent<Image>();
            _veil.sprite = rounded;
            _veil.type = Image.Type.Sliced;
            _veil.color = UiLightChromeUtility.FrostBlurVeil;
            _veil.raycastTarget = false;
            _veil.maskable = true;
            _veil.enabled = false;
        }

        private void SetVeilVisible(bool visible)
        {
            if (_veil == null)
                EnsureVeil();
            if (_veil == null)
                return;
            _veil.enabled = visible;
            _veil.color = UiLightChromeUtility.FrostBlurVeil;
        }

        private void OnEnable()
        {
            TryRegister();
            UpdateUv();
        }

        private void OnDisable()
        {
            if (_host != null && _registered)
            {
                _host.Unregister(this);
                _registered = false;
            }
        }

        private void OnDestroy()
        {
            if (_host != null && _registered)
            {
                _host.Unregister(this);
                _registered = false;
            }
        }

        private void LateUpdate()
        {
            if (_raw != null && _raw.enabled && _raw.texture != null)
                UpdateUv();
        }

        private void TryRegister()
        {
            if (_registered)
                return;
            _host = UiFrostedGlassHost.FindFor(transform);
            if (_host == null)
                return;
            _host.Register(this);
            _registered = true;
        }

        public void SetBlurVisible(bool visible)
        {
            if (_raw == null)
                return;
            bool show = visible && _raw.texture != null;
            _raw.enabled = show;
            _raw.color = UiLightChromeUtility.FrostBlurTint;
            SetVeilVisible(show);
            if (_mask != null)
                _mask.showMaskGraphic = !show;
            if (_panel != null && !show)
                _panel.color = FallbackGlass;
        }

        public void ApplyTexture(Texture2D texture)
        {
            if (_raw == null)
                EnsureRaw();
            EnsureVeil();
            if (_panel == null)
                _panel = GetComponent<Image>();
            if (_mask == null)
                _mask = GetComponent<Mask>();

            _raw.texture = texture;
            _raw.color = UiLightChromeUtility.FrostBlurTint;
            bool hasBlur = texture != null;
            _raw.enabled = hasBlur;
            SetVeilVisible(hasBlur);
            if (_mask != null)
                _mask.showMaskGraphic = !hasBlur;

            if (_panel != null)
            {
                // Stencil white when blur draws; soft navy rounded glass when not.
                _panel.color = hasBlur ? Color.white : FallbackGlass;
            }

            if (hasBlur)
                UpdateUv();
        }

        public void UpdateUv()
        {
            if (_raw == null || _rawRect == null || _raw.texture == null)
                return;

            Canvas canvas = _rawRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            _rawRect.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            float x = Mathf.Clamp01(bl.x / sw);
            float y = Mathf.Clamp01(bl.y / sh);
            float w = Mathf.Clamp01((tr.x - bl.x) / sw);
            float h = Mathf.Clamp01((tr.y - bl.y) / sh);
            if (w < 0.001f || h < 0.001f)
                return;
            _raw.uvRect = new Rect(x, y, w, h);
        }
    }
}
