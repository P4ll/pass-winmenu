using System.Collections.Generic;
using Moq;
using PassWinmenu.Configuration;
using PassWinmenu.ExternalPrograms.Gpg;
using PassWinmenuTests.Utilities;
using Shouldly;
using Xunit;

namespace PassWinmenuTests.ExternalPrograms.Gpg
{
	public class GpgTests
	{
		[Fact]
		public void Decrypt_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg(It.IsAny<string>(), null), Times.Once);
		}

		[Fact]
		public void Decrypt_PinentryFixEnabled_DoesNotThrow()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig
			{
				PinentryFix = true,
			});

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg(It.IsAny<string>(), null), Times.Once);
		}

		[Theory]
		[InlineData("password")]
		[InlineData("password\nline2")]
		[InlineData("password\r\nline2")]
		[InlineData("\npassword\r\n")]
		public void Decrypt_ReturnsFileContent(string fileContent)
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), null))
				.Returns(new GpgResultBuilder()
					.WithStdout(fileContent)
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var decryptedContent = gpg.Decrypt("file");

			decryptedContent.ShouldBe(fileContent);
		}

		[Fact]
		public void Decrypt_WithExtraOptions_IncludesOptionsInGpgCall()
		{
			var config = new GpgConfig
			{
				AdditionalOptions = new AdditionalOptionsConfig()
				{
					Always = new Dictionary<string, string>
					{
						{
							"verbose", ""
						}
					},
					Decrypt = new Dictionary<string, string>
					{
						{
							"try-secret-key", "mysecret"
						}
					}
				}
			};
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, config);

			gpg.Decrypt("file");

			transportMock.Verify(t => t.CallGpg("--verbose --try-secret-key \"mysecret\" --decrypt \"file\"", null), Times.Once);
		}

		[Fact]
		public void Encrypt_NoRecipients_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false);

			transportMock.Verify(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_NullRecipients_CallsGpg()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false, null);

			transportMock.Verify(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_Recipients_CallsGpgWithRecipients()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", false, "rcp_0", "rcp_1");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args =>
				args.Contains("rcp_0") && args.Contains("rcp_1")), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_Overwrite_CallsGpgWithYesOption()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsNotNull<string>(), It.IsNotNull<string>())).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.Encrypt("data", "file", true);

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args =>
				args.Contains("--yes")), It.IsNotNull<string>()), Times.Once);
		}

		[Fact]
		public void Encrypt_WithExtraOptions_IncludesOptionsInGpgCall()
		{
			var config = new GpgConfig
			{
				AdditionalOptions = new AdditionalOptionsConfig()
				{
					Always = new Dictionary<string, string>
					{
						{
							"verbose", ""
						}
					},
				}
			};
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(t => t.CallGpg(It.IsAny<string>(), null)).Returns(GetSuccessResult);
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, config);

			gpg.Encrypt("data", "file", true, "rcp_0");

			transportMock.Verify(t => t.CallGpg(It.Is<string>(args => args.Contains("--verbose")), It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public void StartAgent_CallsListSecretKeys()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("secret keys")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			gpg.StartAgent();

			transportMock.Verify(t => t.CallGpg(It.IsRegex("--list-secret-keys"), null), Times.Once);
		}

		[Fact]
		public void StartAgent_NoSecretKeys_ThrowsGpgError()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			Should.Throw<GpgError>(() => gpg.StartAgent());
		}

		[Fact]
		public void GetVersion_ReturnsFirstOutputLine()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("GPG version 1.0\r\nmore info")
					.Build());
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var version = gpg.GetVersion();

			version.ShouldBe("GPG version 1.0");
		}

		[Fact]
		public void GetRecipients_ReturnsRecipients()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("GPG version 1.0\r\nmore info")
					.WithStatusMessage(GpgStatusCode.ENC_TO, "user0 0 0")
					.WithStatusMessage(GpgStatusCode.ENC_TO, "user1 0 0")
					.Build()); ;
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var recipients = gpg.GetRecipients("testFile");

			recipients.ShouldBe(new[] { "user0", "user1" });
		}

		[Fact]
		public void FindShortKeyId_ReturnsKeyId()
		{
			var transportMock = new Mock<IGpgTransport>();
			transportMock.Setup(
					t => t.CallGpg(It.IsNotNull<string>(), It.IsAny<string>()))
				.Returns(new GpgResultBuilder()
					.WithStdout("uid:u::::1627027253::B3B2807670A468499DF1292C1140265C5D4B56E1::Test User <test@geluk.io>::::::::::0:" +
					"\r\nsub:u:3072:1:EDE97135FC244819:1627027253:1690099253:::::e::::::23:" +
					"\r\nfpr:::::::::63BAC0DAFD648D28BC675FF2EDE97135FC244819:")
					.Build()); ;
			var gpg = new GPG(transportMock.Object, StubGpgResultVerifier.AlwaysValid, new GpgConfig());

			var shortKeyId = gpg.FindShortKeyId("testFile");

			shortKeyId.ShouldBe("EDE97135FC244819");
		}

		private GpgResult GetSuccessResult()
		{
			return new GpgResultBuilder().Build();
		}

		private class StubGpgResultVerifier : IGpgResultVerifier
		{
			private readonly bool valid;

			private StubGpgResultVerifier(bool valid)
			{
				this.valid = valid;
			}

			public void VerifyDecryption(GpgResult result)
			{
				if (!valid)
				{
					throw new GpgError("Invalid result.");
				}
			}

			public void VerifyEncryption(GpgResult result)
			{
				if (!valid)
				{
					throw new GpgError("Invalid result.");
				}
			}

			public static IGpgResultVerifier AlwaysValid => new StubGpgResultVerifier(true);
		}
	}
}
