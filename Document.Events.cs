using System;
using NeoIni.Models;

namespace NeoIni
{
    public partial class NeoIniDocument
    {
        /// <summary>Called before saving a file to disk</summary>
        public event EventHandler? Saved;

        /// <summary>Called after successfully loading data from a file or reloading</summary>
        public event EventHandler? Loaded;

        /// <summary>Called when the value of an existing key in a section changes</summary>
        public event EventHandler<KeyEventArgs>? KeyChanged;

        /// <summary>Called when a key is renamed within a specific section.</summary>
        public event EventHandler<KeyRenamedEventArgs>? KeyRenamed;

        /// <summary>Called when a new key is added to a section</summary>
        public event EventHandler<KeyEventArgs>? KeyAdded;

        /// <summary>Called when a key is removed from a section</summary>
        public event EventHandler<KeyRemovedEventArgs>? KeyRemoved;

        /// <summary>Called whenever a section changes (keys are changed/added/removed)</summary>
        public event EventHandler<SectionEventArgs>? SectionChanged;

        /// <summary>Called when a section is renamed.</summary>
        public event EventHandler<SectionRenamedEventArgs>? SectionRenamed;

        /// <summary>Called when a new section is added</summary>
        public event EventHandler<SectionEventArgs>? SectionAdded;

        /// <summary>Called when a section is deleted</summary>
        public event EventHandler<SectionEventArgs>? SectionRemoved;

        /// <summary>Called when the data is completely cleared</summary>
        public event EventHandler? DataCleared;

        /// <summary>Called before automatic saving</summary>
        public event EventHandler? AutoSave;

        /// <summary>Called when errors occur (parsing, saving, reading a file, etc.)</summary>
        public event EventHandler<ProviderErrorEventArgs>? Error
        {
            add => Provider.Error += value;
            remove => Provider.Error -= value;
        }

        /// <summary>Called when the checksum does not match when loading a file</summary>
        public event EventHandler<ChecksumMismatchEventArgs>? ChecksumMismatch
        {
            add => Provider.ChecksumMismatch += value;
            remove => Provider.ChecksumMismatch -= value;
        }

        /// <summary>Called after each search</summary>
        public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;
    }
}
