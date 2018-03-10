using System;
using System.Collections.Generic;
using System.Linq;
using NCrontab;

namespace Lumini.Scheduled.Extensions
{
    public static class JobExtensions
    {
        public static void AddSchedule(this IJob job, params ISchedule[] schedules)
        {
            foreach (var schedule in schedules)
                job.Schedules.Append(schedule);
        }

        private static Occurrence GetDefaultOccurrence(IJob job, DateTime? startDate = null)
        {
            var occurrence = new Occurrence(job, startDate ?? job.LastExecution ?? DateTime.UtcNow);
            return occurrence;
        }

        private static IList<Occurrence> BuildOccurrences(Occurrence defaultOccurence,
            Action<ISchedule, IList<Occurrence>> action)
        {
            if (defaultOccurence == null) throw new ArgumentNullException(nameof(defaultOccurence));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var occurrences = new List<Occurrence>();
            if (defaultOccurence.Job.Schedules == null || defaultOccurence.Job.Schedules.Count == 0)
                occurrences.Add(defaultOccurence);
            else
                foreach (var schedule in defaultOccurence.Job.Schedules)
                    action(schedule, occurrences);
            return occurrences;
        }

        public static Occurrence GetNextOccurrence(this IJob job, DateTime? startDate = null)
        {
            var occurrence = GetDefaultOccurrence(job, startDate);
            return occurrence.GetNextOccurrence();
        }

        public static Occurrence GetNextOccurrence(this Occurrence occurrence)
        {
            return BuildOccurrences(occurrence, (schedule, occurrences) =>
            {
                var nextExecutionDate = GetNextExecutionDate(schedule, occurrence.StartTime);
                occurrences.AddOccurence(occurrence.Job, nextExecutionDate, schedule);
            }).OrderBy(o => o.StartTime).First();
        }

        public static DateTime GetNextExecutionDate(this ISchedule schedule, DateTime? startDate = null)
        {
            var cronSchedule = CrontabSchedule.Parse(schedule.ToString());
            var nextOccurrence = cronSchedule.GetNextOccurrence(startDate ?? DateTime.UtcNow);
            if (!string.IsNullOrEmpty(schedule.Year) && schedule.Year.IsNumeric())
                nextOccurrence = new DateTime(int.Parse(schedule.Year), nextOccurrence.Month,
                    nextOccurrence.Day, nextOccurrence.Hour, nextOccurrence.Minute, nextOccurrence.Second);
            return nextOccurrence;
        }

        public static IList<Occurrence> GetNextOccurrences(this IJob job, DateTime endDate, DateTime? startDate = null)
        {
            var occurrence = GetDefaultOccurrence(job, startDate);
            return occurrence.GetNextOccurrences(endDate);
        }

        public static IList<Occurrence> GetNextOccurrences(this Occurrence occurrence, DateTime endDate)
        {
            return BuildOccurrences(occurrence, (schedule, occurrences) =>
            {
                var nextExecutionDates = GetNextExecutionDateList(schedule, occurrence.StartTime, endDate);
                foreach (var nextExecutionDate in nextExecutionDates)
                    occurrences.AddOccurence(occurrence.Job, nextExecutionDate, schedule);
            });
        }

        public static IList<Occurrence> GetNextNOccurrences(this IJob job, int numberOfOccurences)
        {
            var occurrence = GetDefaultOccurrence(job);
            return occurrence.GetNextNOccurrences(numberOfOccurences);
        }

        public static IList<Occurrence> GetNextNOccurrences(this Occurrence occurence, int numberOfOccurences)
        {
            var occurences = new List<Occurrence>();
            var current = occurence;
            for (var i = 0; i < numberOfOccurences; i++)
            {
                current = current.GetNextOccurrence();
                occurences.Add(current);
            }
            return occurences;
        }

        private static void AddOccurence(this ICollection<Occurrence> occurences, IJob job, DateTime nextExecutionDate,
            ISchedule schedule)
        {
            if (occurences.Any())
            {
                var existing = occurences.FirstOrDefault(o => o.StartTime.Equals(nextExecutionDate));
                if (existing != null)
                {
                    if (!existing.Schedules.Contains(schedule))
                        existing.Schedules.Append(schedule);
                }
                else
                {
                    occurences.Add(new Occurrence(job, schedule, nextExecutionDate));
                }
            }
            else
            {
                occurences.Add(new Occurrence(job, schedule, nextExecutionDate));
            }
        }

        public static IEnumerable<DateTime> GetNextExecutionDateList(this ISchedule schedule, DateTime startDate,
            DateTime endDate)
        {
            var cronSchedule = CrontabSchedule.Parse(schedule.ToString());
            return cronSchedule.GetNextOccurrences(startDate, endDate);
        }
    }
}