namespace Hudl.Mjolnir.Config
{
    public class CommandConfiguration
    {
        /// <summary>
        /// Per-Command Timeout - Timeouts are configurable per-command.
        /// </summary>
        public virtual long Timeout { get; set; }
    }
}