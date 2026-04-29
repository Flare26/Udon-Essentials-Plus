using UnityEngine;

namespace AudioLink
{
#if UDONSHARP
    using UdonSharp;

    public class AudioLinkParticleEmit : UdonSharpBehaviour
#else
    public class AudioLinkParticleEmit : MonoBehaviour
#endif
    {
        public AudioLink audioLink;
        public ParticleSystem particleSystem;
        public int emitCount;
        public int band;
        [Range(0, 127)]
        public int delay;
        public float sensitivity;
        public float speedToAudioFactor;
        //public Vector3 position;
        //public Vector3 rotation;
        //public Vector3 scale = new Vector3(1f, 1f, 1f);


        private int _dataIndex;
        //private Vector3 _initialPosition;
        //private Vector3 _initialRotation;
        //private Vector3 _initialScale;

        void Start()
        {
            UpdateDataIndex();
            //_initialPosition = transform.localPosition;
            //_initialRotation = transform.localEulerAngles;
            //_initialScale = transform.localScale;

        }

        void Update()
        {
            Color[] audioData = audioLink.audioData;
            if (audioData.Length != 0)      // check for audioLink initialization
            {
                float amplitude = audioData[_dataIndex].grayscale;

                //Debug.Log("Amplitude = " + amplitude);
                if (amplitude >= sensitivity)
                {
                    var velocityModule = particleSystem.velocityOverLifetime;
                    velocityModule.speedModifier = 1 + (speedToAudioFactor * amplitude);
                    particleSystem.Emit(emitCount);
                }
                //transform.localPosition = _initialPosition + (position * amplitude);
                //transform.localEulerAngles = _initialRotation + (rotation * amplitude);

                //transform.localScale = new Vector3(_initialScale.x * Mathf.Lerp(1f, scale.x, amplitude), _initialScale.y * Mathf.Lerp(1f, scale.y, amplitude), _initialScale.z * Mathf.Lerp(1f, scale.z, amplitude));
            }
        }

        public void UpdateDataIndex()
        {
            _dataIndex = (band * 128) + delay;
        }
    }
}
