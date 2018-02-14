namespace Lumini.Framework.Common
{
    public interface IThreadable
    {
        ThreadStatus Status { get; set; }
    }
}