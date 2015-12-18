using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Pcc
{
    public class PrizmApplicationServices
    {
        /// <summary>
        /// Forwards any given request to PrizmApplicationServices
        /// </summary>
        /// <param name="context">The HTTP Context that will be used to access both request and response</param>
        /// <param name="path">The path that will be used to invoke PrizmApplicationServices</param>
        public static void ForwardRequest(HttpContext context, string path)
        {
            // Create a request with same data in navigator request
            var request = GetProxyRequest(context, context.Request.HttpMethod, path, context.Request.Url.Query);

            // Send the request to the remote server and return the response
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
            }

            // Set current response based on the PAS response
            UpdateContextResponse(context, response);

            // Close streams
            response.Close();
            context.Response.End();
        }

        /// <summary>
        /// Creates a new viewing session from a file
        /// </summary>
        /// <param name="filePath">The full path of the file that will be uploaded</param>
        public static string CreateSessionFromFileUpload(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var query = new NameValueCollection();
            query["fileId"] = fileInfo.Name;
            query["fileExtension"] = fileInfo.Extension;
            var emptySessionRequest = GetProxyRequestWithQuery(HttpContext.Current, "GET", "/CreateSession", query);
            var response = (HttpWebResponse)emptySessionRequest.GetResponse();
            var json = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var responseData = JsonToDictionary(json);
            var viewingSessionId = responseData["viewingSessionId"].ToString();

            var uploadUrl = PccConfig.WebTierAddress + string.Format("/CreateSession/u{0}", viewingSessionId);
            using (var client = new WebClient())
            {
                client.UploadFile(uploadUrl, "PUT", filePath);
            }
            return viewingSessionId;
        }

        /// <summary>
        /// Creates a new viewing session from an existing document in document storage
        /// </summary>
        /// <param name="documentName">The name of the document</param>
        public static string CreateSessionFromDocument(string documentName)
        {
            var query = new NameValueCollection();
            query["document"] = documentName;
            var request = GetProxyRequestWithQuery(HttpContext.Current, "GET", "/CreateSession", query);
            var response = (HttpWebResponse)request.GetResponse();
            var json = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var responseData = JsonToDictionary(json);
            var viewingSessionId = responseData["viewingSessionId"].ToString();
            return viewingSessionId;
        }

        /// <summary>
        /// Creates a new viewing session from an existing form
        /// </summary>
        /// <param name="formId">The ID of the previously saved form</param>
        public static string CreateSessionFromForm(string formId)
        {
            var query = new NameValueCollection();
            query["form"] = formId;
            var request = GetProxyRequestWithQuery(HttpContext.Current, "GET", "/CreateSession", query);
            var response = (HttpWebResponse)request.GetResponse();
            var json = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var responseData = JsonToDictionary(json);
            var viewingSessionId = responseData["viewingSessionId"].ToString();
            return viewingSessionId;
        }

        private static HttpWebRequest GetProxyRequest(HttpContext context, string method, string path, string query)
        {
            var cookieContainer = new CookieContainer();

            // Create a request to the server
            var request = CreatePasRequest(method, path, query);
            request.KeepAlive = true;
            request.CookieContainer = cookieContainer;

            // Set special headers
            if (context.Request.AcceptTypes != null && context.Request.AcceptTypes.Any())
            {
                request.Accept = string.Join(",", context.Request.AcceptTypes);
            }
            request.ContentType = context.Request.ContentType;
            request.UserAgent = context.Request.UserAgent;

            // Copy headers
            foreach (var headerKey in context.Request.Headers.AllKeys)
            {
                if (WebHeaderCollection.IsRestricted(headerKey))
                {
                    continue;
                }
                request.Headers[headerKey] = context.Request.Headers[headerKey];
            }

            // Send Cookie extracted from the original request
            for (var i = 0; i < context.Request.Cookies.Count; i++)
            {
                var navigatorCookie = context.Request.Cookies[i];
                var c = new Cookie(navigatorCookie.Name, navigatorCookie.Value)
                {
                    Domain = request.RequestUri.Host,
                    Expires = navigatorCookie.Expires,
                    HttpOnly = navigatorCookie.HttpOnly,
                    Path = navigatorCookie.Path,
                    Secure = navigatorCookie.Secure
                };
                cookieContainer.Add(c);
            }

            // Write the body extracted from the incoming request
            if (request.Method != "GET"
                && request.Method != "HEAD")
            {
                context.Request.InputStream.Position = 0;
                var clientStream = context.Request.InputStream;
                var clientPostData = new byte[context.Request.InputStream.Length];
                clientStream.Read(clientPostData, 0,
                                 (int)context.Request.InputStream.Length);

                request.ContentType = context.Request.ContentType;
                request.ContentLength = clientPostData.Length;
                var stream = request.GetRequestStream();
                stream.Write(clientPostData, 0, clientPostData.Length);
                stream.Close();
            }

            return request;

        }

        private static HttpWebRequest GetProxyRequestWithQuery(HttpContext context, string method, string path = "", NameValueCollection query = null)
        {
            var queryString = "";
            if (query != null)
            {
                queryString = ToQueryString(query);
            }
            return GetProxyRequest(context, method, path, queryString);
        }

        private static byte[] GetResponseStreamBytes(WebResponse response)
        {
            const int bufferSize = 256;
            var buffer = new byte[bufferSize];
            var memoryStream = new MemoryStream();

            var responseStream = response.GetResponseStream();
            var remoteResponseCount = responseStream.Read(buffer, 0, bufferSize);

            while (remoteResponseCount > 0)
            {
                memoryStream.Write(buffer, 0, remoteResponseCount);
                remoteResponseCount = responseStream.Read(buffer, 0, bufferSize);
            }

            var responseData = memoryStream.ToArray();

            memoryStream.Close();
            responseStream.Close();

            memoryStream.Dispose();
            responseStream.Dispose();

            return responseData;
        }

        private static void UpdateContextResponse(HttpContext context, HttpWebResponse response)
        {
            // Copy headers
            foreach (var headerKey in response.Headers.AllKeys)
            {
                if (WebHeaderCollection.IsRestricted(headerKey))
                {
                    continue;
                }
                context.Response.AddHeader(headerKey, response.Headers[headerKey]);
            }

            context.Response.ContentType = response.ContentType;

            context.Response.Cookies.Clear();

            foreach (Cookie receivedCookie in response.Cookies)
            {
                var c = new HttpCookie(receivedCookie.Name,
                    receivedCookie.Value)
                {
                    Domain = context.Request.Url.Host,
                    Expires = receivedCookie.Expires,
                    HttpOnly = receivedCookie.HttpOnly,
                    Path = receivedCookie.Path,
                    Secure = receivedCookie.Secure
                };
                context.Response.Cookies.Add(c);
            }

            var responseData = GetResponseStreamBytes(response);

            // Send the response to client
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = response.ContentType;
            context.Response.OutputStream.Write(responseData, 0,
                             responseData.Length);

            context.Response.StatusCode = (int)response.StatusCode;
        }

        private static HttpWebRequest 

CreatePasRequest(string method, string path = "", string queryString = "")
        {
            queryString = queryString ?? "";
            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Remove(0, 1);
            }
            var uriBuilder = new UriBuilder(PccConfig.PrizmApplicationServicesScheme, PccConfig.PrizmApplicationServicesHost,
                PccConfig.PrizmApplicationServicesPort, path)
            { Query = queryString };

            var url = uriBuilder.ToString();
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method.ToUpper().Trim();
            return request;
        }

        private static string ToQueryString(NameValueCollection nvc)
        {
            var list = new List<string>();
            foreach (var key in nvc.AllKeys)
            {
                foreach (var value in nvc.GetValues(key))
                {
                    list.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)));
                }
            }

            return string.Join("&", list.ToArray());
        }

        private static Dictionary<string, object> JsonToDictionary(string json)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, object>>(json);
        }
    }
}