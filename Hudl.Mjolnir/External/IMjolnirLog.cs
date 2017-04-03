using System;

namespace Hudl.Mjolnir.External
{
    public interface IMjolnirLog
    {
        void Info(string message);
        void Error(string message);
        void Error(string message, Exception exception);
    }

    public interface IMjolnirLogFactory
    {
        IMjolnirLog CreateLog(string name);
        IMjolnirLog CreateLog(Type type);
    }
}
