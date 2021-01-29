using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHelpers.Selenium
{
	public class WebDriverFactoryOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WebDriverFactoryOptions"/> class.
		/// </summary>
		public WebDriverFactoryOptions()
		{
		}

		/// <summary>
		/// Gets or sets the browser to create the driver for.
		/// </summary>
		public WebBrowserType Browser
		{
			get;
			set;
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
		/// Gets or sets the duration of the implicit wait to use, if any.
		/// </summary>
		public TimeSpan? ImplicitWait
		{
			get;
			set;
		}
	}
}
