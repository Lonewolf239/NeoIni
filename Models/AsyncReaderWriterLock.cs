using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni.Models;

internal sealed class AsyncReaderWriterLock : IDisposable
{
    internal struct Releaser : IDisposable
    {
        private AsyncReaderWriterLock? Owner;
        private readonly bool IsWrite;

        internal Releaser(AsyncReaderWriterLock owner, bool isWrite)
        {
            Owner = owner;
            IsWrite = isWrite;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref Owner, null);
            owner?.Release(IsWrite);
        }
    }

    private abstract class Waiter
    {
        internal bool IsWrite { get; }
        internal LinkedListNode<Waiter>? Node { get; set; }

        protected Waiter(bool isWrite) => IsWrite = isWrite;

        internal abstract void Complete(Releaser releaser);
        internal abstract void Fail(Exception ex);
    }

    private sealed class AsyncWaiter : Waiter
    {
        internal AsyncReaderWriterLock Owner { get; }
        internal CancellationToken Ct { get; }
        internal TaskCompletionSource<Releaser> Tcs { get; }

        internal AsyncWaiter(AsyncReaderWriterLock owner, bool isWrite, CancellationToken ct) : base(isWrite)
        {
            Owner = owner;
            Ct = ct;
            Tcs = new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal override void Complete(Releaser releaser) => Tcs.TrySetResult(releaser);
        internal override void Fail(Exception ex) => Tcs.TrySetException(ex);
    }

    private sealed class SyncWaiter : Waiter
    {
        internal ManualResetEventSlim Event { get; } = new(false);
        internal Exception? Error { get; private set; }

        internal SyncWaiter(bool isWrite) : base(isWrite) { }

        internal override void Complete(Releaser releaser) => Event.Set();

        internal override void Fail(Exception ex)
        {
            Error = ex;
            Event.Set();
        }
    }

#if NET9_0_OR_GREATER
    private readonly Lock Gate = new();
#else
    private readonly object Gate = new();
#endif
    private readonly LinkedList<Waiter> Waiters = new();

    private int ActiveReaders;
    private bool WriterActive;
    private bool Disposed;

    private static readonly Action<object?> CancelWaiterAction = state =>
    {
        var waiter = (AsyncWaiter)state!;
        waiter.Owner.CancelWaiter(waiter);
    };

    internal Releaser ReadLock() => Acquire(false);

    internal ValueTask<Releaser> ReadLockAsync(CancellationToken ct = default) => AcquireAsync(false, ct);

    internal Releaser WriteLock() => Acquire(true);

    internal ValueTask<Releaser> WriteLockAsync(CancellationToken ct = default) => AcquireAsync(true, ct);

    private Releaser Acquire(bool isWrite)
    {
        SyncWaiter waiter;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            ThrowIfDisposed();
            if (CanAcquireImmediately(isWrite))
            {
                GrantImmediate(isWrite);
                return new Releaser(this, isWrite);
            }
            waiter = new SyncWaiter(isWrite);
            waiter.Node = Waiters.AddLast(waiter);
        }
        waiter.Event.Wait();
        waiter.Event.Dispose();
        if (waiter.Error is not null) throw waiter.Error;
        return new Releaser(this, isWrite);
    }

    private ValueTask<Releaser> AcquireAsync(bool isWrite, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            ThrowIfDisposed();
            if (CanAcquireImmediately(isWrite))
            {
                GrantImmediate(isWrite);
                return new ValueTask<Releaser>(new Releaser(this, isWrite));
            }
        }
        return new ValueTask<Releaser>(AcquireAsyncSlow(isWrite, ct));
    }

    private async Task<Releaser> AcquireAsyncSlow(bool isWrite, CancellationToken ct)
    {
        AsyncWaiter waiter;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            if (CanAcquireImmediately(isWrite))
            {
                GrantImmediate(isWrite);
                return new Releaser(this, isWrite);
            }
            waiter = new AsyncWaiter(this, isWrite, ct);
            waiter.Node = Waiters.AddLast(waiter);
        }
        using (ct.CanBeCanceled ? ct.Register(CancelWaiterAction, waiter) : default)
            return await waiter.Tcs.Task.ConfigureAwait(false);
    }

    private void CancelWaiter(AsyncWaiter waiter)
    {
        List<Waiter>? toWake = null;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            if (waiter.Node is not null)
            {
                Waiters.Remove(waiter.Node);
                waiter.Node = null;
                waiter.Fail(new OperationCanceledException(waiter.Ct));
                toWake = DrainReadyWaiters();
            }
        }
        if (toWake is not null)
        {
            foreach (var w in toWake)
                w.Complete(new Releaser(this, w.IsWrite));
        }
    }

    private void Release(bool isWrite)
    {
        List<Waiter>? toWake;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            if (isWrite)
            {
                if (!WriterActive) throw new SynchronizationLockException("No writer lock is currently held.");
                WriterActive = false;
            }
            else
            {
                if (ActiveReaders <= 0) throw new SynchronizationLockException("No reader lock is currently held.");
                ActiveReaders--;
            }
            toWake = DrainReadyWaiters();
        }
        if (toWake is not null)
        {
            foreach (var waiter in toWake)
                waiter.Complete(new Releaser(this, waiter.IsWrite));
        }
    }

    private bool CanAcquireImmediately(bool isWrite)
    {
        if (Waiters.Count > 0 || WriterActive) return false;
        if (isWrite && ActiveReaders > 0) return false;
        return true;
    }

    private void GrantImmediate(bool isWrite)
    {
        if (isWrite) WriterActive = true;
        else ActiveReaders++;
    }

    private List<Waiter>? DrainReadyWaiters()
    {
        if (Disposed || WriterActive) return null;
        if (Waiters.Count == 0) return null;
        var first = Waiters.First!.Value;
        if (first.IsWrite)
        {
            if (ActiveReaders > 0) return null;
            Waiters.RemoveFirst();
            first.Node = null;
            WriterActive = true;
            return new List<Waiter> { first };
        }
        else
        {
            List<Waiter> readers = new();
            while (Waiters.First is not null && !Waiters.First.Value.IsWrite)
            {
                var reader = Waiters.First.Value;
                Waiters.RemoveFirst();
                reader.Node = null;
                ActiveReaders++;
                readers.Add(reader);
            }
            return readers;
        }
    }

    private void ThrowIfDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(Disposed, nameof(AsyncReaderWriterLock));
#else
        if (Disposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));
#endif
    }

    public void Dispose()
    {
        List<Waiter>? toFail = null;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
        lock (Gate)
#endif
        {
            if (Disposed) return;
            Disposed = true;
            if (Waiters.Count > 0)
            {
                toFail = new List<Waiter>(Waiters.Count);
                foreach (var waiter in Waiters)
                {
                    waiter.Node = null;
                    toFail.Add(waiter);
                }
                Waiters.Clear();
            }
        }
        if (toFail is not null)
        {
            var ex = new ObjectDisposedException(nameof(AsyncReaderWriterLock));
            foreach (var waiter in toFail) waiter.Fail(ex);
        }
    }
}
