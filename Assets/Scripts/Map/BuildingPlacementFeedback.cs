using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace LoadGame.Map
{
    public sealed class BuildingPlacementFeedback : MonoBehaviour
    {
        [Header("Landing")]
        [SerializeField, Min(0f)] private float startYOffset = 0.75f;
        [SerializeField, Min(0f)] private float landingOvershoot = 0.06f;
        [SerializeField, Min(0f)] private float fallDuration = 0.18f;
        [SerializeField, Min(0f)] private float settleDuration = 0.1f;
        [SerializeField] private Ease fallEase = Ease.InQuad;
        [SerializeField] private Ease settleEase = Ease.OutQuad;

        [Header("Dust Pool")]
        [SerializeField] private ParticleSystem dustParticlePrefab;
        [SerializeField] private Transform dustRoot;
        [SerializeField] private Vector3 dustWorldOffset;
        [SerializeField, Min(0)] private int initialPoolSize = 4;
        [SerializeField, Min(1)] private int maxPoolSize = 12;

        private readonly Queue<BuildingPlacementDustParticle> availableParticles =
            new Queue<BuildingPlacementDustParticle>(8);
        private int createdParticleCount;

        private void Awake()
        {
            int preloadCount = Mathf.Min(Mathf.Max(0, initialPoolSize), Mathf.Max(1, maxPoolSize));
            for (int i = 0; i < preloadCount; i++)
            {
                CreateParticle(addToPool: true);
            }
        }

        public void Play(Transform buildingVisual, Vector3 dustWorldPosition)
        {
            if (buildingVisual == null)
            {
                return;
            }

            Vector3 finalPosition = buildingVisual.localPosition;
            buildingVisual.DOKill();
            buildingVisual.localPosition = finalPosition + Vector3.up * startYOffset;

            DOTween.Sequence()
                .Append(buildingVisual
                    .DOLocalMoveY(finalPosition.y - landingOvershoot, fallDuration)
                    .SetEase(fallEase))
                .AppendCallback(() => PlayDust(dustWorldPosition + dustWorldOffset))
                .Append(buildingVisual
                    .DOLocalMoveY(finalPosition.y, settleDuration)
                    .SetEase(settleEase))
                .SetLink(buildingVisual.gameObject);
        }

        private void PlayDust(Vector3 worldPosition)
        {
            if (dustParticlePrefab == null)
            {
                return;
            }

            BuildingPlacementDustParticle particle = availableParticles.Count > 0
                ? availableParticles.Dequeue()
                : CreateParticle(addToPool: false);

            if (particle != null)
            {
                particle.Play(worldPosition);
            }
        }

        private BuildingPlacementDustParticle CreateParticle(bool addToPool)
        {
            if (dustParticlePrefab == null || createdParticleCount >= Mathf.Max(1, maxPoolSize))
            {
                return null;
            }

            Transform parent = dustRoot != null ? dustRoot : transform;
            ParticleSystem particleSystem = Instantiate(dustParticlePrefab, parent);
            BuildingPlacementDustParticle particle = particleSystem.GetComponent<BuildingPlacementDustParticle>();
            if (particle == null)
            {
                particle = particleSystem.gameObject.AddComponent<BuildingPlacementDustParticle>();
            }

            particle.Initialize(particleSystem, ReturnParticle);
            createdParticleCount++;
            if (addToPool)
            {
                availableParticles.Enqueue(particle);
            }

            return particle;
        }

        private void ReturnParticle(BuildingPlacementDustParticle particle)
        {
            if (particle == null)
            {
                return;
            }

            particle.ResetForPool();
            availableParticles.Enqueue(particle);
        }
    }

    public sealed class BuildingPlacementDustParticle : MonoBehaviour
    {
        private ParticleSystem particleSystemComponent;
        private System.Action<BuildingPlacementDustParticle> returnToPool;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;
        private bool isPlaying;

        public void Initialize(
            ParticleSystem source,
            System.Action<BuildingPlacementDustParticle> onFinished)
        {
            particleSystemComponent = source;
            returnToPool = onFinished;
            baseLocalRotation = transform.localRotation;
            baseLocalScale = transform.localScale;

            ParticleSystem.MainModule main = particleSystemComponent.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Callback;
            ResetForPool();
        }

        public void Play(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;
            gameObject.SetActive(true);
            isPlaying = true;
            particleSystemComponent.Clear(withChildren: true);
            particleSystemComponent.Play(withChildren: true);
        }

        private void OnParticleSystemStopped()
        {
            if (!isPlaying)
            {
                return;
            }

            isPlaying = false;
            returnToPool?.Invoke(this);
        }

        public void ResetForPool()
        {
            isPlaying = false;
            if (particleSystemComponent != null)
            {
                particleSystemComponent.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;
            gameObject.SetActive(false);
        }
    }
}
