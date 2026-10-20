namespace LoadGame.Economy
{
    public sealed class DevelopmentProjectRuntime
    {
        public ProjectDefinition Definition { get; }
        public ProjectRuntimeState RuntimeState { get; }
        public ProjectStatus Status
        {
            get => RuntimeState.Status;
            set => RuntimeState.Status = value;
        }

        public DevelopmentProjectState State
        {
            get => ToLegacyState(Status);
            set => Status = FromLegacyState(value);
        }

        public DevelopmentProjectRuntime(ProjectDefinition definition, ProjectRuntimeState runtimeState)
        {
            Definition = definition;
            RuntimeState = runtimeState;
        }

        public DevelopmentProjectRuntime(ProjectDefinition definition, DevelopmentProjectState state)
        {
            Definition = definition;
            RuntimeState = new ProjectRuntimeState(definition == null ? string.Empty : definition.projectId, FromLegacyState(state));
        }

        private static DevelopmentProjectState ToLegacyState(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.Locked:
                    return DevelopmentProjectState.Locked;
                case ProjectStatus.Active:
                    return DevelopmentProjectState.Active;
                case ProjectStatus.Completed:
                    return DevelopmentProjectState.Completed;
                default:
                    return DevelopmentProjectState.Available;
            }
        }

        private static ProjectStatus FromLegacyState(DevelopmentProjectState state)
        {
            switch (state)
            {
                case DevelopmentProjectState.Locked:
                    return ProjectStatus.Locked;
                case DevelopmentProjectState.Active:
                    return ProjectStatus.Active;
                case DevelopmentProjectState.Completed:
                    return ProjectStatus.Completed;
                default:
                    return ProjectStatus.Available;
            }
        }
    }
}
