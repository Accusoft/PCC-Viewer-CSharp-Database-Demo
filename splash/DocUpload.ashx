<%@ WebHandler Language="C#" Class="DocUpload" Debug="true" %>

using System;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using Pcc;

public class DocUpload : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        var file = context.Request.Files["file"];
        //if there is a file, handle it
        if (file != null)
        {
            //get epoch time to generate semi-unique filenames
            var epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000)/10000000;
            var fn = Path.GetFileName(string.Join("", file.FileName.Split(Path.GetInvalidFileNameChars())));
            var uniqueName = epoch + "_" + fn;
            var saveLocation = Path.Combine(PccConfig.DocumentFolder, uniqueName);

            file.SaveAs(saveLocation);
            var json = ToJson(new
            {
                filename = Path.GetFileName(saveLocation)
            });

            var format = context.Request.QueryString["f"];
            if (format == "jsonp")
            {
                context.Response.ContentType = "text/html";
                context.Response.Write("<script>window.res = " + json + ";</script>");
            }
            else
            {
                context.Response.ContentType = "application/json";
                context.Response.Write(json);
            }
        }
        else
        {
            throw new Exception("No File Found!");
        }
    }

    private static string ToJson(object obj)
    {
        var serializer = new JavaScriptSerializer();
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

