namespace Medo;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides in-memory configuration file handling.
/// </summary>
internal sealed class ConfigDummySource : ConfigSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public ConfigDummySource()
        : base(string.Empty) {
    }


    private readonly Dictionary<string, string[]> Storage = [];


    #region ConfigSource

    /// <summary>
    /// Loads all settings from a file.
    /// </summary>
    protected override void LoadCore() {
    }

    /// <summary>
    /// Saves all settings to a file.
    /// </summary>
    protected override void SaveCore() {
    }

    /// <summary>
    /// Returns all the values for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override string[] ReadCore(string key) {
        if (Storage.TryGetValue(key, out var values)) {
            return [.. values];
        }
        return [];
    }

    /// <summary>
    /// Writes the values for the specified key.
    /// If the specified key does not exist, it is created.
    /// If value is null or empty, key is deleted.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="values">The values to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override void WriteCore(string key, string[] values) {
        if (values == null || values.Length == 0) {
            Storage.Remove(key);
        } else {
            Storage[key] = values;
        }
    }

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override void DeleteCore(string key) {
        Storage.Remove(key);
    }

    /// <summary>
    /// Deletes all settings.
    /// </summary>
    protected override void ClearCore() {
        Storage.Clear();
    }

    #endregion  ConfigSource

}
