using System;

namespace Hudl.Mjolnir.External
{
    public interface IMjolnirLog<out T>
    {
        void SetLogName(string name);
        void Debug(string message);
        void Info(string message);
        void Error(string message);
        void Error(string message, Exception exception);
    }

    public interface IMjolnirLogFactory
    {
        IMjolnirLog<T> CreateLog<T>();
    }
}
