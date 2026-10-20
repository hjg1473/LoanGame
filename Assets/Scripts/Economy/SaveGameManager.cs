using System;
using System.Collections.Generic;
using System.IO;
using LoadGame.Map;
using LoadGame.Upgrades;
using UnityEngine;

namespace LoadGame.Economy
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version = SaveGameManager.CurrentVersion;
        public string savedAtUtc;
        public EconomySaveState economy;
        public double population;
        public List<ProjectSaveState> projects = new List<ProjectSaveState>();
        public List<UpgradeSaveState> upgrades = new List<UpgradeSaveState>();
        public List<MapCellSaveState> ownedCells = new List<MapCellSaveState>();
        public List<MapBuildingSaveState> buildings = new List<MapBuildingSaveState>();
        public List<MapCellSaveState> revealedCells = new List<MapCellSaveState>();
        public StockMarketSaveState stockMarket;
    }

    public sealed class SaveGameManager : MonoBehaviour
    {
        public const int CurrentVersion = 1;
        private const string FileName = "save_v1.json";
        private const float AutosaveInterval = 15f;

        private EconomyManager economy;
        private PopulationManager population;
        private DevelopmentProjectManager projects;
        private UpgradeManager upgrades;
        private StockMarketManager stockMarket;
        private GridMapData mapData;
        private ChunkedGridMapRenderer mapRenderer;
        private float autosaveTimer;
        private bool initialized;

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool HasSave => File.Exists(SavePath);

        public void Initialize(
            GridMapData data,
            ChunkedGridMapRenderer mapRenderer,
            EconomyManager economyManager,
            PopulationManager populationManager,
            DevelopmentProjectManager projectManager)
        {
            mapData = data;
            this.mapRenderer = mapRenderer;
            economy = economyManager;
            population = populationManager;
            projects = projectManager;
            upgrades = UpgradeManager.Instance;
            stockMarket = FindFirstObjectByType<StockMarketManager>();
            initialized = mapData != null && this.mapRenderer != null && economy != null;
            if (initialized)
            {
                TryLoad();
            }
        }

        private void Update()
        {
            if (!initialized || economy.IsBankrupt() || economy.IsGameCompleted())
            {
                return;
            }
            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveInterval)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit() => Save();

        public void Save()
        {
            if (!initialized || economy.IsBankrupt() || economy.IsGameCompleted())
            {
                return;
            }

            GameSaveData data = new GameSaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                economy = economy.CaptureSaveState(),
                population = population == null ? 0d : population.Population,
                projects = projects == null ? new List<ProjectSaveState>() : projects.CaptureSaveState(),
                upgrades = upgrades == null ? new List<UpgradeSaveState>() : upgrades.CaptureSaveState(),
                ownedCells = mapData.CaptureOwnedCellSaveState(),
                buildings = mapData.CaptureBuildingSaveState(),
                stockMarket = stockMarket == null ? null : stockMarket.CaptureSaveState()
            };
            foreach (Vector2Int position in mapData.RevealedCells)
            {
                data.revealedCells.Add(new MapCellSaveState { x = position.x, y = position.y });
            }

            string directory = Path.GetDirectoryName(SavePath);
            Directory.CreateDirectory(directory);
            string temporaryPath = SavePath + ".tmp";
            string backupPath = SavePath + ".bak";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath))
            {
                File.Replace(temporaryPath, SavePath, backupPath);
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }
        }

        public bool TryLoad()
        {
            if (!HasSave)
            {
                return false;
            }
            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
                if (data == null || data.version != CurrentVersion || data.economy == null)
                {
                    throw new InvalidDataException("Unsupported or incomplete save data.");
                }

                mapData.RestoreRuntimeState(data.ownedCells, data.buildings, mapRenderer);
                List<Vector2Int> revealed = new List<Vector2Int>();
                if (data.revealedCells != null)
                {
                    for (int i = 0; i < data.revealedCells.Count; i++)
                    {
                        MapCellSaveState cell = data.revealedCells[i];
                        revealed.Add(new Vector2Int(cell.x, cell.y));
                    }
                }
                mapData.RestoreRuntimeRevealedCells(revealed);
                upgrades?.RestoreSaveState(data.upgrades);
                economy.RestoreSaveState(data.economy);
                stockMarket?.RestoreSaveState(data.stockMarket);
                population?.RestorePopulation(data.population);
                projects?.RestoreSaveState(data.projects);
                Debug.Log($"Save restored: {SavePath}", this);
                return true;
            }
            catch (Exception exception)
            {
                string corruptPath = SavePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                try { File.Move(SavePath, corruptPath); } catch (Exception) { }
                Debug.LogWarning($"Save recovery started after invalid data: {exception.Message}", this);
                return false;
            }
        }

        public static void DeleteSave()
        {
            TryDelete(SavePath);
            TryDelete(SavePath + ".tmp");
            TryDelete(SavePath + ".bak");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
        }
    }
}
