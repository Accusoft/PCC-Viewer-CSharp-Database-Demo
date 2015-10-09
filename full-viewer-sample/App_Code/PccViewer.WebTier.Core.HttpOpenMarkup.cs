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


    /// <summary>
    /// Handles the request from the Flash viewer to load a particular 
    /// annotation file.
    /// </summary>
    public class OpenMarkup : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");

            string documentname = context.Request.Form["fileName"];
            string annotationName = context.Request.Form["annotationName"];
            string annotationId = context.Request.Form["annotationId"];
            string annotationLabel = context.Request.Form["annotationLabel"];

            //location of saved markups
            string path2Save = PccConfig.MarkupFolder;

            string annotationPath = path2Save + annotationName;

            context.Response.ContentType = "application/xml";
            context.Response.AppendHeader("Content-Disposition", "inline; filename=" + annotationName);
            context.Response.Charset = "UTF-8";
            context.Response.TransmitFile(annotationPath);
            context.Response.End();
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