using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NeoIni.Core;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni;

public partial class NeoIniReader
{
    private readonly INeoIniProvider Provider;
    private readonly string? FilePath;

    private readonly IEncryptionProvider EncryptionProvider;
    private readonly bool AutoEncryption = false;
    private bool CustomEncryptionPassword = false;

    private Dictionary<string, Dictionary<string, string>>? Data;
    private List<Comment>? Comments;
    private readonly AsyncReaderWriterLock Lock = new();

    private bool Disposed = false;
    private int DisposeState = 0;

    private int HotReloadState = 0;
    private CancellationTokenSource? HotReloadCts;
    private byte[]? PrevHotReloadChecksum;
    private readonly ManualResetEventSlim PauseHotReload = new(true);

    private bool HumanMode = false;

    private int _AutoSaveInterval;
    private int SaveIterationCounter = 0;

    private bool _UseChecksum;

    private bool _UseShielding;

    private string? ExtractContent()
    {
        using (Lock.WriteLock())
        {
            string? content = SaveOnDispose ? NeoIniParser.GetContent(Data, Comments, UseShielding) : null;
            Data?.Clear();
            Comments?.Clear();
            return content;
        }
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

    internal void Load()
    {
        var neoIniData = Provider.GetData();
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var neoIniData = await Provider.GetDataAsync(ct: cancellationToken).ConfigureAwait(false);
        Data = neoIniData.Data;
        Comments = neoIniData.Comments;
    }

    private void ApplyOptions(NeoIniReaderOptions? options)
    {
        options ??= new();
        UseAutoSave = options.UseAutoSave;
        AutoSaveInterval = options.AutoSaveInterval;
        if (Provider is NeoIniFileProvider) UseAutoBackup = options.UseAutoBackup;
        UseAutoAdd = options.UseAutoAdd;
        UseChecksum = options.UseChecksum;
        SaveOnDispose = options.SaveOnDispose;
        AllowEmptyValues = options.AllowEmptyValues;
        UseShielding = options.UseShielding;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(Disposed, nameof(NeoIniReader));
#else
        if (Disposed) throw new ObjectDisposedException(nameof(NeoIniReader));
#endif
    }

    private void ThrowIfEmpty(string value, bool useAEVRule = true)
    {
        if (useAEVRule && AllowEmptyValues) return;
        if (string.IsNullOrEmpty(value)) throw new EmptyValueNotAllowedException(nameof(value));
    }

    private void ThrowIfContainsUnsupportedChars(string? value, bool isValue)
    {
        if (value is null) return;
        if (isValue && UseShielding)
        {
            if (value.AsSpan().IndexOfAny("\"".AsSpan()) >= 0) throw new UnsupportedIniCharacterException("\"");
            return;
        }
        if (value.AsSpan().IndexOfAny(";=\"".AsSpan()) >= 0) throw new UnsupportedIniCharacterException("; = \"");
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
        HotReloadCts?.Cancel();
    }

    private async Task ExecuteWithReadLockAsync(Action action, CancellationToken ct)
    {
        using (await Lock.ReadLockAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            action();
        }
    }

    private async Task ExecuteWithWriteLockAsync(Action action, CancellationToken ct)
    {
        using (await Lock.WriteLockAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            action();
        }
    }

    private void ValidateValue(string value, bool isValue = false)
    {
        ThrowIfEmpty(value, isValue);
        ThrowIfContainsUnsupportedChars(value, isValue);
    }

    private void ValidateTwoValue(string value1, string value2)
    {
        ValidateValue(value1, false);
        ValidateValue(value2, false);
    }
}
