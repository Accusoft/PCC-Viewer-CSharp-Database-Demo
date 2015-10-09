namespace PccViewer.WebTier.Core
{
    using System;
    using System.Web;
    using System.IO;
    using System.Net;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using System.Text.RegularExpressions;
    using System.Drawing;
    using System.Web.Script.Serialization;

    /// <summary>
    /// Handles the acquiring and saving of an annotation file for the HTML5 viewer.
    /// Annotation files are tied to a particular document and have an annotation ID.
    /// The annotation ID is useful for tying the annotations to a page within the document.
    /// </summary>
    public class DocumentArt : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            try
            {
                string documentID = GetStringFromUrl(context, match, "DocumentID");
                string annotationID = GetStringFromUrl(context, match, "AnnotationID");
                // make sure target directory exists
                String targetDir = System.IO.Path.GetDirectoryName(PccConfig.MarkupFolder);
                if (!System.IO.Directory.Exists(targetDir))
                {
                    System.IO.Directory.CreateDirectory(targetDir);
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();

                // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
                // The properties will include an identifier of the source document that will be used below
                // to construct the name of file where markups are stored.
                string uriString = PccConfig.ImagingService + "/ViewingSession/u" + HttpUtility.UrlEncode(documentID);
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

                string documentMarkupId = string.Empty;
                viewingSessionProperties.origin.TryGetValue("documentMarkupId", out documentMarkupId);
                string annotationFileName = PccConfig.MarkupFolder + documentMarkupId + "_" + viewingSessionProperties.attachmentIndex + "_" + annotationID + ".xml";

                if (context.Request.RequestType == "POST")
                {
                    using (FileStream annotationFile = new FileStream(annotationFileName, FileMode.Create, FileAccess.Write))
                    {
                        context.Request.InputStream.CopyTo(annotationFile);
                    }
                }
                else
                {
                    context.Response.ContentType = "application/xml";
                    if (File.Exists(annotationFileName))
                    {
                        using (FileStream annotationFile = new FileStream(annotationFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            annotationFile.CopyTo(context.Response.OutputStream);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Write(e.Message);
                context.Response.ContentType = "text/plain";
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}