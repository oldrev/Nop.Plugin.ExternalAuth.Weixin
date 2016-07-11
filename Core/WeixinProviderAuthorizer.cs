using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication.External;
using Nop.Services.Logging;

namespace Nop.Plugin.ExternalAuth.Weixin.Core {
    public class WeixinProviderAuthorizer : IOAuthProviderWeixinAuthorizer {
        #region Fields

        private readonly IExternalAuthorizer _authorizer;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly WeixinExternalAuthSettings _weixinExternalAuthSettings;
        private readonly HttpContextBase _httpContext;
        private readonly IWebHelper _webHelper;
        private WeixinClient _weixinApplication;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public WeixinProviderAuthorizer(IExternalAuthorizer authorizer,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            WeixinExternalAuthSettings weixinExternalAuthSettings,
            HttpContextBase httpContext,
            ILogger logger,
            IWebHelper webHelper) {
            this._authorizer = authorizer;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._weixinExternalAuthSettings = weixinExternalAuthSettings;
            this._httpContext = httpContext;
            this._logger = logger;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /*
        private string RequestEmailFromWeixin(string accessToken)
        {
            var request = WebRequest.Create("https://graph.weixin.com/me?fields=email&access_token=" + EscapeUriDataStringRfc3986(accessToken));
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var reader = new StreamReader(responseStream);
                    var responseFromServer = reader.ReadToEnd();
                    var userInfo = JObject.Parse(responseFromServer);
                    if (userInfo["email"] != null)
                    {
                        return userInfo["email"].ToString();
                    }
                }
            }

            return string.Empty;
        }
        */

        private WeixinClient WeixinApplication
        {
            get { return _weixinApplication ?? (_weixinApplication = new WeixinClient(
                _weixinExternalAuthSettings.ClientKeyIdentifier,
                _weixinExternalAuthSettings.ClientSecret,
                this._logger)); }
        }

        private AuthorizeState VerifyAuthentication(string returnUrl) {
            var authResult = this.WeixinApplication.VerifyAuthentication(
                _httpContext, GenerateLocalCallbackUri(returnUrl));

            if (authResult.IsSuccessful) {
                if (!authResult.ExtraData.ContainsKey("id"))
                    throw new NopException("Authentication result does not contain id data");

                if (!authResult.ExtraData.ContainsKey("accesstoken"))
                    throw new NopException("Authentication result does not contain accesstoken data");

                var parameters = new OAuthAuthenticationParameters(Provider.SystemName) {
                    ExternalIdentifier = authResult.ProviderUserId,
                    OAuthToken = authResult.ExtraData["accesstoken"],
                    OAuthAccessToken = authResult.ProviderUserId,
                };

                if (_externalAuthenticationSettings.AutoRegisterEnabled)
                    ParseClaims(authResult, parameters);

                var result = _authorizer.Authorize(parameters);

                return new AuthorizeState(returnUrl, result);
            }

            var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
            var error = authResult.Error != null ? authResult.Error.Message : "Unknown error";
            state.AddError(error);
            return state;
        }

        private void ParseClaims(AuthenticationResult authenticationResult, OAuthAuthenticationParameters parameters) {
            var claims = new UserClaims();
            claims.Name = new NameClaims();
            if (authenticationResult.ExtraData.ContainsKey("id"))
            {
                claims.Name.Alias = authenticationResult.ExtraData["id"];
            }
            if (authenticationResult.ExtraData.ContainsKey("nickname"))
            {
                claims.Name.Nickname = authenticationResult.ExtraData["nickname"];
                claims.Name.First = claims.Name.Nickname;
            }

            parameters.AddClaim(claims);
        }

        private AuthorizeState RequestAuthentication(string returnUrl) {
            var authUrl = this.GenerateServiceLoginUrl(returnUrl).AbsoluteUri;
            return new AuthorizeState("", OpenAuthenticationStatus.RequiresRedirect) { Result = new RedirectResult(authUrl) };
        }

        private Uri GenerateLocalCallbackUri(string returnUrl) {
            string url = _webHelper.GetStoreLocation(false) +
                "Plugins/ExternalAuthWeixin/LoginCallback?ReturnUrl=" + returnUrl;
            var uri = new Uri(url);
            return uri;
        }

        private Uri GenerateServiceLoginUrl(string returnUrl) {
            //code copied from DotNetOpenAuth.AspNet.Clients.WeixinClient file
            var redirectUri = HttpUtility.UrlEncode(this.GenerateLocalCallbackUri(returnUrl).AbsoluteUri);
            var appid = this._weixinExternalAuthSettings.ClientKeyIdentifier;
            var url =
                $"https://open.weixin.qq.com/connect/oauth2/authorize?appid={appid}&redirect_uri={redirectUri}&response_type=code&scope=snsapi_userinfo&state=STATE#wechat_redirect";
            _logger.Information($"微信验证：产生登陆 Uri:[{url}]");
            return new Uri(url);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authorize response
        /// </summary>
        /// <param name="returnUrl">Return URL</param>
        /// <param name="verifyResponse">true - Verify response;false - request authentication;null - determine automatically</param>
        /// <returns>Authorize state</returns>
        public AuthorizeState Authorize(string returnUrl, bool? verifyResponse = null) {
            if (!verifyResponse.HasValue)
                throw new ArgumentException("Weixin plugin cannot automatically determine verifyResponse property");

            if (verifyResponse.Value)
                return VerifyAuthentication(returnUrl);

            return RequestAuthentication(returnUrl);
        }

        #endregion
    }
}