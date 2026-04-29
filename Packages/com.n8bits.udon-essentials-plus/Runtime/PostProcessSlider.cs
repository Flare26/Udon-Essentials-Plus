using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class PostProcessSlider : UdonSharpBehaviour
{
    [SerializeField] PostProcessVolume volume;
    [SerializeField] Slider bloomSlider;

    void Start()
    {
        bloomSlider.value = volume.weight;
    }

    public void OnSliderChanged()
    {
        volume.weight = bloomSlider.value;
    }
}