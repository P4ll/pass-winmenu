using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PassWinmenu.Utilities;

namespace PassWinmenu.WinApi
{
	static class KeyboardEmulator
	{
		private static readonly SendKeysEscapeGenerator escapeGenerator = new SendKeysEscapeGenerator();

		/// <summary>
		/// Sends text directly to the topmost window, as if it was entered by the user.
		/// This method automatically escapes all characters with special meaning, 
		/// then calls SendKeys.Send().
		/// </summary>
		/// <param name="text">The text to be sent to the active window.</param>
		/// <param name="escapeDeadKeys">Whether dead keys should be escaped or not. 
		/// If true, inserts a space after every dead key in order to prevent it from being combined with the next character.</param>
		internal static void EnterText(string text, bool escapeDeadKeys)
		{
			var inputs = new List<INPUT>();
			foreach (var ch in text)
			{
				ushort scanCode = ch;
				var down = new INPUT
				{
					Type = INPUT_TYPE.INPUT_KEYBOARD,
					Data = new KEYBDINPUT
					{
						KeyCode = 0,
						ScanCode = scanCode,
						Flags = KEYEVENTF.UNICODE,
						Time = 0,
						ExtraInfo = IntPtr.Zero
					}
				};
				var up = new INPUT
				{
					Type = INPUT_TYPE.INPUT_KEYBOARD,
					Data = new KEYBDINPUT
					{
						KeyCode = 0,
						ScanCode = scanCode,
						Flags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP,
						Time = 0,
						ExtraInfo = IntPtr.Zero
					}
				};
				// Handle extended keys:
				// If the scan code is preceded by a prefix byte that has the value 0xE0 (224),
				// we need to include the EXTENDEDKEY flag in the Flags property.
				if ((scanCode & 0xFF00) == 0xE000)
				{
					down.Data.Flags |= KEYEVENTF.EXTENDEDKEY;
					up.Data.Flags |= KEYEVENTF.EXTENDEDKEY;
				}

				inputs.Add(down);
				inputs.Add(up);
			}

			var size = Marshal.SizeOf(typeof(INPUT));
			var success = NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), size);
			if (success != inputs.Count)
			{
				var exc = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
				throw exc;
			}
		}

		internal static void EnterRawText(string text)
		{
			SendKeys.SendWait(text);
		}
	}
}
