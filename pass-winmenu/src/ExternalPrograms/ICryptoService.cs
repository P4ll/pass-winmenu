using System.Collections.Generic;

namespace PassWinmenu.ExternalPrograms
{
	internal interface ICryptoService
	{
		string Decrypt(string file);
		void Encrypt(string data, string outputFile, bool overwrite, params string[] recipients);
		List<string> GetRecipients(string file);
		string FindShortKeyId(string target);
	}
}
