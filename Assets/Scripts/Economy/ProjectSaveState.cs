using System;

namespace LoadGame.Economy
{
    [Serializable]
    public sealed class ProjectSaveState
    {
        public string projectId;
        public int status;
    }
}
