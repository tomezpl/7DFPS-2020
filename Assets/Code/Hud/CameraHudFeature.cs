using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;


class CameraHudFeature : ScriptableRendererFeature
{
    [Serializable]
    public class CameraHudFeatureSettings
    {
        public bool IsEnabled = true;
        public RenderPassEvent InsertionEvent = RenderPassEvent.AfterRendering;
        public Material HudMaterial;
    }

    public CameraHudFeatureSettings settings = new CameraHudFeatureSettings();
    CameraHudPass cameraHudPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(!settings.IsEnabled)
        {
            return;
        }

        cameraHudPass.CameraColorRt = renderer.cameraColorTarget;

        renderer.EnqueuePass(cameraHudPass);
    }

    public override void Create()
    {
        cameraHudPass = new CameraHudPass("Camera HUD Pass", settings.InsertionEvent, settings.HudMaterial);
    }
}
