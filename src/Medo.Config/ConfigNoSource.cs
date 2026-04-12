namespace Medo;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides in-memory configuration file handling.
/// </summary>
internal sealed class ConfigNoSource : ConfigSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public ConfigNoSource()
        : base(string.Empty) {
    }


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
        throw new NotSupportedException("Configuration source is not supported.");
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
        throw new NotSupportedException("Configuration source is not supported.");
    }

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override void DeleteCore(string key) {
        throw new NotSupportedException("Configuration source is not supported.");
    }

    /// <summary>
    /// Deletes all settings.
    /// </summary>
    protected override void ClearCore() {
        throw new NotSupportedException("Configuration source is not supported.");
    }

    #endregion  ConfigSource

}
