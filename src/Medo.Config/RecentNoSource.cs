namespace Medo;

using System;
using System.IO;

/// <summary>
/// Provides recent file handling.
/// </summary>
public sealed class RecentNoSource : RecentSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public RecentNoSource()
        : base("") {
    }


    #region RecentSource

    /// <summary>
    /// Returns all the recent files for the specified key.
    /// </summary>
    protected override FileInfo[] ReadCore() {
        throw new NotSupportedException("Configuration source is not supported.");
    }

    /// <summary>
    /// Writes all the recent files.
    /// </summary>
    /// <param name="files">Files to write.</param>
    protected override void WriteCore(FileInfo[] files) {
        throw new NotSupportedException("Configuration source is not supported.");
    }

    #endregion  RecentSource

}
