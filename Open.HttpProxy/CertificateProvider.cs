using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	public static class CertificateProvider
	{
		private static readonly CertificateCache Cache = new CertificateCache();

		public static async Task<X509Certificate2> GetCertificateForHost(string hostname)
		{
			X509Certificate2 cert;
			if (Cache.TryGet(hostname, out cert)) return cert;

			var x509Certificate = LoadCertificateFromWindowsStore(hostname);
			if (x509Certificate == null)
			{
				x509Certificate = await CreateCertificate(hostname);
				Cache.Put(hostname, x509Certificate);
			}
			return x509Certificate;
		}


		private static X509Certificate2 LoadCertificateFromWindowsStore(string hostname)
		{
			var x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			x509Store.Open(OpenFlags.ReadOnly);
			var inStr = $"CN={hostname}";

			try
			{
				foreach (var certificate in x509Store.Certificates)
				{
					if (inStr.Equals(certificate.Subject))
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

		private static Task<X509Certificate2> CreateCertificate(string hostname)
		{
			//makecert -r -ss root -n "CN=DigiTrust Global Assured ID Root, OU=www.digitrust.com, O=DigiTrust Inc, C=US" -sky signature -eku 1.3.6.1.5.5.7.3.1 -h 1 -cy authority -a sha256 -m 60
			//makecert -pe -ss my -n "CN=*.google.com" -sky exchange -in "DigiTrust Global Assured ID Root" -is root -eku 1.3.6.1.5.5.7.3.1 -cy end -a sha256 -m 60

			var isRoot =false;
			var makecertArgs = isRoot
				? "-r -ss root -n \"CN=DigiTrust Global Assured ID Root, OU=www.digitrust.com, O=DigiTrust Inc, C=US\" -sky signature -eku 1.3.6.1.5.5.7.3.1 -h 1 -cy authority -a sha256 -m 60"
				: $"-pe -ss my -n \"CN={hostname}\" -sky exchange -in \"DigiTrust Global Assured ID Root\" -is root -eku 1.3.6.1.5.5.7.3.1 -cy end -a sha256 -m 60";

			var tcs = new TaskCompletionSource<X509Certificate2>();

			var process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = false;
			process.StartInfo.RedirectStandardError = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;
			process.StartInfo.FileName = "makecert.exe";
			process.StartInfo.Arguments = makecertArgs;
			process.Start();
			process.Exited += (s, e) =>
			{
				X509Certificate2 x509Certificate;

				var num3 = 6;
				do
				{
					x509Certificate = LoadCertificateFromWindowsStore(hostname);
					Thread.Sleep(50 * (6 - num3));
					num3--;
				}
				while (x509Certificate == null && num3 >= 0);
				tcs.SetResult(x509Certificate);
			};

			return tcs.Task;
		}
	}
}