using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.Economy
{
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text finalCashText;
        [SerializeField] private TMP_Text finalDebtText;
        [SerializeField] private TMP_Text highestProductionText;
        [SerializeField] private TMP_Text survivalTimeText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Color titleStartColor = Color.white;
        [SerializeField] private Color titleEndColor = Color.red;
        [SerializeField] private float titleColorDuration = 1f;
        [SerializeField] private float valueFadeDuration = 0.22f;
        [SerializeField] private float valueFadeStagger = 0.08f;

        private readonly TMP_Text[] valueTexts = new TMP_Text[4];
        private Sequence showSequence;
        private DevelopmentProjectManager projectManager;

        private void Awake()
        {
            valueTexts[0] = finalCashText;
            valueTexts[1] = finalDebtText;
            valueTexts[2] = highestProductionText;
            valueTexts[3] = survivalTimeText;

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
                restartButton.onClick.AddListener(Restart);
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager != null)
            {
                economyManager.Bankrupt += Show;
            }

            SubscribeToProjectManager();
        }

        private void Start()
        {
            // Script execution order can create the project manager after this panel's
            // OnEnable. Retry once all scene Awake/OnEnable calls have completed.
            SubscribeToProjectManager();
        }

        private void OnDisable()
        {
            showSequence?.Kill();
            showSequence = null;

            if (economyManager != null)
            {
                economyManager.Bankrupt -= Show;
            }

            if (projectManager != null)
            {
                projectManager.Victory -= ShowVictory;
                projectManager = null;
            }
        }

        private void SubscribeToProjectManager()
        {
            DevelopmentProjectManager current = DevelopmentProjectManager.Instance;
            if (current == null || current == projectManager)
            {
                return;
            }

            if (projectManager != null)
            {
                projectManager.Victory -= ShowVictory;
            }

            projectManager = current;
            projectManager.Victory += ShowVictory;
        }

        private void ShowVictory(EconomyGameOverStats stats)
        {
            Show(stats);
            if (titleText != null)
            {
                titleText.text = "CITY COMPLETE";
                titleText.color = new Color(0.3f, 1f, 0.55f);
            }
        }

        private void Show(EconomyGameOverStats stats)
        {
            SetVisible(true);
            showSequence?.Kill();

            if (titleText != null)
            {
                titleText.text = "BANKRUPT";
                titleText.color = titleStartColor;
            }

            if (finalCashText != null)
            {
                finalCashText.text = stats.FinalCash.ToString("N0");
            }

            if (finalDebtText != null)
            {
                finalDebtText.text = stats.FinalDebt.ToString("N0");
            }

            if (highestProductionText != null)
            {
                highestProductionText.text = $"+{stats.HighestCashPerSecond:N0}/sec";
            }

            if (survivalTimeText != null)
            {
                survivalTimeText.text = FormatTime(stats.SurvivalTimeSeconds);
            }

            SetValueTextsAlpha(0f);
            SetRestartInteractable(false);
            PlayShowSequence();
        }

        private void Restart()
        {
            if (economyManager != null)
            {
                economyManager.RestartGame();
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void PlayShowSequence()
        {
            showSequence = DOTween.Sequence();

            if (titleText != null)
            {
                showSequence.Append(titleText.DOColor(titleEndColor, Mathf.Max(0f, titleColorDuration)));
            }

            for (int i = 0; i < valueTexts.Length; i++)
            {
                TMP_Text text = valueTexts[i];
                if (text == null)
                {
                    continue;
                }

                if (showSequence.Duration() > 0f && valueFadeStagger > 0f)
                {
                    showSequence.AppendInterval(valueFadeStagger);
                }

                showSequence.Append(text.DOFade(1f, Mathf.Max(0f, valueFadeDuration)));
            }

            showSequence.OnComplete(() => SetRestartInteractable(true));
        }

        private void SetValueTextsAlpha(float alpha)
        {
            for (int i = 0; i < valueTexts.Length; i++)
            {
                if (valueTexts[i] == null)
                {
                    continue;
                }

                Color color = valueTexts[i].color;
                color.a = alpha;
                valueTexts[i].color = color;
            }
        }

        private void SetRestartInteractable(bool interactable)
        {
            if (restartButton != null)
            {
                restartButton.interactable = interactable;
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
