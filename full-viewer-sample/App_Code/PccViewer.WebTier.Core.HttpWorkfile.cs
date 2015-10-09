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
    using System.Drawing;
    using System.Web.Script.Serialization;

    /// <summary>
    /// Handles the request from the viewer to download files from the PCC Workfile service
    /// </summary>
    public class Workfile : PccHandler
    {
        public override void ProcessRequest(HttpContext context, Match match)
        {
            HttpRequest requestFromClient = context.Request;

            string affinityToken = requestFromClient.QueryString["affinityToken"];

            ImagingServiceProxy imagingService =  new ImagingServiceProxy();

            imagingService.QueryParameterWhiteList = new string[] {
                "ContentDispositionFilename",
            };

            imagingService.ResponseHeaderWhiteList = new string[] {
               "Content-Type",
               "Cache-Control",
               "Content-Disposition"
            };

            imagingService.RequestHeaders = new Dictionary<string, string>() {
               {"Accusoft-Affinity-Token", affinityToken}
            };

            imagingService.ResponseHeaders = new Dictionary<string, string>() {
               {"Content-Type","application/pdf"}
            };

            imagingService.ProcessRequest(context, match);
        }

    }

}