using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;
using Org.BouncyCastle.Asn1.X509;

namespace Open.HttpProxy
{
	public interface ICertificateProvider
	{
		Task<X509Certificate2> GetCertificateForSubjectAsync(string subject);
	}

	public class CertificateProvider : ICertificateProvider
	{
		public const string RootCA = "Open.HttpProxy Root CA";
		public static readonly ICertificateProvider Default;

		static CertificateProvider()
		{
			//var rootCA = X509CertificateFactory.CreateCertificateAuthorityCertificate($"CN={RootCA}", null, null);
			//X509CertificateFactory.SaveCertificateToWindowsStore(rootCA);
			Default = new CachedCertificateProvider(
				new StoredCertificateProvider(
				new BouncyCastleCertificateProvider(
					X509CertificateFactory.LoadCertificate(RootCA))));
		}

		private CertificateProvider()
		{
		}

		public async Task<X509Certificate2> GetCertificateForSubjectAsync(string hostname)
		{
			return await Default.GetCertificateForSubjectAsync(hostname).WithoutCapturingContext();
		}
	}

	public class CachedCertificateProvider : ICertificateProvider
	{
		private readonly ICertificateProvider _provider;
		private readonly Dictionary<string, X509Certificate2> _certServerCache = new Dictionary<string, X509Certificate2>();
		private readonly ReaderWriterLock _oRwLock = new ReaderWriterLock();
		private static readonly SemaphoreSlim SemaphoreLock = new SemaphoreSlim(1);

		public CachedCertificateProvider(ICertificateProvider provider)
		{
			_provider = provider;
		}

		public async Task<X509Certificate2> GetCertificateForSubjectAsync(string domain)
		{
			try
			{
				var cn = "CN=" + domain;
				if (_certServerCache.ContainsKey(cn))
				{
					return _certServerCache[cn];
				}

				await SemaphoreLock.WaitAsync().WithoutCapturingContext();

				if (_certServerCache.ContainsKey(cn))
				{
					HttpProxy.Trace.TraceInformation($"Certificate for {domain} got from cache");
					return _certServerCache[cn];
				}

				var x509Certificate = await _provider.GetCertificateForSubjectAsync(domain).WithoutCapturingContext();
				if (x509Certificate == null)
				{
					HttpProxy.Trace.TraceEvent(TraceEventType.Error, 0, $"Certificate for {domain} is null");
				}
				else
				{
					_certServerCache["CN=" + domain] = x509Certificate;
					Console.WriteLine(" generated & cached");
					HttpProxy.Trace.TraceInformation($"Certificate for {domain} generated & cached");
				}

				return x509Certificate;
			}
			finally
			{
				SemaphoreLock.Release();
			}
		}
	}

	public class StoredCertificateProvider : ICertificateProvider
	{
		private readonly ICertificateProvider _provider;

		public StoredCertificateProvider(ICertificateProvider provider)
		{
			_provider = provider;
		}

		public async Task<X509Certificate2> GetCertificateForSubjectAsync(string domain)
		{
			var fileName = SanityFileName(domain);
			if (File.Exists(fileName))
			{
				return X509CertificateFactory.LoadCertificateFromFile(fileName);
			}

			var x509Certificate = await _provider.GetCertificateForSubjectAsync(domain).WithoutCapturingContext();
			x509Certificate.Save(fileName);
			return x509Certificate;
		}

		private static string SanityFileName(string name)
		{
			var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return Regex.Replace(name, invalidRegStr, "_");
		}

	}

	public class BouncyCastleCertificateProvider : ICertificateProvider
	{
		public X509Certificate2 CertificateAuthorityCert { get; }

		public BouncyCastleCertificateProvider(X509Certificate2 certificateAuthorityCert)
		{
			CertificateAuthorityCert = certificateAuthorityCert;
		}

		public async Task<X509Certificate2> GetCertificateForSubjectAsync(string hostname)
		{
			Console.WriteLine($"!!!! CREATING {hostname} cert");
			return await Task.Run(()=> X509CertificateFactory.IssueCertificate(
				$"CN={hostname}", CertificateAuthorityCert, null, new [] {KeyPurposeID.IdKPServerAuth}))
				.WithoutCapturingContext();
		}
	}
}