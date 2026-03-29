/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.IO;
using System.Threading;

/// <summary>
/// Base class for recent file list sources.
/// All reads and writes are thread-safe.
/// </summary>
public abstract class RecentSource {

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="fileName">The path to the configuration file.</param>
    protected RecentSource(string fileName) {
        FileName = fileName;
        MaxCount = 20;
    }

    /// <summary>
    /// Gets the name of the file used for reccent files.
    /// This is always an empty string because this configuration is in-memory only.
    /// </summary>
    public string FileName { get; }


    private readonly Lock SyncRoot = new();


    #region Abstract

    /// <summary>
    /// Returns all the recent files.
    /// </summary>
    protected abstract FileInfo[] ReadCore();

    /// <summary>
    /// Writes all the recent files.
    /// </summary>
    /// <param name="files">Files to write.</param>
    protected abstract void WriteCore(FileInfo[] files);

    #endregion Abstract


    #region Internal

    /// <summary>
    /// Returns all the recent files.
    /// </summary>
    internal FileInfo[] GetFiles() {
        lock (SyncRoot) {
            var files = ReadCore();
            if (files.Length > MaxCount) {
                var filesNew = new FileInfo[MaxCount];
                Array.Copy(files, filesNew, MaxCount);
                files = filesNew;
            }
            return files;
        }
    }

    /// <summary>
    /// Writes all the recent files.
    /// </summary>
    /// <param name="files">Files to write.</param>
    internal void SetFiles(FileInfo[] files) {
        lock (SyncRoot) {
            if (files.Length > MaxCount) {
                var filesNew = new FileInfo[MaxCount];
                Array.Copy(files, filesNew, MaxCount);
                files = filesNew;
            }
            WriteCore(files);
        }
    }

    #endregion Internal


    /// <summary>
    /// Gets or sets the maximum number of recent files to keep.
    /// Value must be between 1 and 100.
    /// </summary>
    public int MaxCount {
        get {
            lock (SyncRoot) {
                return field;
            }
        }
        set {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
            lock (SyncRoot) {
                field = value;
            }
        }
    }

    private RecentFiles? _files;
    /// <summary>
    /// Returns available recent files.
    /// </summary>
    public RecentFiles Files {
        get {
            lock (SyncRoot) {
                _files ??= new RecentFiles(this);
                return _files;
            }
        }
    }

}
