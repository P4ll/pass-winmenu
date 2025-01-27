using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using LibGit2Sharp;

namespace PassWinmenu.ExternalPrograms
{
	internal class NativeGitSyncStrategy : IGitSyncStrategy
	{
		private readonly string gitPath;
		private readonly string repositoryPath;
		private readonly TimeSpan gitCallTimeout = TimeSpan.FromSeconds(5);

		public NativeGitSyncStrategy(string gitPath, string repositoryPath)
		{
			this.gitPath = gitPath;
			this.repositoryPath = repositoryPath;
		}

		public void Fetch(Branch branch)
		{
			CallGit("fetch " + branch.RemoteName);
		}

		/// <summary>
		/// Pushes changes to remote.
		/// </summary>
		public void Push()
		{
			CallGit("push");
		}
		private void CallGit(string arguments)
		{
			var argList = new List<string>
			{
				// May be required in certain cases?
				//"--non-interactive"
			};

			var psi = new ProcessStartInfo
			{
				FileName = gitPath,
				WorkingDirectory = repositoryPath,
				Arguments = $"{arguments} {string.Join(" ", argList)}",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			if (!string.IsNullOrEmpty(Configuration.ConfigManager.Config.Git.SshPath))
			{
				// Remove is a no-op if the variable is not set.
				psi.EnvironmentVariables.Remove("GIT_SSH");
				psi.EnvironmentVariables.Add("GIT_SSH", Configuration.ConfigManager.Config.Git.SshPath);
			}
			Process gitProc;
			try
			{
				gitProc = Process.Start(psi) ?? throw new GitException("Failed to start Git process");
			}
			catch (Win32Exception e)
			{
				throw new GitException("Git failed to start. " + e.Message, e);
			}

			gitProc.WaitForExit((int)gitCallTimeout.TotalMilliseconds);
			var error = gitProc.StandardError.ReadToEnd();
			if (gitProc.ExitCode != 0)
			{
				throw new GitException($"Git exited with code {gitProc.ExitCode}", error);
			}
		}
	}
}
