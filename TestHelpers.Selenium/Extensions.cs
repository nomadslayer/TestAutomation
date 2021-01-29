using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TestHelpers.Selenium
{
	/// <summary>
	/// A class containing extension methods.  This class cannot be inherited.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class Extensions
	{
		/// <summary>
		/// The default timeout to use.  This field is read-only.
		/// </summary>
		private static readonly TimeSpan DefaultTimeout = LoadDefaultTimeout();

		/// <summary>
		/// Closes the current tab (or window).
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to close the current tab for.</param>
		/// <returns>
		/// The page specified by <paramref name="value"/> for which the tab was closed.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T CloseCurrentTab<T>(T value)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			value.Driver.Close();
			return value;
		}

		/// <summary>
		/// Ensures that the specified element is visible in the viewport.
		/// </summary>
		/// <param name="value">The <see cref="IWebDriver"/> to use to ensure the element is visible.</param>
		/// <param name="element">The <see cref="IWebElement"/> to ensure is visible.</param>
		/// <returns>
		/// The value specified as the <paramref name="element"/> parameter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="element"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// <paramref name="value"/> does not implement <see cref="IJavaScriptExecutor"/>.
		/// </exception>
		public static IWebElement EnsureElementVisible(this IWebDriver value, IWebElement element)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (element == null)
			{
				throw new ArgumentNullException("element");
			}

			IJavaScriptExecutor executor = value as IJavaScriptExecutor;

			if (executor == null)
			{
				string message = string.Format(
					CultureInfo.InvariantCulture,
					"Type {0} does not implement {1}.",
					value.GetType().FullName,
					typeof(IJavaScriptExecutor).Name);

				throw new NotSupportedException(message);
			}

			////// Only scroll if the width is less than 1600 pixels, as that's what creates the horizontal button overlap with the chat window
			////if (!element.Displayed && System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width < 1600)
			if (!element.Displayed)
			{
				executor.ExecuteScript("window.scrollTo(0, arguments[0]);", element.Location.Y);

				// Needed as sometimes the element is displayed but not yet clickable
				Thread.Sleep(TimeSpan.FromSeconds(0.75));

				if (!element.Displayed)
				{
					string message = string.Format(
						CultureInfo.InvariantCulture,
						"Element of type <{0}> was not visible.",
						element.TagName);

					throw new ElementNotVisibleException(message);
				}
			}

			return element;
		}

		/// <summary>
		/// Switches to the initial browser tab (or window).
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to switch to the initial tab for.</param>
		/// <returns>
		/// The page specified by <paramref name="value"/> for which the tab was switched.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T SwitchToInitialTab<T>(T value)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			value.SwitchToInitialTab();
			return value;
		}

		/// <summary>
		/// Waits until a condition is true or times out.
		/// </summary>
		/// <typeparam name="T">The type of result to expect from the condition.</typeparam>
		/// <param name="value">The waiter to use to wait until.</param>
		/// <param name="condition">A delegate taking a <see cref="WebDriverWait"/> as its parameter, and returning a <typeparamref name="T"/>.</param>
		/// <param name="message">A message to display if the wait fails. This message can be seen in the unit test results.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static void Until<T>(
			this IWait<IWebDriver> value,
			Func<IWebDriver, T> condition,
			string message)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			try
			{
				value.Until(condition);
			}
			catch (NoSuchElementException ex)
			{
				Trace.TraceError(ex.ToString());
				Assert.Fail(message);
			}
			catch (WebDriverTimeoutException ex)
			{
				Trace.TraceError(ex.ToString());
				Assert.Fail(message);
			}
		}

		/// <summary>
		/// Waits on the page for the approximate period of time required for an animation to complete.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <returns>
		/// A new instance of type <typeparamref name="T"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitForAnimation<T>(this T value)
			where T : PageBase
		{
			return value.Wait(TimeSpan.FromSeconds(0.4));
		}

		/// <summary>
		/// Waits on the web driver for the approximate period of time required for an animation to complete.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static void WaitForAnimation(this IWebDriver value)
		{
			value.Wait(TimeSpan.FromSeconds(0.4));
		}

		/// <summary>
		/// Waits on the page for the specified period of time.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="timeout">The period of time to wait for.</param>
		/// <returns>
		/// A new instance of type <typeparamref name="T"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T Wait<T>(this T value, TimeSpan timeout)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			value.Driver.Wait(timeout);
			return PageBase.CreatePage<T>(value);
		}

		/// <summary>
		/// Waits on the web driver for the specified period of time.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <param name="timeout">The period of time to wait for.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static void Wait(this IWebDriver value, TimeSpan timeout)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			Thread.Sleep(timeout);
		}

		/// <summary>
		/// Waits for the page to load.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="timeout">The maximum amount of time to wait for the page to load.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitForPageToLoad<T>(this T value, TimeSpan timeout)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			value.WaitForUniqueElement(timeout);

			return value;
		}

		/// <summary>
		/// Waits for the page to load.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// <paramref name="value"/> does not have a populated
		/// value for the <see cref="PageBase.ExpectedTitle"/> property.
		/// </exception>
		public static T WaitForPageToLoad<T>(this T value)
			where T : PageBase
		{
			return value.WaitForPageToLoad(DefaultTimeout);
		}

		/// <summary>
		/// Waits for the page with the specified title to load.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="title">The title of the page to wait to load.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitForPageToLoad<T>(this T value, string title)
			where T : PageBase
		{
			return value.WaitForPageToLoad(title, DefaultTimeout);
		}

		/// <summary>
		/// Waits for the page with the specified title to load.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="title">The title of the page to wait to load.</param>
		/// <param name="timeout">The maximum amount of time to wait for the page to load.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitForPageToLoad<T>(this T value, string title, TimeSpan timeout)
			where T : PageBase
		{
			return value.WaitUntil((p) => string.Equals(p.Title, title, StringComparison.Ordinal), timeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitUntil<T>(this T value, Func<bool> condition)
			where T : PageBase
		{
			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}

			return value.WaitUntil((p) => condition(), DefaultTimeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitUntil<T>(this T value, Func<IWebDriver, bool> condition)
			where T : PageBase
		{
			return value.WaitUntil(condition, DefaultTimeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static void WaitUntil(this IWebDriver value, Func<IWebDriver, bool> condition)
		{
			value.WaitUntil(condition, DefaultTimeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static void WaitUntil(this IWebDriver value, Func<bool> condition)
		{
			value.WaitUntil(condition, DefaultTimeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <param name="timeout">The maximum amount of time to wait for the condition to evaluate to <see langword="true"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static void WaitUntil(this IWebDriver value, Func<bool> condition, TimeSpan timeout)
		{
			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}

			value.WaitUntil((p) => condition(), timeout);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <param name="message">A message to display if the wait fails. This message can be seen in the unit test results.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitUntil<T>(this T value, Func<IWebDriver, bool> condition, string message)
			where T : PageBase
		{
			return value.WaitUntil(condition, DefaultTimeout, message);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <param name="timeout">The maximum amount of time to wait for the condition to evaluate to <see langword="true"/>.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitUntil<T>(this T value, Func<IWebDriver, bool> condition, TimeSpan timeout)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			value.Driver.WaitUntil(condition, timeout);
			return value;
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <param name="value">The web driver to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <param name="timeout">The maximum amount of time to wait for the condition to evaluate to <see langword="true"/>.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static void WaitUntil(this IWebDriver value, Func<IWebDriver, bool> condition, TimeSpan timeout)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}

			var wait = new WebDriverWait(value, timeout);
			wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));
			wait.Until(condition);
		}

		/// <summary>
		/// Waits until the specified condition evaluates to <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of the page.</typeparam>
		/// <param name="value">The page to use to wait for.</param>
		/// <param name="condition">The condition to wait for.</param>
		/// <param name="timeout">The maximum amount of time to wait for the condition to evaluate to <see langword="true"/>.</param>
		/// <param name="message">A message to display if the wait fails. This message can be seen in the unit test results.</param>
		/// <returns>
		/// The value specified by <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="value"/> or <paramref name="condition"/> is <see langword="null"/>.
		/// </exception>
		public static T WaitUntil<T>(this T value, Func<IWebDriver, bool> condition, TimeSpan timeout, string message)
			where T : PageBase
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}

			if (condition == null)
			{
				throw new ArgumentNullException("condition");
			}

			var wait = new WebDriverWait(value.Driver, timeout);
			wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
			wait.Until(condition, message);

			return value;
		}

		/// <summary>
		/// Loads the default timeout to use.
		/// </summary>
		/// <returns>
		/// The default timeout to use.
		/// </returns>
		internal static TimeSpan LoadDefaultTimeout()
		{
			return TimeSpan.FromSeconds(60);
		}
	}
}
