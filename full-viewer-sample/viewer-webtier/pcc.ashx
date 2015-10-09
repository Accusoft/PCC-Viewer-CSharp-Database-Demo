<%@ WebHandler Language="C#" Class="Convert" Debug="true" %>

using System;
using System.Web;
using System.IO;
using System.Net;
using System.Web.Configuration;
using System.Web.SessionState;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using PccViewer.WebTier.Core;

/// <summary>
/// Maps the requested URL to the appropriate method.
/// </summary>
public class Convert : IHttpHandler
{
    private static ImagingServiceProxy page = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "DocumentID",
            "Scale",
            "ContentType",
            "Quality",
            "iv"
        },
        ResponseHeaderWhiteList = new string[] {
			"Content-Type",
			"Cache-Control",
			"Accusoft-Data-Encrypted",
			"Accusoft-Data-SK",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy pageTile = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "DocumentID",
            "Scale",
            "ContentType",
            "Quality",
            "iv"
        },
        ResponseHeaderWhiteList = new string[] {
			"Content-Type",
			"Cache-Control",
			"Accusoft-Data-Encrypted",
			"Accusoft-Data-SK",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy pageAttributes = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "DocumentID",
            "ContentType"
        },
        ResponseHeaderWhiteList = new string[] {
			"Content-Type",
			"Cache-Control",
			"Accusoft-Data-Encrypted",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy documentAttributes = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "DocumentID",
            "DesiredPageCountConfidence"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy pageText = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "DocumentID",
            "iv"
        },
        ResponseHeaderWhiteList = new string[] {
			"Content-Type",
			"Cache-Control",
			"Accusoft-Data-Encrypted",
			"Accusoft-Data-SK",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy flashConvert = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "documentname",
            "fileName",
            "instanceid",
            "pageNumber"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy flashPassThrough = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "documentname",
            "fileName",
            "instanceid",
            "pageNumber"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy license = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "v",
            "iv",
            "p"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy attachments = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

    private static ImagingServiceProxy getDocument = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
            "ViewingSessionId",
            "SourceFile"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Accusoft-Status-Message",
            "Accusoft-Status-Number"
        }
    };

	private static ImagingServiceProxy markupBurner = new ImagingServiceProxy()
    {
        QueryParameterWhiteList = new string[] {
             "ContentDispositionFilename"
        },
        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Content-Disposition",
            "filename"
        }
    };

    private static ImagingServiceProxy getConversionStatus = new ImagingServiceProxy("v2")
    {
        RequestHeaderWhiteList = new string[] {
            "Accusoft-Affinity-Token"
        },

        ResponseHeaderWhiteList = new string[] {
            "Content-Type",
            "Cache-Control",
            "Content-Disposition"
        }
    };

    private KeyValuePair<Regex, PccHandler>[] routes = {
        // Resources to create new viewing sessions
        new KeyValuePair<Regex, PccHandler>(new Regex("^/CreateSession$"), new CreateSession()),
        
        // Resources requested by the HTML5 viewer
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Page/(?<DocumentID>[^/]+)/(?<PageNumber>\\d+)$"), page),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Page/(?<DocumentID>[^/]+)/(?<PageNumber>\\d+)/Tile/(?<X>\\d+)/(?<Y>\\d+)/(?<Width>\\d+)/(?<Height>\\d+)$"), pageTile),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Page/(?<DocumentID>[^/]+)/(?<PageNumber>\\d+)/Attributes$"), pageAttributes),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Document/(?<DocumentID>[^/]+)/Attributes$"), documentAttributes),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Document/(?<DocumentID>[^/]+)/(?<PageNumberStart>\\d+)-(?<PageNumberEnd>\\d+)/Text$"), pageText),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ViewingSession/(?<ViewingSessionId>[^/]+)/Attachments"), attachments),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/Document/(?<DocumentID>[^/]+)/Art/(?<AnnotationID>[^/]+)$"), new DocumentArt()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/SearchTerms/(?<SearchTermsId>[^/]+)/Text$"), new SearchTerm()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/AnnotationList/(?<DocumentID>[^/]+)/Art/(?<AnnotationID>[^/]+)$"), new AnnotationList()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ViewingSession/(?<ViewingSessionId>[^/]+)/SourceFile$"), getDocument),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/SaveDocument/(?<DocumentID>[^/])"), new SaveDocument()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ViewingSession/(?<ViewingSessionId>[^/]+)/MarkupBurner$"), markupBurner),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ViewingSession/(?<ViewingSessionId>[^/]+)/MarkupBurner/(?<MarkupBurnerId>[^/]+)$"), markupBurner),
		new KeyValuePair<Regex, PccHandler>(new Regex("^/ViewingSession/(?<ViewingSessionId>[^/]+)/MarkupBurner/(?<MarkupBurnerId>[^/]+)/Document$"), markupBurner),
        //new KeyValuePair<Regex, PccHandler>(new Regex("^/MarkupLayers/(?<ViewingSessionId>[^/]+)(/(?<LayerRecordId>[^/]+))?$"), new MarkupLayers()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/MarkupLayers/(?<ViewingSessionId>[^/]+)(/)?((?!/)(?<LayerRecordId>[^/]+))?$"), new MarkupLayers()),
        // Resources requested by both viewers
        new KeyValuePair<Regex, PccHandler>(new Regex("^/License/ClientViewer$"), license),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ImageStampList$"), new ImageStampList()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/ImageStamp/(?<ImageStampId>[^/]+)/(?<Format>[^/]+)"), new ImageStamp()),

        new KeyValuePair<Regex, PccHandler>(new Regex("^/contentConverters/$"), new ContentConversion()),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/contentConverters/(?<FileId>[^/]+)$"), getConversionStatus),
        new KeyValuePair<Regex, PccHandler>(new Regex("^/WorkFile/(?<WorkFileId>[^/]+)$"), new Workfile()),
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
        }

        // This is for debugging regular expression problems.
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.Cache.SetNoStore();
        context.Response.Write(context.Request.PathInfo);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}
