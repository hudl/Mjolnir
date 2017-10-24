using Hudl.Mjolnir.External;
using System;

namespace Hudl.Mjolnir.Log
{
    internal class DefaultMjolnirLogFactory : IMjolnirLogFactory
    {
        public IMjolnirLog<T> CreateLog<T>()
        {
            return new DefaultMjolnirLog<T>();
        }
    }
}
