namespace TestHelpers.Selenium
{
	/// <summary>
	/// A static class containing helper methods for use with the <see cref="SR"/> class.
	/// </summary>
	public static class SRHelper
	{
		/// <summary>
		/// Replaces the format item in a specified string with the string representation
		/// of a corresponding object in a specified array.
		/// </summary>
		/// <param name="format">A composite format string.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		/// <returns>
		/// A copy of format in which the format items have been replaced by the string
		/// representation of the corresponding objects in args.
		/// </returns>
		public static string Format(string format, params object[] args)
		{
			return string.Format(
				SR.Culture,
				format,
				args);
		}
	}
}
