namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Web;
    using System.Text.RegularExpressions;
    using PccViewer.WebTier.Core;
    using System.Web.SessionState;
    using System.Security.Cryptography;

    /// <summary>
    /// This is the base class for PCC request handlers.
    /// </summary>
    public abstract class PccHandler
    {
        abstract public void ProcessRequest(HttpContext context, Match match);

        /// <summary>
        /// Tries to extract the document identification string from the url requested and return that string.    
        /// </summary>
        /// <param name="context">Current HTTP context</param>
        /// <param name="match">Matching parameter found for the url being requested.</param>
        /// <param name="parameterName">Parameter name that is being matched or looked for to get its content or value</param>
        /// <returns>Unencoded document identifier.</returns>
        protected string GetStringFromUrl(HttpContext context, Match match, string parameterName)
        {
            string value = match.Groups[parameterName].Value;
            if (value == "q")
            {
                value = context.Request.QueryString[parameterName];
            }

            if (value.StartsWith("e"))
            {
                value = Encoder.DecodeURLString(value.Substring(1));
            }
            else if (value.StartsWith("u"))
            {
                value = value.Substring(1);
            }
            else
            {
                throw new Exception("Unable to extract data from URL.");
            }

            return value;
        }
    }
}