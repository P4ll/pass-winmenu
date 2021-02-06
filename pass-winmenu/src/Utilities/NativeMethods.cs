using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PassWinmenu.Utilities
{
	internal class NativeMethods
	{
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs,
			[MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
		 int cbSize);

		public static Process GetWindowProcess(IntPtr hWnd)
		{
			GetWindowThreadProcessId(hWnd, out uint pid);
			return Process.GetProcessById((int)pid);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct INPUT
	{
		internal INPUT_TYPE Type;
		internal KEYBDINPUT Data;
	}

	internal enum INPUT_TYPE : uint
	{
		INPUT_MOUSE = 0,
		INPUT_KEYBOARD = 1,
		INPUT_HARDWARE = 2
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct KEYBDINPUT
	{
		internal ushort KeyCode;
		internal ushort ScanCode;
		internal KEYEVENTF Flags;
		internal uint Time;
		internal IntPtr ExtraInfo;
		private uint padding;
		private uint padding_;
	}

	[Flags]
	internal enum KEYEVENTF : uint
	{
		EXTENDEDKEY = 0x0001,
		KEYUP = 0x0002,
		SCANCODE = 0x0008,
		UNICODE = 0x0004
	}
}
