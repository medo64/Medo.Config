namespace Medo;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

internal class PropertiesFile {

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;
    private const StringComparison KeyComparison = StringComparison.OrdinalIgnoreCase;

    private readonly string FileName;
    private readonly string LineEnding;
    private readonly List<LineData> Lines = [];

    public PropertiesFile(string fileName) {
        FileName = fileName;

        string? fileContent = null;
        try {
            fileContent = File.ReadAllText(fileName, Utf8);
        } catch (IOException) {
        } catch (UnauthorizedAccessException) { }

        string? lineEnding = null;
        if (fileContent != null) {
            var currLine = new StringBuilder();
            var lineEndingDetermined = false;

            var prevChar = '\0';
            foreach (var ch in fileContent) {
                if (ch is '\n') {
                    if (prevChar == '\r') { //CRLF pair
                        if (!lineEndingDetermined) {
                            lineEnding = "\r\n";
                            lineEndingDetermined = true;
                        }
                    } else {
                        if (!lineEndingDetermined) {
                            lineEnding = "\n";
                            lineEndingDetermined = true;
                        }
                        processLine(currLine);
                        currLine.Clear();
                    }
                } else if (ch is '\r') {
                    processLine(currLine);
                    if (!lineEndingDetermined) { lineEnding = "\r"; } //do not set as determined as there is possibility of trailing LF
                } else {
                    if (lineEnding != null) { lineEndingDetermined = true; } //if there was a line ending before, mark it as determined
                    currLine.Append(ch);
                }
                prevChar = ch;
            }
            FileExists = true;

            processLine(currLine);
        }
        LineEnding = lineEnding ?? Environment.NewLine;

        void processLine(StringBuilder line) {
            var lineText = line.ToString();
            line.Clear();

            char? valueSeparator = null;

            var sbKey = new StringBuilder();
            var sbValue = new StringBuilder();
            var sbComment = new StringBuilder();
            var sbWhitespace = new StringBuilder();
            var sbEscapeLong = new StringBuilder();
            string? separatorPrefix = null;
            string? separatorSuffix = null;
            string? commentPrefix = null;

            var state = State.Default;
            var prevState = State.Default;
            foreach (var ch in lineText) {
                switch (state) {
                    case State.Default:
                        if (char.IsWhiteSpace(ch)) {
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            state = State.KeyEscape;
                        } else {
                            sbKey.Append(ch);
                            state = State.Key;
                        }
                        break;

                    case State.Comment:
                        sbComment.Append(ch);
                        break;

                    case State.Key:
                        if (char.IsWhiteSpace(ch)) {
                            valueSeparator = ch;
                            state = State.SeparatorOrValue;
                        } else if (ch is ':' or '=') {
                            valueSeparator = ch;
                            state = State.ValueOrWhitespace;
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            state = State.KeyEscape;
                        } else {
                            sbKey.Append(ch);
                        }
                        break;

                    case State.SeparatorOrValue:
                        if (char.IsWhiteSpace(ch)) {
                        } else if (ch is ':' or '=') {
                            valueSeparator = ch;
                            state = State.ValueOrWhitespace;
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            state = State.ValueEscape;
                        } else {
                            sbValue.Append(ch);
                            state = State.Value;
                        }
                        break;

                    case State.ValueOrWhitespace:
                        if (char.IsWhiteSpace(ch)) {
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            state = State.ValueEscape;
                        } else {
                            sbValue.Append(ch);
                            state = State.Value;
                        }
                        break;

                    case State.Value:
                        if (char.IsWhiteSpace(ch)) {
                            state = State.ValueOrComment;
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            state = State.ValueEscape;
                        } else {
                            sbValue.Append(ch);
                        }
                        break;

                    case State.ValueOrComment:
                        if (char.IsWhiteSpace(ch)) {
                        } else if (ch is '#') {
                            sbComment.Append(ch);
                            state = State.Comment;
                        } else if (ch is '\\') {
                            sbValue.Append(sbWhitespace);
                            state = State.ValueEscape;
                        } else {
                            sbValue.Append(sbWhitespace);
                            sbValue.Append(ch);
                            state = State.Value;
                        }
                        break;

                    case State.KeyEscape:
                    case State.ValueEscape:
                        if (ch is 'u') {
                            state = (state is State.KeyEscape) ? State.KeyEscapeLong : State.ValueEscapeLong;
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
                            if (state is State.KeyEscape) {
                                sbKey.Append(newCh);
                            } else {
                                sbValue.Append(newCh);
                            }
                            state = (state is State.KeyEscape) ? State.Key : State.Value;
                        }
                        break;

                    case State.KeyEscapeLong:
                    case State.ValueEscapeLong:
                        sbEscapeLong.Append(ch);
                        if (sbEscapeLong.Length == 4) {
                            if (int.TryParse(sbEscapeLong.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chValue)) {
                                if (state is State.KeyEscape) {
                                    sbKey.Append((char)chValue);
                                } else {
                                    sbValue.Append((char)chValue);
                                }
                            }
                            state = (state is State.KeyEscapeLong) ? State.Key : State.Value;
                        }
                        break;

                    default:
                        throw new InvalidOperationException("Unexpected state.");
                }

                if (char.IsWhiteSpace(ch) && (prevState != State.KeyEscape) && (prevState != State.ValueEscape) && (prevState != State.KeyEscapeLong) && (prevState != State.ValueEscapeLong)) {
                    sbWhitespace.Append(ch);
                } else if (state != prevState) { //on state change, clean comment prefix
                    if ((state is State.ValueOrWhitespace) && (separatorPrefix == null)) {
                        separatorPrefix = sbWhitespace.ToString();
                        sbWhitespace.Clear();
                    } else if ((state is State.Value) && (separatorSuffix == null)) {
                        separatorSuffix = sbWhitespace.ToString();
                        sbWhitespace.Clear();
                    } else if ((state is State.Comment) && (commentPrefix == null)) {
                        commentPrefix = sbWhitespace.ToString();
                        sbWhitespace.Clear();
                    } else if (state is State.Key or State.ValueOrWhitespace or State.Value) {
                        sbWhitespace.Clear();
                    }
                }

                prevState = state;
            }

            Lines.Add(new LineData(sbKey.ToString(), separatorPrefix, valueSeparator, separatorSuffix, sbValue.ToString(), commentPrefix, sbComment.ToString()));
        }

#if DEBUG
        foreach (var line in Lines) {
            if (!string.IsNullOrEmpty(line.Key)) {
                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "[Config] {0}: {1}", line.Key, line.Value));
            }
        }
#endif
    }

    public bool FileExists { get; } //false if there was an error during load

    public bool Save() {
        var fileContent = string.Join(LineEnding, Lines);
        try {
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
                    } catch (IOException) {
                        break;
                    } catch (UnauthorizedAccessException) {
                        break;
                    }
                }
            }

            File.WriteAllText(FileName, fileContent, Utf8);
            return true;
        } catch (IOException) {
            return false;
        } catch (UnauthorizedAccessException) {
            return false;
        }
    }


    private enum State {
        Default,
        Comment,
        Key,
        KeyEscape,
        KeyEscapeLong,
        SeparatorOrValue,
        ValueOrWhitespace,
        Value,
        ValueEscape,
        ValueEscapeLong,
        ValueOrComment,
    }


    private class LineData {
        public LineData()
            : this(null, null, null, null, null, null, null) {
        }
        public LineData(LineData? template, string key, string value)
            : this(key,
                  template?.SeparatorPrefix ?? "",
                  template?.Separator ?? ':',
                  template?.SeparatorSuffix ?? " ",
                  value,
                  null, null) {
            if (template != null) {
                var firstKeyTotalLength = (template.Key?.Length ?? 0) + (template.SeparatorPrefix?.Length ?? 0) + 1 + (template.SeparatorSuffix?.Length ?? 0);
                var totalLengthWithoutSuffix = key.Length + (template.SeparatorPrefix?.Length ?? 0) + 1;
                var maxSuffixLength = firstKeyTotalLength - totalLengthWithoutSuffix;
                if (maxSuffixLength < 1) { maxSuffixLength = 1; } //leave at least one space
                if (SeparatorSuffix?.Length > maxSuffixLength) {
                    SeparatorSuffix = SeparatorSuffix[..maxSuffixLength];
                }
            }
        }

        public LineData(string? key, string? separatorPrefix, char? separator, string? separatorSuffix, string? value, string? commentPrefix, string? comment) {
            Key = key;
            SeparatorPrefix = separatorPrefix;
            Separator = separator ?? ':';
            SeparatorSuffix = separatorSuffix;
            Value = value;
            CommentPrefix = commentPrefix;
            Comment = comment;
        }

        public string? Key { get; set; }
        public string? SeparatorPrefix { get; set; }
        public char? Separator { get; }
        public string? SeparatorSuffix { get; set; }
        public string? Value { get; set; }
        public string? CommentPrefix { get; }
        public string? Comment { get; }

        public override string ToString() {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Key)) {
                EscapeIntoStringBuilder(sb, Key, isKey: true);

                if (!string.IsNullOrEmpty(Value)) {
                    if (Separator is ':' or '=') {
                        sb.Append(SeparatorPrefix);
                        sb.Append(Separator);
                        sb.Append(SeparatorSuffix);
                    } else {
                        sb.Append(string.IsNullOrEmpty(SeparatorSuffix) ? " " : SeparatorSuffix);
                    }
                    EscapeIntoStringBuilder(sb, Value);
                } else { //try to preserve formatting in case of spaces (thus omitted)
                    sb.Append(SeparatorPrefix);
                    switch (Separator) {
                        case ':': sb.Append(':'); break;
                        case '=': sb.Append('='); break;
                        default: break;
                    }
                    sb.Append(SeparatorSuffix);
                }
            }

            if (!string.IsNullOrEmpty(Comment)) {
                if (!string.IsNullOrEmpty(CommentPrefix)) { sb.Append(CommentPrefix); }
                sb.Append(Comment);
            }

            return sb.ToString();
        }

        private static void EscapeIntoStringBuilder(StringBuilder sb, string text, bool isKey = false) {
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
                    case '#':
                        sb.Append(@"\#");
                        break;
                    default:
                        if (char.IsControl(ch)) {
                            sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                        } else if (ch is ' ') {
                            if ((i == 0) || (i == (text.Length - 1)) || isKey) {
                                sb.Append(@"\_");
                            } else {
                                sb.Append(ch);
                            }
                        } else if (char.IsWhiteSpace(ch)) {
                            switch (ch) {
                                case '\0':
                                    sb.Append(@"\0");
                                    break;
                                case '\b':
                                    sb.Append(@"\b");
                                    break;
                                case '\t':
                                    sb.Append(@"\t");
                                    break;
                                case '\n':
                                    sb.Append(@"\n");
                                    break;
                                case '\r':
                                    sb.Append(@"\r");
                                    break;
                                default:
                                    sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                                    break;
                            }
                        } else if (ch is '\\') {
                            sb.Append(@"\\");
                        } else {
                            sb.Append(ch);
                        }
                        break;
                }
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(Key) && string.IsNullOrEmpty(Value) && string.IsNullOrEmpty(CommentPrefix) && string.IsNullOrEmpty(Comment);

    }


    private Dictionary<string, int>? CachedEntries;
    private void FillCache() {
        CachedEntries = new Dictionary<string, int>(KeyComparer);
        for (var i = 0; i < Lines.Count; i++) {
            var line = Lines[i];
            if (!line.IsEmpty && line.Key is not null) {
                if (!CachedEntries.TryAdd(line.Key, i)) {
                    CachedEntries[line.Key] = i;  // last key takes precedence
                }
            }
        }
    }

    public string[] ReadMany(string key) {
        if (CachedEntries == null) { FillCache(); }

        var lines = new List<string>();
        foreach (var line in Lines) {
            if (string.Equals(key, line.Key, KeyComparison)) {
                lines.Add(line.Value ?? "");
            }
        }
        return [.. lines];
    }

    public void WriteMany(string key, string[] values) {
        if (CachedEntries == null) { FillCache(); }

        if (values.Length == 0) {

            Delete(key);

        } else if (values.Length == 1) {  // special handling to preserve line comments

            if (CachedEntries!.TryGetValue(key, out var lineIndex)) {
                var data = Lines[lineIndex];
                data.Key = key;
                data.Value = values[0];
            } else {
                var hasLines = (Lines.Count > 0);
                var newData = new LineData(hasLines ? Lines[0] : null, key, values[0]);
                if (!hasLines) {
                    CachedEntries.Add(key, Lines.Count);
                    Lines.Add(newData);
                    Lines.Add(new LineData());
                } else if (!Lines[^1].IsEmpty) {
                    CachedEntries.Add(key, Lines.Count);
                    Lines.Add(newData);
                } else {
                    CachedEntries.Add(key, Lines.Count - 1);
                    Lines.Insert(Lines.Count - 1, newData);
                }
            }

        } else {  // multiple values, not so gentle with inline comments

            if (CachedEntries!.TryGetValue(key, out _)) {
                var lastIndex = 0;
                LineData? lastLine = null;
                for (var i = Lines.Count - 1; i >= 0; i--) { //find insertion point
                    var line = Lines[i];
                    if (string.Equals(key, line.Key, KeyComparison)) {
                        if (lastLine == null) {
                            lastLine = line;
                            lastIndex = i;
                        } else {
                            lastIndex--;
                        }
                        Lines.RemoveAt(i);
                    }
                }
                var hasLines = (Lines.Count > 0);
                foreach (var value in values) {
                    Lines.Insert(lastIndex, new LineData(lastLine ?? (hasLines ? Lines[0] : null), key, value));
                    lastIndex++;
                }
                FillCache();
            } else {
                var hasLines = (Lines.Count > 0);
                if (!hasLines) {
                    foreach (var value in values) {
                        CachedEntries[key] = Lines.Count;
                        Lines.Add(new LineData(null, key, value));
                    }
                    Lines.Add(new LineData());
                } else if (!Lines[^1].IsEmpty) {
                    foreach (var value in values) {
                        CachedEntries[key] = Lines.Count;
                        Lines.Add(new LineData(Lines[0], key, value));
                    }
                } else {
                    foreach (var value in values) {
                        CachedEntries[key] = Lines.Count - 1;
                        Lines.Insert(Lines.Count - 1, new LineData(Lines[0], key, value));
                    }
                }
            }

        }
    }

    public void Delete(string key) {
        if (CachedEntries == null) { FillCache(); }

        CachedEntries!.Remove(key);
        for (var i = Lines.Count - 1; i >= 0; i--) {
            var line = Lines[i];
            if (string.Equals(key, line.Key, KeyComparison)) {
                Lines.RemoveAt(i);
            }
        }
    }

    public void Clear() {
        Lines.Clear();
        FillCache();
    }

}
