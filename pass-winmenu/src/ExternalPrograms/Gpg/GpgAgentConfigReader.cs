using System;
using System.IO;
using System.IO.Abstractions;

namespace PassWinmenu.ExternalPrograms.Gpg
{
	internal class GpgAgentConfigReader : IGpgAgentConfigReader
	{
		private const string GpgAgentConfigFileName = "gpg-agent.conf";

		private readonly IFileSystem fileSystem;
		private readonly IGpgHomedirResolver homedirResolver;

		public GpgAgentConfigReader(IFileSystem fileSystem, IGpgHomedirResolver homedirResolver)
		{
			this.fileSystem = fileSystem;
			this.homedirResolver = homedirResolver;
		}

		public string[] ReadConfigLines()
		{
			var homeDir = GetHomeDir();
			var agentConf = fileSystem.Path.Combine(homeDir, GpgAgentConfigFileName);

			if (fileSystem.File.Exists(agentConf))
			{
				return fileSystem.File.ReadAllLines(agentConf);
			}

			using (fileSystem.File.Create(agentConf))
			{
				return Array.Empty<string>();
			}
		}

		public void WriteConfigLines(string[] lines)
		{
			var homeDir = GetHomeDir();

			var agentConf = fileSystem.Path.Combine(homeDir, GpgAgentConfigFileName);

			fileSystem.File.WriteAllLines(agentConf, lines);
		}

		private string GetHomeDir()
		{
			var homeDir = homedirResolver.GetHomeDir();

			if (fileSystem.Directory.Exists(homeDir))
			{
				return homeDir;
			}

			throw new DirectoryNotFoundException("GPG Homedir does not exist");
		}
	}
}
