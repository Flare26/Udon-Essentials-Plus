using UnityEngine;

namespace AudioLink
{
#if UDONSHARP
    using UdonSharp;

    public class AudioLinkAnimatorPlayback : UdonSharpBehaviour
#else
    public class AudioLinkAnimatorPlayback : MonoBehaviour
#endif
    {
        public AudioLink audioLink;
        public Animator anim;
        public int emitCount;
        public int band;
        [Range(0, 127)]
        public int delay;
        public float sensitivity;
        public float speedToAudioFactor;
        public float speedDampening;
        [Range(0f, 1f)] public float animOffset;  // New animation offset parameter (0 to 1)

        private int _dataIndex;
        private bool animationStarted = false;  // To track if animation has been started

        void Start()
        {
            UpdateDataIndex();
            anim.speed = 0;  // Start with the animation paused

            // Set the animation start position based on the animOffset
            SetAnimationOffset(animOffset);
        }

        void Update()
        {
            Color[] audioData = audioLink.audioData;
            if (audioData.Length != 0)  // Check for audioLink initialization
            {
                // Get the amplitude for the current band
                float amplitude = audioData[_dataIndex].grayscale;

                //Debug.Log("Amplitude = " + amplitude);

                // If amplitude meets the sensitivity threshold, set the animation speed
                if (amplitude >= sensitivity)
                {
                    if (!animationStarted)
                    {
                        // Set the animation offset the first time the animation starts
                        SetAnimationOffset(animOffset);
                        animationStarted = true;
                    }

                    anim.speed = amplitude * speedToAudioFactor;
                }
                else
                {
                    // Smoothly decrease the animation speed based on speed dampening
                    anim.speed = Mathf.Lerp(anim.speed, 0f, Time.deltaTime * speedDampening);
                }
            }
        }

        // Update the data index based on band and delay
        public void UpdateDataIndex()
        {
            _dataIndex = (band * 128) + delay;
        }

        // Set the normalized time (offset) for the animation
        private void SetAnimationOffset(float offset)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            // Play the animation at the specified offset (normalized time)
            anim.Play(stateInfo.fullPathHash, -1, offset);
        }
    }
}
