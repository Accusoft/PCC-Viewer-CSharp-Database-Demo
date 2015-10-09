namespace PccViewer.WebTier.Core
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web;
    using System.IO;
    using System.Collections.Generic;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using System.Text.RegularExpressions;
    using System.Web.Script.Serialization;
    using System.Threading.Tasks;

    public class CreateSession : PccHandler
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        
        public override void ProcessRequest(HttpContext context, Match match)
        {
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            string documentQueryParameter = context.Request.QueryString["document"];
            string viewingSessionId = string.Empty;

            if (!String.IsNullOrEmpty(documentQueryParameter)) {
                viewingSessionId = fromDocumentName(documentQueryParameter);
            }

            if (!String.IsNullOrEmpty(viewingSessionId)) {
                var json = new Dictionary<string, object>();
                json.Add("viewingSessionId", viewingSessionId);

                context.Response.ContentType = "application/json";
                context.Response.Write(toJSON(json));
                context.Response.StatusCode = (int)HttpStatusCode.OK;

                return;
            }

            var error = new Dictionary<string, object>();
            
            context.Response.ContentType = "application/json";
            context.Response.Write(toJSON(error));
            context.Response.StatusCode = 480;
            context.Response.StatusDescription = "Resource Does Not Exist";
        }

        private string toJSON(Object obj)
        {
            return serializer.Serialize(obj);
        }

        private Dictionary<string, object> parseJSON(String jsonStr)
        {
            return serializer.Deserialize<Dictionary<string, object>>(jsonStr);
        }

        public string fromDocumentName(String documentName) {
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            // Get the full document path
            string documentPath = Path.Combine(PccConfig.DocumentFolder, documentName); ;
            // Get the document's extension because PCCIS will need it later.
            string extension = System.IO.Path.GetExtension(documentPath).TrimStart(new char[] { '.' }).ToLower();

            Stream documentStream = new FileStream(documentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Create a viewing session using the stream
            string viewingSessionId = fromStream(documentStream, documentPath, extension);

            documentStream.Dispose();

            return viewingSessionId;
        }

        public string fromStream(Stream fileStream, String documentId, String fileExtension) {
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            Stream streamCopy = new MemoryStream();
            // makre sure the stream is at the beginning
            fileStream.Position = 0;
            fileStream.CopyTo(streamCopy);
            // reset the stream back to the beginning, just in case
            fileStream.Position = 0;
            
            string[] transferProtocols = { "http://", "https://", "ftp://" };
            string document = string.Empty;
            string viewingSessionId = string.Empty;

            // Construct the full path to the source document
            if (transferProtocols.Any(documentId.Contains))
            {
                document = documentId;
            }
            else
            {
                document = Path.Combine(PccConfig.DocumentFolder, documentId);
            }

            // Request a new viewing session from PCCIS.
            //   POST http://localhost:18681/PCCIS/V1/ViewingSession
            // 
            string uriString = string.Format("{0}/ViewingSession", PccConfig.ImagingService);
            string documentHash = PccViewer.WebTier.Core.Encoder.GetHashString(documentId);
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriString);
            request.Method = "POST";
            request.Headers.Add("acs-api-key", PccConfig.ApiKey);
            request.Headers.Add("Accusoft-Affinity-Hint", documentHash);

            using (StreamWriter requestStream = new StreamWriter(request.GetRequestStream(), Encoding.UTF8))
            {
                ViewingSessionProperties viewingSessionProperties = new ViewingSessionProperties();

                // Store some information in PCCIS to be retrieved later.
                viewingSessionProperties.tenantId = "My User ID";

                viewingSessionProperties.documentExtension = fileExtension;

                // The following are examples of arbitrary information as key-value 
                // pairs that PCCIS will associate with this document request.
                Dictionary<string, string> originInfo = new Dictionary<string, string>();
                originInfo.Add("ipAddress", HttpContext.Current.Request.UserHostAddress);
                originInfo.Add("hostName", HttpContext.Current.Request.UserHostName);
                originInfo.Add("sourceDocument", documentId);
                originInfo.Add("documentMarkupId", documentHash);
                viewingSessionProperties.origin = originInfo;

                // Specify rendering properties.
                viewingSessionProperties.render = new RenderProperties() { 
                    flash = new FlashRenderProperties() { 
                        optimizationLevel = 1 
                    }, 
                    html5 = new Html5RenderProperties { 
                        alwaysUseRaster = false 
                    } 
                };

                // Serialize document properties as JSON which will go into the body of the request
                string requestBody = serializer.Serialize(viewingSessionProperties);
                requestStream.Write(requestBody);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseBody = null;
            using (StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                responseBody = sr.ReadToEnd();
            }

            // Store the ID for this viewing session that is returned by PCCIS
            Dictionary<string, object> responseValues = (Dictionary<string, object>)serializer.DeserializeObject(responseBody);
            viewingSessionId = responseValues["viewingSessionId"].ToString();

            // Get the user agent from the Request object so we can send to PCCIS in the background thread.
            // PCCIS uses this information to determine support for SVG and logging purposes.
            string userAgent = HttpContext.Current.Request.Headers["User-Agent"];

            // Use a background thread to send the document to PCCIS and begin a viewing session.
            // This allows the current web page to finish loading and the PCC viewer to appear sooner.
            Task notificationTask = new Task(() =>
            {
                try
                {
                    // Upload File to PCCIS.
                    //   PUT http://localhost:18681/PCCIS/V1/ViewingSessions/u{ViewingSessionID}/SourceFile?FileExtension={FileExtension}
                    // Note the "u" prefixed to the Viewing Session ID. This is required when providing
                    //   an unencoded Viewing Session ID, which is what PCCIS returns from the initial POST.
                    //     
                    uriString = string.Format("{0}/ViewingSession/u{1}/SourceFile?FileExtension={2}", PccConfig.ImagingService, viewingSessionId, HttpUtility.UrlEncode(fileExtension));
                    request = (HttpWebRequest)WebRequest.Create(uriString);
                    request.Method = "PUT";
                    request.Headers.Add("acs-api-key", PccConfig.ApiKey);
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        streamCopy.Position = 0;
                        streamCopy.CopyTo(requestStream);
                    }
                    response = (HttpWebResponse)request.GetResponse();

                    // Start Viewing Session in PCCIS.
                    //   POST http://localhost:18681/PCCIS/V1/ViewingSessions/u{ViewingSessionID}/Notification/SessionStarted
                    //    
                    uriString = string.Format("{0}/ViewingSession/u{1}/Notification/SessionStarted", PccConfig.ImagingService, viewingSessionId);
                    request = (HttpWebRequest)WebRequest.Create(uriString);
                    request.Method = "POST";
                    request.Headers.Add("acs-api-key", PccConfig.ApiKey);
                    request.UserAgent = userAgent;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        using (TextWriter requestStreamWriter = new StreamWriter(requestStream))
                        {
                            serializer = new JavaScriptSerializer();
                            string requestBody = serializer.Serialize(new { viewer = "HTML5" });
                            requestStreamWriter.Write(requestBody);
                        }
                    }
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (Exception ex)
                {
                    // If a problem was encountered in the background thread, notify PCCIS 
                    // that the session should be stopped so it can return appropriate status
                    // to the viewer requests made to it.
                    //   POST http://localhost:18681/PCCIS/V1/ViewingSessions/u{ViewingSessionID}/Notification/SessionStopped
                    //
                    uriString = string.Format("{0}/ViewingSession/u{1}/Notification/SessionStopped", PccConfig.ImagingService, viewingSessionId);
                    request = (HttpWebRequest)WebRequest.Create(uriString);
                    request.Method = "POST";
                    request.Headers.Add("acs-api-key", PccConfig.ApiKey);
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        using (TextWriter requestStreamWriter = new StreamWriter(requestStream))
                        {
                            string requestBody = serializer.Serialize(new { endUserMessage = ex.Message, httpStatus = 504 });
                            requestStreamWriter.Write(requestBody);
                        }
                    }
                    response = (HttpWebResponse)request.GetResponse();
                }
                finally
                {
                    streamCopy.Dispose();
                }
            });

            notificationTask.Start();

            return viewingSessionId;
        }
    }
}
