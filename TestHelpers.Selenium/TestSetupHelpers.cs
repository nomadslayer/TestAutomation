using System;
using System.Diagnostics.Contracts;
using System.Net;

namespace TestHelpers.Selenium
{
	public static class TestSetupHelpers
	{
		public static void DisableX509CertificateValidation()
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
		}

		/// <summary>
		/// Restores X.509 certificate validation in the current <see cref="AppDomain"/>.
		/// </summary>
		public static void RestoreX509CertificateValidation()
		{
			ServicePointManager.ServerCertificateValidationCallback = null;
		}
	}
}
