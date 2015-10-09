namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using PccViewer.WebTier.Core;
    using System.Text.RegularExpressions;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Handles the request from the viewer to acquire a list of 
    /// imagestamp files from.
    /// </summary>
    public class ImageStampList : PccHandler
    {

        public override void ProcessRequest(HttpContext context, Match match)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            try
            {

                // Environmental Setup
                PccConfig.LoadConfig("viewer-webtier/pcc.config");

                //location of the saved markup ImageStamps. 
                DirectoryInfo di = new DirectoryInfo(PccConfig.ImageStampFolder);

                string[] extensions = PccConfig.ValidImageStampTypes.Split(',');

                FileInfo[] rgFiles = di.EnumerateFiles()
                 .Where(f => extensions.Contains(f.Extension.ToLower()))
                 .ToArray();

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                char[] charsToTrim = { ',' };

                foreach (FileInfo fi in rgFiles)
                {
                    String fullPath = PccConfig.ImageStampFolder + "\\" + fi.Name;
                    sb1.AppendFormat("{{\"id\": \"{0}\", \"displayName\": \"{1}\"}},", PccViewer.WebTier.Core.Encoder.EncodeURLString(fi.Name), fi.Name);
                }

                sb1.ToString().TrimEnd(charsToTrim);
                //sb.Append("{\"imageStampList\":");
                sb.Append("{\"imageStamps\":[");
                sb.Append(sb1.ToString().TrimEnd(charsToTrim));
                sb.Append("] }");
                //sb.Append("}");
                context.Response.Write(sb.ToString());
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


        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}