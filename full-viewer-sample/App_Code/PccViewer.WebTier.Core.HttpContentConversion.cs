namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using PccViewer.WebTier.Core;
    using System.Text.RegularExpressions;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Drawing;
    using System.Web.Script.Serialization;

    /// <summary>
    /// Handles the request from the viewer to acquire image from the web server
    /// in binary format or base64
    /// </summary>
    public class ContentConversion : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            HttpRequest requestFromClient = context.Request;
            HttpResponse responseToClient = context.Response;
            HttpWebRequest requestToImagingService = null;
            HttpWebResponse responseFromImagingService = null;
            HttpWebRequest requestToWorkFile = null;
            HttpWebResponse responseFromWorkFile = null;

            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");
            string imagingServiceUri = PccConfig.ImagingService;

            try
            {
                string viewingSessionId = context.Request["viewingSessionId"];

                // get the document file extension from the viewing session properties
                ViewingSessionProperties viewingSessionProperties = this.getViewingSessionProperties(viewingSessionId);
                string documentExtension = viewingSessionProperties.documentExtension;

                // download the original document
                requestToImagingService = (HttpWebRequest)WebRequest.Create(imagingServiceUri + "/ViewingSession/u" + viewingSessionId + "/SourceFile");
                requestToImagingService.Method = "GET";
                requestToImagingService.Headers.Add("acs-api-key", PccConfig.ApiKey);
                responseFromImagingService = (HttpWebResponse)requestToImagingService.GetResponse();

                int statusCode = (int)(responseFromImagingService.StatusCode);

                // upload the original document to the work file service where the content conversion service will pick it up from
                if (statusCode == 200)
                {
                    byte[] buffer = new byte[8192];
                    Stream responseBodyFromImagingService = responseFromImagingService.GetResponseStream();
                    requestToWorkFile = (HttpWebRequest)WebRequest.Create(imagingServiceUri + "/WorkFile?FileExtension=" + documentExtension);
                    requestToWorkFile.Method = "POST";
                    requestToWorkFile.Headers.Add("acs-api-key", PccConfig.ApiKey);
                    Stream requestBodyToWorkFile = requestToWorkFile.GetRequestStream();

                    int totalBytesCopied = 0;
                    while (true)
                    {
                        int bytesRead = responseBodyFromImagingService.Read(buffer, 0, buffer.Length);
                        if (bytesRead < 1)
                        {
                            break;
                        }

                        totalBytesCopied += bytesRead;

                        requestToWorkFile.GetRequestStream();
                        requestBodyToWorkFile.Write(buffer, 0, bytesRead);
                    }

                    responseFromWorkFile = (HttpWebResponse)requestToWorkFile.GetResponse();

                    statusCode = (int)(responseFromWorkFile.StatusCode);

                    string responseBody = null;

                    using (StreamReader sr = new StreamReader(responseFromWorkFile.GetResponseStream(), System.Text.Encoding.UTF8))
                    {
                        responseBody = sr.ReadToEnd();
                    }

                    // read the file id assigned to upload document by the work file service
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    Dictionary<string, string> dict = serializer.Deserialize<Dictionary<string, string>>(responseBody);
                    string fileId;
                    dict.TryGetValue("fileId", out fileId);

                    string affinityToken;
                    dict.TryGetValue("affinityToken", out affinityToken);

                    // Kickoff the conversion process
                    string imagingServiceV2Uri = PccConfig.ImagingServiceV2;

                    string requestBody = "{\"input\":{\"src\":{\"fileId\":\"" + fileId + "\"},\"dest\":{\"format\":\"pdf\"}}}";
                    HttpWebRequest requestToConversion = (HttpWebRequest)WebRequest.Create(imagingServiceV2Uri + "/contentConverters");
                    requestToConversion.Method = "POST";
                    requestToConversion.Headers.Add("acs-api-key", PccConfig.ApiKey);
                    requestToConversion.Headers.Add("Accusoft-Affinity-Token", affinityToken);
                    requestToConversion.ContentType = "text/json; charset=utf-8";

                    StreamWriter writer = new StreamWriter(requestToConversion.GetRequestStream());
                    writer.Write(requestBody);
                    writer.Close();

                    HttpWebResponse response = (HttpWebResponse)requestToConversion.GetResponse();
                    responseBody = null;
                    using (StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                    {
                        responseBody = sr.ReadToEnd();
                    }

                    statusCode = (int)(response.StatusCode);

                    // Send the content conversion process data to the client
                    responseToClient.StatusCode = statusCode;
                    responseToClient.ContentType = "application/json";
                    responseToClient.Write(responseBody);
                    responseToClient.OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                WebException webException = ex as WebException;

                if (webException != null && webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    using (HttpWebResponse response = (HttpWebResponse)webException.Response)
                    {
                        responseToClient.StatusCode = (int)response.StatusCode;
                        responseToClient.StatusDescription = response.StatusDescription;
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

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        private ViewingSessionProperties getViewingSessionProperties(String viewingSessionId)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");
            string imagingServiceUri = PccConfig.ImagingService;

            HttpWebRequest requestToImagingService = (HttpWebRequest)WebRequest.Create(imagingServiceUri + "/ViewingSession/u" + viewingSessionId);
            requestToImagingService.Method = "GET";
            requestToImagingService.Headers.Add("acs-api-key", PccConfig.ApiKey);

            HttpWebResponse responseFromImagingService = (HttpWebResponse)requestToImagingService.GetResponse();
            string responseBody = null;
            using (StreamReader sr = new StreamReader(responseFromImagingService.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                responseBody = sr.ReadToEnd();
            }

            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);
            return viewingSessionProperties;
        }
    }
}