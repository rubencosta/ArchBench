
using System;
using System.Collections.Generic;
using HttpServer;

namespace ArchBench.PlugIns.Broker
{

    public class CookieHandler
    {
        private const string CookieName = "gluta_broker";
        private DateTime _cookieExpireDate;
        private readonly string _name;

        public struct ParsedCookie
        {
            public string ServerId;
            public string OriginalCookie;
        }

        public CookieHandler(string aName)
        {
            _name = aName;
        }
        public ParsedCookie ParseCookie(RequestCookies aCookies)
        {
            var parsedCookie = new ParsedCookie();
            foreach (RequestCookie requestCookie in aCookies)
            {
                if (requestCookie.Name.Contains(CookieName) && requestCookie.Name.IndexOf('@') > -1 &&
                    requestCookie.Name.Substring(0, requestCookie.Name.IndexOf('@')) == _name)
                {
                    foreach (var subCookie in DecodeCookie(requestCookie.Value))
                    {
                        if (subCookie.Contains("__broker__"))
                        {
                            if (subCookie.IndexOf('=') > -1)
                                parsedCookie.ServerId = subCookie.Substring(subCookie.IndexOf('=') + 1);
                        }
                        else
                        {
                            if (parsedCookie.OriginalCookie != null)
                                parsedCookie.OriginalCookie += ';' + subCookie;
                            else
                                parsedCookie.OriginalCookie = subCookie;
                        }
                    }
                }
            }
            return parsedCookie;
        }

        public string EncodeSetCookie(int serverId, string originalSetCookie)
        {
            if (originalSetCookie == null)
                return "";
            var encodedSetCookie = string.Format("{0}@{1}=__broker__={2}&{3}", _name, CookieName, serverId,
                originalSetCookie);
            var splitCookie = originalSetCookie.Split(';');
            // Set-Cookie contains Expires attr
            foreach (var cookieOpt in splitCookie)
            {
                if (cookieOpt.IndexOf('=') > -1 && cookieOpt.Substring(0, cookieOpt.IndexOf('=')) == "Expires")
                {
                    SetCookieExpire(DateTime.Parse(cookieOpt.Substring(cookieOpt.IndexOf('=') + 1)));
                    encodedSetCookie += string.Format(";Expires={0}", _cookieExpireDate.ToString("R"));
                }
            }
            return encodedSetCookie;
        }

       
        private IList<string> DecodeCookie(string encoded)
        {
            IList<string> cookies = new List<string>();
            foreach (var cookie in encoded.Split('&'))
            {
                if (cookie.Length > 0)
                    cookies.Add(cookie);
            }
            return cookies;
        }

        private void SetCookieExpire(DateTime date)
        {
            if (date.CompareTo(_cookieExpireDate) > 0)
            {
                _cookieExpireDate = date;
            }
        }

    }



}