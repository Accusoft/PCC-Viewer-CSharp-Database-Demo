namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using PccViewer.WebTier.Core;
    using System.Text.RegularExpressions;
    using System.Web.Script.Serialization;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Handles the request from the viewer to acquire a list of 
    /// annotation files associated with the current document.
    /// </summary>
    public class AnnotationList : PccHandler
    {

        public override void ProcessRequest(HttpContext context, Match match)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            string origDocument = GetStringFromUrl(context, match, "DocumentID");

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
            // The properties will include an identifier of the source document that will be used below
            // to locate the name of file where the markups for the current document were written.
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

            // Deserialize the JSON response into a new DocumentProperties instance, which will provide
            // the original document name (a local file name in this case) which was specified by this application
            // during the original POST of the document.
            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);
            int i = 1;

            //location of the saved markup xmls. This could be on webserver or network storage or database.
            DirectoryInfo di = new DirectoryInfo(PccConfig.MarkupFolder);
            FileInfo[] rgFiles = di.GetFiles("*.xml");
            string documentMarkupId = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            char[] charsToTrim = { ',' };

            viewingSessionProperties.origin.TryGetValue("documentMarkupId", out documentMarkupId);
            foreach (FileInfo fi in rgFiles)
            {
                if (fi.Name.Contains(documentMarkupId + "_" + viewingSessionProperties.attachmentIndex))
                {
                    string fileName = fi.Name;
                    fileName = fi.Name.Replace(documentMarkupId + "_" + viewingSessionProperties.attachmentIndex + "_", "").Replace(".xml", "");
                    sb1.AppendFormat("{{\"ID\": \"{0}\", \"annotationName\": \"{1}\", \"annotationLabel\": \"{2}\"}},", i.ToString(), fi.Name, fileName);
                    i++;
                }
            }

            sb1.ToString().TrimEnd(charsToTrim);
            sb.Append("{\"annotationFiles\":[");
            sb.Append(sb1.ToString().TrimEnd(charsToTrim));
            sb.Append("] }");
            context.Response.Write(sb.ToString());
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