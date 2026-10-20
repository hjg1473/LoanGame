using System;
using LoadGame.Audio;
using LoadGame.Balance;
using LoadGame.Upgrades;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoadGame.Economy
{
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private long startingCash = 1000L;
        [SerializeField] private long startingDebt = 0L;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float bankruptcyCountdownSeconds = 20f;
        [SerializeField] private bool autoRepayDebtWithIncome;
        [SerializeField] private UpgradeManager upgradeManager;

        [Header("Variable Interest Rate")]
        [SerializeField] private double baseInterestRate = 0.03d;
        [SerializeField] private double startingInterestRate = 0.03d;
        [SerializeField] private double minInterestRate = 0.03d;
        [SerializeField] private double maxInterestRate = 0.25d;
        [SerializeField] private double debtRateIncreasePerDebt = 0.00005d;
        [SerializeField] private double recentDebtRateMultiplier = 0.00002d;
        [SerializeField] private double recentDebtDecayPerSecond = 0.55d;
        [SerializeField] private double repayRateDecreasePerDebt = 0.00008d;
        [SerializeField] private double rateReturnSpeedPerSecond = 0.01d;

        private long cash;
        private long debt;
        private double currentInterestRate;
        private double recentDebtPressure;
        private double cashPerSecond;
        private double productionBuffer;
        private double maintenancePerSecond;
        private double maintenanceBuffer;
        private double debtInterestBuffer;
        private float tickTimer;
        private float survivalTimeSeconds;
        private double highestCashPerSecond;
        private DebtRiskState debtRiskState = DebtRiskState.Safe;
        private float bankruptcyTimeRemaining;
        private bool bankruptcyCountdownActive;
        private bool bankruptcyCountdownTicked;
        private bool isBankrupt;
        private bool isGameCompleted;

        public static EconomyManager Instance { get; private set; }
        public event Action<long> CashChanged;
        public event Action<long> DebtChanged;
        public event Action<double> ProductionChanged;
        public event Action<double> InterestChanged;
        public event Action<double> MaintenanceChanged;
        public event Action<double> NetIncomeChanged;
        public event Action<double> RateChanged;
        public event Action<DebtRiskState> DebtRiskStateChanged;
        public event Action<float, bool> BankruptcyCountdownChanged;
        public event Action Recovered;
        public event Action<EconomyGameOverStats> Bankrupt;
        public event Action<long, long> EconomyChanged;
        public event Action<float> EconomyTickProcessed;
        public event Action<long> DebtAdded;
        public event Action<long> Borrowed;
        public event Action<long> DebtRepaid;
        public event Action<bool> AutoRepayDebtWithIncomeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple EconomyManager instances found. Disabling duplicate.", this);
                enabled = false;
                return;
            }

            Instance = this;
            cash = Math.Max(0L, startingCash);
            debt = Math.Max(0L, startingDebt);
            currentInterestRate = ClampInterestRate(startingInterestRate);
        }

        private void Start()
        {
            if (upgradeManager == null)
            {
                upgradeManager = UpgradeManager.Instance;
            }

            SubscribeToUpgradeManager();
            NotifyChanged();
            ProductionChanged?.Invoke(GetCashPerSecond());
            InterestChanged?.Invoke(GetDebtInterestPerSecond());
            MaintenanceChanged?.Invoke(GetMaintenancePerSecond());
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            RateChanged?.Invoke(currentInterestRate);
            DebtRiskStateChanged?.Invoke(debtRiskState);
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, bankruptcyCountdownActive);
        }

        private void Update()
        {
            if (isBankrupt || isGameCompleted)
            {
                return;
            }

            tickTimer += Time.deltaTime;
            float safeTickInterval = GetSafeTickInterval();
            while (tickTimer >= safeTickInterval)
            {
                tickTimer -= safeTickInterval;
                ProcessEconomyTick(safeTickInterval);
            }
        }

        private void ProcessEconomyTick(float elapsedSeconds)
        {
            survivalTimeSeconds += elapsedSeconds;
            TickOperatingCashFlow(elapsedSeconds);
            TickDebtInterest(elapsedSeconds);
            TickInterestRate(elapsedSeconds);
            UpdateDebtRisk();
            TickBankruptcyCountdown(elapsedSeconds);
            if (!isBankrupt)
            {
                EconomyTickProcessed?.Invoke(elapsedSeconds);
            }
        }

        private float GetSafeTickInterval()
        {
            return tickInterval > 0f ? tickInterval : 0.5f;
        }

        private void TickOperatingCashFlow(float elapsedSeconds)
        {
            double effectiveProduction = GetCashPerSecond();
            productionBuffer += Math.Max(0d, effectiveProduction) * elapsedSeconds;
            maintenanceBuffer += maintenancePerSecond * elapsedSeconds;

            long wholeIncome = (long)productionBuffer;
            long wholeMaintenance = (long)maintenanceBuffer;
            productionBuffer -= wholeIncome;
            maintenanceBuffer -= wholeMaintenance;

            OperatingFlow flow = EconomyRules.ResolveWholeOperatingFlow(cash, wholeIncome, wholeMaintenance);
            long maintenancePaidFromCash = Math.Min(cash, wholeMaintenance);
            cash = flow.CashAfter;
            long remainingMaintenance = flow.DebtAdded;
            wholeIncome = flow.Income;

            if (remainingMaintenance > 0L)
            {
                AddDebtInternal(remainingMaintenance, notifyImmediately: false);
                DebtAdded?.Invoke(remainingMaintenance);
                IncreaseRateForNewDebt(remainingMaintenance);
            }

            if (wholeIncome > 0L)
            {
                NotifyChanged();
            }
            else if (maintenancePaidFromCash > 0L || remainingMaintenance > 0L)
            {
                NotifyChanged();
            }
        }

        private void TickDebtInterest(float elapsedSeconds)
        {
            if (debt <= 0L || currentInterestRate <= 0d)
            {
                return;
            }

            debtInterestBuffer += GetDebtInterestPerSecond() * elapsedSeconds;
            if (debtInterestBuffer < 1d)
            {
                return;
            }

            long wholeDebt = (long)debtInterestBuffer;
            debtInterestBuffer -= wholeDebt;
            AddDebtInternal(wholeDebt);
        }

        private void OnDestroy()
        {
            UnsubscribeFromUpgradeManager();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public long GetCash()
        {
            return cash;
        }

        public long GetDebt()
        {
            return debt;
        }

        public double GetCashPerSecond()
        {
            double multiplier = upgradeManager == null ? 1d : upgradeManager.GetCityEfficiencyMultiplier();
            return cashPerSecond * multiplier;
        }

        public double GetDebtInterestPerSecond()
        {
            return EconomyRules.InterestPerSecond(debt, currentInterestRate);
        }

        public double GetMaintenancePerSecond()
        {
            return maintenancePerSecond;
        }

        public double GetNetIncomePerSecond()
        {
            return EconomyRules.NetIncomePerSecond(GetCashPerSecond(), maintenancePerSecond, debt, currentInterestRate);
        }

        public double GetCurrentInterestRate()
        {
            return currentInterestRate;
        }

        public double GetBaseInterestRate()
        {
            return baseInterestRate;
        }

        public DebtRiskState GetDebtRiskState()
        {
            return debtRiskState;
        }

        public float GetBankruptcyTimeRemaining()
        {
            return bankruptcyTimeRemaining;
        }

        public bool IsBankruptcyCountdownActive()
        {
            return bankruptcyCountdownActive;
        }

        public bool IsBankrupt()
        {
            return isBankrupt;
        }

        public float GetSurvivalTimeSeconds()
        {
            return survivalTimeSeconds;
        }

        public double GetHighestCashPerSecond()
        {
            return highestCashPerSecond;
        }

        public bool IsAutoRepayDebtWithIncomeEnabled()
        {
            return autoRepayDebtWithIncome;
        }

        public void SetAutoRepayDebtWithIncome(bool enabled)
        {
            if (autoRepayDebtWithIncome == enabled)
            {
                return;
            }

            autoRepayDebtWithIncome = enabled;
            AutoRepayDebtWithIncomeChanged?.Invoke(autoRepayDebtWithIncome);
        }

        public void AddCash(long amount)
        {
            if (isBankrupt)
            {
                return;
            }

            if (amount <= 0L)
            {
                Debug.LogWarning($"AddCash ignored non-positive amount: {amount}", this);
                return;
            }

            if (autoRepayDebtWithIncome && debt > 0L)
            {
                long repaidAmount = Math.Min(amount, debt);
                debt -= repaidAmount;
                amount -= repaidAmount;
                DebtRepaid?.Invoke(repaidAmount);
                DecreaseRateForRepayment(repaidAmount);

                if (debt <= 0L)
                {
                    debt = 0L;
                    debtInterestBuffer = 0d;
                }
            }

            if (amount > 0L)
            {
                cash = SafeAdd(cash, amount);
            }

            NotifyChanged();
        }

        public void AddCash(float amount)
        {
            AddCash(ToCashAmount(amount));
        }

        public void AddCash(double amount)
        {
            AddCash(ToCashAmount(amount));
        }

        public void AddDebt(long amount)
        {
            if (isBankrupt)
            {
                return;
            }

            if (amount <= 0L)
            {
                Debug.LogWarning($"AddDebt ignored non-positive amount: {amount}", this);
                return;
            }

            AddDebtInternal(amount, notifyImmediately: false);
            DebtAdded?.Invoke(amount);
            IncreaseRateForNewDebt(amount);
            NotifyChanged();
        }

        public void RegisterProduction(double amountPerSecond)
        {
            if (isBankrupt)
            {
                return;
            }

            if (amountPerSecond <= 0d)
            {
                Debug.LogWarning($"RegisterProduction ignored non-positive amount: {amountPerSecond}", this);
                return;
            }

            cashPerSecond += amountPerSecond;
            highestCashPerSecond = Math.Max(highestCashPerSecond, GetCashPerSecond());
            ProductionChanged?.Invoke(GetCashPerSecond());
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            UpdateDebtRisk();
        }

        public void SetMaintenancePerSecond(double amountPerSecond)
        {
            SetOperatingRates(cashPerSecond, amountPerSecond);
        }

        public void SetBaseProduction(double amountPerSecond)
        {
            SetOperatingRates(amountPerSecond, maintenancePerSecond);
        }

        public void SetOperatingRates(double baseProductionPerSecond, double totalMaintenancePerSecond)
        {
            if (isBankrupt)
            {
                return;
            }

            cashPerSecond = Math.Max(0d, baseProductionPerSecond);
            maintenancePerSecond = Math.Max(0d, totalMaintenancePerSecond);
            double effectiveProduction = GetCashPerSecond();
            highestCashPerSecond = Math.Max(highestCashPerSecond, effectiveProduction);
            ProductionChanged?.Invoke(effectiveProduction);
            MaintenanceChanged?.Invoke(maintenancePerSecond);
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            UpdateDebtRisk();
        }

        public void UnregisterProduction(double amountPerSecond)
        {
            if (isBankrupt)
            {
                return;
            }

            if (amountPerSecond <= 0d)
            {
                Debug.LogWarning($"UnregisterProduction ignored non-positive amount: {amountPerSecond}", this);
                return;
            }

            cashPerSecond = Math.Max(0d, cashPerSecond - amountPerSecond);
            ProductionChanged?.Invoke(GetCashPerSecond());
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            UpdateDebtRisk();
        }

        public void SetUpgradeManager(UpgradeManager manager)
        {
            if (upgradeManager == manager)
            {
                return;
            }

            UnsubscribeFromUpgradeManager();
            upgradeManager = manager;
            SubscribeToUpgradeManager();
            OnUpgradeChanged(null, 0);
        }

        private void SubscribeToUpgradeManager()
        {
            if (upgradeManager != null)
            {
                upgradeManager.UpgradeChanged -= OnUpgradeChanged;
                upgradeManager.UpgradeChanged += OnUpgradeChanged;
            }
        }

        private void UnsubscribeFromUpgradeManager()
        {
            if (upgradeManager != null)
            {
                upgradeManager.UpgradeChanged -= OnUpgradeChanged;
            }
        }

        private void OnUpgradeChanged(UpgradeDefinition definition, int level)
        {
            double effectiveProduction = GetCashPerSecond();
            highestCashPerSecond = Math.Max(highestCashPerSecond, effectiveProduction);
            ProductionChanged?.Invoke(effectiveProduction);
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            UpdateDebtRisk();
        }

        public void Pay(long cost)
        {
            if (isBankrupt)
            {
                return;
            }

            if (cost <= 0L)
            {
                Debug.LogWarning($"Pay ignored non-positive cost: {cost}", this);
                return;
            }

            long paidFromCash = Math.Min(cash, cost);
            cash -= paidFromCash;

            long unpaidCost = cost - paidFromCash;
            if (unpaidCost > 0L)
            {
                AddDebtInternal(unpaidCost, notifyImmediately: false);
                DebtAdded?.Invoke(unpaidCost);
                Borrowed?.Invoke(unpaidCost);
                SoundManager.Instance?.PlaySFX(SoundId.Borrowed);
                IncreaseRateForNewDebt(unpaidCost);
            }

            NotifyChanged();
        }

        public long RepayDebt(long amount)
        {
            if (isBankrupt)
            {
                return 0L;
            }

            if (amount <= 0L)
            {
                Debug.LogWarning($"RepayDebt ignored non-positive amount: {amount}", this);
                return 0L;
            }

            long repaidAmount = Math.Min(amount, Math.Min(cash, debt));
            if (repaidAmount <= 0L)
            {
                return 0L;
            }

            cash -= repaidAmount;
            debt -= repaidAmount;
            DebtRepaid?.Invoke(repaidAmount);
            DecreaseRateForRepayment(repaidAmount);
            if (debt <= 0L)
            {
                debt = 0L;
                debtInterestBuffer = 0d;
            }

            NotifyChanged();
            return repaidAmount;
        }

        public void Repay100()
        {
            RepayDebt(100L);
        }

        public void RepayAll()
        {
            RepayDebt(Math.Min(cash, debt));
        }

        public bool TrySpendCash(long amount)
        {
            if (isBankrupt)
            {
                return false;
            }

            if (amount <= 0L)
            {
                Debug.LogWarning($"TrySpendCash ignored non-positive amount: {amount}", this);
                return false;
            }

            if (cash < amount)
            {
                return false;
            }

            cash -= amount;
            NotifyChanged();
            return true;
        }

        public void RestartGame()
        {
            SaveGameManager.DeleteSave();
            SoundManager.Instance?.ResumeBGM();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void CompleteGame()
        {
            if (isBankrupt || isGameCompleted)
            {
                return;
            }

            isGameCompleted = true;
        }

        public bool IsGameCompleted() => isGameCompleted;

        public EconomyGameOverStats GetGameOverStats()
        {
            return new EconomyGameOverStats(cash, debt, highestCashPerSecond, survivalTimeSeconds);
        }

        public EconomySaveState CaptureSaveState()
        {
            return new EconomySaveState
            {
                cash = cash,
                debt = debt,
                interestRate = currentInterestRate,
                survivalTimeSeconds = survivalTimeSeconds,
                highestCashPerSecond = highestCashPerSecond
            };
        }

        public void RestoreSaveState(EconomySaveState state)
        {
            if (state == null)
            {
                return;
            }

            cash = Math.Max(0L, state.cash);
            debt = Math.Max(0L, state.debt);
            currentInterestRate = ClampInterestRate(state.interestRate);
            survivalTimeSeconds = Math.Max(0f, state.survivalTimeSeconds);
            highestCashPerSecond = Math.Max(0d, state.highestCashPerSecond);
            isBankrupt = false;
            isGameCompleted = false;
            debtRiskState = DebtRiskState.Safe;
            bankruptcyCountdownActive = false;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            CashChanged?.Invoke(cash);
            DebtChanged?.Invoke(debt);
            InterestChanged?.Invoke(GetDebtInterestPerSecond());
            NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
            RateChanged?.Invoke(currentInterestRate);
            EconomyChanged?.Invoke(cash, debt);
            UpdateDebtRisk();
        }

        private void AddDebtInternal(long amount, bool notifyImmediately = true)
        {
            debt = SafeAdd(debt, amount);
            if (notifyImmediately)
            {
                NotifyChanged();
            }
        }

        private void TickInterestRate(float deltaTime)
        {
            bool changed = false;
            if (recentDebtPressure > 0d)
            {
                recentDebtPressure = Math.Max(0d, recentDebtPressure - recentDebtPressure * recentDebtDecayPerSecond * deltaTime);
                changed = true;
            }

            if (Math.Abs(currentInterestRate - baseInterestRate) > 0.000001d)
            {
                currentInterestRate = MoveTowards(currentInterestRate, baseInterestRate, rateReturnSpeedPerSecond * deltaTime);
                changed = true;
            }

            if (changed)
            {
                InterestChanged?.Invoke(GetDebtInterestPerSecond());
                NetIncomeChanged?.Invoke(GetNetIncomePerSecond());
                RateChanged?.Invoke(currentInterestRate);
            }
        }

        private void IncreaseRateForNewDebt(long amount)
        {
            recentDebtPressure = SafeAddDouble(recentDebtPressure, amount);
            double increase = amount * debtRateIncreasePerDebt + recentDebtPressure * recentDebtRateMultiplier;
            SetCurrentInterestRate(currentInterestRate + increase);
        }

        private void DecreaseRateForRepayment(long amount)
        {
            double decrease = amount * repayRateDecreasePerDebt;
            SetCurrentInterestRate(currentInterestRate - decrease);
        }

        private void SetCurrentInterestRate(double rate)
        {
            currentInterestRate = ClampInterestRate(rate);
        }

        private double ClampInterestRate(double rate)
        {
            double lower = Math.Min(minInterestRate, maxInterestRate);
            double upper = Math.Max(minInterestRate, maxInterestRate);
            return Math.Min(upper, Math.Max(lower, rate));
        }

        private static double MoveTowards(double current, double target, double maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }

            return current + Math.Sign(target - current) * maxDelta;
        }

        private void TickBankruptcyCountdown(float deltaTime)
        {
            if (!bankruptcyCountdownActive || debtRiskState == DebtRiskState.Bankrupt)
            {
                return;
            }

            bankruptcyCountdownTicked = true;
            bankruptcyTimeRemaining = Math.Max(0f, bankruptcyTimeRemaining - deltaTime);
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, true);

            if (bankruptcyTimeRemaining > 0f)
            {
                return;
            }

            debtRiskState = DebtRiskState.Bankrupt;
            isBankrupt = true;
            bankruptcyCountdownActive = false;
            DebtRiskStateChanged?.Invoke(debtRiskState);
            BankruptcyCountdownChanged?.Invoke(0f, false);
            SoundManager.Instance?.StopBGM();
            SoundManager.Instance?.PlaySFX(SoundId.Bankrupt);
            Bankrupt?.Invoke(new EconomyGameOverStats(cash, debt, highestCashPerSecond, survivalTimeSeconds));
            Debug.Log("BANKRUPT: game-over state reached.", this);
        }

        private void UpdateDebtRisk()
        {
            if (debtRiskState == DebtRiskState.Bankrupt)
            {
                return;
            }

            DebtRiskState nextState = CalculateDebtRiskState();
            if (nextState != debtRiskState)
            {
                debtRiskState = nextState;
                DebtRiskStateChanged?.Invoke(debtRiskState);
                if (debtRiskState == DebtRiskState.Insolvent)
                {
                    SoundManager.Instance?.PlaySFX(SoundId.DebtWarning);
                }
            }

            if (debtRiskState == DebtRiskState.Insolvent)
            {
                StartBankruptcyCountdown();
                return;
            }

            CancelBankruptcyCountdownIfNeeded();
        }

        private DebtRiskState CalculateDebtRiskState()
        {
            if (debt <= 0L)
            {
                return DebtRiskState.Safe;
            }

            double debtServiceCapacity = Math.Max(0d, GetCashPerSecond() - maintenancePerSecond);
            if (debtServiceCapacity <= 0d)
            {
                return DebtRiskState.Insolvent;
            }

            double debtPaybackSeconds = debt / debtServiceCapacity;
            if (debtPaybackSeconds <= 30d)
            {
                return DebtRiskState.Safe;
            }

            if (debtPaybackSeconds < 60d)
            {
                return DebtRiskState.Risky;
            }

            if (debtPaybackSeconds < 120d)
            {
                return DebtRiskState.Danger;
            }

            return DebtRiskState.Insolvent;
        }

        private void StartBankruptcyCountdown()
        {
            if (bankruptcyCountdownActive)
            {
                return;
            }

            bankruptcyTimeRemaining = bankruptcyCountdownSeconds;
            bankruptcyCountdownActive = true;
            bankruptcyCountdownTicked = false;
            BankruptcyCountdownChanged?.Invoke(bankruptcyTimeRemaining, true);
        }

        private void CancelBankruptcyCountdownIfNeeded()
        {
            if (!bankruptcyCountdownActive)
            {
                return;
            }

            bankruptcyCountdownActive = false;
            bankruptcyTimeRemaining = 0f;
            BankruptcyCountdownChanged?.Invoke(0f, false);
            if (bankruptcyCountdownTicked)
            {
                SoundManager.Instance?.PlaySFX(SoundId.Recovered);
                Recovered?.Invoke();
            }

            bankruptcyCountdownTicked = false;
        }

        private static long SafeAdd(long current, long amount)
        {
            return long.MaxValue - current < amount ? long.MaxValue : current + amount;
        }

        private static double SafeAddDouble(double current, double amount)
        {
            return double.MaxValue - current < amount ? double.MaxValue : current + amount;
        }

        private static long ToCashAmount(double amount)
        {
            if (amount <= 0d)
            {
                return 0L;
            }

            return (long)Math.Min(long.MaxValue, Math.Round(amount, MidpointRounding.AwayFromZero));
        }
    }
}
