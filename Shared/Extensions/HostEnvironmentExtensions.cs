namespace Shared.Extensions
{
    using System;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// 
    /// </summary>
    public static class HostEnvironmentExtensions
    {
        #region Methods

        /// <summary>
        /// Determines whether [is pre production].
        /// </summary>
        /// <param name="hostEnvironment">The host environment.</param>
        /// <returns>
        ///   <c>true</c> if [is pre production] [the specified host environment]; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">hostEnvironment</exception>
        public static Boolean IsPreProduction(this IHostEnvironment hostEnvironment)
        {
            if (hostEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostEnvironment));
            }

            return hostEnvironment.IsEnvironment("PreProduction");
        }

        #endregion
    }
}