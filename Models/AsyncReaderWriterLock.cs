using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoIni.Models;

internal sealed class AsyncReaderWriterLock : IDisposable
{
    private sealed class Releaser : IDisposable
    {
        private AsyncReaderWriterLock Owner;
        private readonly bool IsWrite;

        public Releaser(AsyncReaderWriterLock owner, bool isWrite)
        {
            Owner = owner;
            IsWrite = isWrite;
        }

        public void Dispose() => Interlocked.Exchange(ref Owner, null)?.Release(IsWrite);
    }

    private abstract class Waiter
    {
        public bool IsWrite { get; }
        public LinkedListNode<Waiter> Node { get; set; }

        protected Waiter(bool isWrite) => IsWrite = isWrite;

        public abstract void Complete(Releaser releaser);
        public abstract void Fail(Exception ex);
    }

    private sealed class AsyncWaiter : Waiter
    {
        public AsyncReaderWriterLock Owner { get; }
        public CancellationToken Ct { get; }
        public TaskCompletionSource<IDisposable> Tcs { get; }

        public AsyncWaiter(AsyncReaderWriterLock owner, bool isWrite, CancellationToken ct) : base(isWrite)
        {
            Owner = owner;
            Ct = ct;
            Tcs = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override void Complete(Releaser releaser) => Tcs.TrySetResult(releaser);
        public override void Fail(Exception ex) => Tcs.TrySetException(ex);
    }

    private sealed class SyncWaiter : Waiter
    {
        public ManualResetEventSlim Event { get; } = new(false);
        public Releaser Result { get; private set; }
        public Exception Error { get; private set; }

        public SyncWaiter(bool isWrite) : base(isWrite) { }

        public override void Complete(Releaser releaser)
        {
            Result = releaser;
            Event.Set();
        }

        public override void Fail(Exception ex)
        {
            Error = ex;
            Event.Set();
        }
    }

    private readonly object Gate = new();
    private readonly LinkedList<Waiter> Waiters = new();

    private int ActiveReaders;
    private bool WriterActive;
    private bool Disposed;

    private static readonly Action<object> CancelWaiterAction = state =>
    {
        var waiter = (AsyncWaiter)state!;
        waiter.Owner.CancelWaiter(waiter);
    };

    public IDisposable ReadLock() => Acquire(false);

    public Task<IDisposable> ReadLockAsync(CancellationToken ct = default) => AcquireAsync(false, ct);

    public IDisposable WriteLock() => Acquire(true);

    public Task<IDisposable> WriteLockAsync(CancellationToken ct = default) => AcquireAsync(true, ct);

    private IDisposable Acquire(bool isWrite)
    {
        SyncWaiter waiter;
        lock (Gate)
        {
            ThrowIfDisposed_NoLock();
            if (CanAcquireImmediately_NoLock(isWrite))
            {
                GrantImmediate_NoLock(isWrite);
                return new Releaser(this, isWrite);
            }
            waiter = new SyncWaiter(isWrite);
            waiter.Node = Waiters.AddLast(waiter);
        }
        waiter.Event.Wait();
        waiter.Event.Dispose();
        if (waiter.Error != null) throw waiter.Error;
        return waiter.Result!;
    }

    private async Task<IDisposable> AcquireAsync(bool isWrite, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        AsyncWaiter waiter;
        lock (Gate)
        {
            ThrowIfDisposed_NoLock();
            if (CanAcquireImmediately_NoLock(isWrite))
            {
                GrantImmediate_NoLock(isWrite);
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
        List<Waiter> toWake = null;
        lock (Gate)
        {
            if (waiter.Node != null)
            {
                Waiters.Remove(waiter.Node);
                waiter.Node = null;
                waiter.Fail(new OperationCanceledException(waiter.Ct));
                toWake = DrainReadyWaiters_NoLock();
            }
        }
        if (toWake != null) { foreach (var w in toWake) w.Complete(new Releaser(this, w.IsWrite)); }
    }

    private void Release(bool isWrite)
    {
        List<Waiter> toWake;
        lock (Gate)
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
            toWake = DrainReadyWaiters_NoLock();
        }
        if (toWake != null) { foreach (var waiter in toWake) waiter.Complete(new Releaser(this, waiter.IsWrite)); }
    }

    private bool CanAcquireImmediately_NoLock(bool isWrite)
    {
        if (Waiters.Count > 0) return false;
        if (isWrite) return !WriterActive && ActiveReaders == 0;
        return !WriterActive;
    }

    private void GrantImmediate_NoLock(bool isWrite)
    {
        if (isWrite) WriterActive = true;
        else ActiveReaders++;
    }

    private List<Waiter> DrainReadyWaiters_NoLock()
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
            var readers = new List<Waiter>();
            while (Waiters.First != null && !Waiters.First.Value.IsWrite)
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

    private void ThrowIfDisposed_NoLock() { if (Disposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock)); }

    public void Dispose()
    {
        List<Waiter> toFail = null;
        lock (Gate)
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
        if (toFail != null)
        {
            var ex = new ObjectDisposedException(nameof(AsyncReaderWriterLock));
            foreach (var waiter in toFail) waiter.Fail(ex);
        }
    }
}
