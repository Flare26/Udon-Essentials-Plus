using UnityEngine;

#if UDONSHARP
using VRC.SDKBase;
using UdonSharp;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;
#endif


namespace VRCLightVolumes
{
#if UDONSHARP
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LightVolumeTVGICustom : UdonSharpBehaviour
#else
    public class LightVolumeTVGI : MonoBehaviour
#endif
    {
        [Tooltip("Render Texture used by your video player. Can be just a static texture if you want it to be. Make sure that Enable Mip Maps and Auto Generate Mip Maps are Enabled in the texture's import settings.")]
        public Texture TargetRenderTexture;
        [Tooltip("Enables smoothing algorithm that tries to smooth out flickering that is usually a problem. Recommended to always be turned on.")]
        public bool AntiFlickering = true;
        [Tooltip("Minimum brightness (value in HSV) applied to the sampled color. Pixels above the clip threshold will be boosted to at least this value, preventing the light from going fully dark during dim scenes.")]
        public float minimumBrightness;
        [Tooltip("HSV value threshold below which the minimum brightness boost is not applied. Pixels darker than this are left untouched, allowing true blacks to remain black instead of being artificially lifted.")]
        public float clipThreshold;
        [Space]
        [Tooltip("List of the Light Volumes that should be affected by the Light Volume TVGI script.")]
        public LightVolumeInstance[] TargetLightVolumes;
        [Tooltip("List of the Point Light Volumes that should be affected by the Light Volume TVGI script. Usually you don't need it at all.")]
        public PointLightVolumeInstance[] TargetPointLightVolumes;

#if UDONSHARP
        private Color32[] _pixels;
#else
        private Unity.Collections.NativeArray<Color32> _pixels;
#endif
        private Color _prevColor;
        private float _timePrev;
        private RenderTexture _downsampledTex;
        private bool _pending;

#if UDONSHARP

        private void Start()
        {
            _timePrev = Time.time;
            _prevColor = Color.black;
            _pixels = new Color32[1];
            _downsampledTex = new RenderTexture(64, 32, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _downsampledTex.useMipMap = true;
            _downsampledTex.autoGenerateMips = true;
            _downsampledTex.Create();
        }

        void Update()
        {
            if (_pending) return;
            VRCGraphics.Blit(TargetRenderTexture, _downsampledTex);
            VRCAsyncGPUReadback.Request(_downsampledTex, _downsampledTex.mipmapCount - 1, (IUdonEventReceiver)this);
            _pending = true;
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            _pending = false;
            if (request.TryGetData(_pixels))
            {
                SetColor();
            }
        }

#else
        void Update() {
            Graphics.Blit(TargetRenderTexture, _downsampledTex);
            UnityEngine.Rendering.AsyncGPUReadback.Request(_downsampledTex, _downsampledTex.mipmapCount - 1, OnAsyncGpuReadbackComplete);
        }

        public void OnAsyncGpuReadbackComplete(UnityEngine.Rendering.AsyncGPUReadbackRequest request) {
            var _pixels = request.GetData<Color32>();
            SetColor();
            _pixels.Dispose();
        }
#endif

        private void SetColor()
        {

            // Custom delta time for the async stuff 
            float dTime = Time.time - _timePrev;
            _timePrev = Time.time;

            Color color = _pixels[0]; // Current color

            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            if (v > clipThreshold)
            {
                v = Mathf.Max(v, minimumBrightness);    // set your floor here
                color = Color.HSVToRGB(h, s, v);
            }

            if (AntiFlickering)
            {
                float diff = ColorDifference(color, _prevColor); // Difference between prev and current color
                float smoothing = dTime / Mathf.Lerp(0.25f, 1e-05f, Mathf.Pow(diff * 1.5f, 0.1f)); // Smoothing speed depends on the color difference
                _prevColor = Color.Lerp(_prevColor, color, smoothing); // Actually smoothing colors
            }
            else
            {
                _prevColor = color;
            }

            // Applying all colors
            for (int i = 0; i < TargetLightVolumes.Length; i++)
            {
                TargetLightVolumes[i].Color = _prevColor;
            }

            for (int i = 0; i < TargetPointLightVolumes.Length; i++)
            {
                TargetPointLightVolumes[i].Color = _prevColor;
                TargetPointLightVolumes[i].IsRangeDirty = true;
            }

        }

        private float ColorDifference(Color colorA, Color colorB)
        {
            float rmean = (colorA.r + colorB.r) * 0.5f;
            float r = colorA.r - colorB.r;
            float g = colorA.g - colorB.g;
            float b = colorA.b - colorB.b;
            return Mathf.Sqrt((2f + rmean) * r * r + 4f * g * g + (3f - rmean) * b * b) / 3;
        }

    }
}