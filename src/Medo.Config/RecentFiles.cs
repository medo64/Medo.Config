namespace Medo;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

/// <summary>
/// Provides recent file handling.
/// This class is thread-safe.
/// </summary>
public sealed class RecentFiles : IEnumerable<FileInfo> {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="source">Source to use.</param>
    internal RecentFiles(RecentSource source) {
        Source = source;
    }


    private readonly RecentSource Source;
#if NET10_0_OR_GREATER
    private readonly Lock SyncRoot = new();  // contention unlikely, lock is good enough
#else
    private readonly object SyncRoot = new();
#endif
    private FileInfo[]? FilesCache;


    /// <summary>
    /// Gets the recent file at the specified index.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Index must be between 0 and Count</exception>
    public FileInfo this[int index] {
        get {
            lock (SyncRoot) {
                FilesCache ??= Source.GetFiles();
#if NET10_0_OR_GREATER
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, FilesCache.Length);
#else
                if (index < 0 || index >= FilesCache.Length) {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and Count.");
                }
#endif
                return FilesCache[index];
            }
        }
    }

    /// <summary>
    /// Gets the number of recent files.
    /// </summary>
    public int Count {
        get {
            lock (SyncRoot) {
                FilesCache ??= Source.GetFiles();
                return FilesCache.Length;
            }
        }
    }

    /// <summary>
    /// Adds a file to the recent files list.
    /// </summary>
    /// <param name="file">File to add.</param>
    /// <exception cref="ArgumentNullException">File cannot be null.</exception>
    public void Add(FileInfo file) {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(file);
#else
        if (file is null) { throw new ArgumentNullException(nameof(file), "File cannot be null."); }
#endif
        lock (SyncRoot) {
            var files = new List<FileInfo>(Source.GetFiles());  // fresh read when adding
            for (int i = 0; i < files.Count; i++) {
                if (files[i].FullName.Equals(file.FullName, StringComparison.OrdinalIgnoreCase)) {
                    files.RemoveAt(i);
                    break;
                }
            }
            files.Insert(0, file);
            Source.SetFiles([.. files]);
            FilesCache = null;  // force read on on next access
        }
    }

    /// <summary>
    /// Removes a file from the recent files list.
    /// </summary>
    /// <param name="file">File to remove</param>
    /// <exception cref="ArgumentNullException">File cannot be null.</exception>
    public void Remove(FileInfo file) {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(file);
#else
        if (file is null) { throw new ArgumentNullException(nameof(file), "File cannot be null."); }
#endif
        lock (SyncRoot) {
            var files = new List<FileInfo>(Source.GetFiles());  // fresh read when removing
            for (int i = 0; i < files.Count; i++) {
                if (files[i].FullName.Equals(file.FullName, StringComparison.OrdinalIgnoreCase)) {
                    files.RemoveAt(i);
                    break;
                }
            }
            Source.SetFiles([.. files]);
            FilesCache = null;  // force read on on next access
        }
    }

    /// <summary>
    /// Clears all recent files.
    /// </summary>
    public void Clear() {
        lock (SyncRoot) {
            Source.SetFiles([]);
            FilesCache = null;  // force read on on next access
        }
    }


    #region IEnumerable<FileInfo>

    /// <summary>
    /// Returns an enumerator that iterates through the collection of recent files.
    /// </summary>
    public IEnumerator<FileInfo> GetEnumerator() {
        FilesCache ??= Source.GetFiles();
        return ((IEnumerable<FileInfo>)FilesCache).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        FilesCache ??= Source.GetFiles();
        return FilesCache.GetEnumerator();
    }

    #endregion IEnumerable<FileInfo>

}
