using System.IO;
using PassWinmenu.Configuration;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.Configuration
{
	public class ConfigManagerTests
	{
		[Fact]
		public void Load_EmptyFile_NeedsUpgrade()
		{
			var tempFile = Path.GetTempFileName();
			var result = ConfigManager.Load(tempFile);
			File.Delete(tempFile);

			result.ShouldBe(LoadResult.NeedsUpgrade);
		}

		[Fact]
		public void Load_NonexistentFile_Created()
		{
			var tempFile = Path.GetTempFileName();
			File.Delete(tempFile);

			var result = ConfigManager.Load(tempFile);
			File.Delete(tempFile);

			result.ShouldBe(LoadResult.NewFileCreated);
		}
	}
}
