using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Press feedback for white CTAs, and short enter/exit for occasional career cards.
    /// Rest scale is captured at press time so a close button parked at 0.9 stays square.
    /// </summary>
    public sealed class UiPressScale : MonoBehaviour
    {
        public const float Duration = 0.14f;
        public const float PressedMul = 0.97f;

        private Vector3 _rest;
        private bool _hasRest;
        private Vector3 _from;
        private Vector3 _to;
        private float _elapsed;
        private Coroutine _run;

        public void SetPressed(bool pressed)
        {
            if (!_hasRest)
            {
                _rest = transform.localScale;
                _hasRest = true;
            }

            _from = transform.localScale;
            _to = pressed
                ? new Vector3(_rest.x * PressedMul, _rest.y * PressedMul, _rest.z)
                : _rest;
            _elapsed = 0f;
            if (_run == null && isActiveAndEnabled)
                _run = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (true)
            {
                _elapsed += Time.unscaledDeltaTime;
                float u = UiChromeMotion.EaseOut(Mathf.Clamp01(_elapsed / Duration));
                transform.localScale = Vector3.LerpUnclamped(_from, _to, u);
                if (_elapsed >= Duration)
                {
                    transform.localScale = _to;
                    _run = null;
                    yield break;
                }

                yield return null;
            }
        }
    }

    /// <summary>
    /// Fades and scales the card only. Waits if a parent CanvasGroup is dimmed for blur capture.
    /// </summary>
    public sealed class UiChromeMotion : MonoBehaviour
    {
        public const float InDuration = 0.9f;
        public const float OutDuration = 0.32f;
        public const float StartScale = 0.9f;
        public const float SlideX = 240f;

        private RectTransform _card;
        private CanvasGroup _cardGroup;
        private Vector2 _restPos;
        private bool _hasRestPos;
        private int _generation;
        private bool _hasOpened;

        public bool HasOpened => _hasOpened;

        public static RectTransform CardOf(Image face)
        {
            if (face == null)
                return null;
            Transform frame = face.transform.parent;
            if (frame == null)
                return face.rectTransform;
            return frame.parent as RectTransform ?? face.rectTransform;
        }

        public static void Bind(GameObject overlay, RectTransform card)
        {
            if (overlay == null || card == null)
                return;
            UiChromeMotion motion = overlay.GetComponent<UiChromeMotion>();
            if (motion == null)
                motion = overlay.AddComponent<UiChromeMotion>();
            motion._card = card;
            motion.EnsureGroup();
        }

        public static void BindFace(GameObject overlay, Image face) =>
            Bind(overlay, CardOf(face));

        public static void Show(GameObject overlay)
        {
            if (overlay == null)
                return;
            bool wasActive = overlay.activeSelf;
            UiChromeMotion motion = overlay.GetComponent<UiChromeMotion>();
            bool resume = motion != null && motion._playingOut;
            overlay.SetActive(true);
            if (motion == null)
                return;
            if (!wasActive || resume)
                motion.PlayIn();
        }

        public static void Hide(GameObject overlay, Action onHidden = null)
        {
            if (overlay == null)
            {
                onHidden?.Invoke();
                return;
            }

            UiChromeMotion motion = overlay.GetComponent<UiChromeMotion>();
            if (motion == null || !motion._hasOpened || motion._card == null || !overlay.activeInHierarchy)
            {
                overlay.SetActive(false);
                onHidden?.Invoke();
                return;
            }

            motion.PlayOut(onHidden);
        }

        private bool _playingOut;

        private void EnsureGroup()
        {
            if (_card == null)
                return;
            _cardGroup = _card.GetComponent<CanvasGroup>();
            if (_cardGroup == null)
                _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
        }

        private void PlayIn()
        {
            bool wasOut = _playingOut;
            _playingOut = false;
            _hasOpened = true;
            _generation++;
            int gen = _generation;
            EnsureGroup();
            CaptureRest();
            // Fresh open starts slightly in and to the right, not from nothing. A reversed close keeps its current pose.
            if (!wasOut && _cardGroup != null && _cardGroup.alpha >= 0.99f)
                Apply(StartScale, 0f, false, _restPos + new Vector2(SlideX, 0f));
            StopAllCoroutines();
            StartCoroutine(Play(gen, opening: true, null));
        }

        private void PlayOut(Action onHidden)
        {
            _playingOut = true;
            _generation++;
            int gen = _generation;
            EnsureGroup();
            if (_cardGroup != null)
            {
                _cardGroup.blocksRaycasts = false;
                _cardGroup.interactable = false;
            }

            StopAllCoroutines();
            StartCoroutine(Play(gen, opening: false, onHidden));
        }

        private IEnumerator Play(int gen, bool opening, Action onHidden)
        {
            if (opening)
            {
                yield return null;
                float waited = 0f;
                while (gen == _generation && waited < 1.5f)
                {
                    CanvasGroup overlay = GetComponent<CanvasGroup>();
                    if (overlay == null || overlay.alpha >= 0.9f)
                        break;
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (gen != _generation || _card == null)
                yield break;

            float duration = opening ? InDuration : OutDuration;
            float startScale = _card.localScale.x;
            float startAlpha = _cardGroup != null ? _cardGroup.alpha : (opening ? 0f : 1f);
            float endScale = opening ? 1f : StartScale;
            float endAlpha = opening ? 1f : 0f;
            Vector2 startPos = _card.anchoredPosition;
            Vector2 endPos = _hasRestPos
                ? (opening ? _restPos : _restPos + new Vector2(SlideX, 0f))
                : startPos;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (gen != _generation)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                float u = EaseOut(Mathf.Clamp01(elapsed / duration));
                Apply(
                    Mathf.Lerp(startScale, endScale, u),
                    Mathf.Lerp(startAlpha, endAlpha, u),
                    false,
                    Vector2.Lerp(startPos, endPos, u));
                yield return null;
            }

            if (gen != _generation)
                yield break;

            Apply(endScale, endAlpha, opening, endPos);
            _playingOut = false;
            if (!opening)
            {
                gameObject.SetActive(false);
                onHidden?.Invoke();
            }
        }

        private void CaptureRest()
        {
            if (_hasRestPos || _card == null)
                return;
            _restPos = _card.anchoredPosition;
            _hasRestPos = true;
        }

        private void Apply(float scale, float alpha, bool interactable, Vector2 anchored)
        {
            if (_card != null)
            {
                _card.localScale = new Vector3(scale, scale, 1f);
                _card.anchoredPosition = anchored;
            }
            if (_cardGroup == null)
                return;
            _cardGroup.alpha = alpha;
            _cardGroup.blocksRaycasts = interactable;
            _cardGroup.interactable = interactable;
        }

        /// <summary>cubic-bezier(0.23, 1, 0.32, 1): fast start, soft settle.</summary>
        public static float EaseOut(float x)
        {
            if (x <= 0f)
                return 0f;
            if (x >= 1f)
                return 1f;

            const float x1 = 0.23f;
            const float y1 = 1f;
            const float x2 = 0.32f;
            const float y2 = 1f;
            float t = x;
            for (int i = 0; i < 5; i++)
            {
                float u = 1f - t;
                float bx = (3f * u * u * t * x1) + (3f * u * t * t * x2) + (t * t * t);
                float dx = (3f * u * u * x1) + (6f * u * t * (x2 - x1)) + (3f * t * t * (1f - x2));
                if (Mathf.Abs(dx) < 1e-4f)
                    break;
                t = Mathf.Clamp01(t - ((bx - x) / dx));
            }

            float uu = 1f - t;
            return (3f * uu * uu * t * y1) + (3f * uu * t * t * y2) + (t * t * t);
        }

        /// <summary>Title index rows fade in 40ms apart. Color alpha only; the label color is kept.</summary>
        public static void StaggerIn(Graphic graphic, int index)
        {
            if (graphic == null)
                return;
            UiIndexFade fade = graphic.GetComponent<UiIndexFade>();
            if (fade == null)
                fade = graphic.gameObject.AddComponent<UiIndexFade>();
            fade.Play(index < 0 ? 0 : index);
        }
    }

    /// <summary>Short delayed fade for a title-screen index row.</summary>
    public sealed class UiIndexFade : MonoBehaviour
    {
        public const float Step = 0.04f;
        public const float Duration = 0.20f;

        private Coroutine _run;

        public void Play(int index)
        {
            Graphic graphic = GetComponent<Graphic>();
            if (graphic == null)
                return;

            Color rest = graphic.color;
            rest.a = 1f;
            Color hidden = rest;
            hidden.a = 0f;
            graphic.color = hidden;
            if (_run != null)
                StopCoroutine(_run);
            if (!isActiveAndEnabled)
                return;
            _run = StartCoroutine(Run(graphic, rest, index * Step));
        }

        public void Halt()
        {
            if (_run != null)
                StopCoroutine(_run);
            _run = null;
        }

        private IEnumerator Run(Graphic graphic, Color rest, float delay)
        {
            float waited = 0f;
            while (waited < delay)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            float elapsed = 0f;
            Color from = rest;
            from.a = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = UiChromeMotion.EaseOut(Mathf.Clamp01(elapsed / Duration));
                if (graphic != null)
                    graphic.color = Color.Lerp(from, rest, u);
                yield return null;
            }

            if (graphic != null)
                graphic.color = rest;
            _run = null;
        }
    }

    /// <summary>
    /// Slides a layout block in from the right and settles on its original anchor.
    /// </summary>
    public sealed class UiSettleSlide : MonoBehaviour
    {
        public const float Distance = 180f;
        public const float Duration = 0.7f;
        public const float Stagger = 0.07f;
        public const float ShortDuration = 0.45f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _rest;
        private bool _captured;
        private Coroutine _run;

        public void Play(int index, bool shortMove)
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            if (!_captured)
            {
                _rest = _rect.anchoredPosition;
                _captured = true;
            }

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                    _group = gameObject.AddComponent<CanvasGroup>();
            }

            if (_run != null)
                StopCoroutine(_run);
            if (!isActiveAndEnabled)
                return;
            _run = StartCoroutine(Run(Mathf.Max(0, index) * Stagger, shortMove ? ShortDuration : Duration));
        }

        private IEnumerator Run(float delay, float duration)
        {
            _group.alpha = 0f;
            _rect.anchoredPosition = _rest + new Vector2(Distance, 0f);
            float waited = 0f;
            while (waited < delay)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Vector2 from = _rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = UiChromeMotion.EaseOut(Mathf.Clamp01(elapsed / duration));
                _rect.anchoredPosition = Vector2.Lerp(from, _rest, u);
                _group.alpha = u;
                yield return null;
            }

            _rect.anchoredPosition = _rest;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _run = null;
        }
    }

    /// <summary>Slow loop for a decorative gold disc. Does not take clicks.</summary>
    public sealed class UiGoldDrift : MonoBehaviour
    {
        public float Period = 10f;
        public float Amplitude = 18f;

        private RectTransform _rect;
        private Vector2 _rest;
        private float _phase;

        private void OnEnable()
        {
            _rect = transform as RectTransform;
            if (_rect != null)
                _rest = _rect.anchoredPosition;
            _phase = Period * 0.37f;
        }

        private void Update()
        {
            if (_rect == null)
                return;
            float period = Period < 0.1f ? 10f : Period;
            float t = Time.unscaledTime + _phase;
            float x = Mathf.Sin(t * (Mathf.PI * 2f / period)) * Amplitude;
            float y = Mathf.Cos(t * (Mathf.PI * 2f / (period * 1.3f))) * (Amplitude * 0.35f);
            _rect.anchoredPosition = _rest + new Vector2(x, y);
        }
    }

    /// <summary>
    /// Chapter rows drop 40px into place. LateUpdate wins over the layout group for the short move.
    /// </summary>
    public sealed class UiRowStagger : MonoBehaviour
    {
        public const float Distance = 40f;
        public const float Duration = 0.28f;
        public const float Step = 0.04f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _rest;
        private bool _restCaptured;
        private float _delay;
        private float _elapsed;
        private bool _playing;

        public static void Play(Transform target, int index)
        {
            if (target == null)
                return;
            UiRowStagger stagger = target.GetComponent<UiRowStagger>();
            if (stagger == null)
                stagger = target.gameObject.AddComponent<UiRowStagger>();
            stagger.Begin(index);
        }

        private void Begin(int index)
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                    _group = gameObject.AddComponent<CanvasGroup>();
            }

            _delay = Mathf.Max(0, index) * Step;
            _elapsed = 0f;
            _restCaptured = false;
            _playing = true;
            _group.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (!_playing || _rect == null)
                return;

            if (!_restCaptured)
            {
                _rest = _rect.anchoredPosition;
                _restCaptured = true;
            }

            _elapsed += Time.unscaledDeltaTime;
            float local = _elapsed - _delay;
            if (local <= 0f)
            {
                _rect.anchoredPosition = _rest + new Vector2(0f, Distance);
                _group.alpha = 0f;
                return;
            }

            float u = UiChromeMotion.EaseOut(Mathf.Clamp01(local / Duration));
            _rect.anchoredPosition = Vector2.Lerp(_rest + new Vector2(0f, Distance), _rest, u);
            _group.alpha = u;
            if (local >= Duration)
            {
                _rect.anchoredPosition = _rest;
                _group.alpha = 1f;
                _playing = false;
            }
        }
    }

    /// <summary>Pigment copy of a headline. Fades out after a handoff.</summary>
    public sealed class UiPigmentGhost : MonoBehaviour
    {
        private Graphic _graphic;
        private Color _rest;
        private float _elapsed;
        private bool _playing;

        public void Flash()
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic == null)
                return;
            _rest = _graphic.color;
            _rest.a = 0.8f;
            _elapsed = 0f;
            _playing = true;
            _graphic.color = _rest;
        }

        private void Update()
        {
            if (!_playing || _graphic == null)
                return;
            _elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_elapsed / 0.35f);
            Color c = _rest;
            c.a = Mathf.Lerp(0.8f, 0f, UiChromeMotion.EaseOut(u));
            _graphic.color = c;
            if (u >= 1f)
                _playing = false;
        }
    }

    /// <summary>Chapter paper enters from the side and settles flat. About 0.9 seconds.</summary>
    public sealed class UiChapterEnter : MonoBehaviour
    {
        public const float Duration = 0.9f;
        public const float Distance = 48f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _rest;
        private bool _captured;
        private Coroutine _run;

        public void Play(bool fromRight)
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            if (!_captured)
            {
                _rest = _rect.anchoredPosition;
                _captured = true;
            }

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                    _group = gameObject.AddComponent<CanvasGroup>();
            }

            if (_run != null)
                StopCoroutine(_run);
            if (!isActiveAndEnabled)
                return;
            _run = StartCoroutine(Run(fromRight));
        }

        private IEnumerator Run(bool fromRight)
        {
            Vector2 from = _rest + new Vector2(fromRight ? Distance : -Distance, 0f);
            _rect.anchoredPosition = from;
            _rect.localScale = Vector3.one;
            _group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = UiChromeMotion.EaseOut(Mathf.Clamp01(elapsed / Duration));
                _rect.anchoredPosition = Vector2.Lerp(from, _rest, u);
                _group.alpha = u;
                yield return null;
            }

            _rect.anchoredPosition = _rest;
            _rect.localScale = Vector3.one;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _run = null;
        }
    }

    /// <summary>Display word arrives after the paper has started moving.</summary>
    public sealed class UiLateType : MonoBehaviour
    {
        public const float Delay = 0.34f;
        public const float Duration = 0.48f;
        public const float Distance = 96f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _rest;
        private bool _captured;
        private Coroutine _run;

        public void Play()
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            if (!_captured)
            {
                _rest = _rect.anchoredPosition;
                _captured = true;
            }

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                    _group = gameObject.AddComponent<CanvasGroup>();
            }

            if (_run != null)
                StopCoroutine(_run);
            if (!isActiveAndEnabled)
                return;
            _run = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _group.alpha = 0f;
            _rect.anchoredPosition = _rest + new Vector2(Distance, -18f);
            float waited = 0f;
            while (waited < Delay)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Vector2 from = _rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = UiChromeMotion.EaseOut(Mathf.Clamp01(elapsed / Duration));
                _rect.anchoredPosition = Vector2.Lerp(from, _rest, u);
                _group.alpha = u;
                yield return null;
            }

            _rect.anchoredPosition = _rest;
            _group.alpha = 1f;
            _run = null;
        }
    }

    /// <summary>Selected index mark grows out over a quarter second.</summary>
    public sealed class UiAccentGrow : MonoBehaviour
    {
        public const float Duration = 0.25f;

        private RectTransform _rect;
        private Vector2 _restMax;
        private bool _captured;
        private float _elapsed;
        private bool _playing;

        public void Play()
        {
            _rect = transform as RectTransform;
            if (_rect == null)
                return;
            if (!_captured)
            {
                _restMax = _rect.anchorMax;
                _captured = true;
            }

            _rect.anchorMax = new Vector2(_rect.anchorMin.x, _restMax.y);
            _elapsed = 0f;
            _playing = true;
        }

        private void LateUpdate()
        {
            if (!_playing || _rect == null)
                return;
            _elapsed += Time.unscaledDeltaTime;
            float u = UiChromeMotion.EaseOut(Mathf.Clamp01(_elapsed / Duration));
            _rect.anchorMax = new Vector2(Mathf.Lerp(_rect.anchorMin.x, _restMax.x, u), _restMax.y);
            if (_elapsed >= Duration)
            {
                _rect.anchorMax = _restMax;
                _playing = false;
            }
        }
    }

    /// <summary>Flat pigment eases into the color of the arriving chapter.</summary>
    public sealed class UiPigmentTint : MonoBehaviour
    {
        private Graphic _graphic;
        private Color _from;
        private Color _to;
        private float _elapsed;
        private bool _playing;

        public void Set(Color next)
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic == null)
                return;
            _from = _graphic.color;
            _to = next;
            _to.a = 1f;
            _elapsed = 0f;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing || _graphic == null)
                return;
            _elapsed += Time.unscaledDeltaTime;
            float u = UiChromeMotion.EaseOut(Mathf.Clamp01(_elapsed / 0.9f));
            _graphic.color = Color.Lerp(_from, _to, u);
            if (u >= 1f)
                _playing = false;
        }
    }
}
