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
    using System.Web.Script.Serialization;

    /// <summary>
    /// Handles the request from the Flash viewer to save the current 
    /// document locally.
    /// </summary>
    public class SaveDocument : PccHandler
    {

        public override void ProcessRequest(HttpContext context, Match match)
        {
            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            string viewingSessionID = GetStringFromUrl(context, match, "DocumentID");

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
            // The properties will include the source document name.
            string uriString = PccConfig.ImagingService + "/ViewingSession/u" + HttpUtility.UrlEncode(viewingSessionID);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriString);
            request.Method = "GET";
            request.Headers.Add("acs-api-key", PccConfig.ApiKey);

            // Send request to PCCIS and get response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseBody = null;
            using (StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                responseBody = sr.ReadToEnd();
            }

            // Deserialize the JSON response into a new DocumentProperties instance, which will provide
            // the source document name (a local file name in this case) which was specified by this application
            // during the original POST of the document.
            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);

            string sourceDocument = string.Empty;
            viewingSessionProperties.origin.TryGetValue("sourceDocument", out sourceDocument);

            string uriString1 = PccConfig.ImagingService + "/ViewingSession/u" + HttpUtility.UrlEncode(viewingSessionID) + "/SourceFile";

            HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(uriString1);
            request1.Method = "GET";
            request1.Headers.Add("acs-api-key", PccConfig.ApiKey);

            // Send request to PCCIS and get response for file download
            HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();


            //Clear the header information that was sent by the PCCIS and add new header information with file name
            context.Response.ClearHeaders();
            context.Response.ContentType = "application/octet-stream";
            context.Response.AddHeader("Content-Disposition", "attachment;filename=" + "\"" + Path.GetFileName(sourceDocument) + "\"");

            byte[] buffer = new byte[8192];
            Stream responseBodyFromImagingService = response1.GetResponseStream();

            int totalBytesCopied = 0;
            while (true)
            {
                int bytesRead = responseBodyFromImagingService.Read(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                {
                    break;
                }
                totalBytesCopied += bytesRead;
                context.Response.OutputStream.Write(buffer, 0, bytesRead);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}