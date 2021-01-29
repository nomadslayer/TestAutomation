
using System;
using System.Configuration;
using BoDi;
using OpenQA.Selenium;
using TechTalk.SpecFlow;
using TestHelpers.Selenium;

namespace TestProject.Tests.Hooks
{
	[Binding]
	public sealed class DriverHook
	{
		private static IWebDriver _driver;
		private readonly IObjectContainer _objectContainer;

		public DriverHook(IObjectContainer objectContainer)
		{
			_objectContainer = objectContainer;
		}

		public static IWebDriver Driver
		{
			get { return _driver; }
		}

		[BeforeScenario("SeleniumTests")]
		public void TestInitialize()
		{
			TestSetupHelpers.DisableX509CertificateValidation();

			_driver = CreateWebDriver();
			_objectContainer.RegisterInstanceAs<IWebDriver>(_driver);
		}

		[AfterScenario("SeleniumTests")]
		public void TestCleanup()
		{
			if (_driver != null)
			{
				_driver.Quit();
			}

			TestSetupHelpers.RestoreX509CertificateValidation();
		}

		private IWebDriver CreateWebDriver()
		{
			// TODO Deduplicate this code between this class and the two WebDriverClient classes
			WebBrowserType browser;

			switch (ConfigurationSettings.AppSettings["BrowserType"].ToUpperInvariant())
			{
				case "FIREFOX":
					browser = WebBrowserType.Firefox;
					break;

				case "IE":
					browser = WebBrowserType.InternetExplorer;
					break;

				case "CHROME":
				default:
					browser = WebBrowserType.GoogleChrome;
					break;
			}

			WebDriverFactoryOptions options = new WebDriverFactoryOptions()
			{
				Browser = browser,
				ImplicitWait = TimeSpan.FromMinutes(2),
			};

			IWebDriverFactory factory = new WebDriverFactory();
			IWebDriver driver = factory.Create(options, null);

			try
			{
				var manage = driver.Manage();

				manage.Window.Maximize();
				return driver;
			}
			catch (Exception)
			{
				driver.Dispose();
				throw;
			}
		}
	}
}
