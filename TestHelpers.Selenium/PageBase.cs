using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.PageObjects;

namespace TestHelpers.Selenium
{
	/// <summary>
	/// The base class for classes representing pages.
	/// </summary>
	public abstract class PageBase
	{
		/// <summary>
		/// Initializes static members of the <see cref="PageBase"/> class.
		/// </summary>
		static PageBase()
		{
			DefaultWait = TimeSpan.FromSeconds(30);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PageBase"/> class.
		/// </summary>
		/// <param name="driver">The driver.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="driver"/> is <see langword="null"/>.
		/// </exception>
		protected PageBase(IWebDriver driver)
		{
			if (driver == null)
			{
				throw new ArgumentNullException("driver");
			}

			Driver = driver;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PageBase"/> class.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="page"/> is <see langword="null"/>.
		/// </exception>
		protected PageBase(PageBase page)
		{
			if (page == null)
			{
				throw new ArgumentNullException("page");
			}

			Driver = page.Driver;
		}

		/// <summary>
		/// Gets or sets the default wait to use.
		/// </summary>
		public static TimeSpan DefaultWait { get; set; }

		/// <summary>
		/// Gets the <see cref="IWebDriver"/> associated with the page.
		/// </summary>
		public IWebDriver Driver
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the expected title of the page, if any.
		/// </summary>
		public virtual string ExpectedTitle
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the page's current title.
		/// </summary>
		public string Title
		{
			get { return Driver.Title; }
		}

		/// <summary>
		/// Gets the page's current URL.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Design",
			"CA1056:UriPropertiesShouldNotBeStrings",
			Justification = "Fits the Selenium API design.")]
		public string Url
		{
			get { return Driver.Url; }
		}

		/// <summary>
		/// Gets the Id of the unique element.
		/// </summary>
		protected virtual string UniqueElementId
		{
			get { return null; }
		}

		/// <summary>
		/// Creates a page of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the page to create.</typeparam>
		/// <param name="driver">The <see cref="IWebDriver"/> to use to create the page.</param>
		/// <returns>
		/// The created instance of <typeparamref name="T"/>.
		/// </returns>
		/// <remarks>
		/// The type specified by <typeparamref name="T"/> must contain a public
		/// constructor that accepts a single parameter of <see cref="IWebDriver"/>.
		/// </remarks>
		public static T CreatePage<T>(IWebDriver driver)
			where T : PageBase
		{
			return Activator.CreateInstance(typeof(T), driver) as T;
		}

		/// <summary>
		/// Creates a page of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the page to create.</typeparam>
		/// <param name="page">The <see cref="PageBase"/> to use to create the page.</param>
		/// <returns>
		/// The created instance of <typeparamref name="T"/>.
		/// </returns>
		/// <remarks>
		/// The type specified by <typeparamref name="T"/> must contain a public
		/// constructor that accepts a single parameter of <see cref="PageBase"/>.
		/// </remarks>
		public static T CreatePage<T>(PageBase page)
			where T : PageBase
		{
			return Activator.CreateInstance(typeof(T), page) as T;
		}

		/// <summary>
		/// Returns a new instance of <see cref="Actions"/> for the current page.
		/// </summary>
		/// <returns>
		/// The created instance of <see cref="Actions"/>.
		/// </returns>
		public Actions Actions()
		{
			return new Actions(Driver);
		}

		/// <summary>
		/// Returns a new page of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the new page.</typeparam>
		/// <returns>
		/// The page as the type specified by <typeparamref name="T"/>.
		/// </returns>
		public T As<T>()
			where T : PageBase
		{
			if (GetType() == typeof(T))
			{
				return this as T;
			}
			else
			{
				return CreatePage<T>(this);
			}
		}

		/// <summary>
		/// Closes the current tab (or window).
		/// </summary>
		public virtual void CloseCurrentTab()
		{
			Driver.Close();
		}

		/// <summary>
		/// Switches to the initial browser tab (or window).
		/// </summary>
		public virtual void SwitchToInitialTab()
		{
			Driver
				.SwitchTo()
				.Window(Driver.WindowHandles[0]);
		}

		/// <summary>
		/// Waits for the unique element.
		/// </summary>
		public virtual void WaitForUniqueElement()
		{
			WaitForUniqueElement(DefaultWait);
		}

		/// <summary>
		/// Waits for an element.
		/// </summary>
		/// <param name="id">The Id of the element.</param>
		public virtual void WaitForElement(string id)
		{
			WaitForElement(id, DefaultWait);
		}

		/// <summary>
		/// Waits for an element.
		/// </summary>
		/// <param name="id">The Id of the element.</param>
		/// <param name="timeout">The maximum amount of time to wait for the element to be displayed.</param>
		public virtual void WaitForElement(string id, TimeSpan timeout)
		{
			if (id == null)
			{
				throw new ArgumentNullException("id");
			}

			this.WaitUntil((p) => p.FindElements(By.Id(id)).Any((r) => r.Displayed), timeout);
		}

		/// <summary>
		/// Waits for an element.
		/// </summary>
		/// <param name="className">A CSS class of the element.</param>
		public virtual void WaitForElementWithClassName(string className)
		{
			WaitForElementWithClassName(className, DefaultWait);
		}

		/// <summary>
		/// Waits for an element.
		/// </summary>
		/// <param name="className">A CSS class of the element.</param>
		/// <param name="timeout">The maximum amount of time to wait for the element to be displayed.</param>
		public virtual void WaitForElementWithClassName(string className, TimeSpan timeout)
		{
			if (className == null)
			{
				throw new ArgumentNullException("className");
			}

			this.WaitUntil((p) => p.FindElements(By.ClassName(className)).Any((r) => r.Displayed), timeout);
		}

		/// <summary>
		/// Waits for the unique element.
		/// </summary>
		/// <param name="timeout">The maximum amount of time to wait for the unique element to be displayed.</param>
		public virtual void WaitForUniqueElement(TimeSpan timeout)
		{
			if (UniqueElementId != null)
			{
				WaitForElement(UniqueElementId, timeout);
			}
		}

		/// <summary>
		/// Returns the Id attribute value of a child of the specified web element using xpath.
		/// </summary>
		/// <param name="context">The element to find the Id attribute from.</param>
		/// <param name="xpathToFind">The xpath of the element to find the attribute for.</param>
		/// <returns>
		/// The value of the Id attribute of the element found using the xpath specified by <paramref name="xpathToFind"/>.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Naming",
			"CA1704:IdentifiersShouldBeSpelledCorrectly",
			MessageId = "xpath",
			Justification = "Naming is correct.")]
		protected static string GetIdOfElement(ISearchContext context, string xpathToFind)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			return context.FindElement(By.XPath(xpathToFind)).GetAttribute("id");
		}

		/// <summary>
		/// Returns the value attribute value of a child of the specified web element using xpath.
		/// </summary>
		/// <param name="context">The element to find the value attribute from.</param>
		/// <param name="xpathToFind">The xpath of the element to find the attribute for.</param>
		/// <returns>
		/// The value of the value attribute of the element found using the xpath specified by <paramref name="xpathToFind"/>.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Naming",
			"CA1704:IdentifiersShouldBeSpelledCorrectly",
			MessageId = "xpath",
			Justification = "Naming is correct.")]
		protected static string GetValueOfElement(ISearchContext context, string xpathToFind)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			return context.FindElement(By.XPath(xpathToFind)).GetAttribute("value");
		}

		/// <summary>
		/// Tries to switch to a new tab (or window) if one exists.
		/// </summary>
		protected virtual void TrySwitchToNewTab()
		{
			if (Driver.WindowHandles.Count > 1)
			{
				Driver
					.SwitchTo()
					.Window(Driver.WindowHandles[1]);
			}
		}
	}
}
