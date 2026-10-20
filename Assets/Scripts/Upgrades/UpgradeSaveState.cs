using System;

namespace LoadGame.Upgrades
{
    [Serializable]
    public sealed class UpgradeSaveState
    {
        public string upgradeId;
        public int level;
    }
}
