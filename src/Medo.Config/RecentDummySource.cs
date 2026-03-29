namespace Medo;

using System.IO;

/// <summary>
/// Provides recent file handling.
/// </summary>
public sealed class RecentDummySource : RecentSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public RecentDummySource()
        : base("") {
    }


    private FileInfo[] Storage = [];


    #region RecentSource

    /// <summary>
    /// Returns all the recent files for the specified key.
    /// </summary>
    protected override FileInfo[] ReadCore() {
        return Storage;
    }

    /// <summary>
    /// Writes all the recent files.
    /// </summary>
    /// <param name="files">Files to write.</param>
    protected override void WriteCore(FileInfo[] files) {
        Storage = files;
    }

    #endregion  RecentSource

}
