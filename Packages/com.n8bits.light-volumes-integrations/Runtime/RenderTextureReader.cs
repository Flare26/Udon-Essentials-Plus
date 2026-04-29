using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RenderTextureReader : UdonSharpBehaviour
{
    public RenderTexture renderTexture;
    public int readInterval = 2;

    [HideInInspector] public Color[] colors;

    private Color32[] _pixels;
    private bool _pending;
    private int _frameCounter;

    private void Start()
    {
        if (renderTexture != null)
        {
            int size = renderTexture.width * renderTexture.height;
            colors = new Color[size];
            _pixels = new Color32[size];
        }
    }

    private void Update()
    {
        _frameCounter++;
        if (_frameCounter % readInterval != 0) return;
        if (_pending || renderTexture == null) return;

        VRCAsyncGPUReadback.Request(renderTexture, 0, (IUdonEventReceiver)this);
        _pending = true;
    }

    public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
    {
        _pending = false;
        if (request.hasError) return;

        if (request.TryGetData(_pixels))
        {
            for (int i = 0; i < _pixels.Length; i++)
                colors[i] = _pixels[i];
        }
    }

    public Color GetColor(int index)
    {
        if (colors == null || index < 0 || index >= colors.Length)
            return Color.black;
        return colors[index];
    }
}