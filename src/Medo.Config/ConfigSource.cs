/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Base class for configuration sources.
/// All reads and writes are thread-safe.
/// </summary>
public abstract class ConfigSource {

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="fileName">The path to the configuration file.</param>
    protected ConfigSource(string fileName) {
        FileName = fileName ?? string.Empty;
    }


    /// <summary>
    /// Gets the name of the file used for settings.
    /// </summary>
    public string FileName { get; }


#if NET10_0_OR_GREATER
    private readonly Lock SyncRoot = new();
#else
    private readonly object SyncRoot = new();
#endif
    private bool WasLoaded;


    /// <summary>
    /// Loads all settings from a file.
    /// </summary>
    public void Load() {
        lock (SyncRoot) {
            LoadCore();
            WasLoaded = true;
        }
    }

    /// <summary>
    /// Saves all settings to a file.
    /// </summary>
    public void Save() {
        lock (SyncRoot) {
            SaveCore();
        }
    }

    #region Abstract

    /// <summary>
    /// Loads all settings from a file.
    /// </summary>
    protected abstract void LoadCore();

    /// <summary>
    /// Saves all settings to a file.
    /// </summary>
    protected abstract void SaveCore();

    /// <summary>
    /// Returns all the values for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    protected abstract string[] ReadCore(string key);

    /// <summary>
    /// Writes the values for the specified key.
    /// If the specified key does not exist, it is created.
    /// If value is empty, key is deleted.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="values">The value to write.</param>
    protected abstract void WriteCore(string key, string[] values);

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    protected abstract void DeleteCore(string key);

    /// <summary>
    /// Deletes all settings.
    /// </summary>
    protected abstract void ClearCore();

    #endregion Abstract

    #region Read

    /// <summary>
    /// Returns all the values for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public string[] ReadMany(string key) {
        ValidateKey(ref key);
        lock (SyncRoot) {
            if (!WasLoaded) { Load(); }
            return ReadCore(key);
        }
    }

    /// <summary>
    /// Returns the value for the specified key or null if value is not found.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public string? Read(string key) {
        var values = ReadMany(key);
#if NET10_0_OR_GREATER
        return (values.Length > 0) ? values[^1] : null;
#else
        return (values.Length > 0) ? values[values.Length - 1] : null;
#endif
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Default value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public string Read(string key, string defaultValue) {
        return Read(key) ?? defaultValue;

    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public bool Read(string key, bool defaultValue) {
        var value = Read(key)?.Trim();
        if ("T".Equals(value, StringComparison.OrdinalIgnoreCase) || "TRUE".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return true;
        } else if ("F".Equals(value, StringComparison.OrdinalIgnoreCase) || "FALSE".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return false;
        } else if ("Y".Equals(value, StringComparison.OrdinalIgnoreCase) || "YES".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return true;
        } else if ("N".Equals(value, StringComparison.OrdinalIgnoreCase) || "NO".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return false;
        } else if ("+".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return true;
        } else if ("-".Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return false;
        } else if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultInt)) {
            return resultInt != 0;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public int Read(string key, int defaultValue) {
        var value = Read(key)?.Trim();
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public long Read(string key, long defaultValue) {
        var value = Read(key)?.Trim();
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public float Read(string key, float defaultValue) {
        var value = Read(key)?.Trim();
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public double Read(string key, double defaultValue) {
        var value = Read(key)?.Trim();
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public decimal Read(string key, decimal defaultValue) {
        var value = Read(key)?.Trim();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)) {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns the value for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public DateTime Read(string key, DateTime defaultValue) {
        var value = Read(key)?.Trim();
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)) {
            return result;
        } else if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2)) {
            return result2;
        }
        return defaultValue;
    }

    #endregion Read

    #region Write

    /// <summary>
    /// Writes the values for the specified key.
    /// If the specified key does not exist, it is created.
    /// If value is empty, key is deleted.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="values">The value to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void WriteMany(string key, string[] values) {
        ValidateKey(ref key);
        lock (SyncRoot) {
            if (!WasLoaded) { Load(); }
            WriteCore(key, values);
            SaveCore();
        }
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value), "Value cannot be null."); }
        WriteMany(key, [value]);
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, bool value) {
        Write(key, value ? "true" : "false");
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, int value) {
        Write(key, value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, long value) {
        Write(key, value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, float value) {
        Write(key, value.ToString("r", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, double value) {
        Write(key, value.ToString("r", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, decimal value) {
#if NET10_0_OR_GREATER
        Write(key, value.ToString("r", CultureInfo.InvariantCulture));
#else
        Write(key, value.ToString(CultureInfo.InvariantCulture));
#endif
    }

    /// <summary>
    /// Writes the value for the specified key.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Write(string key, DateTime value) {
        Write(key, value.ToString("o", CultureInfo.InvariantCulture));
    }

    #endregion Write

    #region Delete

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public void Delete(string key) {
        ValidateKey(ref key);
        lock (SyncRoot) {
            DeleteCore(key);
            SaveCore();
        }
    }

    /// <summary>
    /// Deletes all settings.
    /// </summary>
    public void Clear() {
        lock (SyncRoot) {
            ClearCore();
            SaveCore();
        }
    }

    #endregion Delete


    /// <summary>
    /// Throws exception if the specified argument is not a valid key.
    /// </summary>
    /// <param name="key">Argument.</param>
    /// <param name="paramName">Param name.</param>
#if NET10_0_OR_GREATER
    private static void ValidateKey(ref string key, [CallerArgumentExpression(nameof(key))] string? paramName = null) {
#else
    private static void ValidateKey(ref string key, string? paramName = null) {
#endif
        if (key is null) { throw new ArgumentNullException(paramName, "Key cannot be null."); }
        key = key.Trim();
        if (string.IsNullOrEmpty(key)) { throw new ArgumentOutOfRangeException(paramName, "Key cannot be empty."); }
    }

}
