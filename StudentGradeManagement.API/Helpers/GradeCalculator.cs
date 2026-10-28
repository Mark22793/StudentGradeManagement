namespace StudentGradeManagement.API.Helpers
{
    public static class GradeCalculator
    {
        public static string ComputeRemarks(int grade)
        {
            return grade switch
            {
                >= 95 and <= 100 => "CMS",
                >= 90 and <= 94 => "SP",
                >= 85 and <= 89 => "SAP",
                >= 80 and <= 84 => "GP",
                >= 75 and <= 79 => "EP",
                >= 0 and <= 74 => "BEP/FAILED",
                _ => throw new ArgumentOutOfRangeException(nameof(grade), "Grade must be between 0 and 100.")
            };
        }
    }
}