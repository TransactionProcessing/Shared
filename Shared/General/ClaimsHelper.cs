namespace Shared.General
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Security.Claims;
    using Exceptions;

    [ExcludeFromCodeCoverage]
    public class ClaimsHelper
    {
        #region Methods

        /// <summary>
        /// Gets the user claims.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="customClaimType">Type of the custom claim.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">No claim [{customClaimType}] found for user id [{userIdClaim.Value}</exception>
        public static Claim GetUserClaim(ClaimsPrincipal user,
                                         String customClaimType,
                                         String defaultValue = "") {
            Claim userClaim = null;

            if (ClaimsHelper.IsPasswordToken(user)) {
                // Get the claim from the token
                userClaim = user.Claims.SingleOrDefault(c => c.Type.ToLower() == customClaimType.ToLower());

                if (userClaim == null) {
                    throw new NotFoundException($"Claim type [{customClaimType}] not found");
                }
            }
            else {
                userClaim = new Claim(customClaimType, defaultValue);
            }

            return userClaim;
        }

        /// <summary>
        /// Determines whether [is client token] [the specified user].
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>
        ///   <c>true</c> if [is client token] [the specified user]; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsPasswordToken(ClaimsPrincipal user) {
            Boolean result = false;

            Claim userIdClaim = user.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim != null) {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Determines whether [is user roles valid] [the specified user].
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="allowedRoles">The allowed roles.</param>
        /// <returns>
        ///   <c>true</c> if [is user roles valid] [the specified user]; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsUserRolesValid(ClaimsPrincipal user,
                                               String[] allowedRoles) {
            if (ClaimsHelper.IsPasswordToken(user) == false) {
                return true;
            }

            return allowedRoles.Any(r => user.IsInRole(r));
        }

        /// <summary>
        /// Validates the route parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeParameter">The route parameter.</param>
        /// <param name="userClaim">The user claim.</param>
        public static Boolean ValidateRouteParameter<T>(T routeParameter,
                                                        Claim userClaim) {
            if (userClaim != null && userClaim.Value != String.Empty) {
                if (routeParameter.ToString() != userClaim.Value) {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}