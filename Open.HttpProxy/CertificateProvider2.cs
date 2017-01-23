using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using X509Certificates = System.Security.Cryptography.X509Certificates;

namespace Open.HttpProxy
{
	internal static class X509CertificateFactory
	{
		public static X509Certificates.X509Certificate2 LoadCertificateFromFile(string issuerFileName)
		{
			var bytes = File.ReadAllBytes(issuerFileName);
			var issuerCertificate = new X509Certificates.X509Certificate2(bytes, "", X509Certificates.X509KeyStorageFlags.Exportable);
			//var issuerCertificate1 = new X509Certificates.X509Certificate2(bytes, "");
			//var issuerCertificate2 = new X509Certificates.X509Certificate2(bytes, "", X509Certificates.X509KeyStorageFlags.MachineKeySet | X509Certificates.X509KeyStorageFlags.PersistKeySet | X509Certificates.X509KeyStorageFlags.Exportable);
			//	issuerCertificate.Import(bytes, "", X509Certificates.X509KeyStorageFlags.MachineKeySet | X509Certificates.X509KeyStorageFlags.PersistKeySet | X509Certificates.X509KeyStorageFlags.Exportable);
			return issuerCertificate;

		}

		public static X509Certificates.X509Certificate2 LoadCertificate(string hostname)
		{
			var x509Store = new X509Certificates.X509Store(X509Certificates.StoreName.Root, X509Certificates.StoreLocation.CurrentUser);
			x509Store.Open(X509Certificates.OpenFlags.ReadOnly);
			var inStr = $"CN={hostname}";

			try
			{
				foreach (var certificate in x509Store.Certificates)
				{
					if (certificate.Subject.StartsWith(inStr))
					{
						return certificate;
					}
				}
			}
			finally
			{
				x509Store.Close();
			}
			return null;
		}

		public static void SaveCertificateToWindowsStore(X509Certificates.X509Certificate2 cert)
		{
			var x509Store = new X509Certificates.X509Store(X509Certificates.StoreName.Root, X509Certificates.StoreLocation.CurrentUser);
			x509Store.Open(X509Certificates.OpenFlags.ReadWrite);

			try
			{
				x509Store.Add(cert);
			}
			finally
			{
				x509Store.Close();
			}
		}

		public static X509Certificates.X509Certificate2 IssueCertificate(
			string subjectName,
			X509Certificates.X509Certificate2 issuerCertificate,
			string[] subjectAlternativeNames, 
			KeyPurposeID[] usages)
		{
			var random = new SecureRandom(new CryptoApiRandomGenerator());
			var issuerName = issuerCertificate.FriendlyName;
			var subjectKeyPair = GenerateKeyPair(random, 2048);
			var issuerKeyPair = DotNetUtilities.GetKeyPair(issuerCertificate.PrivateKey);

			var subjectSerialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			var issuerSerialNumber = new BigInteger(issuerCertificate.GetSerialNumber());

			var certificate = GenerateCertificate(random,
				subjectName, subjectKeyPair, subjectSerialNumber, subjectAlternativeNames,
				issuerName, issuerKeyPair, issuerSerialNumber, false, usages);
			return certificate.ToX509Certificate2(subjectKeyPair, random);
		}

		public static X509Certificates.X509Certificate2 CreateCertificateAuthorityCertificate(
			string subjectName,
			string[] subjectAlternativeNames, 
			KeyPurposeID[] usages)
		{
			var random = new SecureRandom(new CryptoApiRandomGenerator());
			var issuerName = subjectName;
			var subjectKeyPair = GenerateKeyPair(random, 2048);
			var issuerKeyPair = subjectKeyPair;

			var subjectSerialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			var issuerSerialNumber = subjectSerialNumber;

			var certificate = GenerateCertificate(random,
				subjectName, subjectKeyPair, subjectSerialNumber, subjectAlternativeNames,
				issuerName, issuerKeyPair, issuerSerialNumber, true, usages);
			return certificate.ToX509Certificate2(subjectKeyPair, random);
		}

		private static X509Certificate GenerateCertificate(
			SecureRandom random,
			string subjectName,
			AsymmetricCipherKeyPair subjectKeyPair,
			BigInteger subjectSerialNumber,
			string[] subjectAlternativeNames,
			string issuerName,
			AsymmetricCipherKeyPair issuerKeyPair,
			BigInteger issuerSerialNumber,
			bool isCertificateAuthority,
			KeyPurposeID[] usages)
		{
			var certificateGenerator = new X509V3CertificateGenerator();

			certificateGenerator.SetSerialNumber(subjectSerialNumber);

			var issuerDN = new X509Name(issuerName);
			certificateGenerator.SetIssuerDN(issuerDN);

			var subjectDN = new X509Name(subjectName);
			certificateGenerator.SetSubjectDN(subjectDN);

			var notBefore = DateTime.UtcNow.Date;
			var notAfter = notBefore.AddYears(1);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// The subject's public key goes in the certificate.
			certificateGenerator.SetPublicKey(subjectKeyPair.Public);

			AddAuthorityKeyIdentifier(certificateGenerator, issuerDN, issuerKeyPair, issuerSerialNumber);
			AddSubjectKeyIdentifier(certificateGenerator, subjectKeyPair);
			certificateGenerator.AddExtension(X509Extensions.BasicConstraints.Id, true, 
				new BasicConstraints(isCertificateAuthority));

			if (usages != null && usages.Any())
			{
				certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage.Id, false, 
					new ExtendedKeyUsage(usages));
			}

			if (subjectAlternativeNames != null && subjectAlternativeNames.Any())
			{
				AddSubjectAlternativeNames(certificateGenerator, subjectAlternativeNames);
			}

			var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", issuerKeyPair.Private, random);
			var certificate = certificateGenerator.Generate(signatureFactory);
			return certificate;
		}

		private static AsymmetricCipherKeyPair GenerateKeyPair(SecureRandom random, int strength)
		{
			//var keyGenerationParameters = new KeyGenerationParameters(random, strength);

			//var keyPairGenerator = new RsaKeyPairGenerator();
			//keyPairGenerator.Init(keyGenerationParameters);
			//var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
			//return subjectKeyPair;
			var modulus =
				new BigInteger(
					"bba11d11d4ade8a63bc4d0b9fc7fc8322d233bfb76992f849f00e14ce59e297767cc2679f3f50260f8e1b4ad591035433c341ff3c4c75deb07d0e4c22963f375979bb45adc2c7be7318940bd9cb62872d8f4487e9350f1c11f4eba0032f1b1d1494c7b8435e11901ca71efece299bdf6d39e534d848da4894651cf106ce2bf5f6be067ccf90c637b66f810011d2e070f89eb2b56b4628bc5fb99d87efa720c62f215d4809faea2945cb60b8631c1eefc094fa83a11e15096cf10099faccb5fb509f34632438d5a4450c410053bb818866bde8c581f47aca8594da5fc63eb85e77e7f9c32def167d06fc16adb51de1383373a87b0e9fa0d2426e79e1b5ba6e353", 16);
			var publicExponent = new BigInteger("10001", 16);
			var privateExponent = new BigInteger("2d6461f3015ffd6bd203e0774e55dfa9fbef8d405dc5db901b238c5e1d22f16738ce53f4a7077c46d78f4b35b0d951f96d243322b755802c2f1ce36af1b6839ccc4bd80b2f3ef7cecbd627ba77a23e10e2d9de7bf8c9d962de5f1e7293e093234db75a1e772678f54cf3d996968f993c909fba0983eafc865563cf536af44455b78bc38b886bd8f6a9da4d301a4aef08df1cf61a591a937fba226998cebbbce3f575f167d90d119bd6f509a3cfe1322263d0587e17f95feb243b49d3f0a023b865e9932469762c5710ce15886cb85235ccebbe910dec765338566f83a65503169653419cb80461b964a0b51037f467259a2f5d45d8cff9b1bd8e09801d5b0ba1", 16);
			var dP = new BigInteger("75f2a64e03cf0d6a3318ab6ba91c6ae0f4ca19999575bd48f413511f495dc11791386598d95908f7902530cd802d74fc6ea8fbdaa82cb964e93dcc5b2623dd91dd9dc232b9d88c85a1fffb3c3f27ba0ef0192a51b390c2d3eb17d2f1553ed8196cf57ca8d4d7bd53e8c9d1db2ee7c5f15b4c26eb170d1844360a83cccf7630c1", 16);
			var dQ = new BigInteger("5a3e3194fc26f69728e10d2fdc246bd7dc8e6fb9e7e95a071b5b4c9b1e849943d2755f925e5a502b294a8f40dc655175d38d0543b98a4e95171c9e2c9f8723af4fbcb5cfc05694a906e6a0c63d8d7510becbab9c6fd0a1e5f0a78918be264dca407bdf035cf43b5b12f10b81ccd0c21083485b2dace59e5cf13a10060bc596c7", 16);
			var qInv = new BigInteger("2292781d7d6e92ae70160a7993b37defa9bacba95a53086fbc12d079cf5b427f8e6edd502fd89e700cfedcc98b857d110642188bd55c5c158d9ba67d04596c2a3fe6716e3c0f528fe2ada724226385fefe7b23742b61a9a87df642c048ff723419b7e8ba2af2c894af334906de0af17a709af47922f4ddf5624cf1cffd94eb41", 16);
			var P = new BigInteger("f8ffefa50b286ad132a803fe743dc819db9bf990b30a154c3d9607149208fe08222c30d427558ce2c04741a15701ca727d52c7fcdddadf0f9fb4718b2fa7e4bc3dbee29d2d0d587c251f36e3c04e0db5ff9ae2f6a5ce1cde2c3b39c45d26fff8200d899182dba28ca266cdefb623120d66fe343e20b91bd1475645a8e11e0d31", 16);
			var Q = new BigInteger("c0e77dd5ae2fe6bbf3be7cb48946619ce3ce09568b3862ef00b68fec2fcae48a67e64c99967232a126192bd2e8545981019a1677ad7c85f602cd9a3107387f4e79dbdb9925a2c5f09517da5da7c5b4c094cc86b3a29f9d535412268fedf4cb3dd55983e5ae48cc10eb5e4ca0388898613cd61cc1e2b8248f83cfd092ca2e87c3", 16);
			return new AsymmetricCipherKeyPair((AsymmetricKeyParameter)new RsaKeyParameters(false, modulus, publicExponent), (AsymmetricKeyParameter)new RsaPrivateCrtKeyParameters(modulus, publicExponent, privateExponent, P, Q, dP, dQ, qInv));

		}

		/// <summary>
		/// Add the Authority Key Identifier. According to http://www.alvestrand.no/objectid/2.5.29.35.html, this
		/// identifies the public key to be used to verify the signature on this certificate.
		/// In a certificate chain, this corresponds to the "Subject Key Identifier" on the *issuer* certificate.
		/// The Bouncy Castle documentation, at http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation,
		/// shows how to create this from the issuing certificate. Since we're creating a self-signed certificate, we have to do this slightly differently.
		/// </summary>
		/// <param name="certificateGenerator"></param>
		/// <param name="issuerDN"></param>
		/// <param name="issuerKeyPair"></param>
		/// <param name="issuerSerialNumber"></param>
		private static void AddAuthorityKeyIdentifier(
			X509V3CertificateGenerator certificateGenerator,
			X509Name issuerDN,
			AsymmetricCipherKeyPair issuerKeyPair,
			BigInteger issuerSerialNumber)
		{
			var authorityKeyIdentifierExtension =
				new AuthorityKeyIdentifier(
					SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerKeyPair.Public),
					new GeneralNames(new GeneralName(issuerDN)),
					issuerSerialNumber);
			certificateGenerator.AddExtension(
				X509Extensions.AuthorityKeyIdentifier.Id, false, authorityKeyIdentifierExtension);
		}

		private static void AddSubjectAlternativeNames(X509V3CertificateGenerator certificateGenerator,
			IEnumerable<string> subjectAlternativeNames)
		{
			var subjectAlternativeNamesExtension = new DerSequence(subjectAlternativeNames
				.Select(name => new GeneralName(GeneralName.DnsName, name))
				.ToArray<Asn1Encodable>());

			certificateGenerator.AddExtension(
				X509Extensions.SubjectAlternativeName.Id, false, subjectAlternativeNamesExtension);
		}

		private static void AddSubjectKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
			AsymmetricCipherKeyPair subjectKeyPair)
		{
			var subjectKeyIdentifierExtension = new SubjectKeyIdentifier(
				SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(subjectKeyPair.Public));
			certificateGenerator.AddExtension(
				X509Extensions.SubjectKeyIdentifier.Id, false, subjectKeyIdentifierExtension);
		}
	}

	static class X509CertificateExtensions
	{
		public static void Save(this X509Certificates.X509Certificate2 certificate, string fileName)
		{
			var bytes = certificate.Export(X509Certificates.X509ContentType.Pfx, "");
			File.WriteAllBytes(fileName, bytes);
		}

		public static X509Certificates.X509Certificate2 ToX509Certificate2(
			this X509Certificate certificate,
			AsymmetricCipherKeyPair subjectKeyPair,
			SecureRandom random)
		{
			var store = new Pkcs12Store();
			var friendlyName = certificate.SubjectDN.ToString();

			// Add the certificate.
			var certificateEntry = new X509CertificateEntry(certificate);
			store.SetCertificateEntry(friendlyName, certificateEntry);

			// Add the private key.
			store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { certificateEntry });

			// Convert it to an X509Certificate2 object by saving/loading it from a MemoryStream.
			// It needs a password. Since we'll remove this later, it doesn't particularly matter what we use.
			const string password = "password";
			X509Certificates.X509Certificate2 convertedCertificate;
			using (var stream = new MemoryStream())
			{
				store.Save(stream, password.ToCharArray(), random);

				convertedCertificate = new X509Certificates.X509Certificate2(stream.ToArray(),
					password,
					//					X509Certificates.X509KeyStorageFlags.DefaultKeySet);
					X509Certificates.X509KeyStorageFlags.MachineKeySet | X509Certificates.X509KeyStorageFlags.PersistKeySet | X509Certificates.X509KeyStorageFlags.Exportable);
			}
			return convertedCertificate;
		}
	}
}