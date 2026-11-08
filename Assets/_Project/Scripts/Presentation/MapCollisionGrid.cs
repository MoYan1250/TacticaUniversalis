using System.Collections.Generic;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Overview-derived collision cells: black/void walls and gray crates block LOS.
    /// Presentation samples the sprite; Domain <see cref="MapLosGrid"/> owns UV raycast.
    /// </summary>
    public sealed class MapCollisionGrid : ICombatLosQuery
    {
        private const float ColorGridMax = 256f;
        private const float MaskGridMax = 1024f;
        private readonly MapLosGrid _los = new MapLosGrid();
        private float _mapWidth = 1f;
        private float _mapHeight = 1f;

        public bool IsReady => _los.IsReady;
        public MapLosGrid LosGrid => _los;

        public void Clear()
        {
            _los.Clear();
        }

        public void Rebuild(Sprite sprite, float mapWidth, float mapHeight, Texture2D mask = null)
        {
            _mapWidth = Mathf.Max(1f, mapWidth);
            _mapHeight = Mathf.Max(1f, mapHeight);
            if (mask != null)
            {
                RebuildFromMask(mask);
                return;
            }
            if (sprite == null || sprite.texture == null)
            {
                Clear();
                return;
            }

            Texture2D readable = CopySpriteReadable(sprite);
            if (readable == null)
            {
                Clear();
                return;
            }

            try
            {
                Rect rect = sprite.textureRect;
                int srcW = Mathf.Max(1, Mathf.RoundToInt(rect.width));
                int srcH = Mathf.Max(1, Mathf.RoundToInt(rect.height));
                float scale = Mathf.Min(1f, ColorGridMax / Mathf.Max(srcW, srcH));
                int width = Mathf.Max(8, Mathf.RoundToInt(srcW * scale));
                int height = Mathf.Max(8, Mathf.RoundToInt(srcH * scale));
                var solid = new bool[width * height];

                float srcX0 = rect.x;
                float srcY0 = rect.y;
                for (int y = 0; y < height; y++)
                {
                    float v = (height <= 1) ? 0.5f : y / (float)(height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = (width <= 1) ? 0.5f : x / (float)(width - 1);
                        float px = srcX0 + u * (srcW - 1);
                        float py = srcY0 + v * (srcH - 1);
                        Color c = readable.GetPixel(
                            Mathf.Clamp(Mathf.RoundToInt(px), 0, readable.width - 1),
                            Mathf.Clamp(Mathf.RoundToInt(py), 0, readable.height - 1));
                        solid[y * width + x] = IsSolidPixel(c);
                    }
                }

                _los.SetCells(solid, width, height);
            }
            finally
            {
                Object.Destroy(readable);
            }
        }

        private void RebuildFromMask(Texture2D mask)
        {
            Texture2D readable = CopyTextureReadable(mask);
            if (readable == null)
            {
                Clear();
                return;
            }

            try
            {
                int srcW = Mathf.Max(1, readable.width);
                int srcH = Mathf.Max(1, readable.height);
                float scale = Mathf.Min(1f, MaskGridMax / Mathf.Max(srcW, srcH));
                int width = Mathf.Max(8, Mathf.RoundToInt(srcW * scale));
                int height = Mathf.Max(8, Mathf.RoundToInt(srcH * scale));
                var solid = new bool[width * height];
                for (int y = 0; y < height; y++)
                {
                    float v = (height <= 1) ? 0.5f : y / (float)(height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = (width <= 1) ? 0.5f : x / (float)(width - 1);
                        int px = Mathf.Clamp(Mathf.RoundToInt(u * (srcW - 1)), 0, srcW - 1);
                        int py = Mathf.Clamp(Mathf.RoundToInt(v * (srcH - 1)), 0, srcH - 1);
                        solid[y * width + x] = readable.GetPixel(px, py).r > 0.5f;
                    }
                }

                _los.SetCells(solid, width, height);
            }
            finally
            {
                Object.Destroy(readable);
            }
        }

        public static bool IsSolidPixel(Color c)
        {
            if (c.a < 0.12f)
                return true;
            float lum = (c.r + c.g + c.b) / 3f;
            if (lum < 0.12f)
                return true;
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float sat = max > 0.001f ? (max - min) / max : 0f;
            return sat < 0.15f && lum > 0.20f;
        }

        public bool HasClearLineUv(float x0, float y0, float x1, float y1) =>
            _los.HasClearLineUv(x0, y0, x1, y1);

        public bool IsSolidLocal(Vector2 mapLocal)
        {
            LocalToUv(mapLocal, out float u, out float v);
            return _los.IsSolidUv(u, v);
        }

        /// <summary>Push a body circle out of solid cells. A fitted doorway keeps one body.</summary>
        public Vector2 ResolveCircle(Vector2 local, float radius)
        {
            if (!IsReady || radius <= 0.01f)
                return local;
            for (int iter = 0; iter < 3; iter++)
            {
                if (!TryPushCircle(local, radius, out Vector2 pushed))
                    break;
                if ((pushed - local).sqrMagnitude < 0.04f)
                    break;
                local = pushed;
            }
            return local;
        }

        private bool TryPushCircle(Vector2 local, float radius, out Vector2 pushed)
        {
            pushed = local;
            if (!_los.IsReady)
                return false;
            LocalToUv(local, out float u, out float v);
            float ru = radius / Mathf.Max(1f, _mapWidth);
            float rv = radius / Mathf.Max(1f, _mapHeight);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((u - ru) * _los.Width), 0, _los.Width - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt((u + ru) * _los.Width), 0, _los.Width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((v - rv) * _los.Height), 0, _los.Height - 1);
            int y1 = Mathf.Clamp(Mathf.FloorToInt((v + rv) * _los.Height), 0, _los.Height - 1);
            Vector2 sum = Vector2.zero;
            int hits = 0;
            for (int y = y0; y <= y1; y++)
            {
                float cv = (y + 0.5f) / _los.Height;
                for (int x = x0; x <= x1; x++)
                {
                    float cu = (x + 0.5f) / _los.Width;
                    if (!_los.IsSolidUv(cu, cv))
                        continue;
                    Vector2 cell = new Vector2((cu - 0.5f) * _mapWidth, (cv - 0.5f) * _mapHeight);
                    Vector2 delta = local - cell;
                    float dist = delta.magnitude;
                    if (dist >= radius)
                        continue;
                    Vector2 dir = dist < 0.05f ? Vector2.right : delta / dist;
                    sum += dir * (radius - dist);
                    hits++;
                }
            }

            if (hits == 0)
                return false;
            pushed = local + sum / hits;
            return true;
        }

        public float Raycast(Vector2 origin, Vector2 direction, float maxDistance)
        {
            if (!IsReady || maxDistance <= 0.01f)
                return Mathf.Max(0f, maxDistance);
            float mag = direction.magnitude;
            if (mag < 0.0001f)
                return maxDistance;
            Vector2 dir = direction / mag;
            Vector2 end = origin + dir * maxDistance;
            LocalToUv(origin, out float u0, out float v0);
            LocalToUv(end, out float u1, out float v1);
            float du = u1 - u0;
            float dv = v1 - v0;
            float distUv = Mathf.Sqrt(du * du + dv * dv);
            if (distUv < 1e-8f)
                return maxDistance;
            float hitUv = _los.RaycastUv(u0, v0, du, dv, distUv);
            float t = Mathf.Clamp01(hitUv / distUv);
            return Mathf.Clamp(t * maxDistance, 0f, maxDistance);
        }

        public bool TryClipSegment(Vector2 from, Vector2 to, out Vector2 clippedTo)
        {
            clippedTo = to;
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 0.01f || !IsReady)
                return false;
            float hit = Raycast(from, delta, dist);
            if (hit >= dist - 0.5f)
                return false;
            clippedTo = from + delta / dist * hit;
            return true;
        }

        private void LocalToUv(Vector2 mapLocal, out float u, out float v)
        {
            u = mapLocal.x / _mapWidth + 0.5f;
            v = mapLocal.y / _mapHeight + 0.5f;
        }

        private static Texture2D CopySpriteReadable(Sprite sprite)
        {
            return sprite == null ? null : CopyTextureReadable(sprite.texture);
        }

        private static Texture2D CopyTextureReadable(Texture src)
        {
            if (src == null)
                return null;
            int w = src.width;
            int h = src.height;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            RenderTexture.active = rt;
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }
    }

    /// <summary>Hammer units per overview pixel, written by the map collision bake.</summary>
    public static class MapCollisionScale
    {
        private static Dictionary<string, float> _table;

        public static float HammerPerPixel(string overviewResource)
        {
            if (_table == null)
                _table = Load();
            if (string.IsNullOrEmpty(overviewResource))
                return 0f;
            return _table.TryGetValue(overviewResource, out float value) ? value : 0f;
        }

        private static Dictionary<string, float> Load()
        {
            var table = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
            TextAsset asset = Resources.Load<TextAsset>("Maps/collision_scale");
            if (asset == null || string.IsNullOrEmpty(asset.text))
                return table;
            string[] lines = asset.text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                int split = line.LastIndexOf(' ');
                if (split <= 0)
                    continue;
                string key = line.Substring(0, split).Trim();
                if (float.TryParse(line.Substring(split + 1), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float hammer))
                    table[key] = hammer;
            }
            return table;
        }
    }
}
