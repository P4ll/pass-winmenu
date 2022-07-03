using System;
using System.IO.Abstractions;
using PassWinmenu.Configuration;
using PassWinmenu.WinApi;

#nullable enable
namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgHomeDirResolver : IGpgHomedirResolver
	{
		private const string DefaultHomeDirName = "gnupg";
		private const string HomeDirEnvironmentVariableName = "GNUPGHOME";

		private readonly GpgConfig config;
		private readonly IEnvironment environment;
		private readonly IFileSystem fileSystem;

		public GpgHomeDirResolver(GpgConfig config, IEnvironment environment, IFileSystem fileSystem)
		{
			this.config = config;
			this.environment = environment;
			this.fileSystem = fileSystem;
		}

		/// <summary>
		/// Returns the path GPG will use as its home directory.
		/// </summary>
		public string GetHomeDir() => GetConfiguredHomeDir() ?? GetDefaultHomeDir();

		/// <summary>
		/// Returns the home directory as configured by the user, or null if no home directory has been defined.
		/// </summary>
		public string? GetConfiguredHomeDir()
		{
			return config.GnupghomeOverride ?? environment.GetEnvironmentVariable(HomeDirEnvironmentVariableName);
		}

		/// <summary>
		/// Returns the default home directory used by GPG when no user-defined home directory is available.
		/// </summary>
		public string GetDefaultHomeDir()
		{
			var appData = environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return fileSystem.Path.Combine(appData, DefaultHomeDirName);
		}
	}
}
