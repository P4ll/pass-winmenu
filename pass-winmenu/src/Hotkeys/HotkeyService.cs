using System;
using System.Collections.Generic;
using PassWinmenu.Actions;
using PassWinmenu.Configuration;
using PassWinmenu.WinApi;

namespace PassWinmenu.Hotkeys
{
	internal class HotkeyService : IDisposable
	{
		private readonly List<IDisposable> registrations = new List<IDisposable>();
		private readonly IHotkeyRegistrar registrar;

		/// <summary>
		/// Create a new hotkey manager.
		/// </summary>
		public HotkeyService(IHotkeyRegistrar registrar)
		{
			this.registrar = registrar;
		}

		public void AssignHotkeys(IEnumerable<HotkeyConfig> config, ActionDispatcher actionDispatcher, INotificationService notificationService)
		{
			foreach (var hotkey in config)
			{
				var keys = KeyCombination.Parse(hotkey.Hotkey);
				HotkeyAction action;
				try
				{
					// Reading the Action variable will cause it to be parsed from hotkey.ActionString.
					// If this fails, an ArgumentException is thrown.
					action = hotkey.Action;
				}
				catch (ArgumentException)
				{
					notificationService.Raise($"Invalid hotkey configuration in config.yaml.\nThe action \"{hotkey.ActionString}\" is not known.", Severity.Error);
					continue;
				}
				try
				{
					switch (action)
					{
						case HotkeyAction.DecryptPassword:
							AddHotKey(keys, () => actionDispatcher.DecryptPassword(hotkey.Options.CopyToClipboard, hotkey.Options.TypeUsername, hotkey.Options.TypePassword));
							break;
						case HotkeyAction.GenerateTotpCode:
							AddHotKey(keys, () => actionDispatcher.GenerateTotpCode(hotkey.Options.CopyToClipboard, hotkey.Options.TypeTotpCode));
							break;
						case HotkeyAction.PasswordField:
							AddHotKey(keys, () => actionDispatcher.DecryptPasswordField(hotkey.Options.CopyToClipboard, hotkey.Options.Type, hotkey.Options.FieldName));
							break;
						case HotkeyAction.DecryptMetadata:
							AddHotKey(keys, () => actionDispatcher.DecryptMetadata(hotkey.Options.CopyToClipboard, hotkey.Options.Type));
							break;
						case HotkeyAction.AddPassword:
						case HotkeyAction.EditPassword:
						case HotkeyAction.ShowDebugInfo:
						case HotkeyAction.CheckForUpdates:
						case HotkeyAction.GitPull:
						case HotkeyAction.GitPush:
						case HotkeyAction.OpenShell:
							AddHotKey(keys, () => actionDispatcher.Dispatch(action));
							break;
						default:
							throw new HotkeyException("Invalid hotkey action");
					}
				}
				catch (HotkeyException e) when (e.InnerException?.HResult == HResult.HotkeyAlreadyRegistered)
				{
					throw new HotkeyException($"The hotkey \"{hotkey.Hotkey}\" is already registered by another application.");
				}
			}
		}

		public void Dispose()
		{
			foreach (var reg in registrations)
			{
				reg.Dispose();
			}
		}

		/// <summary>
		/// Register a new hotkey with Windows.
		/// </summary>
		/// <param name="keys">A KeyCombination object representing the keys to be pressed.</param>
		/// <param name="action">The action to be executed when the hotkey is pressed.</param>
		private void AddHotKey(KeyCombination keys, Action action)
		{
			var reg = registrar.Register(keys.ModifierKeys, keys.Key, (sender, args) =>
			{
				action.Invoke();
			});
			registrations.Add(reg);
		}
	}
}
