namespace LoadGame.Economy
{
    public enum BuildingType
    {
        SmallOffice,
        Factory,
        Tower
    }

    public static class BuildingTypeExtensions
    {
        public static string ToBuildingId(this BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Factory:
                    return "factory";
                case BuildingType.Tower:
                    return "tower";
                default:
                    return "small_office";
            }
        }
    }
}
