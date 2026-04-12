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

#if NET10_0_OR_GREATER
    private static readonly Lock SyncRoot = new();
#else
    private static readonly object SyncRoot = new();
#endif
    private static bool WasInitialized;

    /// <summary>
    /// Initializes the configuration system with default files.
    /// Application name will be determined from AssemblyProduct, AssemblyTitle, or assembly name.
    /// The following default locations will be used:
    /// * Windows:
    /// ** System: %ProgramData%/[ApplicationName]/[ApplicationName].conf
    /// ** User: %AppData%/[ApplicationName]/[ApplicationName].conf
    /// ** State: %AppData%/[ApplicationName]/[ApplicationName].state
    /// ** Recent: %AppData%/[ApplicationName]/[ApplicationName].recent
    /// * Other:
    /// ** System: /etc/[applicationname]/[applicationname].conf
    /// ** User: ~/.config/[applicationname]/[applicationname].conf ($XDG_CONFIG_HOME)
    /// ** State: ~/.local/state/[applicationname]/[applicationname].state ($XDG_STATE_HOME)
    /// ** Recent: ~/.local/state/[applicationname]/[applicationname].recent ($XDG_STATE_HOME)
    /// </summary>
    private static void Initialize() {
        lock (SyncRoot) {
            RetrieveApplicationName(out var applicationName);

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
            RetrievePaths(applicationName, out var systemConfigPath, out var userConfigPath, out var stateConfigPath, out var recentPath);
            Initialize(userConfigPath, systemConfigPath, stateConfigPath, recentPath);
        }
    }

    /// <summary>
    /// Initializes the configuration system with specified files.
    /// This is optional and only needed if you want to use a custom setup.
    /// Files that are not specified will be replaced with in-memory configuration
    /// Exceptions during access will be ignored.
    /// </summary>
    /// <param name="systemConfigPath">Full path to the system configuration file or null if system configuration file is not to be used.</param>
    /// <param name="userConfigPath">Full path to the user configuration file.</param>
    /// <param name="stateConfigPath">Full path to the state configuration file.</param>
    /// <param name="recentPath">Full path to the recent file list.</param>
    public static void Initialize(string? userConfigPath, string? systemConfigPath, string? stateConfigPath, string? recentPath) {
        Initialize(userConfigPath, systemConfigPath, stateConfigPath, recentPath, throwAccessExceptions: false);
    }

    /// <summary>
    /// Initializes the configuration system with specified files.
    /// This is optional and only needed if you want to use a custom setup.
    /// Files that are not specified will be replaced with in-memory configuration.
    /// </summary>
    /// <param name="systemConfigPath">Full path to the system configuration file or null if system configuration file is not to be used.</param>
    /// <param name="userConfigPath">Full path to the user configuration file.</param>
    /// <param name="stateConfigPath">Full path to the state configuration file.</param>
    /// <param name="recentPath">Full path to the recent file list.</param>
    /// <param name="throwAccessExceptions">If true, exceptions during file access will not be ignored.</param>
    public static void Initialize(string? userConfigPath, string? systemConfigPath, string? stateConfigPath, string? recentPath, bool throwAccessExceptions) {
        lock (SyncRoot) {
            _user = (userConfigPath == null)
                  ? new ConfigNoSource()  // throws exceptions if accessed
                  : (userConfigPath.Length == 0)
                  ? new ConfigDummySource()  // empty path means no file, but no exceptions
                  : new ConfigFileSource(new FileInfo(userConfigPath).FullName, throwAccessExceptions);

            _system = (systemConfigPath == null)
                    ? new ConfigNoSource()
                    : (systemConfigPath.Length == 0)
                    ? new ConfigDummySource()
                    : new ConfigFileSource(new FileInfo(systemConfigPath).FullName, throwAccessExceptions);

            _state = (stateConfigPath == null)
                   ? new ConfigNoSource()
                   : (stateConfigPath.Length == 0)
                   ? new ConfigDummySource()
                   : new ConfigFileSource(new FileInfo(stateConfigPath).FullName, throwAccessExceptions);

            _recent = (recentPath == null)
                    ? new RecentNoSource()
                    : (recentPath.Length == 0)
                    ? new RecentDummySource()
                    : new RecentFileSource(new FileInfo(recentPath).FullName, throwAccessExceptions);

            WasInitialized = true;
        }
    }

    /// <summary>
    /// Initializes the configuration system with specified files.
    /// This is optional and only needed if you want to use a custom setup.
    /// Files that are not specified will be replaced with in-memory configuration.
    /// </summary>
    /// <param name="noUserConfig">If true, there will be no user config available..</param>
    /// <param name="noSystemConfig">If true, there will be no system config available.</param>
    /// <param name="noStateConfig">If true, there will be no state config available.</param>
    /// <param name="noRecentFiles">If true, there will be no recent files available.</param>
    public static void Initialize(bool noUserConfig, bool noSystemConfig, bool noStateConfig, bool noRecentFiles) {
        RetrieveApplicationName(out var applicationName);
        RetrievePaths(applicationName, out var systemConfigPath, out var userConfigPath, out var stateConfigPath, out var recentPath);
        Initialize(
            noUserConfig ? null : userConfigPath,
            noSystemConfig ? null : systemConfigPath,
            noStateConfig ? null : stateConfigPath,
            noRecentFiles ? null : recentPath
        );
    }


    #region Files

    private static void RetrieveApplicationName(out string applicationName) {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        string? productValue = null;
        string? titleValue = null;
        var attributes = assembly.GetCustomAttributes();
        foreach (var attribute in attributes) {
            if (attribute is AssemblyProductAttribute productAttribute) { productValue = productAttribute.Product.Trim(); }
            if (attribute is AssemblyTitleAttribute titleAttribute) { titleValue = titleAttribute.Title.Trim(); }
        }

        applicationName = productValue ?? titleValue ?? Path.GetFileNameWithoutExtension(assembly.GetName().Name) ?? "application";
    }

    private static void RetrievePaths(string applicationName, out string systemConfigPath, out string userConfigPath, out string stateConfigPath, out string recentPath) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            systemConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), applicationName, applicationName + ".conf");
            userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, applicationName + ".conf");
            stateConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, applicationName + ".state");
            recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName, applicationName + ".recent");
        } else {
            var homeFallback = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var home = Environment.GetEnvironmentVariable("HOME") ?? homeFallback;
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(home, ".config");
            var stateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME") ?? Path.Combine(home, ".local", "state");
            systemConfigPath = Path.Combine("/etc", applicationName, applicationName + ".conf");
            userConfigPath = Path.Combine(configHome, applicationName, applicationName + ".conf");
            stateConfigPath = Path.Combine(stateHome, applicationName, applicationName + ".state");
            recentPath = Path.Combine(stateHome, applicationName, applicationName + ".recent");
        }
    }

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

    private static RecentSource? _recent;
    /// <summary>
    /// Gets the recent file list.
    /// </summary>
    public static RecentSource Recent {
        get {
            lock (SyncRoot) {
                if (!WasInitialized) { Initialize(); }
                return _recent!;
            }
        }
    }

    #endregion Files

    #region Read

    /// <summary>
    /// Returns the value for the specified key or null if value is not found.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static string? Read(string key) {
        if (User is not ConfigNoSource) {
            return User.Read(key) ?? (System is ConfigNoSource ? null : System.Read(key));
        } else if (System is not ConfigNoSource) {
            return System.Read(key);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
    }

    /// <summary>
    /// Returns the value for the specified key from the user configuration file.
    /// If the key does not exist in the user configuration file, the value will be read from the system configuration file.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">The value to return if the key does not exist.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Default value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static string Read(string key, string defaultValue) {
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            return User.Read(key, System is ConfigNoSource ? defaultValue : System.Read(key, defaultValue));
        } else if (System is not ConfigNoSource) {
            return System.Read(key, defaultValue);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
    }

    /// <summary>
    /// Returns all the values for the specified key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static string[] ReadMany(string key) {
        if (User is not ConfigNoSource) {
            var user = User.ReadMany(key);
            if (user.Length > 0) { return user; }
            if (System is ConfigNoSource) { return []; }
            return System.ReadMany(key);
        } else if (System is not ConfigNoSource) {
            return System.ReadMany(key);
        } else {
            throw new NotSupportedException("No readable configuration source available.");
        }
    }

    #endregion Read

    #region Write

    /// <summary>
    /// Writes the value for the specified key to the user configuration file.
    /// If the specified key does not exist, it is created.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null. -or- Value cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Write(string key, string value) {
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
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
        if (User is not ConfigNoSource) {
            User.Write(key, value);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
    }

    /// <summary>
    /// Writes the values for the specified key.
    /// If the specified key does not exist, it is created.
    /// If value is empty, key is deleted.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="values">The value to write.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void WriteMany(string key, string[] values) {
        if (User is not ConfigNoSource) {
            User.WriteMany(key, values);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
    }

    /// <summary>
    /// Deletes key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <exception cref="ArgumentNullException">Key cannot be null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Key cannot be empty.</exception>
    public static void Delete(string key) {
        if (User is not ConfigNoSource) {
            User.Delete(key);
        } else {
            throw new NotSupportedException("No writable configuration source available.");
        }
    }

    #endregion Shortcuts

}
