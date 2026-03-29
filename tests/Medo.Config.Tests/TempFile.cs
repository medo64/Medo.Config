namespace Tests;

using System;
using System.IO;

internal class TempFile : IDisposable {

    public string FileName { get; }

    public TempFile() {
        FileName = System.IO.Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    public string[] GetLines() {
        return File.ReadAllText(FileName).Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }


    public void Dispose() {
        if (File.Exists(FileName)) {
            File.Delete(FileName);
        }
    }
}
