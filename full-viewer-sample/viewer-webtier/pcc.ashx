<%@ WebHandler Language="C#" Class="Convert" Debug="true" %>

using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Pcc;

/// <summary>
/// Maps the requested URL to the appropriate method.
/// </summary>
public class Convert : IHttpHandler
{
    private KeyValuePair<Regex, PccHandler>[] routes = {
        new KeyValuePair<Regex, PccHandler>(new Regex("^/DbDemo/(?<DocumentID>[^/]+)$"), new HTTPDatabaseHandler())
    };

    public void ProcessRequest(HttpContext context)
    {
        foreach (KeyValuePair<Regex, PccHandler> pair in routes)
        {
            Match match = pair.Key.Match(context.Request.PathInfo);
            if (match.Success)
            {
                pair.Value.ProcessRequest(context, match);
                return;
            }
            else
            {
                    PrizmApplicationServices.ForwardRequest(HttpContext.Current, context.Request.PathInfo);
            }
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}
