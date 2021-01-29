using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace TestHelpers.Selenium
{
    /// <summary>
    /// A class containing helper methods for UI testing.  This class cannot be inherited.
    /// </summary>
    public static class UITestHelpers
    {
        /// <summary>
        /// Returns the file download path to use for the specified browser and test context.
        /// </summary>
        /// <param name="browserType">The browser to get the download path for.</param>
        /// <param name="context">The test context.</param>
        /// <returns>
        /// The full path of the download directory for the specified browser and test context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public static string GetDownloadPath(WebBrowserType browserType, TestContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            string downloadDirectory;

            switch (browserType)
            {
                case WebBrowserType.Firefox:
                case WebBrowserType.GoogleChrome:
                    downloadDirectory = Path.Combine(context.TestRunResultsDirectory, "Downloads", browserType.ToString());
                    break;

                // Browsers that do not support changing the download directory
                default:
                    downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    break;
            }

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Created download directory '{0}'.", downloadDirectory));
            }

            return downloadDirectory;
        }

        /// <summary>
        /// Takes a screenshot using the specified web driver and test context.
        /// </summary>
        /// <param name="driver">The web driver to use to take the screenshot.</param>
        /// <param name="context">The current test context.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="driver"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="driver"/> does not implement<see cref="ITakesScreenshot"/>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Localization",
            "QA0011:DoNotUseDateTimeNowOrToday",
            Justification = "Easier to find results on team members' local machines if using DateTime.Now.")]
        public static void TakeScreenshot(IWebDriver driver, TestContext context)
        {
            if (driver == null)
            {
                throw new ArgumentNullException("driver");
            }

            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ITakesScreenshot screenshotDriver = driver as ITakesScreenshot;

            if (screenshotDriver == null)
            {
                throw new NotSupportedException(
                    SRHelper.Format(
                        SR.UITestHelpers_ScreenshotsNotSupportedFormat,
                        driver.GetType().AssemblyQualifiedName,
                        typeof(ITakesScreenshot).FullName));
            }

            string directoryPath = Path.Combine(
                context.TestRunResultsDirectory,
                context.FullyQualifiedTestClassName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string browserInfo = string.Empty;

            DateTime now;

#if TFS_BUILD
            now = DateTime.UtcNow;
#else
            now = DateTime.Now;
#endif

            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}_{2:yyyy-MM-dd_HHmmss}.png",
                context.TestName,
                browserInfo,
                now);

            fileName = Path.Combine(directoryPath, fileName);

            Screenshot screenshot = screenshotDriver.GetScreenshot();
            screenshot.SaveAsFile(fileName, ScreenshotImageFormat.Png);
        }
    }
}