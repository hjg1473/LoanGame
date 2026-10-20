using System.Collections.Generic;
using DG.Tweening;
using LoadGame.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class ProjectPanelUI : MonoBehaviour
    {
        private enum ProjectFilter
        {
            All,
            Available,
            Active,
            Complete
        }

        [Header("References")]
        [SerializeField] private DevelopmentProjectManager projectManager;
        [SerializeField] private PhoneAppManager phoneAppManager;

        [Header("Filter Tabs")]
        [SerializeField] private Button allFilterButton;
        [SerializeField] private Button availableFilterButton;
        [SerializeField] private Button activeFilterButton;
        [SerializeField] private Button completeFilterButton;
        [SerializeField] private Image allFilterImage;
        [SerializeField] private Image availableFilterImage;
        [SerializeField] private Image activeFilterImage;
        [SerializeField] private Image completeFilterImage;
        [SerializeField] private Color selectedFilterColor = new Color(0.2f, 0.55f, 1f, 1f);
        [SerializeField] private Color unselectedFilterColor = new Color(0.14f, 0.15f, 0.18f, 0.92f);

        [Header("Project List")]
        [SerializeField] private ScrollRect projectScrollRect;
        [SerializeField] private RectTransform projectListContent;
        [SerializeField] private ProjectListItemUI listItemPrefab;

        [Header("Bottom Sheet")]
        [SerializeField] private Button dimButton;
        [SerializeField] private CanvasGroup dimCanvasGroup;
        [SerializeField] private RectTransform detailPanel;
        [SerializeField] private Vector2 detailOpenPosition = new Vector2(0f, 0f);
        [SerializeField] private Vector2 detailClosedPosition = new Vector2(0f, -520f);
        [SerializeField] private float sheetTweenDuration = 0.25f;
        [SerializeField] private Ease sheetOpenEase = Ease.OutCubic;
        [SerializeField] private Ease sheetCloseEase = Ease.InCubic;

        [Header("Detail Content")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailTitleText;
        [SerializeField] private TMP_Text detailStatusText;
        [SerializeField] private TMP_Text detailDescriptionText;
        [SerializeField] private RectTransform requirementContainer;
        [SerializeField] private ProjectRequirementRowUI requirementRowPrefab;
        [SerializeField] private TMP_Text developmentCostText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonText;
        [SerializeField] private Button detailCloseButton;
        [SerializeField] private Button closeAppButton;

        [Header("Fallback UI")]
        [SerializeField] private bool createMissingUi = true;
        [SerializeField] private Color metColor = new Color(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color unmetColor = Color.white;

        private readonly List<ProjectListItemUI> listItems = new List<ProjectListItemUI>(8);
        private readonly List<ProjectRequirementRowUI> requirementRows = new List<ProjectRequirementRowUI>(8);
        private RectTransform rectTransform;
        private DevelopmentProjectRuntime selectedProject;
        private ProjectFilter currentFilter = ProjectFilter.All;
        private Tween sheetTween;
        private Tween dimTween;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (createMissingUi)
            {
                CreateMissingUi();
            }

            CloseDetailImmediate();
        }

        private void Start()
        {
            if (projectManager == null)
            {
                projectManager = FindFirstObjectByType<DevelopmentProjectManager>();
            }

            if (phoneAppManager == null)
            {
                phoneAppManager = GetComponentInParent<PhoneAppManager>();
            }

            if (projectManager != null)
            {
                projectManager.ProjectsChanged += Refresh;
            }

            AddListeners();
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();

            if (projectManager != null)
            {
                projectManager.ProjectsChanged -= Refresh;
            }

            sheetTween?.Kill();
            dimTween?.Kill();
        }

        public void SetProjectManager(DevelopmentProjectManager manager)
        {
            if (projectManager != null)
            {
                projectManager.ProjectsChanged -= Refresh;
            }

            projectManager = manager;
            if (projectManager != null)
            {
                projectManager.ProjectsChanged += Refresh;
            }

            Refresh();
        }

        public void ShowAllProjects()
        {
            SetFilter(ProjectFilter.All);
        }

        public void ShowAvailableProjects()
        {
            SetFilter(ProjectFilter.Available);
        }

        public void ShowActiveProjects()
        {
            SetFilter(ProjectFilter.Active);
        }

        public void ShowCompleteProjects()
        {
            SetFilter(ProjectFilter.Complete);
        }

        private void SetFilter(ProjectFilter filter)
        {
            if (currentFilter == filter)
            {
                return;
            }

            currentFilter = filter;
            Refresh();
        }

        private void SelectProject(DevelopmentProjectRuntime project)
        {
            selectedProject = project;
            RenderSelectedProject();
            OpenDetail();
        }

        private void OnActionClicked()
        {
            if (projectManager == null || selectedProject == null)
            {
                return;
            }

            if (selectedProject.Status == ProjectStatus.Available)
            {
                projectManager.AcceptProject(selectedProject);
                RenderSelectedProject();
                return;
            }

            if (selectedProject.Status == ProjectStatus.Active)
            {
                projectManager.CompleteProject(selectedProject);
                RenderSelectedProject();
            }
        }

        private void CloseApp()
        {
            if (phoneAppManager != null)
            {
                phoneAppManager.CloseCurrentApp();
            }
        }

        private void Refresh()
        {
            RenderFilterTabs();
            RenderProjectList();

            if (selectedProject != null)
            {
                RenderSelectedProject();
            }
        }

        private void RenderFilterTabs()
        {
            SetFilterImage(allFilterImage, currentFilter == ProjectFilter.All);
            SetFilterImage(availableFilterImage, currentFilter == ProjectFilter.Available);
            SetFilterImage(activeFilterImage, currentFilter == ProjectFilter.Active);
            SetFilterImage(completeFilterImage, currentFilter == ProjectFilter.Complete);
        }

        private void RenderProjectList()
        {
            if (projectListContent == null)
            {
                return;
            }

            DevelopmentProjectRuntime[] projects = projectManager == null ? null : projectManager.Projects;
            int visibleIndex = 0;
            if (projects != null)
            {
                for (int i = 0; i < projects.Length; i++)
                {
                    DevelopmentProjectRuntime project = projects[i];
                    if (!ShouldShowProject(project))
                    {
                        continue;
                    }

                    ProjectListItemUI item = GetListItem(visibleIndex);
                    item.gameObject.SetActive(true);
                    item.Render(project, projectManager, SelectProject);
                    visibleIndex++;
                }
            }

            for (int i = visibleIndex; i < listItems.Count; i++)
            {
                listItems[i].gameObject.SetActive(false);
            }
        }

        private bool ShouldShowProject(DevelopmentProjectRuntime project)
        {
            if (project == null)
            {
                return false;
            }

            switch (currentFilter)
            {
                case ProjectFilter.Available:
                    return project.Status == ProjectStatus.Available;
                case ProjectFilter.Active:
                    return project.Status == ProjectStatus.Active;
                case ProjectFilter.Complete:
                    return project.Status == ProjectStatus.Completed;
                default:
                    return true;
            }
        }

        private void RenderSelectedProject()
        {
            if (selectedProject == null || selectedProject.Definition == null)
            {
                SetDetailText("Select a project", string.Empty, string.Empty);
                SetAction(false, string.Empty);
                HideRequirementRows();
                return;
            }

            ProjectDefinition definition = selectedProject.Definition;
            bool requirementsMet = projectManager != null && projectManager.AreRequirementsMet(definition);

            SetDetailText(definition.displayName, GetStatusLabel(selectedProject.Status), definition.description);
            if (detailIcon != null)
            {
                detailIcon.sprite = definition.icon;
                detailIcon.enabled = definition.icon != null;
            }
            RenderRequirements(definition);
            SetText(developmentCostText, $"${definition.developmentCost:N0}");
            SetText(rewardText, $"City Value +${definition.cityValueReward:N0}");

            if (selectedProject.Status == ProjectStatus.Locked)
            {
                SetAction(false, "LOCKED");
                return;
            }

            if (selectedProject.Status == ProjectStatus.Completed)
            {
                SetAction(false, "COMPLETED");
                return;
            }

            if (selectedProject.Status == ProjectStatus.Active)
            {
                SetAction(requirementsMet, requirementsMet ? "COMPLETE PROJECT" : "IN PROGRESS");
                return;
            }

            bool canAccept = projectManager != null && projectManager.ActiveProject == null;
            SetAction(canAccept, "ACCEPT PROJECT");
        }

        private void RenderRequirements(ProjectDefinition definition)
        {
            int rowIndex = 0;
            List<ProjectBuildingRequirement> requiredBuildings = definition.requiredBuildings;
            if (requiredBuildings != null)
            {
                for (int i = 0; i < requiredBuildings.Count; i++)
                {
                    ProjectBuildingRequirement requirement = requiredBuildings[i];
                    if (requirement == null)
                    {
                        continue;
                    }

                    int currentCount = projectManager == null ? 0 : projectManager.CountBuildings(requirement);
                    bool met = currentCount >= requirement.requiredCount;
                    string displayName = requirement.building != null
                        ? requirement.building.displayName
                        : projectManager == null ? requirement.buildingType.ToString() : projectManager.GetBuildingDisplayName(requirement.buildingType);
                    RenderRequirementRow(rowIndex, displayName, $"{currentCount:N0} / {requirement.requiredCount:N0}", met);
                    rowIndex++;
                }
            }

            double cityValue = projectManager == null ? 0d : projectManager.CurrentCityValue;
            bool cityMet = cityValue >= definition.requiredCityValue;
            RenderRequirementRow(rowIndex, "City Value", $"${cityValue:N0} / ${definition.requiredCityValue:N0}", cityMet);
            rowIndex++;

            for (int i = rowIndex; i < requirementRows.Count; i++)
            {
                requirementRows[i].gameObject.SetActive(false);
            }
        }

        private void RenderRequirementRow(int index, string label, string progress, bool met)
        {
            ProjectRequirementRowUI row = GetRequirementRow(index);
            row.gameObject.SetActive(true);
            row.Render(label, progress, met);
        }

        private void OpenDetail()
        {
            if (detailPanel == null)
            {
                return;
            }

            sheetTween?.Kill();
            dimTween?.Kill();

            detailPanel.gameObject.SetActive(true);
            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.gameObject.SetActive(true);
                dimCanvasGroup.blocksRaycasts = true;
                dimCanvasGroup.interactable = true;
                dimTween = dimCanvasGroup.DOFade(1f, Mathf.Max(0f, sheetTweenDuration * 0.75f)).SetEase(Ease.OutQuad);
            }

            sheetTween = detailPanel
                .DOAnchorPos(detailOpenPosition, Mathf.Max(0f, sheetTweenDuration))
                .SetEase(sheetOpenEase);
        }

        private void CloseDetail()
        {
            if (detailPanel == null)
            {
                return;
            }

            sheetTween?.Kill();
            dimTween?.Kill();

            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.blocksRaycasts = false;
                dimCanvasGroup.interactable = false;
                dimTween = dimCanvasGroup
                    .DOFade(0f, Mathf.Max(0f, sheetTweenDuration * 0.75f))
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => dimCanvasGroup.gameObject.SetActive(false));
            }

            sheetTween = detailPanel
                .DOAnchorPos(detailClosedPosition, Mathf.Max(0f, sheetTweenDuration))
                .SetEase(sheetCloseEase)
                .OnComplete(() => detailPanel.gameObject.SetActive(false));
        }

        private void CloseDetailImmediate()
        {
            if (detailPanel != null)
            {
                detailPanel.anchoredPosition = detailClosedPosition;
                detailPanel.gameObject.SetActive(false);
            }

            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.alpha = 0f;
                dimCanvasGroup.blocksRaycasts = false;
                dimCanvasGroup.interactable = false;
                dimCanvasGroup.gameObject.SetActive(false);
            }
        }

        private ProjectListItemUI GetListItem(int index)
        {
            while (listItems.Count <= index)
            {
                listItems.Add(CreateListItem(listItems.Count));
            }

            return listItems[index];
        }

        private ProjectRequirementRowUI GetRequirementRow(int index)
        {
            while (requirementRows.Count <= index)
            {
                requirementRows.Add(CreateRequirementRow(requirementRows.Count));
            }

            return requirementRows[index];
        }

        private ProjectListItemUI CreateListItem(int index)
        {
            if (listItemPrefab != null)
            {
                ProjectListItemUI instance = Instantiate(listItemPrefab, projectListContent);
                instance.name = $"Project List Item {index:00}";
                return instance;
            }

            GameObject itemObject = new GameObject($"Project List Item {index:00}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(ProjectListItemUI));
            itemObject.transform.SetParent(projectListContent, worldPositionStays: false);
            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.anchoredPosition = new Vector2(0f, -index * 92f);
            itemRect.sizeDelta = new Vector2(0f, 84f);

            ProjectListItemUI item = itemObject.GetComponent<ProjectListItemUI>();
            Image background = itemObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.13f, 0.16f, 0.94f);

            Image icon = CreateImage(itemObject.transform, "Icon", new Vector2(22f, -16f), new Vector2(42f, 42f));
            TMP_Text name = CreateText(itemObject.transform, "Name", string.Empty, new Vector2(42f, -10f), new Vector2(-94f, 22f), 14f, TextAlignmentOptions.Left);
            TMP_Text description = CreateText(itemObject.transform, "Description", string.Empty, new Vector2(42f, -34f), new Vector2(-94f, 20f), 11f, TextAlignmentOptions.Left);
            TMP_Text summary = CreateText(itemObject.transform, "Summary", string.Empty, new Vector2(42f, -56f), new Vector2(-94f, 18f), 10f, TextAlignmentOptions.Left);
            Image badge = CreateImage(itemObject.transform, "Badge", new Vector2(-46f, -18f), new Vector2(74f, 20f));
            TMP_Text status = CreateText(badge.transform, "Status", string.Empty, Vector2.zero, Vector2.zero, 9f, TextAlignmentOptions.Center);
            Stretch(status.rectTransform);
            Image lockIcon = CreateImage(itemObject.transform, "Lock Icon", new Vector2(-18f, -56f), new Vector2(18f, 18f));
            item.Initialize(background, icon, badge, lockIcon, name, description, status, summary, itemObject.GetComponent<Button>(), itemObject.GetComponent<CanvasGroup>());
            return item;
        }

        private ProjectRequirementRowUI CreateRequirementRow(int index)
        {
            if (requirementRowPrefab != null)
            {
                ProjectRequirementRowUI instance = Instantiate(requirementRowPrefab, requirementContainer);
                instance.name = $"Requirement Row {index:00}";
                return instance;
            }

            GameObject rowObject = new GameObject($"Requirement Row {index:00}", typeof(RectTransform), typeof(ProjectRequirementRowUI));
            rowObject.transform.SetParent(requirementContainer, worldPositionStays: false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -index * 24f);
            rowRect.sizeDelta = new Vector2(0f, 22f);

            TMP_Text name = CreateText(rowObject.transform, "Name", string.Empty, new Vector2(-56f, 0f), new Vector2(130f, 20f), 11f, TextAlignmentOptions.Left);
            TMP_Text progress = CreateText(rowObject.transform, "Progress", string.Empty, new Vector2(74f, 0f), new Vector2(100f, 20f), 11f, TextAlignmentOptions.Right);
            Image check = CreateImage(rowObject.transform, "Check", new Vector2(142f, 0f), new Vector2(14f, 14f));

            ProjectRequirementRowUI row = rowObject.GetComponent<ProjectRequirementRowUI>();
            row.Initialize(name, progress, check);
            return row;
        }

        private void HideRequirementRows()
        {
            for (int i = 0; i < requirementRows.Count; i++)
            {
                requirementRows[i].gameObject.SetActive(false);
            }
        }

        private void AddListeners()
        {
            AddListener(allFilterButton, ShowAllProjects);
            AddListener(availableFilterButton, ShowAvailableProjects);
            AddListener(activeFilterButton, ShowActiveProjects);
            AddListener(completeFilterButton, ShowCompleteProjects);
            AddListener(actionButton, OnActionClicked);
            AddListener(detailCloseButton, CloseDetail);
            AddListener(dimButton, CloseDetail);
            AddListener(closeAppButton, CloseApp);
        }

        private void RemoveListeners()
        {
            RemoveListener(allFilterButton, ShowAllProjects);
            RemoveListener(availableFilterButton, ShowAvailableProjects);
            RemoveListener(activeFilterButton, ShowActiveProjects);
            RemoveListener(completeFilterButton, ShowCompleteProjects);
            RemoveListener(actionButton, OnActionClicked);
            RemoveListener(detailCloseButton, CloseDetail);
            RemoveListener(dimButton, CloseDetail);
            RemoveListener(closeAppButton, CloseApp);
        }

        private void SetDetailText(string title, string status, string description)
        {
            SetText(detailTitleText, title);
            SetText(detailStatusText, status);
            SetText(detailDescriptionText, description);
        }

        private void SetAction(bool interactable, string label)
        {
            if (actionButton != null)
            {
                actionButton.interactable = interactable;
                actionButton.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }

            SetText(actionButtonText, label);
        }

        private void SetFilterImage(Image image, bool selected)
        {
            if (image != null)
            {
                image.color = selected ? selectedFilterColor : unselectedFilterColor;
            }
        }

        private void CreateMissingUi()
        {
            if (rectTransform == null)
            {
                return;
            }

            EnsurePanelSize();
            allFilterButton = allFilterButton != null ? allFilterButton : CreateButton("All Button", "ALL", new Vector2(-108f, -18f), new Vector2(66f, 28f), out _);
            availableFilterButton = availableFilterButton != null ? availableFilterButton : CreateButton("Available Button", "AVAILABLE", new Vector2(-36f, -18f), new Vector2(80f, 28f), out _);
            activeFilterButton = activeFilterButton != null ? activeFilterButton : CreateButton("Active Button", "ACTIVE", new Vector2(42f, -18f), new Vector2(72f, 28f), out _);
            completeFilterButton = completeFilterButton != null ? completeFilterButton : CreateButton("Complete Button", "COMPLETE", new Vector2(118f, -18f), new Vector2(80f, 28f), out _);
            allFilterImage = allFilterImage != null ? allFilterImage : allFilterButton.GetComponent<Image>();
            availableFilterImage = availableFilterImage != null ? availableFilterImage : availableFilterButton.GetComponent<Image>();
            activeFilterImage = activeFilterImage != null ? activeFilterImage : activeFilterButton.GetComponent<Image>();
            completeFilterImage = completeFilterImage != null ? completeFilterImage : completeFilterButton.GetComponent<Image>();

            EnsureScrollView();
            EnsureDimPanel();
            EnsureDetailPanel();
        }

        private void EnsurePanelSize()
        {
            if (rectTransform.sizeDelta.x < 300f || rectTransform.sizeDelta.y < 520f)
            {
                rectTransform.sizeDelta = new Vector2(340f, 560f);
            }
        }

        private void EnsureScrollView()
        {
            if (projectScrollRect != null && projectListContent != null)
            {
                return;
            }

            GameObject scrollObject = new GameObject("ProjectScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(transform, worldPositionStays: false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(12f, 80f);
            scrollRectTransform.offsetMax = new Vector2(-12f, -54f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, worldPositionStays: false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, worldPositionStays: false);
            projectListContent = contentObject.GetComponent<RectTransform>();
            projectListContent.anchorMin = new Vector2(0f, 1f);
            projectListContent.anchorMax = new Vector2(1f, 1f);
            projectListContent.pivot = new Vector2(0.5f, 1f);
            projectListContent.anchoredPosition = Vector2.zero;

            projectScrollRect = scrollObject.GetComponent<ScrollRect>();
            projectScrollRect.viewport = viewport;
            projectScrollRect.content = projectListContent;
            projectScrollRect.horizontal = false;
        }

        private void EnsureDimPanel()
        {
            if (dimCanvasGroup != null && dimButton != null)
            {
                return;
            }

            GameObject dimObject = new GameObject("DimPanel", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            dimObject.transform.SetParent(transform, worldPositionStays: false);
            RectTransform dimRect = dimObject.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            dimObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.48f);
            dimButton = dimObject.GetComponent<Button>();
            dimCanvasGroup = dimObject.GetComponent<CanvasGroup>();
        }

        private void EnsureDetailPanel()
        {
            if (detailPanel != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("ProjectDetailPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, worldPositionStays: false);
            detailPanel = panelObject.GetComponent<RectTransform>();
            detailPanel.anchorMin = new Vector2(0f, 0f);
            detailPanel.anchorMax = new Vector2(1f, 0f);
            detailPanel.pivot = new Vector2(0.5f, 0f);
            detailPanel.anchoredPosition = detailClosedPosition;
            detailPanel.sizeDelta = new Vector2(-18f, 330f);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.98f);

            detailTitleText = detailTitleText != null ? detailTitleText : CreateText(detailPanel, "Title", string.Empty, new Vector2(0f, -18f), new Vector2(-24f, 28f), 18f, TextAlignmentOptions.Left);
            detailStatusText = detailStatusText != null ? detailStatusText : CreateText(detailPanel, "Status", string.Empty, new Vector2(0f, -46f), new Vector2(-24f, 20f), 12f, TextAlignmentOptions.Left);
            detailDescriptionText = detailDescriptionText != null ? detailDescriptionText : CreateText(detailPanel, "Description", string.Empty, new Vector2(0f, -72f), new Vector2(-24f, 44f), 12f, TextAlignmentOptions.TopLeft);

            GameObject requirementsObject = new GameObject("RequirementContainer", typeof(RectTransform));
            requirementsObject.transform.SetParent(detailPanel, worldPositionStays: false);
            requirementContainer = requirementsObject.GetComponent<RectTransform>();
            requirementContainer.anchorMin = new Vector2(0f, 1f);
            requirementContainer.anchorMax = new Vector2(1f, 1f);
            requirementContainer.pivot = new Vector2(0.5f, 1f);
            requirementContainer.anchoredPosition = new Vector2(0f, -128f);
            requirementContainer.sizeDelta = new Vector2(-24f, 92f);

            developmentCostText = developmentCostText != null ? developmentCostText : CreateText(detailPanel, "Development Cost", string.Empty, new Vector2(0f, -232f), new Vector2(-24f, 22f), 13f, TextAlignmentOptions.Left);
            rewardText = rewardText != null ? rewardText : CreateText(detailPanel, "Reward", string.Empty, new Vector2(0f, -258f), new Vector2(-24f, 22f), 13f, TextAlignmentOptions.Left);
            actionButton = actionButton != null ? actionButton : CreateButton("Action Button", "ACCEPT PROJECT", new Vector2(0f, 28f), new Vector2(180f, 34f), out actionButtonText, detailPanel);
            detailCloseButton = detailCloseButton != null ? detailCloseButton : CreateButton("Detail Close Button", "CLOSE", new Vector2(112f, 28f), new Vector2(70f, 34f), out _, detailPanel);
        }

        private TMP_Text CreateText(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = anchoredPosition;
            textRect.sizeDelta = sizeDelta;

            TMP_Text label = textObject.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = unmetColor;
            return label;
        }

        private Button CreateButton(string objectName, string text, Vector2 anchoredPosition, Vector2 sizeDelta, out TMP_Text label, Transform parent = null)
        {
            Transform actualParent = parent == null ? transform : parent;
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(actualParent, worldPositionStays: false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = sizeDelta;
            buttonObject.GetComponent<Image>().color = unselectedFilterColor;

            label = CreateText(buttonObject.transform, "Label", text, Vector2.zero, Vector2.zero, 11f, TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            return buttonObject.GetComponent<Button>();
        }

        private Image CreateImage(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 1f);
            imageRect.anchorMax = new Vector2(0f, 1f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = anchoredPosition;
            imageRect.sizeDelta = sizeDelta;

            Image image = imageObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.18f);
            return image;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private static string GetStatusLabel(ProjectStatus state)
        {
            return state == ProjectStatus.Completed ? "COMPLETED" : state.ToString().ToUpperInvariant();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
