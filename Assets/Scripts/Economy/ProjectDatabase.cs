using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Economy
{
    [CreateAssetMenu(fileName = "ProjectDatabase", menuName = "Game/Project Database")]
    public sealed class ProjectDatabase : ScriptableObject
    {
        public List<ProjectDefinition> projects = new List<ProjectDefinition>();
    }
}
