using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace LoadGame.Ads
{
    public sealed class AdManager : MonoBehaviour
    {
        private const string AndroidTestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

        [Header("Banner")]
        [SerializeField] private bool forceTestAds;
        [SerializeField] private string androidBannerAdUnitId = string.Empty;
        [SerializeField] private bool loadBannerOnStart = true;
        [SerializeField, Min(0)] private int maximumLoadRetries = 2;
        [SerializeField, Min(1f)] private float retryDelaySeconds = 15f;

        public static AdManager Instance { get; private set; }

        private BannerView bannerView;
        private bool initializationRequested;
        private bool isInitialized;
        private int loadRetryCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GatherConsent();
        }

        public void GatherConsent()
        {
            ConsentInformation.Update(new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false
            }, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[AdManager] Consent update failed: {updateError.Message}", this);
                    InitializeIfAllowed();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    if (showError != null)
                    {
                        Debug.LogWarning($"[AdManager] Consent form failed: {showError.Message}", this);
                    }

                    InitializeIfAllowed();
                });
            });
        }

        public void ShowPrivacyOptions()
        {
            if (ConsentInformation.PrivacyOptionsRequirementStatus != PrivacyOptionsRequirementStatus.Required)
            {
                Debug.Log("[AdManager] Privacy options are not required for this user.", this);
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[AdManager] Privacy options failed: {error.Message}", this);
                }
            });
        }

        private void InitializeIfAllowed()
        {
            if (!ConsentInformation.CanRequestAds())
            {
                Debug.LogWarning("[AdManager] Ads remain disabled because consent does not permit requests.", this);
                return;
            }

            Initialize();
        }

        public void Initialize()
        {
            if (initializationRequested || isInitialized)
            {
                return;
            }

            initializationRequested = true;
            Debug.Log("[AdManager] Initializing Google Mobile Ads SDK after consent.", this);

            MobileAds.Initialize(initializationStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    if (initializationStatus == null)
                    {
                        initializationRequested = false;
                        Debug.LogError("[AdManager] Google Mobile Ads SDK initialization failed.", this);
                        return;
                    }

                    isInitialized = true;
                    Debug.Log("[AdManager] Google Mobile Ads SDK initialization completed.", this);

                    if (loadBannerOnStart)
                    {
                        LoadBanner();
                    }
                });
            });
        }

        public void LoadBanner()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdManager] Banner load requested before SDK initialization.", this);
                GatherConsent();
                return;
            }

            if (bannerView != null)
            {
                Debug.Log("[AdManager] Banner already exists. Duplicate load ignored.", this);
                return;
            }

            string adUnitId = GetBannerAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError("[AdManager] Production Android Banner Ad Unit ID is empty. Ads are disabled for this release build.", this);
                return;
            }

            int deviceWidth = MobileAds.Utils.GetDeviceSafeWidth();
            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(deviceWidth);

            bannerView = new BannerView(adUnitId, adaptiveSize, AdPosition.Bottom);
            RegisterBannerEvents(bannerView);

            Debug.Log(
                $"[AdManager] Loading {(ShouldUseTestAds ? "test" : "production")} anchored adaptive banner " +
                $"at the bottom of the screen. Safe width: {deviceWidth}dp.",
                this);
            bannerView.LoadAd(new AdRequest());
        }

        public void ShowBanner()
        {
            bannerView?.Show();
        }

        public void HideBanner()
        {
            bannerView?.Hide();
        }

        public void DestroyBanner()
        {
            if (bannerView == null)
            {
                return;
            }

            bannerView.Destroy();
            bannerView = null;
            Debug.Log("[AdManager] Banner destroyed.", this);
        }

        private string GetBannerAdUnitId()
        {
#if UNITY_ANDROID
            return ShouldUseTestAds ? AndroidTestBannerAdUnitId : androidBannerAdUnitId.Trim();
#elif UNITY_EDITOR
            return AndroidTestBannerAdUnitId;
#else
            return string.Empty;
#endif
        }

        private bool ShouldUseTestAds => Application.isEditor || Debug.isDebugBuild || forceTestAds;

        private void RegisterBannerEvents(BannerView view)
        {
            view.OnBannerAdLoaded += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this != null)
                    {
                        loadRetryCount = 0;
                        Debug.Log($"[AdManager] Banner loaded successfully. Response: {view.GetResponseInfo()}", this);
                    }
                });
            };

            view.OnBannerAdLoadFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this != null)
                    {
                        Debug.LogWarning($"[AdManager] Banner failed to load: {error}", this);
                        bannerView?.Destroy();
                        bannerView = null;
                        if (loadRetryCount < maximumLoadRetries)
                        {
                            loadRetryCount++;
                            CancelInvoke(nameof(LoadBanner));
                            Invoke(nameof(LoadBanner), retryDelaySeconds * loadRetryCount);
                        }
                    }
                });
            };
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            DestroyBanner();
            CancelInvoke(nameof(LoadBanner));
            Instance = null;
        }
    }
}
