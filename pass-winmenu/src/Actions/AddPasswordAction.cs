using System;
using System.IO;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

#nullable enable
namespace PassWinmenu.Actions
{

	/// <summary>
	/// Adds a new password to the password store.
	/// </summary>
	internal class AddPasswordAction : IAction
	{
		private readonly DialogCreator dialogCreator;
		private readonly IPasswordManager passwordManager;
		private readonly ISyncService? syncService;
		private readonly INotificationService notificationService;

		public HotkeyAction ActionType => HotkeyAction.AddPassword;

		public AddPasswordAction(
			DialogCreator dialogCreator,
			IPasswordManager passwordManager,
			Option<ISyncService> syncService,
			INotificationService notificationService)
		{
			this.dialogCreator = dialogCreator;
			this.passwordManager = passwordManager;
			this.syncService = syncService.ValueOrDefault();
			this.notificationService = notificationService;
		}

		public void Execute()
		{
			Helpers.AssertOnUiThread();

			var passwordFilePath = dialogCreator.ShowFileSelectionWindow();
			// passwordFileName will be null if no file was selected
			if (passwordFilePath == null)
			{
				return;
			}

			// Display the password generation window.
			string password;
			string metadata;
			using (var passwordWindow = new PasswordWindow(Path.GetFileName(passwordFilePath), ConfigManager.Config.PasswordStore.PasswordGeneration))
			{
				passwordWindow.ShowDialog();
				if (!passwordWindow.DialogResult.GetValueOrDefault())
				{
					return;
				}
				password = passwordWindow.Password.Text;
				metadata = passwordWindow.ExtraContent.Text.Replace(Environment.NewLine, "\n");
			}

			PasswordFile passwordFile;
			try
			{
				passwordFile = passwordManager.AddPassword(passwordFilePath, password, metadata);
			}
			catch (GpgException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			catch (ConfigurationException e)
			{
				notificationService.ShowErrorWindow("Unable to encrypt your password: " + e.Message);
				return;
			}
			// Copy the newly generated password.
			TemporaryClipboard.Place(password, TimeSpan.FromSeconds(ConfigManager.Config.Interface.ClipboardTimeout));

			if (ConfigManager.Config.Notifications.Types.PasswordGenerated)
			{
				notificationService.Raise($"The new password has been copied to your clipboard.\nIt will be cleared in {ConfigManager.Config.Interface.ClipboardTimeout:0.##} seconds.", Severity.Info);
			}

			try
			{
				// Add the password to Git
				syncService?.AddPassword(passwordFile.FullPath);
			}
			catch (Exception e)
			{
				Log.Send($"Failed to commit {passwordFile.FullPath}");
				Log.ReportException(e);
				notificationService.ShowErrorWindow("Unable to commit your changes: " + e.Message);
			}
		}
	}
}
