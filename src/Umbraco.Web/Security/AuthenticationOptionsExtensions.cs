﻿using System;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Exceptions;

namespace Umbraco.Web.Security
{
    public static class AuthenticationOptionsExtensions
    {

        /// <summary>
        /// When trying to implement an Azure AD B2C provider or other OAuth provider that requires a customized Challenge Result in order to work then
        /// this must be used.
        /// </summary>
        /// <param name="authOptions"></param>
        /// <param name="authProperties"></param>
        /// <remarks>
        /// See: http://issues.umbraco.org/issue/U4-7353
        /// </remarks>
        public static void SetSignInChallengeResultCallback(
            this AuthenticationOptions authOptions,
            Func<IOwinContext, AuthenticationProperties> authProperties)
        {
            authOptions.Description.Properties["ChallengeResultCallback"] = authProperties;
        }

        public static AuthenticationProperties GetSignInChallengeResult(this AuthenticationDescription authenticationDescription, IOwinContext ctx)
        {
            if (authenticationDescription.Properties.ContainsKey("ChallengeResultCallback") == false) return null;
            var cb = authenticationDescription.Properties["ChallengeResultCallback"] as Func<IOwinContext, AuthenticationProperties>;
            if (cb == null) return null;
            return cb(ctx);
        }

        /// <summary>
        /// Used during the External authentication process to assign external sign-in options
        /// that are used by the Umbraco authentication process.
        /// </summary>
        /// <param name="authOptions"></param>
        /// <param name="options"></param>
        public static void SetExternalSignInAutoLinkOptions(
            this AuthenticationOptions authOptions,
            ExternalSignInAutoLinkOptions options)
        {
            authOptions.Description.Properties["ExternalSignInAutoLinkOptions"] = options;
        }

        /// <summary>
        /// Used during the External authentication process to retrieve external sign-in options
        /// that have been set with SetExternalAuthenticationOptions
        /// </summary>
        /// <param name="authenticationDescription"></param>
        public static ExternalSignInAutoLinkOptions GetExternalAuthenticationOptions(this AuthenticationDescription authenticationDescription)
        {
            if (authenticationDescription.Properties.ContainsKey("ExternalSignInAutoLinkOptions") == false) return null;
            var options = authenticationDescription.Properties["ExternalSignInAutoLinkOptions"] as ExternalSignInAutoLinkOptions;
            return options;
        }

        /// <summary>
        /// When set this will disable all local login ability within Umbraco
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>
        /// Even if there are multiple OAuth providers installed if any of these specifies this option then all local login ability is disabled.
        /// </remarks>
        public static void DenyLocalLogin(this AuthenticationOptions options)
        {
            options.Description.Properties["UmbracoBackOffice_DenyLocalLogin"] = true;
        }

        /// <summary>
        /// When specified this will automatically redirect to the OAuth login provider instead of prompting the user to click on the OAuth button first.
        /// </summary>
        /// <param name="options"></param>
        /// <remarks>
        /// This is generally used in conjunction with <see cref="DenyLocalLogin(AuthenticationOptions)"/>. If more than one OAuth provider specifies this, the last registered
        /// provider's redirect settings will win.
        /// </remarks>
        public static void AutoLoginRedirect(this AuthenticationOptions options)
        {
            options.Description.Properties["UmbracoBackOffice_AutoLoginRedirect"] = true;
        }

        /// <summary>
        /// Configures the properties of the authentication description instance for use with Umbraco back office
        /// </summary>
        /// <param name="options"></param>
        /// <param name="style"></param>
        /// <param name="icon"></param>
        /// <param name="callbackPath">
        /// This is important if the identity provider is to be able to authenticate when upgrading Umbraco. We will try to extract this from
        /// any options passed in via reflection since none of the default OWIN providers inherit from a base class but so far all of them have a consistent
        /// name for the 'CallbackPath' property which is of type PathString. So we'll try to extract it if it's not found or supplied.
        ///
        /// If a value is extracted or supplied, this will be added to an internal list which the UmbracoModule will use to allow the request to pass
        /// through without redirecting to the installer.
        /// </param>
        public static void ForUmbracoBackOffice(this AuthenticationOptions options, string style, string icon, string callbackPath = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.AuthenticationType)) throw new InvalidOperationException("The authentication type can't be null or empty.");

            //Ensure the prefix is set
            if (options.AuthenticationType.StartsWith(Constants.Security.BackOfficeExternalAuthenticationTypePrefix) == false)
            {
                options.AuthenticationType = Constants.Security.BackOfficeExternalAuthenticationTypePrefix + options.AuthenticationType;
            }

            options.Description.Properties["SocialStyle"] = style;
            options.Description.Properties["SocialIcon"] = icon;

            //flag for use in back office
            options.Description.Properties["UmbracoBackOffice"] = true;

            if (callbackPath.IsNullOrWhiteSpace())
            {
                try
                {
                    //try to get it with reflection
                    var prop = options.GetType().GetProperty("CallbackPath");
                    if (prop != null && TypeHelper.IsTypeAssignableFrom<PathString>(prop.PropertyType))
                    {
                        //get the value
                        var path = (PathString) prop.GetValue(options);
                        if (path.HasValue)
                        {
                            RoutableDocumentFilter.ReservedPaths.TryAdd(path.ToString());
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Current.Logger.Error(typeof (AuthenticationOptionsExtensions), ex, "Could not read AuthenticationOptions properties");
                }
            }
            else
            {
                RoutableDocumentFilter.ReservedPaths.TryAdd(callbackPath);
            }
        }
    }
}
