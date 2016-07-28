using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class CertificateProviderTests
	{
		[TestCleanup]
		public void CleanCertificates()
		{
			if (File.Exists("_.proxy.com.ar"))
				File.Delete("_.proxy.com.ar");
			if (File.Exists("saved.certificate"))
				File.Delete("saved.certificate");
		}

		[TestMethod]
		public async Task Should_Create_Certificate_With_Private_Key()
		{
			var ca = X509CertificateFactory.LoadCertificate(CertificateProvider.RootCA);
			var provider = new BouncyCastleCertificateProvider(ca);
			var cert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(cert);
			Assert.AreEqual("CN=*.proxy.com.ar", cert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", cert.Issuer);
			Assert.IsTrue(cert.HasPrivateKey);
			VerifyChain(cert);
		}

		[TestMethod]
		public async Task Should_Save_And_Load_Certificate_With_Private_Key()
		{
			var ca = X509CertificateFactory.LoadCertificate(CertificateProvider.RootCA);
			var provider = new BouncyCastleCertificateProvider(ca);
			var createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			createdCert.Save("saved.certificate");
			var loadedCert = X509CertificateFactory.LoadCertificateFromFile("saved.certificate");
			Assert.IsNotNull(loadedCert);
			Assert.AreEqual("CN=*.proxy.com.ar", loadedCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", loadedCert.Issuer);
			Assert.IsTrue(loadedCert.HasPrivateKey);
			VerifyChain(loadedCert);
		}

		[TestMethod]
		public async Task Should_Store_Certificate_On_FileSystem()
		{
			var ca = X509CertificateFactory.LoadCertificate(CertificateProvider.RootCA);
			var provider = new StoredCertificateProvider(new BouncyCastleCertificateProvider(ca));
			var createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);

			createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);

			createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);
		}

		[TestMethod]
		public async Task Should_Store_Certificate_On_Cache()
		{
			var ca = X509CertificateFactory.LoadCertificate(CertificateProvider.RootCA);
			var provider = new CachedCertificateProvider(new BouncyCastleCertificateProvider(ca));
			var createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);

			createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);

			createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
			Assert.IsNotNull(createdCert);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
			Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
			Assert.IsTrue(createdCert.HasPrivateKey);
			VerifyChain(createdCert);
		}

		[TestMethod]
		public async Task Should_Be_Safe_in_Multithreading()
		{
			var ca = X509CertificateFactory.LoadCertificate(CertificateProvider.RootCA);
			var provider = new CachedCertificateProvider(
				new StoredCertificateProvider(
					new BouncyCastleCertificateProvider(ca)));

			var tasks = new Task[10];
			for (var i = 0; i < 10; i++)
			{
				tasks[i] = Task.Run(async () =>
				{
					var createdCert = await provider.GetCertificateForSubjectAsync("*.proxy.com.ar");
					Assert.IsNotNull(createdCert);
					Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Subject);
					Assert.AreEqual("CN=*.proxy.com.ar", createdCert.Issuer);
					Assert.IsTrue(createdCert.HasPrivateKey);
					VerifyChain(createdCert);
				});
			}
			Task.WaitAll(tasks);
		}

		private static void VerifyChain(X509Certificate2 cert)
		{
			var chain = new X509Chain
			{
				ChainPolicy = new X509ChainPolicy()
				{
					RevocationMode = X509RevocationMode.NoCheck,
				}
			};
			var chainBuilt = chain.Build(cert);
			Assert.IsTrue(chainBuilt);
		}
	}
}
