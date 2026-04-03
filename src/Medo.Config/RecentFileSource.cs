namespace Medo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

/// <summary>
/// Provides recent file handling.
/// </summary>
public sealed class RecentFileSource : RecentSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    public RecentFileSource(string filePath)
        : this(filePath, throwAccessExceptions: false) {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <param name="throwAccessExceptions">If true, exceptions during file access will not be ignored.</param>
    public RecentFileSource(string filePath, bool throwAccessExceptions)
        : base(new FileInfo(filePath).FullName) {
        ThrowAccessExceptions = throwAccessExceptions;
    }


    private readonly bool ThrowAccessExceptions;

    private static readonly Encoding Utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly string[] EolSeparators = ["\r\n", "\n", "\r"];


    #region RecentSource

    /// <summary>
    /// Returns all the recent files for the specified key.
    /// </summary>
    protected override FileInfo[] ReadCore() {
        var files = new List<FileInfo>();

        string? allText = null;
        try {  // ignore errors during write
            allText = File.ReadAllText(FileName, Utf8Encoding);
        } catch (Exception ex) when (ex is DirectoryNotFoundException
                                        or IOException
                                        or NotSupportedException
                                        or PathTooLongException
                                        or SecurityException
                                        or UnauthorizedAccessException) {
            Debug.WriteLine($"[Config] {ex.GetType().Name}: {ex.Message}");
            if (ThrowAccessExceptions) { throw; }
        }

        if (allText != null) {
            var lines = allText.Split(EolSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                try {  // ignore errors in file names
                    var filePath = UnescapeText(line);
                    if (string.IsNullOrEmpty(filePath)) { continue; }  // ignore empty file names
                    files.Add(new FileInfo(filePath));
                } catch (Exception ex) when (ex is SecurityException
                                                or NotSupportedException
                                                or PathTooLongException
                                                or UnauthorizedAccessException) {
                    Debug.WriteLine($"[Config] {ex.GetType().Name}: {ex.Message}");
                    if (ThrowAccessExceptions) { throw; }
                }
            }
        }

        return [.. files];
    }

    /// <summary>
    /// Writes all the recent files.
    /// </summary>
    /// <param name="files">Files to write.</param>
    protected override void WriteCore(FileInfo[] files) {
        var sb = new StringBuilder();
        foreach (var file in files) {
            sb.Append(EscapeText(file.FullName));
            sb.Append(Environment.NewLine);
        }

        try {  // ignore errors during write
            var directoryPath = Path.GetDirectoryName(FileName);
            if (directoryPath is not null && !Directory.Exists(directoryPath)) {
                var directoryStack = new Stack<string>();
                do {
                    Debug.WriteLine($"[Config] mkdir {directoryPath}");
                    directoryStack.Push(directoryPath!);
                    directoryPath = Path.GetDirectoryName(directoryPath);
                } while (!Directory.Exists(directoryPath));

                while (directoryStack.Count > 0) {
                    try {
                        Directory.CreateDirectory(directoryStack.Pop());
                    } catch (Exception ex) when (ex is IOException
                                                    or UnauthorizedAccessException) {
                        Debug.WriteLine($"[Config] {ex.GetType().Name}: {ex.Message}");
                        if (ThrowAccessExceptions) { throw; }
                        break;
                    }
                }
            }
            File.WriteAllText(FileName, sb.ToString(), Utf8Encoding);
        } catch (Exception ex) when (ex is DirectoryNotFoundException
                                        or IOException
                                        or NotSupportedException
                                        or PathTooLongException
                                        or SecurityException
                                        or UnauthorizedAccessException) {
            Debug.WriteLine($"[Config] {ex.GetType().Name}: {ex.Message}");
            if (ThrowAccessExceptions) { throw; }
        }
    }

    #endregion  RecentSource


    private static string EscapeText(string text) {
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            switch (ch) {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '\0':
                    sb.Append(@"\0");
                    break;
                case '\b':
                    sb.Append(@"\b");
                    break;
                case '\t':
                    sb.Append(@"\t");
                    break;
                case '\r':
                    sb.Append(@"\r");
                    break;
                case '\n':
                    sb.Append(@"\n");
                    break;
                default:
                    if (char.IsControl(ch)) {
                        sb.Append(@"\u");
                        sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    } else {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private enum State { Default, Escape, EscapeLong };

    private static string UnescapeText(string text) {
        var sb = new StringBuilder();
        var sbEscapeLong = new StringBuilder();

        var state = State.Default;
        foreach (var ch in text) {
            switch (state) {
                case State.Default:
                    if (ch is '\\') {
                        state = State.Escape;
                    } else {
                        sb.Append(ch);
                    }
                    break;

                case State.Escape:
                    if (ch is 'u') {
                        state = State.EscapeLong;
                    } else {
                        var newCh = ch switch {
                            '0' => '\0',
                            'b' => '\b',
                            't' => '\t',
                            'n' => '\n',
                            'r' => '\r',
                            '_' => ' ',
                            _ => ch,
                        };
                        sb.Append(newCh);
                        state = State.Default;
                    }
                    break;

                case State.EscapeLong:
                    sbEscapeLong.Append(ch);
                    if (sbEscapeLong.Length == 4) {
                        if (int.TryParse(sbEscapeLong.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chValue)) {
                            sb.Append((char)chValue);
                        }
                        state = State.Default;
                    }
                    break;

                default: return "";  // should never happen, give up if it does
            }
        }

        return sb.ToString();
    }

}
