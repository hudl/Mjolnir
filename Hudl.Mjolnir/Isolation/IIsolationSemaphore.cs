namespace Hudl.Mjolnir.Isolation
{
    internal interface IIsolationSemaphore
    {
        bool TryEnter();
        void Release();
    }
}
