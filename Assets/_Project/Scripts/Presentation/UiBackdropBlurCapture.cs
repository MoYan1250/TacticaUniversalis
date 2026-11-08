using System;
using System.Collections;
using UnityEngine;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Captures the screen and applies Kawase blur for fullscreen UI overlays.</summary>
    public static class UiBackdropBlurCapture
    {
        private const string ShaderName = "Hidden/Tactica/UIBlurBlit";
        private const float DefaultDownscale = 0.5f;
        private const int DefaultIterations = 4;

        private static Material _blurMaterial;

        public static IEnumerator CaptureBlurredCoroutine(
            Action<Texture2D> onComplete,
            float downscale = DefaultDownscale,
            int iterations = DefaultIterations)
        {
            yield return new WaitForEndOfFrame();

            Texture2D result = null;
            try
            {
                result = CaptureAndBlur(downscale, iterations);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UiBackdropBlurCapture] Capture failed: " + ex.Message);
            }

            onComplete?.Invoke(result);
        }

        public static Texture2D CaptureAndBlur(float downscale = DefaultDownscale, int iterations = DefaultIterations)
        {
            Material blurMat = GetBlurMaterial();
            if (blurMat == null)
                return null;

            Texture2D screen = ScreenCapture.CaptureScreenshotAsTexture();
            if (screen == null)
                return null;

            int width = Mathf.Max(8, Mathf.RoundToInt(screen.width * Mathf.Clamp(downscale, 0.25f, 1f)));
            int height = Mathf.Max(8, Mathf.RoundToInt(screen.height * Mathf.Clamp(downscale, 0.25f, 1f)));

            RenderTexture rtA = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture rtB = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            rtA.filterMode = FilterMode.Bilinear;
            rtB.filterMode = FilterMode.Bilinear;

            Graphics.Blit(screen, rtA);
            UnityEngine.Object.Destroy(screen);

            RenderTexture source = rtA;
            RenderTexture dest = rtB;
            for (int i = 0; i < iterations; i++)
            {
                blurMat.SetFloat("_Offset", 1f + i * 0.75f);
                Graphics.Blit(source, dest, blurMat);
                RenderTexture temp = source;
                source = dest;
                dest = temp;
            }

            Texture2D blurred = RenderTextureToTexture2D(source);
            RenderTexture.ReleaseTemporary(rtA);
            RenderTexture.ReleaseTemporary(rtB);
            return blurred;
        }

        public static void Release(Texture2D texture)
        {
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }

        private static Material GetBlurMaterial()
        {
            if (_blurMaterial != null)
                return _blurMaterial;

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[UiBackdropBlurCapture] Shader not found: " + ShaderName);
                return null;
            }

            _blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return _blurMaterial;
        }

        private static Texture2D RenderTextureToTexture2D(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "UiBackdropBlur"
            };
            tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            tex.Apply(false, true);
            RenderTexture.active = prev;
            return tex;
        }
    }

    /// <summary>Coroutine host for blur capture on Screen Space Overlay canvases.</summary>
    public sealed class UiBackdropBlurRunner : MonoBehaviour
    {
        private Coroutine _running;
        private Action<Texture2D> _pendingComplete;

        public void CaptureBlurred(Action<Texture2D> onComplete, float downscale = 0.5f, int iterations = 4)
        {
            // Stopping a prior capture must still notify the previous waiter; otherwise
            // overlays that gate on an "_opening" flag can stay stuck forever.
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
                Action<Texture2D> cancelled = _pendingComplete;
                _pendingComplete = null;
                cancelled?.Invoke(null);
            }

            _pendingComplete = onComplete;
            _running = StartCoroutine(RunCapture(onComplete, downscale, iterations));
        }

        private IEnumerator RunCapture(Action<Texture2D> onComplete, float downscale, int iterations)
        {
            yield return UiBackdropBlurCapture.CaptureBlurredCoroutine(
                blurred =>
                {
                    if (_pendingComplete == onComplete)
                        _pendingComplete = null;
                    onComplete?.Invoke(blurred);
                },
                downscale,
                iterations);
            _running = null;
        }
    }
}
