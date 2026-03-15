using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni;

public partial class NeoIniReader
{
    private readonly INeoIniProvider Provider;
    private readonly string FilePath;

    private readonly bool AutoEncryption = false;
    private bool CustomEncryptionPassword = false;

    private Dictionary<string, Dictionary<string, string>> Data;
    private List<Comment> Comments;
    private readonly ReaderWriterLockSlim Lock = new(LockRecursionPolicy.NoRecursion);

    private bool Disposed = false;
    private int DisposeState = 0;

    private int HotReloadState = 0;
    private CancellationTokenSource HotReloadCts;
    private byte[] PrevHotReloadChecksum;
    private ManualResetEventSlim PauseHotReload = new(true);

    private bool HumanMode = false;

    private int _AutoSaveInterval;
    private int SaveIterationCounter = 0;

    private bool _UseChecksum;

    private string ExtractContent()
    {
        Lock.EnterWriteLock();
        try
        {
            string content = SaveOnDispose ? NeoIniParser.GetContent(Data, Comments, HumanMode) : null;
            Data.Clear();
            Comments.Clear();
            return content;
        }
        finally { Lock.ExitWriteLock(); }
    }

    /// <summary>Releases managed resources and saves changes to the file</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref DisposeState, 1, 0) != 0) return;
        if (disposing)
        {
            SafeStopHotReload();
            if (ExtractContent() is string content)
            {
                Provider.Save(content, UseChecksum);
                Saved?.Invoke(this, EventArgs.Empty);
            }
            DataCleared?.Invoke(this, EventArgs.Empty);
            Lock.Dispose();
        }
        Disposed = true;
    }

    /// <summary>Asynchronously releases managed resources and saves changes to the file</summary>
    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (Interlocked.CompareExchange(ref DisposeState, 1, 0) != 0) return;
        if (disposing)
        {
            SafeStopHotReload();
            if (ExtractContent() is string content)
            {
                await Provider.SaveAsync(content, UseChecksum, CancellationToken.None).ConfigureAwait(false);
                Saved?.Invoke(this, EventArgs.Empty);
            }
            DataCleared?.Invoke(this, EventArgs.Empty);
            Lock.Dispose();
        }
        Disposed = true;
    }

    private void ApplyOptions(NeoIniReaderOptions options)
    {
        options ??= new();
        UseAutoSave = options.UseAutoSave;
        AutoSaveInterval = options.AutoSaveInterval;
        if (Provider is NeoIniFileProvider) UseAutoBackup = options.UseAutoBackup;
        UseAutoAdd = options.UseAutoAdd;
        UseChecksum = options.UseChecksum;
        SaveOnDispose = options.SaveOnDispose;
        AllowEmptyValues = options.AllowEmptyValues;
    }

    private void ThrowIfDisposed() { if (Disposed) throw new ObjectDisposedException(nameof(NeoIniReader)); }

    private void ThrowIfEmpty(string value, bool useAEVRule = true)
    {
        if (useAEVRule && AllowEmptyValues) return;
        if (string.IsNullOrEmpty(value)) throw new EmptyValueNotAllowedException(nameof(value));
    }

    private static void ThrowIfContainsUnsupportedChars(string value) => ThrowIfContainsUnsupportedChars(new[] { value });

    private static void ThrowIfContainsUnsupportedChars(string[] values)
    {
        if (values is null) return;
        ReadOnlySpan<char> invalid = ";\"=".AsSpan();
        foreach (var value in values)
        {
            if (value is null) continue;
            if (value.AsSpan().IndexOfAny(invalid) >= 0) throw new UnsupportedIniCharacterException();
        }
    }

    private bool ShouldAutoSave()
    {
        if (!UseAutoSave) return false;
        if (AutoSaveInterval == 0) return true;
        return Interlocked.Increment(ref SaveIterationCounter) % AutoSaveInterval == 0;
    }

    private void DoAutoSave()
    {
        if (!ShouldAutoSave()) return;
        AutoSave?.Invoke(this, EventArgs.Empty);
        SaveFile();
    }

    private async Task DoAutoSaveAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!ShouldAutoSave()) return;
        AutoSave?.Invoke(this, EventArgs.Empty);
        await SaveFileAsync(ct).ConfigureAwait(false);
    }

    private void SafeStopHotReload()
    {
        if (Interlocked.CompareExchange(ref HotReloadState, 0, 1) != 1) return;
        HotReloadCts.Cancel();
    }
}
