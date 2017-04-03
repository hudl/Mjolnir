using Hudl.Mjolnir.External;
using System;

namespace Hudl.Mjolnir.Log
{
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
}
