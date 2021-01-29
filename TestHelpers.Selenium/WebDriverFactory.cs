using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;

namespace TestHelpers.Selenium
{
	/// <summary>
	/// A class representing a factory for creating instances of <see cref="IWebDriver"/>.
	/// </summary>
	public class WebDriverFactory : IWebDriverFactory
	{
		/// <summary>
		/// The synchronization object to test for the existence of drivers.
		/// </summary>
		private static readonly object _binaryLock = new object();

		/// <summary>
		/// Whether Internet Explorer 11 (or later) is installed.
		/// </summary>
		private static bool? _isIE11OrLaterInstalled;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebDriverFactory"/> class.
		/// </summary>
		public WebDriverFactory()
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="IWebDriver" />.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The <see cref="TestContext" /> associated with the current test.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		public virtual IWebDriver Create(WebDriverFactoryOptions options, TestContext context)
		{
			if (options == null)
			{
				throw new ArgumentNullException("options");
			}

			IWebDriver webDriver;

			webDriver = CreateLocalDriver(options, context);
			
			try
			{
				if (options.ImplicitWait.HasValue)
				{
					webDriver
						.Manage()
						.Timeouts();
				}

				return webDriver;
			}
			catch (Exception)
			{
				webDriver.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an instance of <see cref="IWebDriver"/> for using a local browser.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The <see cref="TestContext" /> associated with the current test.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="options"/> is <see langword="null"/>.
		/// </exception>
		protected virtual IWebDriver CreateLocalDriver(WebDriverFactoryOptions options, TestContext context)
		{
			if (options == null)
			{
				throw new ArgumentNullException("options");
			}

			switch (options.Browser)
			{
				case WebBrowserType.AppleSafari:
					return CreateSafariDriver(options, context);

				case WebBrowserType.Firefox:
					return CreateFirefoxDriver(options, context);

				case WebBrowserType.GoogleChrome:
					return CreateChromeDriver(options, context);

				case WebBrowserType.InternetExplorer:
					return CreateIEDriver(options, context);

				case WebBrowserType.MicrosoftEdge:
					throw new NotSupportedException("Microsoft Edge is not currently supported for local testing.");

				case WebBrowserType.Android:
				case WebBrowserType.IPad:
				case WebBrowserType.IPhone:
				case WebBrowserType.WindowsPhone:
					throw new NotSupportedException("Mobile devices are not currently supported for local testing.");

				default:
					throw new NotSupportedException(SRHelper.Format(SR.WebDriverFactory_InvalidBrowserTypeFormat, options.Browser));
			}
		}

		/// <summary>
		/// Creates an <see cref="IWebDriver"/> for use with Google Chrome.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The test context.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver"/>.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Reliability",
			"CA2000:Dispose objects before losing scope",
			Justification = "Control is passed to the returned object.")]
		protected virtual IWebDriver CreateChromeDriver(WebDriverFactoryOptions options, TestContext context)
		{
			string downloadDirectory = context == null ? null : UITestHelpers.GetDownloadPath(WebBrowserType.GoogleChrome, context);

			ChromeOptions chromeOptions = CreateChromeOptions(downloadDirectory);

			EnsureChromeDriverPresent();

			if (options != null && options.CommandTimeout.HasValue)
			{
				// Based on the private code of the constructor used if there is no command timeout
				// (https://github.com/SeleniumHQ/selenium/blob/master/dotnet/src/webdriver/Chrome/ChromeDriver.cs)
				var service = ChromeDriverService.CreateDefaultService();

				try
				{
					return new ChromeDriver(
						service,
						chromeOptions,
						options.CommandTimeout.Value);
				}
				catch (Exception)
				{
					service.Dispose();
					throw;
				}
			}
			else
			{
				return new ChromeDriver(chromeOptions);
			}
		}

		/// <summary>
		/// Creates a <see cref="ChromeOptions"/> for use with Google Chrome.
		/// </summary>
		/// <param name="downloadDirectory">The download directory to use, if any.</param>
		/// <returns>
		/// The created instance of <see cref="ChromeOptions"/>.
		/// </returns>
		protected virtual ChromeOptions CreateChromeOptions(string downloadDirectory)
		{
			Dictionary<string, object> preferences = new Dictionary<string, object>()
			{
				 { "download.prompt_for_download", false },
			};

			if (downloadDirectory != null)
			{
				preferences["download.default_directory"] = downloadDirectory;
			}

			var options = new ChromeOptions();

			// Ensure we always start Chrome using US English
			options.AddArgument("--lang=en-US");

			return options;
		}

		/// <summary>
		/// Creates an <see cref="IWebDriver"/> for use with Mozilla Firefox.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The test context.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver"/>.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Reliability",
			"CA2000:Dispose objects before losing scope",
			Justification = "We don't want to dispose of binary as we are returning a driver that uses it.")]
		protected virtual IWebDriver CreateFirefoxDriver(WebDriverFactoryOptions options, TestContext context)
		{
			string downloadDirectory = context == null ? null : UITestHelpers.GetDownloadPath(WebBrowserType.Firefox, context);

			FirefoxDriverService service = FirefoxDriverService.CreateDefaultService(TestHelpers.Selenium.TestConfig.FirefoxCreateDefaultServicePath);
			return new FirefoxDriver(service);
		}

		/// <summary>
		/// Creates a <see cref="FirefoxProfile"/> for use with Mozilla Firefox.
		/// </summary>
		/// <param name="downloadDirectory">The download directory to use, if any.</param>
		/// <returns>
		/// The created instance of <see cref="FirefoxProfile"/>.
		/// </returns>
		protected virtual FirefoxProfile CreateFirefoxProfile(string downloadDirectory)
		{
			FirefoxProfile profile = new FirefoxProfile();

			////profile.AcceptUntrustedCertificates = true;

			// Ensure we always have Firefox using US English
			profile.SetPreference("intl.accept_languages", "en-US");

			if (downloadDirectory != null)
			{
				profile.SetPreference("browser.download.downloadDir", downloadDirectory);
				profile.SetPreference("browser.download.dir", downloadDirectory);
			}

			profile.SetPreference("browser.download.folderList", 2);
			profile.SetPreference("browser.download.manager.addToRecentDocs", false);
			profile.SetPreference("browser.download.manager.alertOnEXEOpen", false);
			profile.SetPreference("browser.download.manager.closeWhenDone", true);
			profile.SetPreference("browser.download.manager.focusWhenStarting", false);
			profile.SetPreference("browser.download.manager.retention", 0);
			profile.SetPreference("browser.download.manager.scanWhenDone", false);
			profile.SetPreference("browser.download.manager.showAlertOnComplete", false);
			profile.SetPreference("browser.download.manager.useWindow", false);
			profile.SetPreference("browser.download.useDownloadDir", true);
			profile.SetPreference("browser.helperApps.alwaysAsk.force", false);
			profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/octet-stream");

			// Prevents Firefox from ever starting in Safe Mode
			profile.SetPreference("toolkit.startup.max_resumed_crashes", "-1");

			return profile;
		}

		/// <summary>
		/// Creates an <see cref="IWebDriver"/> for use with Microsoft Internet Explorer.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The test context.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver"/>.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Reliability",
			"CA2000:Dispose objects before losing scope",
			Justification = "Control is passed to the returned object.")]
		protected virtual IWebDriver CreateIEDriver(WebDriverFactoryOptions options, TestContext context)
		{
			EnsureIESupported();

			var explorerOptions = new InternetExplorerOptions()
			{
				IgnoreZoomLevel = true,

				// This is required to work around an issue with support for IE 11.
				// See the following links:
				// http://code.google.com/p/selenium/wiki/InternetExplorerDriver#Required_Configuration
				// http://code.google.com/p/selenium/issues/detail?id=6511
				IntroduceInstabilityByIgnoringProtectedModeSettings = true,
			};

			EnsureIEDriverPresent();

			if (options != null && options.CommandTimeout.HasValue)
			{
				// Based on the private code of the constructor used if there is no command timeout
				// (https://github.com/SeleniumHQ/selenium/blob/master/dotnet/src/webdriver/IE/InternetExplorerDriver.cs)
				var service = InternetExplorerDriverService.CreateDefaultService();

				try
				{
					return new InternetExplorerDriver(
						service,
						explorerOptions,
						options.CommandTimeout.Value);
				}
				catch (Exception)
				{
					service.Dispose();
					throw;
				}
			}
			else
			{
				return new InternetExplorerDriver(explorerOptions);
			}
		}

		/// <summary>
		/// Creates an <see cref="IWebDriver"/> for use with Apple Safari.
		/// </summary>
		/// <param name="options">The options to use to create the instance.</param>
		/// <param name="context">The test context.</param>
		/// <returns>
		/// The created instance of <see cref="IWebDriver"/>.
		/// </returns>
		protected virtual IWebDriver CreateSafariDriver(WebDriverFactoryOptions options, TestContext context)
		{
			var safariOptions = new SafariOptions();

			// N.B. The SafariDriver class does not support setting the command timeout
			return new SafariDriver(safariOptions);
		}

		/// <summary>
		/// Ensures any driver executable required is available locally on disk.
		/// </summary>
		/// <param name="options">The options to use.</param>
		private static void EnsureDriverExecutableAvailable(WebDriverFactoryOptions options)
		{
			switch (options.Browser)
			{
				case WebBrowserType.GoogleChrome:
					EnsureChromeDriverPresent();
					break;

				case WebBrowserType.InternetExplorer:
					EnsureIEDriverPresent();
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// Ensures that the local machine supports using Internet Explorer.
		/// </summary>
		/// <remarks>
		/// Selenium <c>WebDriver</c> does not support Internet Explorer 11 by default
		/// due to issues with the way it works. Until Internet Explorer implements
		/// the <c>WebDriver</c> protocol this can be worked around by setting a
		/// Registry Key to disable a particular feature. See the following link
		/// for further information <c>https://code.google.com/p/selenium/issues/detail?id=6511</c>.
		/// </remarks>
		private static void EnsureIESupported()
		{
			const string VersionSubkeyName = @"SOFTWARE\Microsoft\Internet Explorer";
			const string FeatureSubkeyName = @"SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BFCACHE";

			using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
			{
				if (!_isIE11OrLaterInstalled.HasValue)
				{
					bool isIE11OrLater = false;

					// Does the user have Internet Explorer 11 (or later) installed?
					using (var versionKey = key.OpenSubKey(VersionSubkeyName))
					{
						// Use they "new" key instead of the "legeacy" key as the legacy key
						// reports the major version as 9 for backwards compatibility reasons.
						string versionString = versionKey.GetValue("svcUpdateVersion", null) as string;
						Version version;

						if (!string.IsNullOrEmpty(versionString) &&
							Version.TryParse(versionString, out version) &&
							version.Major > 10)
						{
							isIE11OrLater = true;
						}
					}

					_isIE11OrLaterInstalled = isIE11OrLater;
				}

				// If IE 11 or later then a registry key needs to be set for WebDriver to work
				if (_isIE11OrLaterInstalled.Value)
				{
					bool foundKey = false;
					var featureKey = key.OpenSubKey(FeatureSubkeyName);

					if (featureKey != null)
					{
						try
						{
							object value = featureKey.GetValue("iexplore.exe", 1);

							if (value != null && (value is int) && (int)value == 0)
							{
								// The registry key has been set to disable the relevant feature
								foundKey = true;
							}
						}
						finally
						{
							featureKey.Dispose();
						}
					}

					if (!foundKey)
					{
						string featureSubkeyNameForHelp = string.Format(
							CultureInfo.InvariantCulture,
							@"SOFTWARE\{0}Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BFCACHE",
							Environment.Is64BitOperatingSystem ? @"Wow6432Node\" : string.Empty);

						Assert.Inconclusive("Internet Explorer 11 is not supported by Selenium Web Driver by default. To use IE 11, add/update the '{0}' Registry Key to have a DWORD value of 0 for 'iexplore.exe'.", featureSubkeyNameForHelp);
					}
				}
			}
		}

		/// <summary>
		/// Ensures that the Chrome driver is present in the current executing directory.
		/// </summary>
		private static void EnsureChromeDriverPresent()
		{
			ExtractDriver("chromedriver.exe");
		}

		/// <summary>
		/// Ensures that the Internet Explorer driver is present in the current executing directory.
		/// </summary>
		private static void EnsureIEDriverPresent()
		{
			ExtractDriver("IEDriverServer.exe");
		}

		/// <summary>
		/// Extracts the driver with the specified name if it does not exist.
		/// </summary>
		/// <param name="name">The name of the driver to extract.</param>
		private static void ExtractDriver(string name)
		{
			string directory = Path.GetDirectoryName(typeof(WebDriverFactory).Assembly.Location);
			string fileName = Path.Combine(directory, name);

			lock (_binaryLock)
			{
				// TODO Fix this so updates to the drivers are extracted
				if (!File.Exists(fileName))
				{
					ResourceHelpers.ExtractBinary(name, fileName);
				}
			}
		}


	}
}
	
