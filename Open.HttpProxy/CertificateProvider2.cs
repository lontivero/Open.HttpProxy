using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
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
			var issuerCertificate1 = new X509Certificates.X509Certificate2(bytes, "");
			var issuerCertificate2 = new X509Certificates.X509Certificate2(bytes, "", X509Certificates.X509KeyStorageFlags.MachineKeySet | X509Certificates.X509KeyStorageFlags.PersistKeySet | X509Certificates.X509KeyStorageFlags.Exportable);
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
			var keyGenerationParameters = new KeyGenerationParameters(random, strength);

			var keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
			return subjectKeyPair;
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