using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrade Database")]
    public sealed class UpgradeDatabase : ScriptableObject
    {
        public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();
    }
}
