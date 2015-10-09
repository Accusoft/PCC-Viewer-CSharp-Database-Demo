namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// This object contains properties of the document. Most of them are for the viewing
    /// operation.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     These will be used in the various opening/conversion processes on the server that
    ///     support client-side viewing.
    /// </para>
    /// </remarks>
    public class ViewingSessionProperties
    {
        /// <summary>
        /// A list of key-value pairs containing user-specific data. This is useful for tracking 
        /// information pertinent to the request, like IP address, Host Name and any other useful
        /// bits of information.
        /// </summary>
        public Dictionary<string, string> origin { get; set; }

        /// <summary>
        /// Specifies rendering properties for the Flash and HTML5 vieweres.
        /// </summary>
        public RenderProperties render { get; set; }

        /// <summary>
        /// Specifies the password to open password-protected PDF documents.
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// Specifies the text to use for watermarking as supported by the Flash viewer. This property
        /// is not currently used for the HTML5 viewer.
        /// </summary>
        public string watermarkText { get; set; }

        /// <summary>
        /// This is an ID defined by this application.
        /// PCCIS will store this value in the current viewing session, but otherwise
        /// will not use it directly. It is useful for association in log files and 
        /// other operations, like annotations.
        /// </summary>
        public string externalId { get; set; }

        /// <summary>
        /// This is the index for an attached document. PCCIS will initialize this 
        /// value as it prepares an attached document for viewing. The first 
        /// attached document is index 1. An original document should have 
        /// an index of 0.
        /// </summary>
        public int attachmentIndex { get; set; }

        /// <summary>
        /// This is the display name for an attached document. PCCIS will initialize this 
        /// value as it prepares an attached document for viewing.
        /// </summary>
        public string attachmentDisplayName { get; set; }

        /// <summary>
        /// This is an ID for a tenant. Tenants are not a formal concept in this system
        /// and are instead monitored and maintained in the calling system. However,
        /// having this data available will allow some optional behavior. For example, with
        /// this information, it would be possible to ensure some level of isolation between
        /// tenants.
        /// </summary>
        public string tenantId { get; set; }

        /// <summary>
        /// This is the number of pages to pre-generate. i.e., the first N pages of the document will be pre-generated.
        /// These pages will be converted as soon as possible; the remaining pages (if any) will be converted on demand.
        /// </summary>
        public int countOfInitialPages { get; set; }

        /// <summary>
        /// Determines the source from which PCCIS should expect the document.
        /// "api", "" or null means that the document should be uploaded via the PUT Document request.
        /// "http" means that externalId specifies an HTTP URL where the document can be downloaded.
        /// "file" Not implemented
        /// "ftp" Not implemented
        /// </summary>
        public string documentSource { get; set; }

        /// <summary>
        /// The extension of the document located in 'externalId' if documentSource is "http", "ftp" or "file".
        /// </summary>
        public string documentExtension { get; set; }

        /// <summary>
        /// This controls server-side caching.
        /// "full", "" or null means to automatically reuse previously-generated output files when possible.
        /// "none" means to never reuse previously-generated output files.
        /// Note that this does not affect the values of the http caching headers.
        /// </summary>
        public string serverCaching { get; set; }

        /// <summary>
        /// This determines when the process to generate pages for viewing is started.
        /// This property requires that documentSource is "http" and contentType is set to a valid value.
        /// "initialPages" means the process to generate pages for viewing is started during the
        /// initial POST /ViewingSession request.
        /// "none" means the process to generate pages for viewing is started later during a request to
        /// POST /ViewingSession/{id}/Notification/SessionStarted, GET /Page or 
        /// GET /PageAttributes, whichever comes first.
        /// </summary>
        public string startConverting { get; set; }

        /// <summary>
        /// This sets the type of pages that are generated for viewing if startConverting is not "none".
        /// "png" means that PNG pages will be generated for viewing.
        /// "svg" means SVG pages.
        /// "swf" means SWF pages.
        /// </summary>
        public string contentType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewingSessionProperties"/> class.
        /// </summary>
        public ViewingSessionProperties()
        {
            this.origin = new Dictionary<string, string>();
            this.render = new RenderProperties();
        }
    }

    /// <summary>
    /// Contains rendering properties for both the Flash and HTML5 viewers.
    /// </summary>
    public class RenderProperties
    {
        /// <summary>
        /// Rendering properties for the Flash viewer.
        /// </summary>
        public FlashRenderProperties flash { get; set; }

        /// <summary>
        /// Rendering properties for the HTML5 viewer.
        /// </summary>
        public Html5RenderProperties html5 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderProperties"/> class.
        /// </summary>
        public RenderProperties()
        {
            this.flash = new FlashRenderProperties();
            this.html5 = new Html5RenderProperties();
        }
    }

    /// <summary>
    /// Contains the rendering properties for the HTML5 viewer.
    /// </summary>
    public class Html5RenderProperties
    {
        /// <summary>
        /// Forces PCCIS to always provide raster image data to the HTML5 viewer instead of SVG, even if the client supports viewing SVG data.
        /// </summary>
        public bool alwaysUseRaster { get; set; }

        /// <summary>
        /// Specifies the resolution to use for raster content generated by PCCIS.
        /// </summary>
        public int rasterResolution { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Html5RenderProperties"/> class.
        /// </summary>
        public Html5RenderProperties()
        {
            this.alwaysUseRaster = true;
            this.rasterResolution = 150;
        }
    }

    /// <summary>
    /// Contains the rendering properties for the Flash viewer.
    /// </summary>
    public class FlashRenderProperties
    {
        /// <summary>
        /// Specifies the optimization level used during SWF generation for the Flash viewer.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     Each optimization level uses a difference rendering algorithm for SWF and adds more compression.
        /// </para>
        /// <para>
        /// <list type="table">
        ///    <listheader>
        ///        <term>Value</term>
        ///        <description>Compression Level</description>
        ///    </listheader>
        ///    <item>
        ///        <term>0</term>
        ///        <description>No compression for text and images. Render everything the same as in the source document. 
        ///        Shapes will be converted to shapes, text to text and bitmaps to bitmaps.</description>
        ///    </item>
        ///    <item>
        ///        <term>1</term>
        ///        <description>Text inside SWF is preserved from the original document, so it can be searched in the Flash viewer. 
        ///        Everything else is converted to images. Images are compressed using JPEG compression.</description>
        ///    </item>
        ///    <item>
        ///        <term>2</term>
        ///        <description>All texts, shapes and images are converted into single image SWF; therefore it cannot be searched in 
        ///        the Flash viewer. Only links are preserved. Use this option when 0 or 1 fails to convert the document to SWF in 
        ///        case of very complex source documents, such as PDFs containing many shapes.</description>
        ///    </item>
        ///</list>
        /// </para>
        /// </remarks>
        public int optimizationLevel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlashRenderProperties"/> class.
        /// </summary>
        public FlashRenderProperties()
        {
            this.optimizationLevel = 1;
        }
    }
}