using System;
using UnityEngine;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Procedural rounded-rect and radial-glow sprites for uGUI glass panels.</summary>
    public static class UiRoundedRectUtility
    {
        private static Sprite _roundedRectSprite;
        private static Sprite _softSquareSprite;
        private static Sprite _pillSprite;
        private static Sprite _radialGlowSprite;
        private static Sprite _verticalFadeSprite;

        public static Sprite GetRoundedRectSprite(int size = 64, int radius = 14)
        {
            if (_roundedRectSprite != null)
                return _roundedRectSprite;
            _roundedRectSprite = CreateRoundedRectSprite(size, radius);
            return _roundedRectSprite;
        }

        /// <summary>Smaller corner radius for close X and other near-square chrome (not pill-round).</summary>
        public static Sprite GetSoftSquareSprite(int size = 64, int radius = 6)
        {
            if (_softSquareSprite != null)
                return _softSquareSprite;
            _softSquareSprite = CreateRoundedRectSprite(size, radius);
            return _softSquareSprite;
        }

        public static Sprite GetPillSprite(int width = 128, int height = 48)
        {
            if (_pillSprite != null)
                return _pillSprite;
            _pillSprite = CreateRoundedRectSprite(Mathf.Max(width, height), Mathf.Max(1, height / 2));
            return _pillSprite;
        }

        public static Sprite GetRadialGlowSprite(int size = 256)
        {
            if (_radialGlowSprite != null)
                return _radialGlowSprite;
            _radialGlowSprite = CreateRadialGlowSprite(size);
            return _radialGlowSprite;
        }

        /// <summary>White sprite: opaque at top, transparent at bottom (Unity tex y=0 is bottom).</summary>
        public static Sprite GetVerticalFadeSprite(int height = 256)
        {
            if (_verticalFadeSprite != null)
                return _verticalFadeSprite;
            _verticalFadeSprite = CreateVerticalFadeSprite(height);
            return _verticalFadeSprite;
        }

        public static Sprite CreateRoundedRectSprite(int size, int radius)
        {
            size = Math.Max(16, size);
            radius = Math.Max(1, Math.Min(radius, size / 2));
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiRoundedRect"
            };

            var pixels = new Color32[size * size];
            float r = radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectCoverage(x + 0.5f, y + 0.5f, size, r);
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            float border = radius;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static Sprite CreateRadialGlowSprite(int size)
        {
            size = Math.Max(64, size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiRadialGlow"
            };

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float maxDist = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * alpha;
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateVerticalFadeSprite(int height)
        {
            height = Math.Max(32, height);
            const int width = 8;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiVerticalFade"
            };

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float alpha = t * t;
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float RoundedRectCoverage(float px, float py, int size, float radius)
        {
            float min = radius;
            float max = size - radius;
            float cx = px < min ? min : px > max ? max : px;
            float cy = py < min ? min : py > max ? max : py;
            if (Mathf.Approximately(cx, px) && Mathf.Approximately(cy, py))
                return 1f;
            float dx = px - cx;
            float dy = py - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius + 0.5f - dist);
        }
    }
}
