namespace Lumini.Concurrent.Enums
{
    public enum ThreadStatus
    {
        Undefined = 0,

        New = 10,
        Available = 15,
        Processing = 20,
        Paused = 25,
        Completed = 30,
        Failed = 40,
        Aborted = 50
    }
}