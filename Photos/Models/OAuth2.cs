using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Photos.Models
{
    public class OAuth2
    {
        public OAuth2()
        {
        }

        public OAuth2(string client_id, string secret_key, string auth_url, string token_url, string redirect_uri)
        {
            _ClientId = client_id;
            _SecretKey = secret_key;
            _AuthUrl = auth_url;
            _TokenUrl = token_url;
            _RedirectURI = redirect_uri;
        }

        string _ClientId = string.Empty;
        public string ClientId
        {
            get
            {
                return _ClientId;
            }
            set
            {
                _ClientId = value;
            }
        }

        string _SecretKey = string.Empty;
        public string SecretKey
        {
            get
            {
                return _SecretKey;
            }
            set
            {
                _SecretKey = value;
            }
        }

        string _AuthUrl = string.Empty;
        public string AuthUrl
        {
            get
            {
                return _AuthUrl;
            }
            set
            {
                _AuthUrl = value;
            }
        }

        string _TokenUrl = string.Empty;
        public string TokenUrl
        {
            get
            {
                return _TokenUrl;
            }
            set
            {
                _TokenUrl = value;
            }
        }

        string _Code = string.Empty;
        public string Code
        {
            get
            {
                return _Code;
            }
            set
            {
                _Code = value;
            }
        }

        string _Token = string.Empty;
        public string Token
        {
            get
            {
                return _Token;
            }
            set
            {
                _Token = value;
            }
        }

        string _RedirectURI = string.Empty;
        public string RedirectURI
        {
            get
            {
                return _RedirectURI;
            }
            set
            {
                _RedirectURI = value;
            }
        }

        public const string OAUTH_CLIENT_ID = "client_id";
        public const string OAUTH_REDIRECT_URI = "redirect_uri";
        public const string OAUTH_EXPIRES = "expires_in";
        public const string OAUTH_ACCESS_TOKEN = "access_token";
        public const string OAUTH_RESPONSE_TYPE = "response_type";
        public const string OAUTH_RESPONSE_TYPE_TOKEN = "token";
        public const string OAUTH_RESPONSE_TYPE_CODE = "code";
        public const string OAUTH_CODE = "code";
        public const string OAUTH_GRANT_TYPE = "grant_type";
        public const string OAUTH_GRANT_TYPE_VALUE = "authorization_code";
        public const string OAUTH_TOKEN = "oauth_token";

        public enum AccessTokenType
        {
            Raw,
            Dictionary,
            JsonDictionary,
            OAuth2Token
        }

        public void GetAuthCode()
        {
            GetAuthCode(_AuthUrl);
        }

        public void GetAuthCode(Dictionary<string, string> additional)
        {
            GetAuthCode(_AuthUrl, additional);
        }

        public void GetAuthCode(string auth_url)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(OAUTH_CLIENT_ID, _ClientId);
            parameters.Add(OAUTH_RESPONSE_TYPE, OAUTH_RESPONSE_TYPE_CODE);
            if (!string.IsNullOrEmpty(_RedirectURI))
                parameters.Add(OAUTH_REDIRECT_URI, _RedirectURI);
            string url = string.Format("{0}{1}", auth_url, OAuthCommonUtils.getParametersString(parameters));
            OAuthCommonUtils.Redirect(url);
        }

        public void GetAuthCode(string auth_url, Dictionary<string, string> additional)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(OAUTH_CLIENT_ID, _ClientId);
            parameters.Add(OAUTH_RESPONSE_TYPE, OAUTH_RESPONSE_TYPE_CODE);
            if (!string.IsNullOrEmpty(_RedirectURI))
                parameters.Add(OAUTH_REDIRECT_URI, _RedirectURI);

            if (additional != null)
                foreach (var item in additional)
                    parameters.Add(item.Key, item.Value);

            string url = string.Format("{0}{1}", auth_url, OAuthCommonUtils.getParametersString(parameters));
            OAuthCommonUtils.Redirect(url);
        }

        public OAuth2Token GetAccessToken(AccessTokenType att)
        {
            return GetAccessToken(_Code, null, att);
        }

        public OAuth2Token GetAccessToken(Dictionary<string, string> additional, AccessTokenType att)
        {
            return GetAccessToken(_Code, additional, att);
        }

        public OAuth2Token GetAccessToken(string code, Dictionary<string, string> additional, AccessTokenType att)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(OAUTH_GRANT_TYPE, OAUTH_GRANT_TYPE_VALUE);
            parameters.Add(OAUTH_CODE, code);
            parameters.Add(OAUTH_CLIENT_ID, _ClientId);
            if (!string.IsNullOrEmpty(_RedirectURI))
                parameters.Add(OAUTH_REDIRECT_URI, _RedirectURI);
            if (additional != null)
                foreach (var item in additional)
                    parameters.Add(item.Key, item.Value);

            string raw_token = OAuthCommonUtils.sendPostRequest(_TokenUrl, OAuthCommonUtils.getParametersString(parameters).Remove(0, 1));
            OAuth2Token token = null;
            switch (att)
            {
                case AccessTokenType.JsonDictionary:
                    token = new OAuth2Token();
                    JavaScriptSerializer s = new JavaScriptSerializer();
                    token.dictionary_token = s.Deserialize<Dictionary<string, string>>(raw_token);
                    break;
                case AccessTokenType.Raw:
                    token = new OAuth2Token();
                    token.raw_token = raw_token;
                    break;
                case AccessTokenType.Dictionary:
                    token = new OAuth2Token();
                    string[] p = raw_token.Split('&');
                    foreach (string item in p)
                    {
                        string[] key_value = item.Split('=');
                        token.dictionary_token.Add(key_value[0], key_value[1]);
                    }
                    break;
                case AccessTokenType.OAuth2Token:
                    try
                    {
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        token = serializer.Deserialize<OAuth2Token>(raw_token);
                        token.raw_token = raw_token;
                    }
                    catch (Exception exc)
                    {
                    }
                    break;
            }

            return token;
        }

    }

    public class OAuth2Token
    {
        public string access_token
        {
            get;
            set;
        }

        public int? expires_in
        {
            get;
            set;
        }

        public string refresh_token
        {
            get;
            set;
        }

        public string x_mailru_vid
        {
            get;
            set;
        }

        public string raw_token
        {
            get;
            set;
        }

        Dictionary<string, string> _dictionary_token = new Dictionary<string, string>();
        public Dictionary<string, string> dictionary_token
        {
            get
            {
                return _dictionary_token;
            }
            set
            {
                _dictionary_token = value;
            }
        }
    }

    public static class OAuthCommonUtils
    {
        public static string getTimestamp(int delta)
        {
            return ((int)((TimeSpan)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds + delta).ToString();
        }

        public static string getNonce()
        {
            Random rnd = new Random();
            int nonce = rnd.Next(1, Int32.MaxValue);
            MD5 md5 = MD5.Create();
            byte[] b = md5.ComputeHash(Encoding.UTF8.GetBytes(nonce.ToString()));
            return string.Concat(b.Select(x => x.ToString("x2")));
        }

        static string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        public static string UrlEncode(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char symbol in value)
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                    result.Append(symbol);
                else
                    result.AppendFormat("%{0:X2}", (int)symbol);
            }
            return result.ToString();
        }

        public static string getBasesign(string method, string baseUrl, Dictionary<string, string> items)
        {
            string parameters = string.Join("%26", items.OrderBy(x => x.Key).Select(x => string.Format("{0}%3D{1}", UrlEncode(x.Key), UrlEncode(x.Value))));
            return string.Format("{2}&{0}&{1}", UrlEncode(baseUrl), parameters, method);
        }

        public static string getHMACSHA1(string key, string token_secret, string sourceStr)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            string token_secret_encoded = string.Empty;
            if (!string.IsNullOrEmpty(token_secret))
                token_secret_encoded = UrlEncode(token_secret);
            HMACSHA1 hmacsha1 = new HMACSHA1(encoding.GetBytes(HttpUtility.UrlEncode(key) + "&" + token_secret_encoded));
            byte[] encoded = hmacsha1.ComputeHash(encoding.GetBytes(sourceStr));
            return Convert.ToBase64String(encoded);
        }

        public static string getAuthHeader(Dictionary<string, string> items)
        {
            string result = string.Empty;
            result = string.Format("OAuth {0}", string.Join(", ", items.Select(x => string.Format("{0}=\"{1}\"", x.Key, OAuthCommonUtils.UrlEncode(x.Value)))));
            return result;
        }

        public static string getParametersString(Dictionary<string, string> items)
        {
            return string.Format("?{0}", string.Join("&", items.Select(x => string.Format("{0}={1}", UrlEncode(x.Key), UrlEncode(x.Value)))));
        }

        public static Dictionary<string, string> sendGetRequest(string url, bool IsExpect, string OAuthHeader)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";

            if (IsExpect)
                httpWebRequest.ServicePoint.Expect100Continue = false;

            if (!string.IsNullOrEmpty(OAuthHeader))
                httpWebRequest.Headers.Add("Authorization", OAuthHeader);

            Dictionary<string, string> dict = new Dictionary<string, string>();
            try
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                string response = string.Empty;
                using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
                    response = sr.ReadToEnd();
                string[] parameters = response.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string item in parameters)
                {
                    string[] p = item.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length == 2)
                        dict.Add(p[0], HttpUtility.UrlDecode(p[1]));
                }
                dict.Add("raw_response", response);
            }
            catch (WebException e)
            {
                string excmsg = string.Empty;
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream()))
                {
                    excmsg = sr.ReadToEnd();
                }
                throw new WebException(excmsg);
            }
            return dict;
        }

        public static void Redirect(string url)
        {
            HttpContext.Current.Response.Redirect(url, true);
        }

        public static string sendPostRequest(string url, string parameters)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "POST";
            httpWebRequest.Accept = "*/*";
            httpWebRequest.AllowAutoRedirect = true;
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            byte[] data = Encoding.UTF8.GetBytes(parameters);
            httpWebRequest.ContentLength = data.Length;
            httpWebRequest.GetRequestStream().Write(data, 0, data.Length);
            string raw_token = string.Empty;
            try
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    raw_token = sr.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                string exception = string.Empty;
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream()))
                {
                    exception = sr.ReadToEnd();
                }
                throw new WebException(exception);
            }
            return raw_token;
        }

        public static string GetPlainTextResponse(string url)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";
            httpWebRequest.AllowAutoRedirect = true;
            string response = "";
            try
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response = sr.ReadToEnd();
                }
            }
            catch (WebException exc)
            {
                string excmsg = string.Empty;
                using (StreamReader sr = new StreamReader(exc.Response.GetResponseStream()))
                {
                    excmsg = sr.ReadToEnd();
                }
                throw new WebException(excmsg);
            }
            return response;
        }
    }

    public static class OAuth2UserData
    {
        const string FACEBOOK_ME_URL = "https://graph.facebook.com/me";
        const string YANDEX_ME_URL = "https://api-yaru.yandex.ru/me/";
        const string MAILRU_ME_URL = "http://www.appsmail.ru/platform/api";
        const string VK_ME_URL = "https://api.vkontakte.ru/method/getProfiles";
        const string LIVE_ME_URL = "https://apis.live.net/v5.0/me";
        const string ODNOKLASSNIKI_ME_URL = "http://api.odnoklassniki.ru/fb.do";

        const string OAUTH_TOKEN = "oauth_token";
        const string FB_OAUTH_TOKEN = "access_token";
        const string MAILRU_SIG = "sig";
        const string LIVE_ACCESS_TOKEN = "access_token";

        public static string GetUserData(string me_url, Dictionary<string, string> parameters)
        {
            string url = string.Format("{0}{1}", me_url, OAuthCommonUtils.getParametersString(parameters));
            return OAuthCommonUtils.GetPlainTextResponse(url);
        }

        public static string GetYandexUserData(string access_token)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> { { OAUTH_TOKEN, access_token } };
            return GetUserData(YANDEX_ME_URL, parameters);
        }

        public static string GetVKUserData(string access_token, Dictionary<string, string> parameters)
        {
            parameters.Add(LIVE_ACCESS_TOKEN, access_token);
            return GetUserData(VK_ME_URL, parameters);
        }

        public static string GetOdnoklassnikiUserData(string access_token, string secret_key, Dictionary<string, string> dict)
        {
            string sig = GetSig(GetMD5(access_token + secret_key), dict);
            dict.Add("access_token", access_token);
            dict.Add("sig", sig);
            return GetUserData(ODNOKLASSNIKI_ME_URL, dict);
        }

        public static string GetLiveUserData(string access_token)
        {
            return GetUserData(LIVE_ME_URL, new Dictionary<string, string>() { { LIVE_ACCESS_TOKEN, access_token } });
        }

        public static string GetFacebookUserData(string access_token)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> { { FB_OAUTH_TOKEN, access_token } };
            return GetUserData(FACEBOOK_ME_URL, parameters);
        }

        public static string GetMailRuUserData(string secretKey, Dictionary<string, string> parameters)
        {
            string sig = GetSig(secretKey, parameters);
            parameters.Add(MAILRU_SIG, sig);
            return GetUserData(MAILRU_ME_URL, parameters);
        }

        private static string GetSig(string secretKey, Dictionary<string, string> p)
        {
            string pp = string.Concat(p.OrderBy(x => x.Key).Select(x => string.Format("{0}={1}", x.Key, x.Value)).ToList());
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(pp + secretKey));

            string sig = string.Concat(hash.Select(x => x.ToString("x2")).ToList());
            return sig;
        }

        private static string GetMD5(string key)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            return string.Concat(hash.Select(x => x.ToString("x2")).ToList());
        }

    }
}