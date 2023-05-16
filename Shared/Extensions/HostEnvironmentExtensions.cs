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