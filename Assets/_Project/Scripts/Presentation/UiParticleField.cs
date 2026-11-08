using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Slow screen-space mote field. Depth layers drift at different speeds so the dark stage
    /// feels like a volume. Does not take clicks.
    /// </summary>
    public sealed class UiParticleField : MonoBehaviour
    {
        const int MoteCount = 72;

        struct Mote
        {
            public RectTransform Rect;
            public Image Image;
            public float X;
            public float Y;
            public float Speed;
            public float Wobble;
            public float Phase;
            public float Alpha;
        }

        Mote[] _motes;
        Color _pigment = new Color(0.78f, 0.63f, 0.35f, 1f);
        Color _target;
        Sprite _sprite;
        bool _built;

        public void Configure(Sprite sprite, Color pigment)
        {
            _sprite = sprite;
            _pigment = pigment;
            _target = pigment;
            if (isActiveAndEnabled)
                EnsureBuilt();
        }

        public void SetPigment(Color pigment)
        {
            _target = pigment;
            _target.a = 1f;
        }

        void OnEnable()
        {
            EnsureBuilt();
        }

        void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;
            _target = _pigment;
            RectTransform host = transform as RectTransform;
            if (host == null)
                return;

            _motes = new Mote[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                float depth = (i % 3) / 2f;
                float size = Mathf.Lerp(5f, 22f, depth);
                float alpha = Mathf.Lerp(0.07f, 0.26f, depth);
                var go = new GameObject("Mote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                Image image = go.GetComponent<Image>();
                image.sprite = _sprite;
                image.raycastTarget = false;
                image.color = new Color(_pigment.r, _pigment.g, _pigment.b, alpha);

                float phase = (i * 17.13f) % (Mathf.PI * 2f);
                _motes[i] = new Mote
                {
                    Rect = rect,
                    Image = image,
                    X = (i * 0.137f) % 1f,
                    Y = (i * 0.073f) % 1f,
                    Speed = Mathf.Lerp(0.008f, 0.028f, depth),
                    Wobble = Mathf.Lerp(0.004f, 0.012f, depth),
                    Phase = phase,
                    Alpha = alpha
                };
            }
        }

        void Update()
        {
            if (_motes == null)
                return;

            _pigment = Color.Lerp(_pigment, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 1.4f));
            RectTransform host = transform as RectTransform;
            if (host == null)
                return;
            Rect area = host.rect;
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            for (int i = 0; i < _motes.Length; i++)
            {
                Mote mote = _motes[i];
                float sway = Mathf.Sin(time * 0.35f + mote.Phase);
                mote.X += (mote.Speed + sway * mote.Wobble) * dt;
                mote.Y += (mote.Speed * 0.28f + Mathf.Cos(time * 0.22f + mote.Phase) * mote.Wobble) * dt;
                mote.X = Mathf.Repeat(mote.X, 1f);
                mote.Y = Mathf.Repeat(mote.Y, 1f);
                mote.Rect.anchoredPosition = new Vector2(
                    (mote.X - 0.5f) * area.width,
                    (mote.Y - 0.5f) * area.height);
                Color color = _pigment;
                color.a = mote.Alpha;
                mote.Image.color = color;
                _motes[i] = mote;
            }
        }
    }
}
