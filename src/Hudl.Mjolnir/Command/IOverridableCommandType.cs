using System;

namespace Hudl.Mjolnir.Command
{
    public interface IOverridableCommandType
    {
        Type GetOverridenType();
    }
}