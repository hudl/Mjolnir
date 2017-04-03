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

    // TODO move these implementations elsewhere. also, is ignoring the right default? maybe a console log instead?

    internal class DefaultMjolnirLogFactory : IMjolnirLogFactory
    {
        public IMjolnirLog CreateLog(string name)
        {
            return new DefaultMjolnirLog();
        }

        public IMjolnirLog CreateLog(Type type)
        {
            return new DefaultMjolnirLog();
        }
    }

    internal class DefaultMjolnirLog : IMjolnirLog
    {
        public void Error(string message)
        {
            return;
        }

        public void Error(string message, Exception exception)
        {
            return;
        }

        public void Info(string message)
        {
            return;
        }
    }
}
