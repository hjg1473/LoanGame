using UnityEngine;

namespace LoadGame.Map
{
    public sealed class BuildingQualityVisual : MonoBehaviour
    {
        private static readonly int QualityModeId = Shader.PropertyToID("_QualityMode");
        private static readonly int QualityPhaseId = Shader.PropertyToID("_QualityPhase");

        private SpriteRenderer spriteRenderer;
        private MaterialPropertyBlock propertyBlock;
        private float qualityPhase;
        private bool initialized;

        public void Initialize(SpriteRenderer targetRenderer)
        {
            spriteRenderer = targetRenderer;
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (!initialized)
            {
                qualityPhase = Random.value;
                initialized = true;
            }
        }

        public void ApplyQuality(BuildingQuality quality)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(QualityModeId, (float)quality);
            propertyBlock.SetFloat(QualityPhaseId, qualityPhase);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
