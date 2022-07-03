using System;

namespace PassWinmenu.Configuration
{
	public class RuntimeConfigurationException : Exception
	{
		public RuntimeConfigurationException(string message) : base(message)
		{
		}
	}
}
