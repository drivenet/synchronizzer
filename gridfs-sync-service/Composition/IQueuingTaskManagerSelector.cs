using GridFSSyncService.Implementation;

namespace GridFSSyncService.Composition
{
    internal interface IQueuingTaskManagerSelector
    {
        IQueuingTaskManager Select(string key);
    }
}
