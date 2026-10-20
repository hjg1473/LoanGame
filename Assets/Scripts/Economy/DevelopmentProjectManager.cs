using System;
using System.Collections.Generic;
using LoadGame.Audio;
using LoadGame.Balance;
using LoadGame.Map;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class DevelopmentProjectManager : MonoBehaviour
    {
        public static DevelopmentProjectManager Instance { get; private set; }
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CityValueManager cityValueManager;
        [SerializeField] private PopulationManager populationManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;

        [Header("Data")]
        [SerializeField] private ProjectDatabase projectDatabase;

        private readonly List<DevelopmentProjectRuntime> projects = new List<DevelopmentProjectRuntime>(16);
        private readonly List<ProjectDefinition> sortedDefinitions = new List<ProjectDefinition>(16);
        private readonly Dictionary<string, DevelopmentProjectRuntime> projectsById = new Dictionary<string, DevelopmentProjectRuntime>(16);

        private GridMapData mapData;
        private DevelopmentProjectRuntime activeProject;
        private bool initialized;
        private float positiveNetSeconds;

        public IReadOnlyList<DevelopmentProjectRuntime> ProjectList => projects;
        public DevelopmentProjectRuntime[] Projects => projects.ToArray();
        public DevelopmentProjectRuntime ActiveProject => activeProject;
        public ProjectDefinition ActiveProjectDefinition => activeProject == null ? null : activeProject.Definition;
        public double CurrentCityValue => cityValueManager == null ? 0d : cityValueManager.GetCityValue();

        public event Action ProjectsChanged;
        public event Action<EconomyGameOverStats> Victory;

        private void Awake()
        {
            if (projectDatabase == null)
            {
                DevelopmentProjectManager[] managers = FindObjectsByType<DevelopmentProjectManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < managers.Length; i++)
                {
                    if (managers[i] != this && managers[i].projectDatabase != null)
                    {
                        enabled = false;
                        return;
                    }
                }
            }

            if (Instance == null || Instance.projectDatabase == null && projectDatabase != null)
            {
                if (Instance != null && Instance != this)
                {
                    Instance.enabled = false;
                }
                Instance = this;
            }
            else if (Instance != this)
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                TryInitializeFromScene();
            }
        }

        private void Update()
        {
            if (!initialized || economyManager == null || economyManager.IsBankrupt() || economyManager.IsGameCompleted())
            {
                return;
            }

            positiveNetSeconds = economyManager.GetNetIncomePerSecond() > 0d
                ? positiveNetSeconds + Time.deltaTime
                : 0f;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(GridMapData data, ChunkedGridMapRenderer renderer, EconomyManager economy, CityValueManager cityValue)
        {
            Unsubscribe();

            mapData = data;
            mapRenderer = renderer;
            economyManager = economy;
            cityValueManager = cityValue;
            if (populationManager == null)
            {
                populationManager = FindFirstObjectByType<PopulationManager>();
            }
            initialized = mapData != null && mapRenderer != null && economyManager != null && cityValueManager != null;

            BuildRuntimeProjects();

            if (!initialized)
            {
                return;
            }

            mapData.CellChanged += OnCellChanged;
            cityValueManager.CityValueChanged += OnCityValueChanged;
            RefreshProjectAvailability();
        }

        public bool AcceptProject(string projectId)
        {
            return TryGetProject(projectId, out DevelopmentProjectRuntime project) && AcceptProject(project);
        }

        public bool AcceptProject(DevelopmentProjectRuntime project)
        {
            if (project == null || project.Status != ProjectStatus.Available || activeProject != null)
            {
                return false;
            }

            activeProject = project;
            project.Status = ProjectStatus.Active;
            SoundManager.Instance?.PlaySFX(SoundId.ProjectAccept);
            ProjectsChanged?.Invoke();
            return true;
        }

        public bool CompleteProject(string projectId)
        {
            return TryGetProject(projectId, out DevelopmentProjectRuntime project) && CompleteProject(project);
        }

        public bool CompleteProject(DevelopmentProjectRuntime project)
        {
            if (project == null || project != activeProject || project.Status != ProjectStatus.Active)
            {
                return false;
            }

            if (!CanComplete(project.Definition))
            {
                return false;
            }

            economyManager.Pay(ToCashAmount(project.Definition.developmentCost));
            project.Status = ProjectStatus.Completed;
            activeProject = null;
            ApplyCompletedProjectValueBonus();
            SoundManager.Instance?.PlaySFX(SoundId.ProjectComplete);
            ProjectsChanged?.Invoke();
            RefreshProjectAvailability();
            if (project.Definition.isFinalProject)
            {
                economyManager.CompleteGame();
                Victory?.Invoke(economyManager.GetGameOverStats());
            }
            return true;
        }

        public bool CanUnlock(ProjectDefinition definition)
        {
            return AreRequirementsMet(definition);
        }

        public bool CanComplete(ProjectDefinition definition)
        {
            return AreRequirementsMet(definition);
        }

        public bool AreRequirementsMet(ProjectDefinition definition)
        {
            if (definition == null || cityValueManager == null)
            {
                return false;
            }

            ProjectRequirementState numericState = new ProjectRequirementState(
                cityValueManager.GetCityValue(),
                populationManager == null ? 0d : populationManager.Population,
                populationManager == null ? 0d : populationManager.EmploymentEfficiency,
                populationManager == null ? 0f : populationManager.Happiness,
                positiveNetSeconds);
            if (!ProjectRules.MeetsNumericRequirements(
                numericState,
                definition.requiredCityValue,
                definition.requiredPopulation,
                definition.requiredEmploymentEfficiency,
                definition.requiredHappiness,
                definition.requiredPositiveNetSeconds))
            {
                return false;
            }

            if (definition.prerequisiteProjectIds != null)
            {
                for (int i = 0; i < definition.prerequisiteProjectIds.Count; i++)
                {
                    string prerequisiteId = definition.prerequisiteProjectIds[i];
                    if (!string.IsNullOrWhiteSpace(prerequisiteId)
                        && (!projectsById.TryGetValue(prerequisiteId, out DevelopmentProjectRuntime prerequisite)
                            || prerequisite.Status != ProjectStatus.Completed))
                    {
                        return false;
                    }
                }
            }

            List<ProjectBuildingRequirement> requiredBuildings = definition.requiredBuildings;
            if (requiredBuildings == null)
            {
                return true;
            }

            for (int i = 0; i < requiredBuildings.Count; i++)
            {
                ProjectBuildingRequirement requirement = requiredBuildings[i];
                if (requirement != null && CountBuildings(requirement) < requirement.requiredCount)
                {
                    return false;
                }
            }

            return true;
        }

        public int CountBuildings(BuildingType buildingType)
        {
            return CountBuildings(buildingType.ToBuildingId());
        }

        public int CountBuildings(ProjectBuildingRequirement requirement)
        {
            if (requirement == null)
            {
                return 0;
            }

            return requirement.building != null
                ? CountBuildingInstances(requirement.building)
                : CountBuildings(requirement.buildingType);
        }

        public int CountBuildingInstances(BuildingDefinition definition)
        {
            if (mapData == null || definition == null)
            {
                return 0;
            }

            int count = 0;
            foreach (BuildingInstance instance in mapData.BuildingInstances)
            {
                if (instance != null && instance.Definition == definition)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountBuildings(string buildingId)
        {
            if (mapData == null || string.IsNullOrEmpty(buildingId))
            {
                return 0;
            }

            int count = 0;
            foreach (BuildingInstance instance in mapData.BuildingInstances)
            {
                if (instance != null && instance.Definition != null && instance.Definition.buildingId == buildingId)
                {
                    count++;
                }
            }

            return count;
        }

        public string GetBuildingDisplayName(BuildingType buildingType)
        {
            return GetBuildingDisplayName(buildingType.ToBuildingId());
        }

        public string GetBuildingDisplayName(string buildingId)
        {
            if (mapRenderer != null && mapRenderer.TryGetBuildingDefinition(buildingId, out BuildingDefinition definition) && definition != null)
            {
                return definition.displayName;
            }

            return buildingId;
        }

        public ProjectStatus GetStatus(ProjectDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.projectId))
            {
                return ProjectStatus.Locked;
            }

            return projectsById.TryGetValue(definition.projectId, out DevelopmentProjectRuntime project) ? project.Status : ProjectStatus.Locked;
        }

        public List<ProjectSaveState> CaptureSaveState()
        {
            List<ProjectSaveState> states = new List<ProjectSaveState>(projects.Count);
            for (int i = 0; i < projects.Count; i++)
            {
                DevelopmentProjectRuntime project = projects[i];
                if (project?.Definition == null)
                {
                    continue;
                }

                states.Add(new ProjectSaveState
                {
                    projectId = project.Definition.projectId,
                    status = (int)project.Status
                });
            }

            return states;
        }

        public void RestoreSaveState(List<ProjectSaveState> states)
        {
            activeProject = null;
            if (states != null)
            {
                for (int i = 0; i < states.Count; i++)
                {
                    ProjectSaveState state = states[i];
                    if (state != null && projectsById.TryGetValue(state.projectId, out DevelopmentProjectRuntime project))
                    {
                        project.Status = (ProjectStatus)Mathf.Clamp(state.status, (int)ProjectStatus.Locked, (int)ProjectStatus.Completed);
                        if (project.Status == ProjectStatus.Active && activeProject == null)
                        {
                            activeProject = project;
                        }
                    }
                }
            }

            ApplyCompletedProjectValueBonus();
            RefreshProjectAvailability();
        }

        private bool TryGetProject(string projectId, out DevelopmentProjectRuntime project)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                project = null;
                return false;
            }

            return projectsById.TryGetValue(projectId, out project);
        }

        private void TryInitializeFromScene()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (mapManager == null)
            {
                mapManager = FindFirstObjectByType<MapManager>();
            }

            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<ChunkedGridMapRenderer>();
            }

            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            GridMapData data = mapManager == null ? null : mapManager.MapData;
            Initialize(data, mapRenderer, economyManager, cityValueManager);
        }

        private void BuildRuntimeProjects()
        {
            projects.Clear();
            sortedDefinitions.Clear();
            projectsById.Clear();
            activeProject = null;

            List<ProjectDefinition> definitions = projectDatabase == null ? null : projectDatabase.projects;
            if (definitions == null || definitions.Count < 5)
            {
                definitions = CreateFallbackProjectChain();
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                {
                    sortedDefinitions.Add(definitions[i]);
                }
            }

            sortedDefinitions.Sort((a, b) =>
            {
                int aOrder = a == null ? int.MaxValue : a.sortOrder;
                int bOrder = b == null ? int.MaxValue : b.sortOrder;
                int orderComparison = aOrder.CompareTo(bOrder);
                if (orderComparison != 0)
                {
                    return orderComparison;
                }

                string aName = a == null ? string.Empty : a.displayName;
                string bName = b == null ? string.Empty : b.displayName;
                return string.Compare(aName, bName, StringComparison.Ordinal);
            });

            for (int i = 0; i < sortedDefinitions.Count; i++)
            {
                ProjectDefinition definition = sortedDefinitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.projectId) || projectsById.ContainsKey(definition.projectId))
                {
                    continue;
                }

                ProjectRuntimeState runtimeState = new ProjectRuntimeState(definition.projectId, ProjectStatus.Locked);
                DevelopmentProjectRuntime runtime = new DevelopmentProjectRuntime(definition, runtimeState);
                projects.Add(runtime);
                projectsById.Add(definition.projectId, runtime);
            }
        }

        private static List<ProjectDefinition> CreateFallbackProjectChain()
        {
            string[] ids = { "first_district", "industrial_hub", "civic_center", "financial_core", "metropolitan_landmark" };
            string[] names = { "First District", "Industrial Hub", "Civic Center", "Financial Core", "Metropolitan Landmark" };
            double[] costs = { 3000d, 10000d, 25000d, 50000d, 100000d };
            double[] values = { 1000d, 5000d, 15000d, 35000d, 70000d };
            List<ProjectDefinition> definitions = new List<ProjectDefinition>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                ProjectDefinition definition = ScriptableObject.CreateInstance<ProjectDefinition>();
                definition.hideFlags = HideFlags.DontSave;
                definition.projectId = ids[i];
                definition.displayName = names[i];
                definition.description = i == ids.Length - 1
                    ? "Complete a balanced metropolis and finish the city run."
                    : "Grow the city to unlock the next development milestone.";
                definition.sortOrder = i + 1;
                definition.developmentCost = costs[i];
                definition.requiredCityValue = values[i];
                definition.cityValueReward = values[i] * 0.2d;
                definition.requiredBuildings = new List<ProjectBuildingRequirement>
                {
                    new ProjectBuildingRequirement
                    {
                        buildingType = i < 2 ? BuildingType.Factory : BuildingType.Tower,
                        requiredCount = i + 1
                    }
                };
                if (i > 0)
                {
                    definition.prerequisiteProjectIds.Add(ids[i - 1]);
                }
                if (i == ids.Length - 1)
                {
                    definition.requiredPopulation = 350d;
                    definition.requiredEmploymentEfficiency = 0.85f;
                    definition.requiredHappiness = 80f;
                    definition.requiredPositiveNetSeconds = 60f;
                    definition.isFinalProject = true;
                }
                definitions.Add(definition);
            }
            return definitions;
        }

        private void RefreshProjectAvailability()
        {
            for (int i = 0; i < projects.Count; i++)
            {
                DevelopmentProjectRuntime project = projects[i];
                if (project == null || project.Status != ProjectStatus.Locked)
                {
                    continue;
                }

                if (CanUnlock(project.Definition))
                {
                    project.Status = ProjectStatus.Available;
                }
            }

            ProjectsChanged?.Invoke();
        }

        private void ApplyCompletedProjectValueBonus()
        {
            if (cityValueManager == null)
            {
                return;
            }

            double totalBonus = 0d;
            for (int i = 0; i < projects.Count; i++)
            {
                DevelopmentProjectRuntime project = projects[i];
                if (project != null && project.Status == ProjectStatus.Completed && project.Definition != null)
                {
                    totalBonus += Math.Max(0d, project.Definition.cityValueReward);
                }
            }

            cityValueManager.SetCompletedProjectValueBonus(totalBonus);
        }

        private void OnCellChanged(GridCellData cell)
        {
            RefreshProjectAvailability();
        }

        private void OnCityValueChanged(double cityValue)
        {
            RefreshProjectAvailability();
        }

        private void Unsubscribe()
        {
            if (mapData != null)
            {
                mapData.CellChanged -= OnCellChanged;
            }

            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged -= OnCityValueChanged;
            }

            initialized = false;
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
