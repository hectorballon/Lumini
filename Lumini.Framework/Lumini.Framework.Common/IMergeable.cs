namespace Lumini.Framework.Common
{
    public interface IMergeable
    {
        IMergeable MergeWith(object obj);
        bool CanMergeWith(object obj);
    }
}