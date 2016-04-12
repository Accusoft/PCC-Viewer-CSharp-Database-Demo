namespace Pcc
{
    using System;
    using System.Net;
    using System.Web;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web.Script.Serialization;
    using System.Data.Entity;
    using System.Text;

    /// <summary>
    /// Summary description for HTTPDatabaseHandler
    /// </summary>
    public class HTTPDatabaseHandler : PccHandler
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        public override void ProcessRequest(HttpContext context, Match match)
        {
            // Environmental Setup
            //PccConfig.LoadConfig("viewer-webtier/pcc.config");

            // find the request method
            string method = context.Request.RequestType.ToLower();
            string methodHeader = context.Request.Headers["X-HTTP-Method-Override"];

            if (!String.IsNullOrEmpty(methodHeader))
            {
                method = methodHeader.ToLower();
            }

            if (!(method == "get" || method == "post"))
            {
                // Request is not a get or a post
                sendResponse(context, (int)HttpStatusCode.MethodNotAllowed);
                return;
            }

            string viewingSessionId = GetStringFromUrl(context, match, "DocumentID");
            string userName = string.Empty;

            // We can check for status of our user's identification in multiple places
            if (context.Request.Headers["username"] != null)
            {
                userName = context.Request.Headers["username"];
            }
            else if (context.Request.QueryString["username"] != null)
            {
                userName = context.Request.QueryString["username"];
            }
            else if (context.Request.Params["username"] != null)
            {
                userName = context.Request.Params["username"];
            }
            else
            {
                sendResponse(context, (int)HttpStatusCode.BadRequest, "No username defined. 1");
                return;
            }

            // Final check to make sure we have a useable user name.
            if (userName == string.Empty)
            {
                sendResponse(context, (int)HttpStatusCode.BadRequest, "No username defined. 2");
                return;
            }

            // Perform an HTTP GET request to retrieve properties about the viewing session from PCCIS. 
            // The properties will include an identifier of the source document that will be used below
            // to construct the name of file where markups are stored.
            string uriString = PccConfig.WebServiceAddress + "/ViewingSession/" + viewingSessionId;
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
                sendResponse(context, (int)HttpStatusCode.BadGateway, "Bad or expired Viewing Session");
                return;
            }

            ViewingSessionProperties viewingSessionProperties = serializer.Deserialize<ViewingSessionProperties>(responseBody);
            string documentMarkupId = string.Empty;
            viewingSessionProperties.origin.TryGetValue("documentMarkupId", out documentMarkupId);

            if (!String.IsNullOrEmpty(documentMarkupId))
            {
                if (method == "post")
                {
                    // Post is used to update an existin annotation or insert a new one.
                    upsertAnnotation(context, documentMarkupId, userName);
                }
                else if (method == "get")
                {
                    // Get is used to retrieve an annotation 
                    selectAnnotaion(context, documentMarkupId, userName);
                }
            }
            else
            {
                // Return an error if the documentMarkupId is not present
                // This means that there was a problem with the response from PCCIS
                sendResponse(context, (int)HttpStatusCode.BadGateway, "Bad Gateway");
                return;
            }
        }

        /// <summary>
        /// Selects an annotation from the current database context.
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="userName">User's name as a query param</param>
        private void selectAnnotaion(HttpContext context, string externalId, string userName)
        {
            using (var dbContext = new AnnotationModel.AnnotationContext())
            {
                // Entity Framework does not currently support unique constraints
                // Check if this document already exists
                // LINQ will return 0 or the primary key for the current document
                Pcc.DocumentModel.Document document = null;
                try
                {
                    document = dbContext.Documents
                               .Where(d => d.ExternalId == externalId)
                               .FirstOrDefault();
                }
                catch (Exception)
                {

                    sendResponse(context, (int)HttpStatusCode.BadGateway, "Failed to connect to database. Please check your conneciton string.");
                }

                if (document != null)
                {
                    // Document exists
                    var annotation = dbContext.Annotations
                        .Where(a => a.UserName == userName &&
                        a.Document.DocumentID == document.DocumentID)
                        .FirstOrDefault();

                    if (annotation != null)
                    {
                        //Annotation exists, return the data
                        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(annotation.AnnotationText ?? ""));
                        //StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
                        //writer.Write(annotation.AnnotationText);
                        //writer.Flush();
                        stream.Position = 0;
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentType = "application/json";
                        stream.CopyTo(context.Response.OutputStream);
                    }
                    else
                    {
                        // Annotation does not exist
                        sendResponse(context, (int)HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    // Document does not exist
                    sendResponse(context, (int)HttpStatusCode.NotFound);
                }
            }
        }

        /// <summary>
        /// Updates or inserts an annotation into the current database context.
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="externalId">Unique document identifier generated from document's absolute path</param>
        /// <param name="userName">User's name as a query param</param>
        private void upsertAnnotation(HttpContext context, string externalId, string userName)
        {
            var rawData = String.Empty;
            // Make sure we're at the beginning
            context.Request.InputStream.Position = 0;
            using (StreamReader inputStream = new StreamReader(context.Request.InputStream))
            {
                rawData = inputStream.ReadToEnd();
            }

            var jsonData = serializer.Deserialize<List<Dictionary<String, Object>>>(rawData);

            var creationDateTime = jsonData[0]["creationDateTime"];

            var type = creationDateTime.GetType();

            using (var dbContext = new AnnotationModel.AnnotationContext())
            {
                // Entity Framework does not currently support unique constraints
                // Check if this document already exists
                // LINQ will return 0 or the primary key for the current document
                Pcc.DocumentModel.Document document = null;

                try
                {
                    document = dbContext.Documents
                                .Where(d => d.ExternalId == externalId)
                                .FirstOrDefault();
                }
                catch (Exception e)
                {

                    sendResponse(context, (int)HttpStatusCode.BadGateway, "Failed to connect to database. Please check your conneciton string.");
                }

                if (document != null)
                {
                    // Document already exists
                    // In this use case, we are only supporting one set of annotations per user.
                    // Does the current user already own a set of annotations for this document?

                    var annotation = dbContext.Annotations
                        .Where(a => a.UserName == userName &&
                        a.DocumentID == document.DocumentID)
                        .FirstOrDefault();

                    if (annotation != null)
                    {
                        // User already owns a set of annotations, update those annotations
                        annotation.AnnotationText = rawData;

                        // Mark as modified and save
                        dbContext.Entry(annotation).State = EntityState.Modified;
                        dbContext.SaveChanges();

                        sendResponse(context, (int)HttpStatusCode.OK, "Annotation Updated");

                    }
                    else
                    {
                        // This is a new set of annotations for this user
                        AnnotationModel.Annotation newAnno = new AnnotationModel.Annotation()
                        {
                            UserName = userName,
                            AnnotationText = rawData,
                            DocumentID = document.DocumentID
                        };

                        // Insert into the local context and save to the store
                        dbContext.Annotations.Add(newAnno);
                        dbContext.SaveChanges();

                        sendResponse(context, (int)HttpStatusCode.OK, "Annotation Created");

                    }


                }
                else
                {
                    // If this document's external ID does not exist in the current context or in the store
                    // Insert the document for this request
                    DocumentModel.Document newDoc = new DocumentModel.Document() { ExternalId = externalId };

                    // Insert into the local context and save to the store
                    dbContext.Documents.Add(newDoc);
                    dbContext.SaveChanges();

                    // Now get the primary key for that document so we can use it
                    document = dbContext.Documents
                    .Where(d => d.ExternalId == externalId)
                    .FirstOrDefault();

                    // In this use case, we are only supporting one set of annotations per user.
                    // Does the current user already own a set of annotations for this document?
                    var annotation = dbContext.Annotations
                        .Where(a => a.UserName == userName &&
                        a.DocumentID == document.DocumentID)
                        .FirstOrDefault();

                    if (annotation != null)
                    {
                        // User already owns a set of annotations, update those annotations locallly
                        // Note that this condition should never happen (ie - no document means no annotation)
                        annotation.AnnotationText = rawData;
                        annotation.AnnotationText = jsonData[0]["modificationDateTime"].ToString();

                        // Mark as modified and save
                        dbContext.Entry(annotation).State = EntityState.Modified;
                        dbContext.SaveChanges();

                        sendResponse(context, (int)HttpStatusCode.OK, "Annotation Updated");
                    }
                    else
                    {
                        // This is a new set of annotations for this user
                        AnnotationModel.Annotation newAnno = new AnnotationModel.Annotation()
                        {
                            UserName = userName,
                            AnnotationText = rawData,
                            DocumentID = document.DocumentID,
                            Document = document
                        };

                        // Insert into the local context and save to the store
                        dbContext.Annotations.Add(newAnno);
                        dbContext.SaveChanges();

                        sendResponse(context, (int)HttpStatusCode.OK, "Annotation Created");
                    }
                }
            }
        }

        private void sendResponse(HttpContext context, int status, Object body = null)
        {
            if (body != null)
            {
                var jsonBody = toJSON(body);

                context.Response.ContentType = "application/json";
                context.Response.Write(jsonBody);
            }

            context.Response.StatusCode = status;
        }

        private string toJSON(Object obj)
        {
            return serializer.Serialize(obj);
        }
    }
}