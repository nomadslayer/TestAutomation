using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace TestHelpers.Selenium
{
    /// <summary>
    /// A class containing helper methods for working with resources.  This class cannot be inherited.
    /// </summary>
    internal static class ResourceHelpers
    {
        /// <summary>
        /// Extracts the specified binary from the assembly's resources.
        /// </summary>
        /// <param name="name">The name of the binary to extract.</param>
        /// <param name="fileName">The path to extract the binary to.</param>
        /// <exception cref="InvalidOperationException">
        /// The binary could not be extracted.
        /// </exception>
        internal static void ExtractBinary(string name, string fileName)
        {
            Type thisType = typeof(WebBrowserType);
            Assembly thisAssembly = thisType.Assembly;

            using (Stream binaryStream = thisAssembly.GetManifestResourceStream(thisType.Namespace + "." + name))
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    using (Stream outputStream = File.OpenWrite(fileName))
                    {
                        binaryStream.CopyTo(outputStream);
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format(CultureInfo.InvariantCulture, "Failed to extract '{0}' to '{1}': {2}", name, fileName, ex.Message);
                    throw new InvalidOperationException(message, ex);
                }
            }
        }
    }
}