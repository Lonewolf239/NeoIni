using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Models;

namespace NeoIni.Providers
{
    internal sealed class HotReloadMonitor : IHotReloadMonitor
    {
        private readonly INeoIniProvider Provider;

        private readonly AsyncReaderWriterLock Lock = new AsyncReaderWriterLock();
        private CancellationTokenSource? CancellationTokenSource;
        private byte[]? PrevChecksum;
        private readonly ManualResetEventSlim PauseEvent = new ManualResetEventSlim(true);
        private int PollingInterval;
        private bool Disposed;

        public event EventHandler? ChangeDetected;

        internal HotReloadMonitor(INeoIniProvider provider) => Provider = provider;

        public void Start(int pollingInterval)
        {
            if (pollingInterval < 1000) throw new InvalidHotReloadDelayException(nameof(pollingInterval));
            using (Lock.WriteLock())
            {
                if (CancellationTokenSource != null) throw new InvalidOperationException("Monitor is already running.");
                CancellationTokenSource = new CancellationTokenSource();
                PrevChecksum = Provider.GetStateChecksum();
                PollingInterval = pollingInterval;
                PauseEvent.Set();
            }
            Task.Run(RunAsync, CancellationToken.None);
        }

        private async Task RunAsync()
        {
            var token = CancellationTokenSource!.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    PauseEvent.Wait(token);
                    var currentChecksum = Provider.GetStateChecksum();
                    byte[]? prev;
                    using (await Lock.WriteLockAsync(token).ConfigureAwait(false))
                        prev = PrevChecksum;
                    if (prev is null || !prev.SequenceEqual(currentChecksum))
                    {
                        ChangeDetected?.Invoke(this, EventArgs.Empty);
                        using (await Lock.WriteLockAsync(token).ConfigureAwait(false))
                            PrevChecksum = currentChecksum;
                    }
                    await Task.Delay(PollingInterval, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Provider.RaiseError(this, new ProviderErrorEventArgs(ex));
                Stop();
            }
        }

        public void Pause() => PauseEvent.Reset();

        public void Continue()
        {
            using (Lock.WriteLock())
            {
                PrevChecksum = Provider.GetStateChecksum();
                PauseEvent.Set();
            }
        }
        public async Task ContinueAsync(CancellationToken cancellationToken)
        {
            using (await Lock.WriteLockAsync(cancellationToken).ConfigureAwait(false))
            {
                PrevChecksum = Provider.GetStateChecksum();
                PauseEvent.Set();
            }
        }

        public void Stop()
        {
            using (Lock.WriteLock())
            {
                CancellationTokenSource?.Cancel();
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Stop();
            Lock.Dispose();
            PauseEvent.Dispose();
            Disposed = true;
        }
    }
}
