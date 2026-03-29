namespace Medo;

using System;
using System.IO;

/// <summary>
/// Provides configuration file handling.
/// </summary>
public sealed class ConfigFileSource : ConfigSource {

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    public ConfigFileSource(string filePath)
        : base(new FileInfo(filePath).FullName) {
        PropertiesFile = new PropertiesFile(filePath);
    }


    private readonly PropertiesFile PropertiesFile;


    #region ConfigSource

    /// <summary>
    /// Loads all settings from a file.
    /// </summary>
    protected override void LoadCore() {
        // loads in constructor
    }

    /// <summary>
    /// Saves all settings to a file.
    /// </summary>
    protected override void SaveCore() {
        PropertiesFile.Save();
    }

    /// <summary>
    /// Returns all the values for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override string[] ReadCore(string key) {
        return PropertiesFile.ReadMany(key);
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
        PropertiesFile.WriteMany(key, values);
    }

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    protected override void DeleteCore(string key) {
        PropertiesFile.Delete(key);
    }

    /// <summary>
    /// Deletes all settings.
    /// </summary>
    protected override void ClearCore() {
        PropertiesFile.Clear();
    }

    #endregion  ConfigSource

}
