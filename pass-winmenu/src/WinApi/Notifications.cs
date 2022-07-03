using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms;
using PassWinmenu.UpdateChecking;
using PassWinmenu.Utilities;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

#nullable enable
namespace PassWinmenu.WinApi
{
	internal class Notifications : INotificationService, ISyncStateTracker
	{
		public NotifyIcon Icon { get; set; }

		private const string DownloadUpdateString = "https://github.com/geluk/pass-winmenu/releases";
		private readonly ToolStripMenuItem downloadUpdate;
		private readonly ToolStripSeparator downloadSeparator;
		private const int ToolTipTimeoutMs = 5000;

		private Notifications(NotifyIcon icon)
		{
			Icon = icon ?? throw new ArgumentNullException(nameof(icon));
			Icon.Click += HandleIconClick;

			downloadUpdate = new ToolStripMenuItem("Download Update");
			downloadUpdate.Click += HandleDownloadUpdateClick;
			downloadUpdate.BackColor = Color.Beige;

			downloadUpdate.Visible = false;
			downloadSeparator = new ToolStripSeparator
			{
				Visible = false
			};
		}

		public static Notifications Create()
		{
			var icon = new NotifyIcon
			{
				Icon = EmbeddedResources.Icon,
				Visible = true
			};


			return new Notifications(icon);
		}

		private void HandleIconClick(object sender, EventArgs e)
		{
			var args = (MouseEventArgs)e;
			if (args.Button == MouseButtons.Left)
			{
				// Unfortunately, calling Show() here does not do what you'd expect.
				// It displays the menu in the wrong place, and the menu won't hide if you click outside it.
				// ShowContextMenu() does the right thing, but it's a private method,
				// so we have to resort to a bit of a hack to get it to work.
				var mi = typeof(NotifyIcon)
					.GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
				mi!.Invoke(Icon, null);
			}
		}

		public void AddMenuActions(ActionDispatcher actionDispatcher)
		{
			var menu = new ContextMenuStrip();
			menu.Items.Add(new ToolStripLabel("pass-winmenu " + Program.Version));
			menu.Items.Add(new ToolStripSeparator());

			menu.Items.Add(downloadUpdate);
			menu.Items.Add(downloadSeparator);

			menu.Items.Add("Decrypt Password", null, (sender, args) => actionDispatcher.DecryptPassword(true, false, false));
			menu.Items.Add("Add new Password", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.AddPassword));
			menu.Items.Add("Edit Password File", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.EditPassword));
			menu.Items.Add("Generate TOTP Code", null, (sender, args) => actionDispatcher.GenerateTotpCode(true, false));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Push to Remote", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.GitPush));
			menu.Items.Add("Pull from Remote", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.GitPull));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Open Explorer", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.OpenExplorer));
			menu.Items.Add("Open Shell", null, (sender, args) => Task.Run(() => actionDispatcher.Dispatch(HotkeyAction.OpenShell)));
			menu.Items.Add(new ToolStripSeparator());

			var dropDown = new ToolStripMenuItem("More Actions");
			dropDown.DropDownItems.Add("Check for Updates", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.CheckForUpdates));
			dropDown.DropDownItems.Add("Edit Configuration", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.EditConfiguration));
			dropDown.DropDownItems.Add("Re-Encrypt Password Store", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ReencryptPasswordStore));
			dropDown.DropDownItems.Add("View Log", null, (sender, args) => actionDispatcher.Dispatch(HotkeyAction.ViewLog));

			menu.Items.Add(dropDown);
			menu.Items.Add(new ToolStripSeparator());

			var startupLink = new StartupLink("pass-winmenu");
			var startWithWindows = new ToolStripMenuItem("Start with Windows")
			{
				Checked = startupLink.Exists
			};
			startWithWindows.Click += (sender, args) =>
			{
				var target = Assembly.GetExecutingAssembly().Location;
				var workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
				startupLink.Toggle(target, workingDirectory);
				startWithWindows.Checked = startupLink.Exists;
			};

			menu.Items.Add(startWithWindows);
			menu.Items.Add("About", null, (sender, args) => Process.Start("https://github.com/geluk/pass-winmenu#readme"));
			menu.Items.Add("Quit", null, (sender, args) => App.Exit());
			Icon.ContextMenuStrip = menu;
		}

		public void Raise(string message, Severity level)
		{
			if (ConfigManager.Config.Notifications.Enabled)
			{
				Icon.ShowBalloonTip(ToolTipTimeoutMs, "pass-winmenu", message, GetIconForSeverity(level));
			}
		}

		private ToolTipIcon GetIconForSeverity(Severity severity)
		{
			return severity switch
			{
				Severity.None => ToolTipIcon.None,
				Severity.Info => ToolTipIcon.Info,
				Severity.Warning => ToolTipIcon.Warning,
				Severity.Error => ToolTipIcon.Error,
				_ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
			};
		}

		/// <summary>
		/// Shows an error dialog to the user.
		/// </summary>
		/// <param name="message">The error message to be displayed.</param>
		/// <param name="title">The window title of the error dialog.</param>
		/// <remarks>
		/// It might seem a bit inconsistent that some errors are sent as notifications, while others are
		/// displayed in a MessageBox using the ShowErrorWindow method below.
		/// The reasoning for this is explained here.
		/// ShowErrorWindow is used for any error that results from an action initiated by a user and 
		/// prevents that action for being completed successfully, as well as any error that forces the
		/// application to exit.
		/// Any other errors should be sent as notifications, which aren't as intrusive as an error dialog that
		/// forces you to stop doing whatever you were doing and click OK before you're allowed to continue.
		/// </remarks>
		public void ShowErrorWindow(string message, string title = "An error occurred.")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public bool ShowYesNoWindow(string message, string title, MessageBoxImage image = MessageBoxImage.None)
		{
			return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.None) == MessageBoxResult.Yes;
		}

		private void HandleDownloadUpdateClick(object sender, EventArgs e)
		{
			Process.Start(DownloadUpdateString);
		}

		public void HandleUpdateAvailable(UpdateAvailableEventArgs args)
		{
			Application.Current.Dispatcher.Invoke(() => HandleUpdateAvailableInternal(args));
		}

		private void HandleUpdateAvailableInternal(UpdateAvailableEventArgs args)
		{
			Helpers.AssertOnUiThread();

			downloadUpdate.Text += $" ({args.Version})";
			downloadUpdate.Visible = true;
			downloadSeparator.Visible = true;

			if (args.Version.Important &&
			    (ConfigManager.Config.Notifications.Types.UpdateAvailable ||
			     ConfigManager.Config.Notifications.Types.ImportantUpdateAvailable))
			{
				Raise($"An important vulnerability fix ({args.Version}) is available. Check the release for more information.", Severity.Info);
			}
			else if (ConfigManager.Config.Notifications.Types.UpdateAvailable)
			{
				if (args.Version.IsPrerelease)
				{
					Raise($"A new pre-release ({args.Version}) is available.", Severity.Info);
				}
				else
				{
					Raise($"A new update ({args.Version}) is available.", Severity.Info);
				}
			}
		}

		public void Dispose()
		{
			Icon.Dispose();
			downloadUpdate.Dispose();
			downloadSeparator.Dispose();
		}

		public void SetSyncState(SyncState state)
		{
			Icon.Icon = state switch
			{
				SyncState.UpToDate => EmbeddedResources.Icon,
				SyncState.Ahead => EmbeddedResources.IconAhead,
				SyncState.Behind => EmbeddedResources.IconBehind,
				SyncState.Diverged => EmbeddedResources.IconDiverged,
				_ => Icon.Icon
			};
		}
	}

	internal enum Severity
	{
		None,
		Info,
		Warning,
		Error,
	}
}
