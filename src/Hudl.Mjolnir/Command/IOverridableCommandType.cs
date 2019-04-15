using System;

namespace Hudl.Mjolnir.Command
{
    interface IOverridableCommandType
    {
        Type GetOverridenType();
    }
}