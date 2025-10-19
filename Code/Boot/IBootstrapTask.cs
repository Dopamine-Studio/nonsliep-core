using System.Collections;

namespace Nonsliep.Core.Boot
{
    /// <summary>
    /// Represents a startup task that the <see cref="Bootstrapper"/> can execute before continuing.
    /// </summary>
    public interface IBootstrapTask
    {
        /// <summary>
        /// Display name used for logging and tooling.
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// Whether the bootstrap flow should abort if this task fails.
        /// </summary>
        bool IsCritical { get; }

        /// <summary>
        /// True when the most recent execution finished successfully.
        /// </summary>
        bool WasSuccessful { get; }

        /// <summary>
        /// Reset internal state before the task executes.
        /// </summary>
        void ResetState();

        /// <summary>
        /// Execute the task. Should set <see cref="WasSuccessful"/> before completing.
        /// Returning null skips coroutine execution.
        /// </summary>
        IEnumerator Execute();
    }
}
