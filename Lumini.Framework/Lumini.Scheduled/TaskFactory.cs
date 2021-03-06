﻿namespace Lumini.Scheduled
{
    public static class TaskFactory<T>
        where T : Task, new()
    {
        public static T Create()
        {
            return new T();
        }
    }
}