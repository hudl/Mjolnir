namespace Hudl.Mjolnir.ThreadPool
{
    internal interface IIsolationSemaphore
    {
        bool TryEnter();
        void Release();
    }
}
