<%@ WebHandler Language="C#" Class="DocUpload" Debug="true" %>

using System;
using System.Net;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Services;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using PccViewer.WebTier.Core;

public class DocUpload : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        //create empty stream
        System.IO.Stream postedFile = Stream.Null;
        string postedFileName = String.Empty;
        
        if (context.Request.HttpMethod == "POST")
        {
            //check for organic posted file
            System.Web.HttpPostedFile file = context.Request.Files["theFile"];
            
            //if there is a file, handle it
            if (file != null)
            {
                postedFileName = uploadFile(file);
            }
        }
        else
        {
            Console.WriteLine("not a post event");
        }
        
        PccConfig.LoadConfig("viewer-webtier/pcc.config");

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        string[] transferProtocols = { "http://", "https://", "ftp://" };
        string document = string.Empty;
        string viewingSessionId = string.Empty;

        string documentQueryParameter = context.Request.QueryString["document"];
        if (!string.IsNullOrEmpty(documentQueryParameter) || postedFileName != String.Empty)
        {
            if (postedFileName != String.Empty)
            {
                document = postedFileName;
            }
            else
                // Construct the full path to the source document
                if (transferProtocols.Any(documentQueryParameter.Contains))
                {
                    document = documentQueryParameter;
                }
                else
                {
                    document = Path.Combine(PccConfig.DocumentFolder, documentQueryParameter);
                }

            // Get the document's extension because PCCIS will need it later.
            string extension = System.IO.Path.GetExtension(document).TrimStart(new char[] { '.' }).ToLower();

            if (Path.IsPathRooted(document))
            {
                bool correctPath = PccConfig.IsFileSafeToOpen(document);
                if (!correctPath)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.Clear();
                    context.Response.Write("{\"error\": \"403 Forbidden\"}");
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                    return;
                }
            }
        }

        //populate return json
        Dictionary<string, object> json = new Dictionary<string, object>(); //JSON object
        json.Add("filename", document.Split('\\').Last()); //uploaded filenames are saved client-side

        Dictionary<string, object> common = new Dictionary<string, object>();
        
        //stingify JSON object
        String jsonString = toJSON(json);

        var format = context.Request.QueryString["f"];
        if (format == "jsonp")
        {
            context.Response.ContentType = "text/html";
            context.Response.Write("<script>window.res = " + jsonString + ";</script>");
        }
        else
        {
            context.Response.ContentType = "application/json";
            context.Response.Write(jsonString);
        }

        
        // This is for debugging regular expression problems.
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.Cache.SetNoStore();
        context.Response.Write(context.Request.PathInfo);
    }

    /*
     *  This function will accept the file upload and save it to the configured Documents folder of PCC.
     *  It appends the current Unix time to the filename to attemp to ensure uniqueness.
     *  Actual uniqueness will require a hash or GUID instead.
     */
    private string uploadFile(System.Web.HttpPostedFile file)
    {
        //get config file
        PccConfig.LoadConfig("viewer-webtier/pcc.config");

        //get epoch time to generate semi-unique filenames
        var epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        //Guid.NewGuid().toString()
        string fn = System.IO.Path.GetFileName(String.Join("", file.FileName.Split(Path.GetInvalidFileNameChars())));
        string uniqueName = epoch + "_" + fn;
        string SaveLocation = PccConfig.DocumentFolder + uniqueName;

        try
        {
            file.SaveAs(SaveLocation);
            //return SaveLocation;
            return uniqueName;
        }
        catch (Exception ex)
        {
            return String.Empty;
            
            //return ex.Message;
            //Note: Exception.Message returns a detailed message that describes the current exception. 
            //For security reasons, we do not recommend that you return Exception.Message to end users in 
            //production environments. It would be better to put a generic error message. 
        }
    }
    
    private string toJSON(Object obj) {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        return serializer.Serialize(obj);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}

