/* Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

namespace Medo;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Class for handling configuration.
/// </summary>
public static class Config {

    private static readonly Lock SyncRoot = new();
    private static bool WasInitialized;

    /// <summary>
    /// Initializes the configuration system with default files.
    /// Application name will be determined from AssemblyProduct, AssemblyTitle, or assembly name.
    /// The following default locations will be used:
    /// * Windows:
    /// ** System: (none)
    /// ** User: ~/ApplicationData/[ApplicationName]/[ApplicationName].conf
    /// ** State: ~/ApplicationData/[ApplicationName]/[ApplicationName].state
    /// * Other:
    /// ** System: /etc/[applicationname]/[applicationname].conf
    /// ** User: ~/.config/[applicationname]/[applicationname].conf ($XDG_CONFIG_HOME)
    /// ** State: ~/.local/state/[applicationname]/[applicationname].state ($XDG_STATE_HOME)
    /// </summary>
    private static void Initialize() {
        lock (SyncRoot) {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

            string? productValue = null;
            string? titleValue = null;
            var attributes = assembly.GetCustomAttributes();
            foreach (var attribute in attributes) {
                if (attribute is AssemblyProductAttribute productAttribute) { productValue = productAttribute.Product.Trim(); }
                if (attribute is AssemblyTitleAttribute titleAttribute) { titleValue = titleAttribute.Title.Trim(); }
            }

            var applicationName = productValue ?? titleValue ?? Path.GetFileNameWithoutExtension(assembly.GetName().Name) ?? "application";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Initialize(applicationName);
            } else {
#pragma warning disable CA1308  // linux uses lowercase application names
                Initialize(applicationName.ToLowerInvariant());
#pragma warning restore CA1308
            }
        }
    }

    /// <summary>
    /// Initializes the configuration system with specified files.
    /// This is optional and only needed if you want to use a custom setup.
    /// The following default locations will be used:
    /// * Windows:
    /// ** System: (none)
    /// ** User: ~/ApplicationData/[ApplicationName]/[ApplicationName].conf
    /// ** State: ~/ApplicationData/[ApplicationName]/[ApplicationName].state
    /// * Other:
    /// ** System: /etc/[applicationname]/[applicationname].conf
    /// ** User: ~/.config/[applicationname]/[applicationname].conf ($XDG_CONFIG_HOME)
    /// ** State: ~/.local/state/[applicationname]/[applicationname].state ($XDG_STATE_HOME)
    /// </summary>
    /// <param name="applicationName">The name of the application used to determine default configuration file name.</param>
    public static void Initialize(string applicationName) {
        lock (SyncRoot) {
            string systemConfigPath;
            string userConfigPath;
            string stateConfigPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                systemConfigPath = "";
                userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, applicationName + ".conf");
                stateConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, applicationName + ".state");
            } else {
                var homeFallback = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var home = Environment.GetEnvironmentVariable("HOME") ?? homeFallback;
                var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(home, ".config");
                var stateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME") ?? Path.Combine(home, ".local", "state");
                systemConfigPath = Path.Combine("/etc", applicationName, applicationName + ".conf");
                userConfigPath = Path.Combine(configHome, applicationName, applicationName + ".conf");
                stateConfigPath = Path.Combine(stateHome, applicationName, applicationName + ".state");
            }

            Initialize(userConfigPath, systemConfigPath, stateConfigPath);
        }
    }

    /// <summary>
    /// Initializes the configuration system with specified files.
    /// This is optional and only needed if you want to use a custom setup.
    /// Files that are not specified will be replaced with in-memory configuration.
    /// </summary>
    /// <param name="systemConfigPath">Full path to the system configuration file or null if system configuration file is not to be used.</param>
    /// <param name="userConfigPath">Full path to the user configuration file.</param>
    /// <param name="stateConfigPath">Full path to the state configuration file.</param>
    public static void Initialize(string? userConfigPath, string? systemConfigPath, string? stateConfigPath) {
        lock (SyncRoot) {
            var userConfigFile = !string.IsNullOrEmpty(userConfigPath) ? new FileInfo(userConfigPath) : null;
            var systemConfigFile = !string.IsNullOrEmpty(systemConfigPath) ? new FileInfo(systemConfigPath) : null;
            var stateConfigFile = !string.IsNullOrEmpty(stateConfigPath) ? new FileInfo(stateConfigPath) : null;

            _system = (systemConfigFile != null) ? new ConfigFileSource(systemConfigFile.FullName) : new ConfigDummySource();
            _user = (userConfigFile != null) ? new ConfigFileSource(userConfigFile.FullName) : new ConfigDummySource();
            _state = (stateConfigFile != null) ? new ConfigFileSource(stateConfigFile.FullName) : new ConfigDummySource();

            WasInitialized = true;
        }
    }


    #region Files

    private static ConfigSource? _system;
    /// <summary>
    /// Gets the system configuration file.
    /// </summary>
    public static ConfigSource System {
        get {
            lock (SyncRoot) {
                if (!WasInitialized) { Initialize(); }
                return _system!;
            }
        }
    }

    private static ConfigSource? _user;
    /// <summary>
    /// Gets the user configuration file.
    /// Any configuration parameter that doesn't exist will be read from the system configuration file.
    /// </summary>
    public static ConfigSource User {
        get {
            lock (SyncRoot) {
                if (!WasInitialized) { Initialize(); }
                return _user!;
            }
        }
    }

    private static ConfigSource? _state;
    /// <summary>
    /// Gets the user state file.
    /// </summary>
    public static ConfigSource State {
        get {
            lock (SyncRoot) {
                if (!WasInitialized) { Initialize(); }
                return _state!;
            }
        }
    }

    #endregion Files

    #region Shortcuts

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Default value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static string Read(string key, string defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static bool Read(string key, bool defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static int Read(string key, int defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static long Read(string key, long defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static float Read(string key, float defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static double Read(string key, double defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static DateTime Read(string key, DateTime defaultValue) {
        return User.Read(key, System.Read(key, defaultValue));
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, string value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, bool value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, int value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, long value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, float value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, double value) {
        User.Write(key, value);
    }

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>4
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, DateTime value) {
        User.Write(key, value);
    }

    #endregion Shortcuts

}
