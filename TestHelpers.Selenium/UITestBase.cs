using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace TestHelpers.Selenium
{
    /// <summary>
    /// The base class for Selenium UI tests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Justification = "This is step one in the removal of this class.")]
    [TestClass]
    public abstract class UITestBase : IDisposable
    {
        /// <summary>
        /// The name of the property containing the browser type.
        /// </summary>
        public const string BrowserTypePropertyName = "BrowserType";

        /// <summary>
        /// The name of the property containing the browser version.
        /// </summary>
        public const string BrowserVersionPropertyName = "BrowserVersion";

        /// <summary>
        /// The name of the property containing the device.
        /// </summary>
        public const string DevicePropertyName = "Device";

        /// <summary>
        /// The name of the property containing the device orientation.
        /// </summary>
        public const string DeviceOrientationPropertyName = "DeviceOrientation";

        /// <summary>
        /// The name of the property containing the OS.
        /// </summary>
        public const string OSPropertyName = "OS";

        /// <summary>
        /// The name of the property containing the OS version.
        /// </summary>
        public const string OSVersionPropertyName = "OSVersion";

        /// <summary>
        /// The name of the browser table.
        /// </summary>
        public const string BrowserTypeTableName = "browser";

        /// <summary>
        /// The name of the query string parameter to use to specify the integration Id.
        /// </summary>
        public const string IntegrationIdParameterName = "id";

        /// <summary>
        /// The name of the query string parameter to use to specify the token.
        /// </summary>
        public const string TokenParameterName = "token";

        /// <summary>
        /// The name of the query string parameter to use to specify local JavaScript customer files instead of blob storage.
        /// </summary>
        public const string UseLocalParameterName = "uselocal";

        /// <summary>
        /// The lazily-initialized <see cref="string"/> containing the build number for use with <c>BrowserStack</c>.
        /// </summary>
        private static string _assemblyBuild;

        /// <summary>
        /// The lazily-initialized debug information for the application being tested.
        /// </summary>
        private static string _debugInfo;

        /// <summary>
        /// Whether the instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UITestBase"/> class.
        /// </summary>
        public UITestBase()
        {
            DriverFactory = new WebDriverFactory();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UITestBase"/> class.
        /// </summary>
        /// <param name="browserType">The type of browser the unit test is for.</param>
        public UITestBase(WebBrowserType browserType)
        {
            BrowserType = browserType;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="UITestBase"/> class.
        /// </summary>
        ~UITestBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the <see cref="IWebDriverFactory"/> to use.
        /// </summary>
        public IWebDriverFactory DriverFactory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the base URI for the website to test.
        /// </summary>
        public virtual Uri BaseUri
        {
            get
            {
                Uri uri = null;
                string uriString = TestHelpers.Selenium.TestConfig.baseURI;

                if (!string.IsNullOrEmpty(uriString))
                {
                    uri = new Uri(uriString, UriKind.Absolute);
                }

                return uri;
            }
        }

        /// <summary>
        /// Gets the <see cref="WebBrowserType"/> for the current instance.
        /// </summary>
        public WebBrowserType BrowserType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the command timeout to use, if any, when creating
        /// instances of <see cref="OpenQA.Selenium.IWebDriver"/> for remote servers.
        /// </summary>
        public TimeSpan? CommandTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="IWebDriver"/> in use by the instance.
        /// </summary>
        public IWebDriver Driver
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="WebDriverFactoryOptions"/> used to create <see cref="Driver"/>.
        /// </summary>
        public WebDriverFactoryOptions DriverOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the default web browser type to use.
        /// </summary>
        public virtual WebBrowserType DefaultBrowser
        {
            get { return WebBrowserType.GoogleChrome; }
        }

        /// <summary>
        /// Gets the default implicit wait to use.
        /// </summary>
        public virtual TimeSpan DefaultImplicitWait
        {
            get { return TimeSpan.FromSeconds(10); }
        }

        /// <summary>
        /// Gets the default timeout to use.
        /// </summary>
        public virtual TimeSpan DefaultTimeout
        {
            get { return TimeSpan.FromSeconds(30); }
        }

        /// <summary>
        /// Gets a value indicating whether the current test is running from a debug build.
        /// </summary>
        public virtual bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Gets the name of the project the tests belong to.
        /// </summary>
        public virtual string ProjectName { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes the test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            bool overrideTestInitialize;

            // HACK At the moment it is not possible to maintain backwards compatibility as well as control test start up this enables us to do that.
            if (bool.TryParse(null, out overrideTestInitialize) && overrideTestInitialize)
            {
                OnTestInitialize();
            }
        }

        /// <summary>
        /// Cleans up the test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            OnTestCleanup();
        }

        /// <summary>
        /// Creates an instance of <see cref="WebDriverWait"/>.
        /// </summary>
        /// <returns>
        /// The created instance of <see cref="WebDriverWait"/>.
        /// </returns>
        public virtual WebDriverWait CreateWait()
        {
            WebDriverWait wait = new WebDriverWait(Driver, DefaultTimeout);

            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
            return wait;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IWebDriver"/>.
        /// </summary>
        /// <param name="options">The options to use to create the instance.</param>
        /// <param name="context">The <see cref="TestContext"/> associated with the current test.</param>
        /// <returns>
        /// The created instance of <see cref="IWebDriver"/>.
        /// </returns>
        public virtual IWebDriver CreateWebDriver(WebDriverFactoryOptions options, TestContext context)
        {
            IWebDriver driver = DriverFactory.Create(options, context);

            try
            {
                InitializeWebDriver(driver);

                return driver;
            }
            catch (Exception)
            {
                if (driver != null)
                {
                    driver.Dispose();
                }

                throw;
            }
        }

        /// <summary>
        /// Returns the build of this assembly.
        /// </summary>
        /// <returns>
        /// A string containing the build of this assembly.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "The operation is potentially expensive.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Localization",
            "QA0011:DoNotUseDateTimeNowOrToday",
            Justification = "It is easier to find the results in BrowserStack if the local time for the user is used. Maybe use UtcNow for TFS_BUILD once automated.")]
        public virtual string GetAssemblyBuild()
        {
            // Only compute the build number once, as if it contains a time,
            // then long running test runs using BrowserStack will have their
            // build number change over time, making the tests appear to be in
            // different test runs then they are actually in.
            if (_assemblyBuild == null)
            {
                _assemblyBuild = GetAssemblyBuildPrivate(GetType());
            }

            return _assemblyBuild;
        }

        /// <summary>
        /// Initializes the specified instance of <see cref="IWebDriver"/>.
        /// </summary>
        /// <param name="driver">The <see cref="IWebDriver"/> to initialize.</param>
        public virtual void InitializeWebDriver(IWebDriver driver)
        {
            if (driver == null)
            {
                throw new ArgumentNullException("driver");
            }

            IOptions options = driver.Manage();
            options.Timeouts().ImplicitWait = DefaultImplicitWait;

            // HACK Edge does not support Maximize()
            if (BrowserType != WebBrowserType.MicrosoftEdge)
            {
                options.Window.Maximize();
            }
        }

        /// <summary>
        /// Loads a new web page in the current browser window.
        /// </summary>
        /// <typeparam name="T">The type of the page being navigated to.</typeparam>
        /// <param name="url">The URL to load.</param>
        /// <returns>
        /// The page navigated to.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1057:StringUriOverloadsCallSystemUriOverloads",
            Justification = "Makes the API easier to use.")]
        public virtual T GoToUrl<T>(string url)
            where T : PageBase
        {
            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);
            return GoToUrl<T>(uri);
        }

        /// <summary>
        /// Loads a new web page in the current browser window.
        /// </summary>
        /// <typeparam name="T">The type of the page being navigated to.</typeparam>
        /// <param name="url">The URL to load.</param>
        /// <returns>
        /// The page navigated to.
        /// </returns>
        public virtual T GoToUrl<T>(Uri url)
            where T : PageBase
        {
            Driver.Navigate().GoToUrl(url);
            return PageBase.CreatePage<T>(Driver);
        }

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public virtual void OnTestInitialize()
        {
            PageBase.DefaultWait = DefaultTimeout;
            var options = GetWebDriverFactoryOptions();

            DriverOptions = options;
            Driver = CreateWebDriver(options, TestContext);
        }

        /// <summary>
        /// Gets the options needed to create a web driver instance.
        /// </summary>
        /// <returns>The options.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Not appropriate.")]
        public WebDriverFactoryOptions GetWebDriverFactoryOptions()
        {
            SetBrowserTypeIfNone();

            WebDriverFactoryOptions options = new WebDriverFactoryOptions()
                                                  {
                                                      Browser = BrowserType,
                                                      CommandTimeout = CommandTimeout,
                                                      ImplicitWait = DefaultImplicitWait,
                                                  };

            return options;
        }

        /// <summary>
        /// Cleans up the test.
        /// </summary>
        public virtual void OnTestCleanup()
        {
            // If the test fails, take a screenshot (if possible)
            if (TestContext != null && TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
            {
                if (Driver is ITakesScreenshot)
                {
                    TakeScreenshot();
                }

                if (Driver != null)
                {
                    try
                    {
                        Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Title: '{0}'; URL: {1}", Driver.Title, Driver.Url));
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore if a driver throws an exception because it has been terminated remotely
                    }
                }
            }

            if (Driver != null)
            {
                if (_debugInfo == null)
                {
                    _debugInfo = ScrapeDebugInfo(Driver, BaseUri);
                }

                if (!string.IsNullOrEmpty(_debugInfo))
                {
                    Trace.WriteLine(_debugInfo);
                }

                Driver.Quit();
            }
        }

        /// <summary>
        /// When overridden in a derived class, scrapes the debug information
        /// for the specified application using the specified driver.
        /// </summary>
        /// <param name="driver">The driver to use to scrape the debug information.</param>
        /// <param name="baseUri">The base URI of the application to scrape the debug information from.</param>
        /// <returns>
        /// A <see cref="string"/> containing the debug information for the specified application.
        /// </returns>
        public virtual string ScrapeDebugInfo(IWebDriver driver, Uri baseUri)
        {
            return string.Empty;
        }

        /// <summary>
        /// Takes a screenshot.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Screenshot failure is not really important.")]
        public virtual void TakeScreenshot()
        {
            if (Driver != null && TestContext != null)
            {
                try
                {
                    UITestHelpers.TakeScreenshot(Driver, TestContext);
                }
                catch (PathTooLongException ex)
                {
                    Trace.TraceError("Failed to take screenshot as the path is too long: {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to take screenshot: {0}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Waits for the page with the specified title to load.
        /// </summary>
        /// <param name="title">The title of the page to wait to load.</param>
        public virtual void WaitForPageLoad(string title)
        {
            var wait = CreateWait();
            wait.Until((p) => string.Equals(p.Title, title, StringComparison.Ordinal));
        }

        /// <summary>
        /// Releases unmanaged and, optionally, managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && Driver != null)
                {
                    Driver.Dispose();
                    Driver = null;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Returns the build of the assembly for the specified type.
        /// </summary>
        /// <param name="type">The type to get the build for.</param>
        /// <returns>
        /// A string containing the build of the assembly that defines <paramref name="type"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "The operation is potentially expensive.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Localization",
            "QA0011:DoNotUseDateTimeNowOrToday",
            Justification = "It is easier to find the results in BrowserStack if the local time for the user is used. Maybe use UtcNow for TFS_BUILD once automated.")]
        private static string GetAssemblyBuildPrivate(Type type)
        {
            string version = type.Assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .Select((p) => p.InformationalVersion)
                .FirstOrDefault();

            string configuration = type.Assembly
                .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
                .OfType<AssemblyMetadataAttribute>()
                .Where((p) => string.Equals("BuildLabel", p.Key, StringComparison.OrdinalIgnoreCase))
                .Select((p) => p.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(configuration))
            {
                configuration = type.Assembly
                    .GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false)
                    .OfType<AssemblyConfigurationAttribute>()
                    .Select((p) => p.Configuration)
                    .FirstOrDefault();
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1})",
                version ?? FileVersionInfo.GetVersionInfo(type.Assembly.Location).FileVersion,
                string.IsNullOrEmpty(configuration) || string.Equals("LOCAL", configuration, StringComparison.OrdinalIgnoreCase) ? Environment.MachineName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) : configuration);
        }

        private void SetBrowserTypeIfNone()
        {
            if (BrowserType == WebBrowserType.None)
            {
                BrowserType = WebBrowserType.GoogleChrome;
            }
        }
    }
}
