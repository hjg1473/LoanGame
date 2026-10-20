using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace LoadGame.Rendering
{
    public sealed class RetroPosterDitherRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public sealed class Settings
        {
            public bool enabled = true;
            [Range(2, 64)] public int colorLevels = 32;
            [Range(0f, 1f)] public float ditherStrength = 0.2f;
            public bool applyInSceneView = true;
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        }

        [SerializeField] private Settings settings = new Settings();

        private RetroPosterDitherPass pass;
        private Material material;

        public override void Create()
        {
            Shader shader = Shader.Find("Hidden/LoadGame/Retro Poster Dither");
            if (shader == null)
            {
                Debug.LogError("Retro Poster Dither shader was not found.", this);
                return;
            }

            if (material == null || material.shader != shader)
            {
                CoreUtils.Destroy(material);
                material = CoreUtils.CreateEngineMaterial(shader);
                material.name = "Retro Poster Dither (Shared)";
            }

            pass ??= new RetroPosterDitherPass();
            pass.renderPassEvent = settings.injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.enabled || material == null || pass == null)
            {
                return;
            }

            CameraData cameraData = renderingData.cameraData;
            if (cameraData.isPreviewCamera
                || cameraData.cameraType == CameraType.Reflection
                || (!settings.applyInSceneView && cameraData.isSceneViewCamera))
            {
                return;
            }

            material.SetFloat("_ColorLevels", Mathf.Clamp(settings.colorLevels, 2, 64));
            material.SetFloat("_DitherStrength", Mathf.Clamp01(settings.ditherStrength));
            pass.renderPassEvent = settings.injectionPoint;
            pass.Setup(material);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private sealed class RetroPosterDitherPass : ScriptableRenderPass
        {
            private const string PassName = "Retro Poster Dither";
            private Material material;

            public void Setup(Material sourceMaterial)
            {
                material = sourceMaterial;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "CameraColor-RetroPosterDither";
                destinationDescriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

                RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, PassName);
                resourceData.cameraColor = destination;
            }
        }
    }
}
