using System;

namespace Hudl.Mjolnir.External
{
    public interface IMjolnirConfig
    {
        T GetConfig<T>(string key, T defaultValue);
        
        // TODO untested interface / implementation. change handler firing needs some more scrutiny.
        // TODO config implementation needs to ensure that change handlers don't get GC'ed

        void AddChangeHandler<T>(string key, Action<T> onConfigChange);
    }
    
    // TODO note about implementation needing to cache dynamic/generated values (e.g. mjolnir.command.{name}.Timeout)
}
