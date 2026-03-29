# Medo.Config

Simple, file-backed configuration handling for .NET applications.

Medo.Config provides a small API for reading and writing application settings
and runtime state, with sensible defaults for config file locations on Windows
and Linux.


## Features

- User configuration with system configuration file as a fallback
- Preserves comments and general formatting when possible
- Thread-safe access
- Separate state file
- Separate recent files handling


## Installation

Install from NuGet:

~~~bash
dotnet add package Medo.Config
~~~


## Quick Start

~~~csharp
using Medo;

var lastRun = Config.Read("LastRun", DateTime.MinValue);
Config.Write("LastRun", DateTime.UtcNow);
~~~

See `examples/ConfigExample` for a minimal usage sample.
See `examples/RecentExample` for a recent file handling example.


## Default File Locations

By default, application name is inferred from assembly metadata
(`AssemblyProduct`, then `AssemblyTitle`, then assembly name).

On Windows:

- System: not used
- User: `%APPDATA%\[ApplicationName]\[ApplicationName].conf`
- State: `%APPDATA%\[ApplicationName]\[ApplicationName].state`
- Recent: `%APPDATA%\[ApplicationName]\[ApplicationName].recent`

On Linux/macOS and other non-Windows platforms:

- System: `/etc/[applicationname]/[applicationname].conf`
- User: `$XDG_CONFIG_HOME/[applicationname]/[applicationname].conf`
	(or `~/.config/[applicationname]/[applicationname].conf`)
- State: `$XDG_STATE_HOME/[applicationname]/[applicationname].state`
	(or `~/.local/state/[applicationname]/[applicationname].state`)
- Recent: `$XDG_STATE_HOME/[applicationname]/[applicationname].recent`
	(or `~/.local/state/[applicationname]/[applicationname].recent`)

Note: non-Windows defaults normalize app name to lowercase.


## File Format

### Configuration and State

Configuration files use a properties-style text format:

~~~properties
# Sample config
Theme: dark
ItemCount: 100
Path: /opt/b
~~~

- `:` and `=` separators are supported
- repeated keys are supported (use `ReadMany`)
- values are stored as UTF-8 (without BOM)
- comments beginning with `#` are supported


### Recent

Recent file storage uses one file name per line in UTF-8 (without BOM) file:

~~~properties
/file1.txt
/file2.txt
~~~


## Custom Initialization

If needed, provide explicit paths:

~~~csharp
using Medo;

Config.Initialize(
		userConfigPath: "/home/app/.config/myapp.conf",
		systemConfigPath: "/etc/myapp.conf",
		stateConfigPath: "/home/app/.local/state/myapp.state"),
		recentPath: "/home/app/.local/state/myapp.recent");
~~~

Pass `null` for any path that is not needed.
