using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Owns a shared backdrop blur texture for frosted glass slices under this transform tree.
    /// Capture dims chrome via CanvasGroup (not SetActive) so UI cannot get stuck hidden.
    /// </summary>
    public sealed class UiFrostedGlassHost : MonoBehaviour
    {
        private readonly List<UiFrostedGlassSlice> _slices = new List<UiFrostedGlassSlice>(16);
        private Texture2D _blurTexture;
        private UiBackdropBlurRunner _runner;
        private bool _capturing;
        private readonly List<CanvasGroup> _dimmedDuringCapture = new List<CanvasGroup>(16);

        public Texture2D BlurTexture => _blurTexture;

        public void Register(UiFrostedGlassSlice slice)
        {
            if (slice == null || _slices.Contains(slice))
                return;
            _slices.Add(slice);
            slice.ApplyTexture(_blurTexture);
        }

        public void Unregister(UiFrostedGlassSlice slice)
        {
            if (slice == null)
                return;
            _slices.Remove(slice);
        }

        /// <param name="hideDuringCapture">Optional roots to fade so blur mostly sees backdrop/atmosphere.</param>
        public void RefreshBlur(List<GameObject> hideDuringCapture = null)
        {
            if (!isActiveAndEnabled || _slices.Count == 0)
                return;

            if (_capturing)
            {
                // A prior capture is still running; restore any dimmed chrome first.
                RestoreDimmed();
                if (_runner != null)
                {
                    // Cancelling invokes prior callback with null; that path also restores.
                }
            }

            if (_runner == null)
            {
                _runner = GetComponent<UiBackdropBlurRunner>();
                if (_runner == null)
                    _runner = gameObject.AddComponent<UiBackdropBlurRunner>();
            }

            _capturing = true;
            DimForCapture(hideDuringCapture);
            SetSlicesBlurVisible(false);
            _runner.CaptureBlurred(OnBlurCaptured, 0.45f, 3);
        }

        private void DimForCapture(List<GameObject> roots)
        {
            RestoreDimmed();
            if (roots == null)
                return;

            for (int i = 0; i < roots.Count; i++)
            {
                GameObject go = roots[i];
                if (go == null)
                    continue;
                CanvasGroup cg = go.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = go.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
                _dimmedDuringCapture.Add(cg);
            }
        }

        private void RestoreDimmed()
        {
            for (int i = 0; i < _dimmedDuringCapture.Count; i++)
            {
                CanvasGroup cg = _dimmedDuringCapture[i];
                if (cg == null)
                    continue;
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            _dimmedDuringCapture.Clear();
        }

        private void OnBlurCaptured(Texture2D blurred)
        {
            try
            {
                if (_blurTexture != null)
                    UiBackdropBlurCapture.Release(_blurTexture);
                _blurTexture = blurred;

                for (int i = 0; i < _slices.Count; i++)
                {
                    if (_slices[i] != null)
                        _slices[i].ApplyTexture(_blurTexture);
                }
            }
            finally
            {
                _capturing = false;
                RestoreDimmed();
                SetSlicesBlurVisible(true);
            }
        }

        private void SetSlicesBlurVisible(bool visible)
        {
            for (int i = 0; i < _slices.Count; i++)
            {
                if (_slices[i] != null)
                    _slices[i].SetBlurVisible(visible);
            }
        }

        private void OnDisable()
        {
            // Never leave chrome invisible if play mode stops mid-capture.
            RestoreDimmed();
            _capturing = false;
        }

        private void OnDestroy()
        {
            RestoreDimmed();
            if (_blurTexture != null)
            {
                UiBackdropBlurCapture.Release(_blurTexture);
                _blurTexture = null;
            }
        }

        public static UiFrostedGlassHost FindFor(Transform t)
        {
            if (t == null)
                return null;
            return t.GetComponentInParent<UiFrostedGlassHost>();
        }
    }
}
