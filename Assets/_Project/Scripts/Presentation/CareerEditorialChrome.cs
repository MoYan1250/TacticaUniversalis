using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Career print system: black stage, warm paper, black ink, one flat pigment per role.
    /// Battle HUD does not use this.
    /// </summary>
    public static class CareerEditorialChrome
    {
        public static readonly Color Stage = Hex(0x242220);
        public static readonly Color Paper = Hex(0xE4DDD0);
        public static readonly Color Ink = Hex(0x2C2A26);
        public static readonly Color InkMuted = new Color(0.42f, 0.40f, 0.36f, 1f);
        public static readonly Color StageType = Hex(0xE4DFD6);
        public static readonly Color StageMuted = new Color(0.72f, 0.70f, 0.64f, 1f);
        public static readonly Color Mustard = Hex(0xC6A15A);
        public static readonly Color Cyan = Hex(0x6E9AA0);
        public static readonly Color Red = Hex(0xC46A62);
        public static readonly Color Rule = new Color(0.08f, 0.08f, 0.07f, 0.14f);

        public static Color PigmentForTab(int tab)
        {
            switch (tab)
            {
                case 0:
                case 6:
                    return Cyan;
                case 2:
                    return Mustard;
                default:
                    return Mustard;
            }
        }

        public static Image CreateBlock(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void CreateCornerTicks(Transform parent)
        {
            Color tick = new Color(StageType.r, StageType.g, StageType.b, 0.7f);
            CreateBlock(parent, "TickTL", new Vector2(0.018f, 0.955f), new Vector2(0.055f, 0.962f), tick);
            CreateBlock(parent, "TickTL2", new Vector2(0.018f, 0.92f), new Vector2(0.022f, 0.962f), tick);
            CreateBlock(parent, "TickBL", new Vector2(0.018f, 0.008f), new Vector2(0.055f, 0.014f), tick);
            CreateBlock(parent, "TickBL2", new Vector2(0.018f, 0.008f), new Vector2(0.022f, 0.018f), tick);
            CreateBlock(parent, "TickBR", new Vector2(0.945f, 0.008f), new Vector2(0.982f, 0.014f), tick);
            CreateBlock(parent, "TickBR2", new Vector2(0.978f, 0.008f), new Vector2(0.982f, 0.018f), tick);
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
