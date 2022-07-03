using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.PasswordManagement;
using PassWinmenu.Utilities;
using PassWinmenu.Utilities.ExtensionMethods;
using PassWinmenu.WinApi;
using PassWinmenu.Windows;

namespace PassWinmenu.Actions
{
	internal class ReencryptPasswordStoreAction : IAction
	{
		private readonly INotificationService notificationService;
		private readonly IPasswordManager passwordManager;
		private readonly ICryptoService cryptoService;
		private readonly IRecipientFinder recipientFinder;
		private readonly DialogCreator dialogCreator;

		public HotkeyAction ActionType => HotkeyAction.ReencryptPasswordStore;

		public ReencryptPasswordStoreAction(
			INotificationService notificationService,
			IPasswordManager passwordManager,
			ICryptoService cryptoService,
			IRecipientFinder recipientFinder,
			DialogCreator dialogCreator)
		{
			this.notificationService = notificationService;
			this.passwordManager = passwordManager;
			this.cryptoService = cryptoService;
			this.recipientFinder = recipientFinder;
			this.dialogCreator = dialogCreator;
		}

		public void Execute()
		{
			Helpers.AssertOnUiThread();

			var result = notificationService.ShowYesNoWindow("The password store will now be re-encrypted. " +
				"If you have added or removed any recipients from your .gpg-id files, " +
				"your password files will be updated to match the recipients specified in those files. " +
				"Note that if your password store contains many passwords, this may take a while.\n\n" +
				"You will also need to have the keys of all recipients in your keyring, and they must be valid " +
				"(unexpired, and trusted).\n\n" +
				"Would you like to continue?", "Re-encrypt your password store");

			if (!result)
			{
				return;
			}

			var files = passwordManager.GetPasswordFiles();
			var directories = files
				.Select(f => new
				{
					f.Directory,
					f.PasswordStore,
				})
				.DistinctBy(d => d.Directory.FullName);

			var selection = dialogCreator
				.ShowPasswordMenu(directories, k => PathUtilities.MakeRelativePathForDisplay(k.PasswordStore, k.Directory), "Select a directory to re-encrypt...")
				.ValueOrDefault();
			if (selection == null)
			{
				return;
			}

			var viewer = new LogViewer("Re-encryption progress", "Re-encrypting the password store...\n");
			viewer.Show();
			Task.Run(() =>
			{
				foreach (var file in files.Where(f => selection.Directory.IsChild(f.FileInfo)))
				{
					try
					{
						var existingRecipients = cryptoService.GetRecipients(file.FullPath);
						var requestedRecipients = recipientFinder.FindRecipients(file)
							.Select(cryptoService.FindShortKeyId)
							.Where(i => i != null);

						var removed = existingRecipients.Except(requestedRecipients);
						var added = requestedRecipients.Except(existingRecipients);
						if (removed.Any() || added.Any())
						{
							var removedKeys = string.Join(", ", removed);
							var addedKeys = string.Join(", ", added);
							var decrypted = passwordManager.DecryptPassword(file, false);
							passwordManager.EncryptPassword(decrypted);
							viewer.AddMessage($"Re-encrypted {file.FullPath} (removed: [{removedKeys}], added: [{addedKeys}])");
						}
						else
						{
							viewer.AddMessage($"Skipped {file.FullPath} (file already encrypted to required recipients)");
						}

					}
					catch (Exception e)
					{
						var message = $"Failed to re-encrypt {file.FullPath}. An error occurred.\n\n" +
							$"{e.GetType().Name}: {e.Message}";
						viewer.AddMessage(message);
						if (!notificationService.ShowYesNoWindow($"{message}\n\nDo you want to continue?", "An error occurred.", MessageBoxImage.Error))
						{
							viewer.AddMessage("Re-encryption aborted.");
							return;
						}
					}
				}
				viewer.AddMessage("Re-encryption finished.");
			});
		}
	}
}
