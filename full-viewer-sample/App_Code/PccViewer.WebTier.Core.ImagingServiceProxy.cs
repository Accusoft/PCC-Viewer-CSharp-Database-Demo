namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using PccViewer.WebTier.Core;

    public class ImagingServiceProxy : PccHandler
    {
        public string[] RequestTypeWhiteList { get; set; }
        public string[] QueryParameterWhiteList { get; set; }
        public string[] RequestHeaderWhiteList { get; set; }
        public string[] ResponseHeaderWhiteList { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }
        public string serviceVersion = "V1";
        public ImagingServiceProxy(string serviceVersion = "V1")
        {
            this.serviceVersion = serviceVersion;
            this.RequestTypeWhiteList = new string[] { "GET", "POST" };
            PccConfig.LoadConfig("viewer-webtier/pcc.config");
        }

        public override void ProcessRequest(HttpContext context, Match match)
        {
            HttpRequest requestFromClient = context.Request;
            HttpResponse responseToClient = context.Response;

            // Verify that the request type is acceptable.
            if (this.RequestTypeWhiteList != null)
            {
                bool requestIsAcceptable = false;
                foreach (string type in this.RequestTypeWhiteList)
                {
                    if (requestFromClient.RequestType == type)
                    {
                        requestIsAcceptable = true;
                        break;
                    }
                }
                if (!requestIsAcceptable)
                {
                    throw new Exception("The request type is not acceptable.");
                }
            }

            string imagingServiceUri = "";
            if (this.serviceVersion == "v2")
            {
                imagingServiceUri = PccConfig.ImagingServiceV2 + context.Request.PathInfo;
            }
            else
            {
                imagingServiceUri = PccConfig.ImagingService + context.Request.PathInfo;
            }

            // Add only the white-listed query parameters to the outgoing request.
            string queryParameters = "";
            if (this.QueryParameterWhiteList != null)
            {
                foreach (string key in this.QueryParameterWhiteList)
                {
                    string data = requestFromClient.QueryString[key];
                    if (data != null)
                    {
                        if (queryParameters != "")
                        {
                            queryParameters += "&";
                        }
                        queryParameters += key + "=" + HttpUtility.UrlEncode(data);
                    }
                }
            }
            if (queryParameters != "")
            {
                imagingServiceUri += "?" + queryParameters;
            }

            HttpWebRequest requestToImagingService = null;
            HttpWebResponse responseFromImagingService = null;
            //string responseBody = null;
            //string responseBodyInHex = null;
            //string requestBody = null;
            //string requestBodyInHex = null;
            DateTime startTime = DateTime.Now;

            try
            {
                requestToImagingService = (HttpWebRequest)WebRequest.Create(imagingServiceUri);
                requestToImagingService.Method = requestFromClient.RequestType;

                // Add specific headers to the request to the imaging service
                if (this.RequestHeaders != null)
                {
                    foreach (KeyValuePair<string, string> requestHeader in this.RequestHeaders)
                    {
                        requestToImagingService.Headers.Add(requestHeader.Key, requestHeader.Value);
                    }
                }

                // Add only the white-listed request header items to the outgoing request.
                if (this.RequestHeaderWhiteList != null)
                {
                    foreach (string key in this.RequestHeaderWhiteList)
                    {
                        string data = requestFromClient.Headers[key];
                        if (data != null)
                        {
                            requestToImagingService.Headers.Add(key, data);
                        }
                    }
                }
                requestToImagingService.Headers.Add("acs-api-key", PccConfig.ApiKey);

                if (requestFromClient.RequestType == "POST" || requestFromClient.RequestType == "PUT")
                {
                    byte[] buffer = new byte[8192];
                    Stream requestBodyFromClient = requestFromClient.InputStream;
                    Stream requestBodyToImagingService = requestToImagingService.GetRequestStream();

                    int totalBytesCopied = 0;
                    while (true)
                    {
                        int bytesRead = requestBodyFromClient.Read(buffer, 0, buffer.Length);
                        if (bytesRead < 1)
                        {
                            break;
                        }
                        totalBytesCopied += bytesRead;

                        requestToImagingService.GetRequestStream();
                        requestBodyToImagingService.Write(buffer, 0, bytesRead);
                    }
                }

                responseFromImagingService = (HttpWebResponse)requestToImagingService.GetResponse();

                // Add only the white-listed response header items to the response (plus the status code).
                int statusCode = (int)(responseFromImagingService.StatusCode);
                if (statusCode == 0)
                {
                    // The imaging service currently returns 0 status sometimes.
                    statusCode = 200;
                }
                responseToClient.StatusCode = statusCode;

                if (this.ResponseHeaderWhiteList != null)
                {
                    foreach (string key in this.ResponseHeaderWhiteList)
                    {
                        string data = responseFromImagingService.Headers[key];
                        if (data != null)
                        {
                            responseToClient.AppendHeader(key, data);
                        }
                    }
                }
                if (this.ResponseHeaders != null)
                {
                    foreach (KeyValuePair<string, string> responseHeader in this.ResponseHeaders)
                    {
                        responseToClient.AppendHeader(responseHeader.Key, responseHeader.Value);
                    }
                }
                // return the body of the reponse only if it did not fail.
                if (statusCode == 200)
                {
                    byte[] buffer = new byte[8192];
                    Stream responseBodyFromImagingService = responseFromImagingService.GetResponseStream();

                    int totalBytesCopied = 0;
                    while (true)
                    {
                        int bytesRead = responseBodyFromImagingService.Read(buffer, 0, buffer.Length);
                        if (bytesRead < 1)
                        {
                            break;
                        }
                        totalBytesCopied += bytesRead;

                        responseToClient.OutputStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                // return error info.
                //if (ex is WebException)
                //{
                //    WebException webEx = (WebException)ex;
                //    HttpWebResponse res = (HttpWebResponse)webEx.Response;
                //    responseToClient.StatusCode = (int)res.StatusCode;
                //    responseToClient.StatusDescription = res.StatusDescription;
                //}
                //else
                //{
                //    responseToClient.StatusCode = 500;
                //}

                WebException webException = ex as WebException;
                if (webException != null && webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    using (HttpWebResponse response = (HttpWebResponse)webException.Response)
                    {
                        responseToClient.StatusCode = (int)response.StatusCode;
                        responseToClient.StatusDescription = response.StatusDescription;

                        foreach (string key in this.ResponseHeaderWhiteList)
                        {
                            string data = webException.Response.Headers[key];
                            if (data != null)
                            {
                                responseToClient.AppendHeader(key, data);
                            }
                        }
                    }
                }
                else
                {
                    responseToClient.StatusCode = 500;
                    if (webException != null && webException.Status != null)
                    {
                        responseToClient.StatusDescription = "Internal Server Error: " + webException.Status.ToString();
                    }
                    else
                    {
                        responseToClient.StatusDescription = "Internal Server Error";
                    }
                }
            }
        }
    }
}
