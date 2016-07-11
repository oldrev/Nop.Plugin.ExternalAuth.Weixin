//-----------------------------------------------------------------------
// <copyright file="FacebookClient.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Web.Script.Serialization;
using System.Web;
using System.Linq;
using System.Text;
using DotNetOpenAuth.Messaging;
using Nop.Services.Logging;

namespace DotNetOpenAuth.AspNet.Clients {

    /// <summary>
        /// The facebook client.
        /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Weixin", Justification = "Brand name")]
    public sealed class WeixinClient : OAuth2Client {
        #region Constants and Fields

        private const string AuthorizationEndpoint = "https://open.weixin.qq.com/connect/oauth2/authorize";
        private const string TokenEndpoint = "https://api.weixin.qq.com/sns/oauth2/access_token";
        private const string UserInfoEndpoint = "https://api.weixin.qq.com/sns/userinfo";

        private readonly string appId;

        private string openId;
        private readonly ILogger _logger;

        /// <summary>
                /// The _app secret.
                /// </summary>
        private readonly string appSecret;

        #endregion

        #region Constructors and Destructors

        /// <summary>
                /// Initializes a new instance of the <see cref="FacebookClient"/> class.
                /// </summary>
                /// <param name="appId">
                /// The app id.
                /// </param>
                /// <param name="appSecret">
                /// The app secret.
                /// </param>
        public WeixinClient(string appId, string appSecret, ILogger logger) : base("facebook") {
            if (string.IsNullOrEmpty(appId)) {
                throw new ArgumentNullException("appId");
            }
            if (string.IsNullOrEmpty(appSecret)) {
                throw new ArgumentNullException("appSecret");
            }

            this.appId = appId;
            this.appSecret = appSecret;
            this._logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get service login url.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <returns>An absolute URI.</returns>
        protected override Uri GetServiceLoginUrl(Uri returnUrl) {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(AuthorizationEndpoint);
            var returnUri = HttpUtility.UrlEncode(returnUrl.AbsolutePath);
            var queryStr = $"appid={this.appId}&redirect_uri={returnUri}&response_type=code&scope=snsapi_userinfo&state=STATE#wechat_redirect";
            builder.Query = queryStr;
            return builder.Uri;
        }

        /// <summary>
        /// The get user data.
        /// </summary>
        /// <param name="accessToken">
        /// The access token.
        /// </param>
        /// <returns>A dictionary of profile data.</returns>
        protected override IDictionary<string, string> GetUserData(string accessToken) {
            var url = $"{UserInfoEndpoint}?access_token={accessToken}&openid={this.openId}&lang=zh_CN";
            _logger.Information($"正在获取用户信息: [{url}]");
            using (var wc = new WebClient()) {
                wc.Encoding = Encoding.UTF8;
                var resultText = wc.DownloadString(url);
                _logger.Information($"获取用户信息结果: [{resultText}]");
                var jss = new JavaScriptSerializer();
                _logger.Information($"正在获取用户信息: [{url}]");
                var returnData = jss.Deserialize<Dictionary<string, object>>(resultText);
                if (returnData.ContainsKey("errcode")) {
                    throw new ApplicationException($"获取微信用户信息发生错误：" + returnData["errmsg"].ToString());
                }
                var userData = new Dictionary<string, string>();
                userData["openid"] = returnData["openid"].ToString();
                userData["id"] = returnData["openid"].ToString();
                userData["nickname"] = returnData["nickname"].ToString();
                userData["sex"] = returnData["sex"].ToString();
                userData["language"] = returnData["language"].ToString();
                userData["city"] = returnData["city"].ToString();
                userData["province"] = returnData["province"].ToString();
                userData["country"] = returnData["country"].ToString();
                userData["headimgurl"] = returnData["headimgurl"].ToString();
                return userData;
            }
        }

        /// <summary>
        /// Obtains an access token given an authorization code and callback URL.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <param name="authorizationCode">
        /// The authorization code.
        /// </param>
        /// <returns>
        /// The access token.
        /// </returns>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode) {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(TokenEndpoint);
            var queryStr = $"appid={this.appId}&secret={this.appSecret}&code={authorizationCode}&grant_type=authorization_code";
            builder.Query = queryStr;
            _logger.Information($"正在查询获取 accessToken: [{builder.Uri}]");
            using (WebClient client = new WebClient()) {
                client.Encoding = Encoding.UTF8;
                string resultText = client.DownloadString(builder.Uri);
                _logger.Information($"已获取结果: [{resultText}]");
                if (!string.IsNullOrEmpty(resultText)) {
                    var jss = new JavaScriptSerializer();
                    var resultData = jss.Deserialize<Dictionary<string, string>>(resultText);
                    if (resultData.ContainsKey("errcode")) {
                        return null;
                    }
                    if (resultData.ContainsKey("openid")) {
                        this.openId = resultData["openid"];
                    }
                    return resultData["access_token"];
                }
                return null;
            }
        }

        /// <summary>
        /// Converts any % encoded values in the URL to uppercase.
        /// </summary>
        /// <param name="url">The URL string to normalize</param>
        /// <returns>The normalized url</returns>
        /// <example>NormalizeHexEncoding("Login.aspx?ReturnUrl=%2fAccount%2fManage.aspx") returns "Login.aspx?ReturnUrl=%2FAccount%2FManage.aspx"</example>
        /// <remarks>
        /// There is an issue in Facebook whereby it will rejects the redirect_uri value if
        /// the url contains lowercase % encoded values.
        /// </remarks>
        private static string NormalizeHexEncoding(string url) {
            var chars = url.ToCharArray();
            for (int i = 0; i < chars.Length - 2; i++) {
                if (chars[i] == '%') {
                    chars[i + 1] = char.ToUpperInvariant(chars[i + 1]);
                    chars[i + 2] = char.ToUpperInvariant(chars[i + 2]);
                    i += 2;
                }
            }
            return new string(chars);
        }

        #endregion
    }
}