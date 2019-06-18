using Synchronizzer.Implementation;

namespace Synchronizzer.Composition
{
    internal interface IQueuingTaskManagerSelector
    {
        IQueuingTaskManager Select(string key);
    }
}
