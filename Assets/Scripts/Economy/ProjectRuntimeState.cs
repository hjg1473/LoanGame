namespace LoadGame.Economy
{
    public enum ProjectStatus
    {
        Locked,
        Available,
        Active,
        Completed
    }

    public sealed class ProjectRuntimeState
    {
        public string ProjectId { get; }
        public ProjectStatus Status { get; set; }

        public ProjectRuntimeState(string projectId, ProjectStatus status)
        {
            ProjectId = projectId;
            Status = status;
        }
    }
}
