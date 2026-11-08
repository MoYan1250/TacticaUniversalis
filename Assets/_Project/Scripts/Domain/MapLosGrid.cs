using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>Optional overview-collision LOS for general / behavior-tree combat.</summary>
    public interface ICombatLosQuery
    {
        bool IsReady { get; }

        /// <summary>True when the segment between UV points is not blocked by solid cells.</summary>
        bool HasClearLineUv(float x0, float y0, float x1, float y1);
    }

    /// <summary>
    /// Pure UV [0,1]^2 solid grid with stepped raycast (aligned with presentation overview collision).
    /// </summary>
    public sealed class MapLosGrid : ICombatLosQuery
    {
        private bool[] _solid;
        private int _width;
        private int _height;

        public bool IsReady => _solid != null && _width > 0 && _height > 0;
        public int Width => _width;
        public int Height => _height;

        public void Clear()
        {
            _solid = null;
            _width = 0;
            _height = 0;
        }

        public void SetCells(bool[] solid, int width, int height)
        {
            if (solid == null || width <= 0 || height <= 0 || solid.Length < width * height)
            {
                Clear();
                return;
            }

            _solid = solid;
            _width = width;
            _height = height;
        }

        public bool IsSolidUv(float u, float v)
        {
            if (!IsReady)
                return false;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return true;
            int x = Clamp(Floor(u * _width), 0, _width - 1);
            int y = Clamp(Floor(v * _height), 0, _height - 1);
            return _solid[y * _width + x];
        }

        /// <summary>
        /// Raycast in UV space. Returns travel distance until first solid after open space,
        /// or maxDistance when clear. Distances are in UV units (0–1 scale).
        /// </summary>
        public float RaycastUv(float x0, float y0, float dx, float dy, float maxDistance)
        {
            if (!IsReady || maxDistance <= 1e-5f)
                return Math.Max(0f, maxDistance);
            float mag = (float)Math.Sqrt(dx * dx + dy * dy);
            if (mag < 1e-8f)
                return maxDistance;
            float inv = 1f / mag;
            float dirX = dx * inv;
            float dirY = dy * inv;

            float cell = Math.Min(1f / _width, 1f / _height);
            float step = Math.Max(cell * 0.45f, 1e-4f);
            bool seenOpen = !IsSolidUv(x0, y0);
            float hit = maxDistance;
            for (float d = step; d <= maxDistance + 1e-6f; d += step)
            {
                float sample = Math.Min(d, maxDistance);
                float px = x0 + dirX * sample;
                float py = y0 + dirY * sample;
                if (IsSolidUv(px, py))
                {
                    if (seenOpen)
                    {
                        hit = Math.Max(step * 0.5f, sample - step);
                        break;
                    }
                }
                else
                    seenOpen = true;
                if (sample >= maxDistance)
                    break;
            }

            return ClampFloat(hit, 0f, maxDistance);
        }

        public bool HasClearLineUv(float x0, float y0, float x1, float y1)
        {
            if (!IsReady)
                return true;
            float dx = x1 - x0;
            float dy = y1 - y0;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1e-6f)
                return true;
            float hit = RaycastUv(x0, y0, dx, dy, dist);
            return hit >= dist - stepEpsilon(dist);
        }

        private float stepEpsilon(float dist)
        {
            float cell = Math.Min(1f / Math.Max(1, _width), 1f / Math.Max(1, _height));
            return Math.Max(cell * 0.6f, dist * 0.01f);
        }

        private static int Floor(float v) => (int)Math.Floor(v);

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static float ClampFloat(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }

    /// <summary>
    /// Shared body size. Dust II's mid door opening is about 39 hammer units,
    /// and the body is 33 so one person fits with a little room left.
    /// </summary>
    public static class MapBodyClearance
    {
        public const float MidDoorHammer = 39f;
        public const float BodyDiameterHammer = 33f;

        public static float LocalRadius(float mapWidth, float overviewWidth, float hammerPerOverviewPixel)
        {
            if (mapWidth <= 1f || overviewWidth <= 1f || hammerPerOverviewPixel <= 0.001f)
                return 0f;
            float radiusOverviewPx = BodyDiameterHammer * 0.5f / hammerPerOverviewPixel;
            return radiusOverviewPx / overviewWidth * mapWidth;
        }
    }
}
