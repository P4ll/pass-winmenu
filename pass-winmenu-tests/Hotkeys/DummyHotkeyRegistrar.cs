using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PassWinmenu.Hotkeys;
using PassWinmenu.Utilities;

namespace PassWinmenuTests.Hotkeys
{
	/// <summary>
	/// A dummy, manipulable <see cref="IHotkeyRegistrar"/> for use in testing.
	/// </summary>
	public sealed class DummyHotkeyRegistrar
		: IHotkeyRegistrar
	{
		private readonly IDictionary<(ModifierKeys, Key), EventHandler> hotkeys;


		/// <summary>
		/// Creates a new dummy hotkey registrar.
		/// </summary>
		public DummyHotkeyRegistrar()
		{
			var dict = new Dictionary<(ModifierKeys, Key), EventHandler>();

			hotkeys = dict;
			Hotkeys = dict;
		}


		/// <summary>
		/// Whether requests to the registrar are to succeed.
		/// </summary>
		public bool WillSucceed { get; set; } = true;
		/// <summary>
		/// Whether the registrar will accept a request to register multiple
		/// hotkeys with the same key combinations.
		/// </summary>
		/// <remarks>
		/// Changing this property will not affect hotkeys which are already
		/// registered with the registrar.
		/// </remarks>
		public bool AllowMulticast { get; set; } = true;

		/// <summary>
		/// All hotkeys registered with the dummy registrar.
		/// </summary>
		public IReadOnlyDictionary<(ModifierKeys, Key), EventHandler> Hotkeys
		{
			get;
		}

		/// <summary>
		/// Occurs when an <see cref="IDisposable"/> provided to unregister a
		/// hotkey is disposed.
		/// </summary>
		public event EventHandler<(ModifierKeys, Key)> Disposal;


		/// <summary>
		/// Convenience method for triggering all hotkeys registered with the
		/// dummy registrar.
		/// </summary>
		public void FireAll() => Hotkeys.Values.ToList()
												.ForEach(eh => eh(this, null));


		IDisposable IHotkeyRegistrar.Register(ModifierKeys modifierKeys, Key key, EventHandler firedHandler)
		{
			// Fail if we're configured to do so
			if (!WillSucceed)
			{
				throw new HotkeyException(
					"The dummy hotkey registrar is configured to fail."
					);
			}

			// If multicast hotkeys are disabled, check that a hotkey with the
			// specified combination is not already present.
			if (!AllowMulticast && Hotkeys.ContainsKey((modifierKeys, key)))
			{
				throw new HotkeyException(
					"Multicast hotkeys are disabled and a hotkey is already " +
					"registered with this key combination."
					);
			}

			// If multicast hotkeys are enabled and a hotkey with the same
			// combination is already registered, use its registered delegate
			// as a multicast delegate to call all handlers at once.
			if (!Hotkeys.TryGetValue((modifierKeys, key), out var handler))
			{
				handler += firedHandler;
			}
			// Otherwise, we need to use the provided handler as the new value
			// to be inserted into the dictionary.
			else
			{
				handler = firedHandler;
			}

			// Delegates are immutable, so creating a multicast delegate from
			// one already registered will not affect the registered copy. We
			// need to explicitly replace it in the dictionary.
			hotkeys[(modifierKeys, key)] = handler;

			return new Disposable(() =>
			{
				// We don't need to do what we did above as we know that the
				// dictionary will always contain a delegate for us to work
				// with, and because we assign the newly-created multicast
				// delegate right back to the dictionary member.
				var h = hotkeys[(modifierKeys, key)] -= firedHandler;

				// Removing the last delegate from a multicast delegate causes
				// the value of that multicast delegate to be null, so we'll 
				// want to remove it from the dictionary to prevent an NRE.
				if (h == null)
				{
					hotkeys.Remove((modifierKeys, key));
				}

				Disposal?.Invoke(this, (modifierKeys, key));
			});
		}
	}
}
