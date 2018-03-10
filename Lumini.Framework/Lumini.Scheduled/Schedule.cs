using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Lumini.Scheduled.Extensions;

namespace Lumini.Scheduled
{
    [StringChecker(nameof(ValidateRegEx))]
    public sealed class Schedule : ISchedule
    {
        internal Schedule(string name, string cronExpression = null)
        {
            var expression = cronExpression ?? "* * * * *";
            Name = name;
            var expressions = expression.Trim().Split(' ');
            Minutes = TrimValue(expressions[CronComponents.Minutes]);
            Hours = TrimValue(expressions[CronComponents.Hours]);
            DayOfMonth = TrimValue(expressions[CronComponents.DayOfMonth]);
            Month = TrimValue(expressions[CronComponents.Month]);
            DayOfWeek = expressions[CronComponents.DayOfWeek];
            Year = expressions.Length == CronComponents.Year + 1 ? expressions[CronComponents.Year] : string.Empty;
        }

        [Key]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForYears)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Year { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForMonths)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Month { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForHours)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Hours { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForMinutes)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Minutes { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForDaysOfMonth)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string DayOfMonth { get; private set; }

        [RegularExpression(Constants.RegEx.ValidationForDaysOfWeek)]
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string DayOfWeek { get; private set; }

        public bool Equals(ISchedule other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Year, other.Year) &&
                   string.Equals(Month, other.Month) && string.Equals(Hours, other.Hours) &&
                   string.Equals(Minutes, other.Minutes) && string.Equals(DayOfMonth, other.DayOfMonth) &&
                   string.Equals(DayOfWeek, other.DayOfWeek);
        }

        private static string TrimValue(string value)
        {
            var newValue = value.Trim();
            if (newValue.Length > 1 && newValue.IsNumeric()) newValue = newValue.TrimStart('0');
            return newValue;
        }

        private static bool ValidateRegEx(string pattern, string input)
        {
            var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            return !m.Success;
        }

        public override string ToString()
        {
            return $"{Minutes} {Hours} {DayOfMonth} {Month} {DayOfWeek}".Trim();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Schedule schedule && Equals(schedule);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name != null ? Name.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Year != null ? Year.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Month != null ? Month.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Hours != null ? Hours.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Minutes != null ? Minutes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DayOfMonth != null ? DayOfMonth.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DayOfWeek != null ? DayOfWeek.GetHashCode() : 0);
                return hashCode;
            }
        }

        private static class CronComponents
        {
            public const int Minutes = 0;
            public const int Hours = 1;
            public const int DayOfMonth = 2;
            public const int Month = 3;
            public const int DayOfWeek = 4;
            public const int Year = 5;
        }
    }
}