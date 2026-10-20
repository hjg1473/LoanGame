namespace LoadGame.Balance
{
    public readonly struct ProjectRequirementState
    {
        public ProjectRequirementState(double cityValue, double population, double employment, float happiness, float positiveNetSeconds)
        {
            CityValue = cityValue;
            Population = population;
            Employment = employment;
            Happiness = happiness;
            PositiveNetSeconds = positiveNetSeconds;
        }
        public double CityValue { get; }
        public double Population { get; }
        public double Employment { get; }
        public float Happiness { get; }
        public float PositiveNetSeconds { get; }
    }

    public static class ProjectRules
    {
        public static bool MeetsNumericRequirements(
            ProjectRequirementState state,
            double requiredCityValue,
            double requiredPopulation,
            float requiredEmployment,
            float requiredHappiness,
            float requiredPositiveNetSeconds)
        {
            return state.CityValue >= requiredCityValue
                && state.Population >= requiredPopulation
                && state.Employment >= requiredEmployment
                && state.Happiness >= requiredHappiness
                && state.PositiveNetSeconds >= requiredPositiveNetSeconds;
        }
    }
}
