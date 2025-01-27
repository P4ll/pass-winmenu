using System.Collections.Generic;
using System.IO.Abstractions;

namespace PassWinmenu.PasswordManagement
{
	internal interface IPasswordManager
	{
		IDirectoryInfo PasswordStore { get; }

		IEnumerable<PasswordFile> GetPasswordFiles();

		KeyedPasswordFile DecryptPassword(PasswordFile file, bool passwordOnFirstLine);

		PasswordFile EncryptPassword(DecryptedPasswordFile file);
		
		PasswordFile AddPassword(string path, string password, string metadata);
	}
}
