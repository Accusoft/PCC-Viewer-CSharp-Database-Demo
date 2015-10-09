namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.IO;
    using System.Net;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using System.Text.RegularExpressions;
    using System.Drawing;
    using System.Web.Script.Serialization;


    /// <summary>
    /// Summary description for HttpSearchTerm
    /// Handles the request for text search terms stored on server host.
    /// </summary>
    public class SearchTerm : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            try
            {
                PccConfig.LoadConfig("viewer-webtier/pcc.config");
                string searchTermsId = GetStringFromUrl(context, match, "SearchTermsId");
                // make sure target directory exists
                String targetDir = System.IO.Path.GetDirectoryName(PccConfig.SearchTermsPath);
                if (!System.IO.Directory.Exists(targetDir))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Write("SearchTermsPath does not exist or is not configured correctly in pcc.config");
                    context.Response.ContentType = "text/plain";
                    return;
                }
                string searchTermsFileName = Path.Combine(targetDir, searchTermsId);
                using (FileStream searchStream = new FileStream(searchTermsFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    searchStream.CopyTo(context.Response.OutputStream);
                }

                context.Response.ContentType = "application/json;charset=utf-8";
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Write(e.Message);
                context.Response.ContentType = "text/plain";
                return;
            }
        }
    }
}