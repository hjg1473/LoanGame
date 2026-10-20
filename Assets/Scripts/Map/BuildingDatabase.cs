using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Map
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Game/Building Database")]
    public sealed class BuildingDatabase : ScriptableObject
    {
        public List<BuildingDefinition> buildings = new List<BuildingDefinition>();
    }
}
