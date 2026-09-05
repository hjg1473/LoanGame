using UnityEngine;

namespace LoadGame.Map
{
    public static class GridMapLoader
    {
        public static GridMapData LoadFromResources(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"Map JSON not found in Resources: {resourcePath}");
                return new GridMapData(new GridMapJson());
            }

            GridMapJson json = JsonUtility.FromJson<GridMapJson>(asset.text);
            return new GridMapData(json ?? new GridMapJson());
        }
    }
}
