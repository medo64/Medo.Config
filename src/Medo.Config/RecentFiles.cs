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
    private readonly Lock SyncRoot = new();  // contention unlikely, lock is good enough
    private FileInfo[]? FilesCache;


    /// <summary>
    /// Gets the recent file at the specified index.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public FileInfo this[int index] {
        get {
            lock (SyncRoot) {
                FilesCache ??= Source.GetFiles();
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, FilesCache.Length);
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
    public void Add(FileInfo file) {
        ArgumentNullException.ThrowIfNull(file);
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
    public void Remove(FileInfo file) {
        ArgumentNullException.ThrowIfNull(file);
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
