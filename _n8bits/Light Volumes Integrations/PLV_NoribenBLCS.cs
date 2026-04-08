// Script by n8bits
// Point light volumes for Noriben Beam Light Control System.

using UnityEngine;
using VRC.SDKBase;
using UdonSharp;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;
using UnityEngine.UI;

namespace VRCLightVolumes
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PLV_NoribenBLCS : UdonSharpBehaviour
    {
        [Header("Color Source")]
        [Tooltip("6x1 RenderTexture that Noriben's beam light control color picker writes to.")]
        public RenderTexture Source6x1;

        [Tooltip("Updating color uses GPU Readback (some small cost). Higher values = less avg cost but less smooth color change + delay. Find a sweet spot.")]
        [Range(1, 90)]
        public int framesBetweenUpdates = 5;

        [Header("Point Light Volumes")]
        [Tooltip("Spotlight volumes to apply colors to. Index matches pixel index in the texture.")]
        public PointLightVolumeInstance[] targetPLV;

        [Header("Beam Width")]
        public Slider beamWidthSlider;

        [Tooltip("PLV full-cone angle (degrees) when the beam is at its thinnest (slider = 0).")]
        public float minAngleDeg = 5f;

        [Tooltip("PLV full-cone angle (degrees) when the beam is at its widest (slider = 1).")]
        public float maxAngleDeg = 30f;

        [Tooltip("Curve exponent for the slider-to-angle mapping. 1 = linear, <1 = opens fast then slows, >1 = slow start then opens fast.")]
        [Range(0.1f, 5f)]
        public float curveExponent = 1f;

        [Tooltip("Spotlight inner-to-outer cone falloff. 0 = hard edge, 1 = smooth gradient from center. Must be set so the PLV falloff recalculates correctly when the angle changes.")]
        [Range(0f, 1f)]
        public float spotFalloff = 0.75f;

        private Color32[] _pixels;

        [UdonSynced]
        private float _syncedSliderValue;

        private bool _initialized;

        private void Start()
        {
            _pixels = new Color32[6];
            _initialized = true;

            // Seed from the slider's current value so PLVs match on load
            if (beamWidthSlider != null)
            {
                _syncedSliderValue = beamWidthSlider.value;
            }
            ApplyBeamWidth();
        }

        private void ApplyColors()
        {
            int n = Mathf.Min(6, targetPLV.Length, _pixels.Length);
            for (int i = 0; i < n; i++)
            {
                if (targetPLV[i] != null)
                {
                    targetPLV[i].SetColor((Color)_pixels[i]);
                }
            }
        }

        public void BeamWidthSliderChanged()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _syncedSliderValue = beamWidthSlider.value;
            ApplyBeamWidth();
            RequestSerialization();
        }

        private void ApplyBeamWidth()
        {
            if (!_initialized) return;

            // Normalize raw slider value (-0.07 … 0.08) into 0-1 using the slider's actual range
            float normalized = beamWidthSlider != null
                ? Mathf.InverseLerp(beamWidthSlider.minValue, beamWidthSlider.maxValue, _syncedSliderValue)
                : 0f;
            float t = Mathf.Pow(normalized, curveExponent);
            float angleDeg = Mathf.Lerp(minAngleDeg, maxAngleDeg, t);
            angleDeg = Mathf.Max(angleDeg, 1f); // safety clamp

            int n = Mathf.Min(6, targetPLV.Length);
            for (int i = 0; i < n; i++)
            {
                if (targetPLV[i] != null)
                {
                    targetPLV[i].SetSpotLight(angleDeg, spotFalloff);
                }
            }
        }

        public override void OnDeserialization()
        {
            ApplyBeamWidth();

            if (beamWidthSlider != null)
            {
                beamWidthSlider.SetValueWithoutNotify(_syncedSliderValue);
            }
        }

        private void Update()
        {
            if (Time.frameCount % framesBetweenUpdates != 0) return;

            VRCAsyncGPUReadback.Request(Source6x1, 0, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            if (!request.TryGetData(_pixels)) return;

            ApplyColors();
        }
    }
}