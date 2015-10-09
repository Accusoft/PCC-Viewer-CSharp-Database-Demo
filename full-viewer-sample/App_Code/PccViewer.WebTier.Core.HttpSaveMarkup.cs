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
    /// annotations to a file.
    /// </summary>
    public class SaveMarkup : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            string path2Save = PccConfig.MarkupFolder;
            string origDocument = PccViewer.WebTier.Core.Encoder.DecodeURLString(context.Request.Form["fileName"]);
            string xml = context.Request.Form["annotations"];
            string markupName = context.Request.Form["annotationName"];
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
            // The properties will include an identifier of the source document that will be used below
            // to construct the name of file to which the markups will be written.
            string uriString = PccConfig.ImagingService + "/ViewingSession/u" + HttpUtility.UrlEncode(origDocument);
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

            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);

            // Construct the name of the file to which the markup will be written based on the 
            // document identifier this application sent to PCCIS in the original document POST request.
            string documentMarkupId = string.Empty;
            viewingSessionProperties.origin.TryGetValue("documentMarkupId", out documentMarkupId);
            string fileName = path2Save + documentMarkupId + "_" + viewingSessionProperties.attachmentIndex + "_" + markupName + ".xml";

            using (StreamWriter sr = new StreamWriter(fileName))
            {
                sr.WriteLine(xml);
            }

            // Return successful response
            context.Response.Write("<root><saveAnnotationResponse saved='1'/></root>");
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