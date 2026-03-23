using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni.Providers
{
    /// <summary>
    /// Provides a mechanism to monitor external changes to the configuration source
    /// and trigger hot reload.
    /// </summary>
    public interface IHotReloadMonitor : IDisposable
    {
        /// <summary>
        /// Occurs when a change is detected in the monitored configuration source.
        /// Provides the new checksum of the source.
        /// </summary>
        event EventHandler? ChangeDetected;

        /// <summary>Starts monitoring for changes.</summary>
        /// <param name="pollingInterval">
        /// The polling interval in milliseconds. 
        /// The exact meaning depends on the implementation; typically it specifies how often to check for changes.
        /// </param>
        void Start(int pollingInterval);

        /// <summary>
        /// Pauses change detection synchronously. 
        /// Used during write operations to avoid triggering a reload while saving.
        /// </summary>
        void Pause();

        /// <summary>Resumes change detection synchronously after a pause.</summary>
        void Continue();

        /// <summary>Asynchronously resumes change detection after a pause.</summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the resume operation.</param>
        /// <returns>A task representing the asynchronous resume operation.</returns>
        Task ContinueAsync(CancellationToken cancellationToken);

        /// <summary>Stops monitoring and releases any resources used by the monitor.</summary>
        void Stop();
    }
}
