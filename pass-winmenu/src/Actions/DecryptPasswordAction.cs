using System;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.PasswordManagement;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	/// <summary>
	/// Asks the user to choose a password file, decrypts it, and copies the resulting value to the clipboard.
	/// </summary>
	internal class DecryptPasswordAction : IParameterisedAction
	{
		private readonly IPasswordManager passwordManager;
		private readonly INotificationService notificationService;
		private readonly DialogCreator dialogCreator;
		private readonly PasswordFileParser passwordFileParser;
		private readonly PasswordStoreConfig passwordStoreConfig;
		private readonly InterfaceConfig interfaceConfig;

		public DecryptPasswordAction(
			IPasswordManager passwordManager,
			INotificationService notificationService,
			DialogCreator dialogCreator,
			PasswordFileParser passwordFileParser,
			PasswordStoreConfig passwordStoreConfig,
			InterfaceConfig interfaceConfig)
		{
			this.passwordManager = passwordManager;
			this.notificationService = notificationService;
			this.dialogCreator = dialogCreator;
			this.passwordFileParser = passwordFileParser;
			this.passwordStoreConfig = passwordStoreConfig;
			this.interfaceConfig = interfaceConfig;
		}

		public void Execute(bool copyToClipboard, bool typeUsername, bool typePassword)
		{
			var selectedFile = dialogCreator.RequestPasswordFile();
			// If the user cancels their selection, the password decryption should be cancelled too.
			if (selectedFile == null)
			{
				return;
			}

			KeyedPasswordFile passFile;
			try
			{
				passFile = passwordManager.DecryptPassword(selectedFile, passwordStoreConfig.FirstLineOnly);
			}
			catch (Exception e) when (e is GpgError || e is GpgException || e is ConfigurationException)
			{
				notificationService.ShowErrorWindow("Password decryption failed: " + e.Message);
				return;
			}
			catch (Exception e)
			{
				notificationService.ShowErrorWindow($"Password decryption failed: An error occurred: {e.GetType().Name}: {e.Message}");
				return;
			}

			if (copyToClipboard)
			{
				TemporaryClipboard.Place(passFile.Password, TimeSpan.FromSeconds(interfaceConfig.ClipboardTimeout));
				if (ConfigManager.Config.Notifications.Types.PasswordCopied)
				{
					notificationService.Raise($"The password has been copied to your clipboard.\nIt will be cleared in {interfaceConfig.ClipboardTimeout:0.##} seconds.", Severity.Info);
				}
			}
			var usernameEntered = false;
			if (typeUsername)
			{
				try
				{
					var username = passwordFileParser.GetUsername(passFile);
					if (username != null)
					{
						KeyboardEmulator.EnterText(username);
						usernameEntered = true;
					}
				}
				catch (Exception e)
				{
					notificationService.ShowErrorWindow($"Could not retrieve your username: {e.Message}");
					return;
				}
			}
			if (typePassword)
			{
				// If a username has also been entered, press Tab to switch to the password field.
				if (usernameEntered)
				{
					KeyboardEmulator.EnterTab();
				}

				KeyboardEmulator.EnterText(passFile.Password);
			}
		}

	}
}
