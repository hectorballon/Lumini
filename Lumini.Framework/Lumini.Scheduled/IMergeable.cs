namespace Lumini.Scheduled
{
    public interface IMergeable
    {
        IMergeable MergeWith(object obj);
        bool CanMergeWith(object obj);
    }
}
