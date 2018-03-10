namespace Lumini.Concurrent
{
    public interface IService
    {
        string Name { get; set; }
        bool Enabled { get; set; }
    }
}