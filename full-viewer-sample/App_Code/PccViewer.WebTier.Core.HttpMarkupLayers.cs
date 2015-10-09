namespace PccViewer.WebTier.Core
{
    using System;
    using System.Text;
    using System.Web;
    using System.IO;
    using System.Net;
    using System.Collections.Generic;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using System.Text.RegularExpressions;
    using System.Drawing;
    using System.Web.Script.Serialization;

    public class MarkupLayers : PccHandler
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();

        public override void ProcessRequest(HttpContext context, Match match)
        {
            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");
            string resourcePath = PccConfig.MarkupLayerRecordsPath;
            
            // Check if this folder exsts, and if it does not, create it
            if (!Directory.Exists(resourcePath))
            {
                Directory.CreateDirectory(resourcePath);
            }

            // find the request method
            string method = context.Request.RequestType.ToLower();
            string methodHeader = context.Request.Headers["X-HTTP-Method-Override"];

            if (!String.IsNullOrEmpty(methodHeader))
            {
                method = methodHeader.ToLower();
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            string viewingSessionId = match.Groups["ViewingSessionId"].Value;
            // get the annotationsLayer (it could be undefined)
            string layerRecordId = match.Groups["LayerRecordId"].Value;

            // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
            // The properties will include an identifier of the source document that will be used below
            // to construct the name of file where markups are stored.
            string uriString = PccConfig.ImagingService + "/ViewingSession/" + viewingSessionId;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriString);
            request.Method = "GET";
            string responseBody = null;
            request.Headers.Add("acs-api-key", PccConfig.ApiKey);
            try
            {
                // Send request to PCCIS and get response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                {
                    responseBody = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                var json = createJSONError("ServerError", layerRecordId, e.Message);
                sendResponse(context, (int)HttpStatusCode.BadGateway, json);
                return;
            }

            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);

            string documentMarkupId = string.Empty;
            viewingSessionProperties.origin.TryGetValue("documentMarkupId", out documentMarkupId);
            string recordNamePrefix = PccConfig.MarkupLayerRecordsPath + documentMarkupId + "_" + viewingSessionProperties.attachmentIndex + "_";
            //var json;

            if (String.IsNullOrEmpty(recordNamePrefix))
            {
                //BadGateway = 502
                var json = createJSONError("UnknownRequest", layerRecordId);
                sendResponse(context, (int)HttpStatusCode.BadGateway, json);
                return;
            }

            // add the file extension
            string resourceName = null;
            if (!String.IsNullOrEmpty(layerRecordId))
            {
                resourceName = recordNamePrefix + layerRecordId + ".json";
            }

            string fullPath = resourceName;
            
            // route to the correct method
            if (String.IsNullOrEmpty(layerRecordId) && method == "get")
            {
                getList(context, resourcePath, viewingSessionProperties, documentMarkupId);
                return;
            }
            else if (String.IsNullOrEmpty(layerRecordId) && method == "post")
            {
                // we are creating a new record, so generate a new layerRecordId
                layerRecordId = generateId();
                fullPath = recordNamePrefix + layerRecordId + ".json";

                createResource(context, fullPath, layerRecordId);
                return;
            }
            else if (!String.IsNullOrEmpty(layerRecordId) && method == "get")
            {
                getResource(context, resourceName, layerRecordId);
                return;
            }
            else if (!String.IsNullOrEmpty(layerRecordId) && (method == "post" || method == "put"))
            {
                updateResource(context, resourceName, layerRecordId);
                return;
            }
            else if (!String.IsNullOrEmpty(layerRecordId) && method == "delete")
            {
                deleteResource(context, resourceName, layerRecordId);
                return;
            }

            var json1 = createJSONError("UnknownRequest", layerRecordId);
            sendResponse(context, (int)HttpStatusCode.BadRequest, json1);
        }

        private Dictionary<string, object> getLayerRecordSummary(String path, ViewingSessionProperties viewingSessionProperties, string documentMarkupId)
        {
            if (File.Exists(path))
            {
                string layerRecordId = "";
                string fileRecord = Path.GetFileNameWithoutExtension(path);
                if (fileRecord.Contains(documentMarkupId + "_" + viewingSessionProperties.attachmentIndex))
                {
                    layerRecordId = fileRecord.Replace(documentMarkupId + "_" + viewingSessionProperties.attachmentIndex + "_", "");

                    try
                    {
                        string text = File.ReadAllText(path);
                        var jsonObj = new Dictionary<string, object>();

                        jsonObj = parseJSON(text);

                        string name = "";
                        if (jsonObj.ContainsKey("name"))
                        {
                            name = (string)jsonObj["name"];
                        }
                        
                        string originalXmlName = "";
                        if (jsonObj.ContainsKey("originalXmlName")) {
                            originalXmlName = (string)jsonObj["originalXmlName"];
                        }

                        var json = new Dictionary<string, object>();
                        json.Add("name", name);
                        json.Add("layerRecordId", layerRecordId);
                        json.Add("originalXmlName", originalXmlName);

                        return json;
                    }
                    catch (Exception e)
                    {
                        // if there is an error parsing the JSON, we will assume 
                        // that this is not a valid JSON file
                        return null;
                    }
                }
                return null;
            }

            // the file does not contain a valid layerRecord
            return null;
        }

        private void getList(HttpContext context, String path, ViewingSessionProperties viewingSessionProperties, string documentMarkupId)
        {
            var list = new List<Dictionary<string, object>>();

            try
            {
                // generate a list of all of the layerRecord files
                if (Directory.Exists(path))
                {
                    string[] fileList = Directory.GetFiles(path);

                    foreach (string filePath in fileList)
                    {
                        var summary = getLayerRecordSummary(filePath, viewingSessionProperties, documentMarkupId);

                        if (summary != null)
                        {
                            list.Add(summary);
                        }
                    }
                }

                sendResponse(context, (int)HttpStatusCode.OK, list);
            }
            catch (Exception e)
            {
                var error = createJSONError("ServerError", "", e.Message);
                sendResponse(context, 580, error);
            }
        }

        private void getResource(HttpContext context, String resourcePath, String layerRecordId)
        {
            if (File.Exists(resourcePath)) {
                Stream contentStream = null;
                try
                {
                    contentStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    context.Response.ContentType = "application/json";
                    contentStream.CopyTo(context.Response.OutputStream);
                    sendResponse(context, (int)HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    var error = createJSONError("ServerError", layerRecordId, e.Message);
                    sendResponse(context, 580, error);
                }
                finally { 
                    if (contentStream != null) {
                        contentStream.Dispose();
                    }
                }

                return;
            }

            // the file does not exist, so send a 404
            sendResponse(context, (int)HttpStatusCode.NotFound);
        }

        private void createResource(HttpContext context, String resourcePath, String layerRecordId)
        {
            try
            {
                Stream file = File.Create(resourcePath);
                context.Request.InputStream.CopyTo(file);
                file.Close();
            }
            catch (Exception e) {
                var error = createJSONError("ServerError", layerRecordId, e.Message);
                sendResponse(context, 580, error);
                return;
            }

            // send the created layerRecordId in the successful response
            var json = new Dictionary<string, object>();
            json.Add("layerRecordId", layerRecordId);
            sendResponse(context, (int)HttpStatusCode.Created, json);
        }

        private void updateResource(HttpContext context, String resourcePath, String layerRecordId)
        {
            // make sure file does already exist
            if (!File.Exists(resourcePath)) {
                sendResponse(context, (int)HttpStatusCode.NotFound);
                return;
            }

            try
            {
                // overwrite any existing file with the new content
                Stream file = File.Create(resourcePath);
                context.Request.InputStream.CopyTo(file);
                file.Close();
            }
            catch (Exception e) {
                var error = createJSONError("ServerError", layerRecordId, e.Message);
                sendResponse(context, 580, error);
                return;
            }

            sendResponse(context, (int)HttpStatusCode.OK);
        }

        private void deleteResource(HttpContext context, String resourcePath, String layerRecordId)
        {
            if (File.Exists(resourcePath)) {
                try
                {
                    File.Delete(resourcePath);
                    sendResponse(context, (int)HttpStatusCode.NoContent);
                }
                catch (Exception e) {
                    var error = createJSONError("ServerError", layerRecordId, e.Message);
                    sendResponse(context, 580, error);
                }

                return;
            }

            // the file does not exist, so send a 404
            // make sure file does already exist
            sendResponse(context, (int)HttpStatusCode.NotFound);
            return;
        }

        private void sendResponse(HttpContext context, int status, Object body = null) {
            if (body != null)
            {
                var jsonBody = toJSON(body);

                context.Response.ContentType = "application/json";
                context.Response.Write(jsonBody);
            }

            context.Response.StatusCode = status;
        }

        private Dictionary<string, object> createJSONError(String errorCode, String layerRecordId, String errorDetails = null)
        {
            var json = new Dictionary<string, object>();
            
            json.Add("errorCode", errorCode);
            json.Add("layerRecordId", layerRecordId);

            if (!String.IsNullOrEmpty(errorDetails)) {
                json.Add("errorDetails", errorDetails);
            }

            return json;
        }

        private string toJSON(Object obj)
        {
            return serializer.Serialize(obj);
        }

        private Dictionary<string, object> parseJSON(String jsonStr) {
            return serializer.Deserialize<Dictionary<string, object>>(jsonStr);
        }

        private string toBase64(String source) {
            var bytes = Encoding.UTF8.GetBytes(source);
            return Convert.ToBase64String(bytes);
        }

        private string generateId() {
            return Guid.NewGuid().ToString("N");
        }
    }
}