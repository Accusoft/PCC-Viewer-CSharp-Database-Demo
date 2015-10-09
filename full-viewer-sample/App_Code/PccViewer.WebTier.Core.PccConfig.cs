namespace PccViewer.WebTier.Core
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Configuration;
    using System.Xml;
    using System.IO;

    /// <summary>
    /// Obtains information from a configuration file (i.e.,"pcc.config").
    /// </summary>
    public class PccConfig
    {
        private static string documentPath = string.Empty;
        private static string webServiceHost = string.Empty;
        private static string webServicePort = string.Empty;
        private static string webServiceScheme = string.Empty;
        private static string webServicePath = string.Empty;
        private static string webServiceV2Path = string.Empty;
        private static string webServiceUrl = string.Empty;
        private static string webServiceV2Url = string.Empty;
        private static string markupsPath = string.Empty;
        private static string imageStampPath = string.Empty;
        private static string validImageStampTypes = string.Empty;
        private static string searchTermsPath = string.Empty;
        private static bool enableDocumentPath = false;
        private static string apiKey = string.Empty;
        private static string markupLayerRecordsPath = string.Empty;

        /// <summary>
        /// Gets the value of an application key.
        /// Not used unless web.config file is switched in for pcc.config.
        /// Then, it fetches the desired key value from the Apps Configuration settings in web.config.
        /// </summary>
        /// <param name="name">Key name.</param>
        /// <returns>String value assigned to key.</returns>
        private static string GetValue(string name)
        {
            return WebConfigurationManager.AppSettings[name];
        }

        /// <summary>
        /// Gets the full (host) path of a named key item referencing a relative object.
        /// Not used unless web.config file is switched in for pcc.config.
        /// </summary>
        /// <param name="name">Key name referencing a local path from web.config application key.</param>
        /// <returns>String value path of named key referencing a local location</returns>
        private static string GetPath(string name)
        {
            string path = GetValue(name);
            if (!System.IO.Path.IsPathRooted(path))
            {
                // This is a relative path.
                // Assume it is relative to the folder that contains web.config.
                path = HttpContext.Current.Server.MapPath(path);
            }

            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            return path;
        }

        /// <summary>
        /// Gets the string path for where the document folder resides.
        /// </summary>
        public static string DocumentFolder
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("PccDocumentFolder").

                // Using pcc.config
                return documentPath;
            }
        }

        /// <summary>
        /// Gets the string path for where the annotation files resides.
        /// </summary>
        public static string MarkupFolder
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("PccMarkupFolder");

                // Using pcc.config
                return markupsPath;
            }
        }

        /// <summary>
        /// Gets the string path for where the Markup Layer records files resides.
        /// </summary>
        public static string MarkupLayerRecordsPath
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("PccMarkupFolder");

                // Using pcc.config
                return markupLayerRecordsPath;
            }
        }
        /// <summary>
        /// Gets path for Text Search terms.
        /// </summary>
        public static string SearchTermsPath
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("PccSearchTermsPath");

                // Using pcc.config
                return searchTermsPath;
            }
        }

        /// <summary>
        /// Gets path for Text Search terms.
        /// </summary>
        public static string ApiKey
        {
            get
            {
                // Using pcc.config
                return apiKey;
            }
        }

        /// <summary>
        /// Gets the HTTP address for the imaging services.
        /// </summary>
        public static string ImagingService
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                //return GetValue("PccImagingService");

                // Using pcc.config
                return webServiceUrl;
            }
        }

        /// <summary>
        /// Gets the HTTP address for the V2 imaging services.
        /// </summary>
        public static string ImagingServiceV2
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                //return GetValue("PccImagingService");

                // Using pcc.config
                return webServiceV2Url;
            }
        }

        /// <summary>
        /// Gets the EnableDocumentPath flag
        /// </summary>
        public static bool EnableDocumentPath
        {
            get
            {
                // Using pcc.config
                return enableDocumentPath;
            }
        }

        /// <summary>
        /// Gets the string path for where the annotation files resides.
        /// </summary>
        public static string ImageStampFolder
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("ImageStampFolder");

                // Using pcc.config
                //remove slash from the dn            
                return imageStampPath;
            }
        }

        /// <summary>
        /// Gets the string path for where the annotation files resides.
        /// </summary>
        public static string ValidImageStampTypes
        {
            get
            {
                // Not used unless web.config file is switched in for pcc.config
                // return GetPath("validImageStampTypes");

                // Using pcc.config
                return validImageStampTypes.ToLower();     //.Replace(".", "").ToLower();
            }
        }

        /// <summary>
        /// Contstructor of PccConfig made inassesable because all properties and methods are static.
        /// </summary>

        private PccConfig()
        {
        }

        /// <summary>
        /// Reads the pcc.config file for local conditions are locations.
        /// </summary>
        /// <param name="configPath">The xml file name to read.</param>
        public static void LoadConfig(string configPath)
        {
            HttpRequest req = HttpContext.Current.Request;
            XmlDocument doc = new XmlDocument();

            if (!(System.IO.Path.IsPathRooted(configPath)))
            {
                configPath = System.IO.Path.Combine(req.PhysicalApplicationPath, configPath);
            }

            try
            {
                doc.Load(configPath);
            }
            catch
            {
                // pcc.config file is missing so assume defaults!
            }

            documentPath = getNode(doc, "DocumentPath");
            webServiceHost = getNode(doc, "WebServiceHost");
            webServicePort = getNode(doc, "WebServicePort");
            webServicePath = getNode(doc, "WebServicePath");
            webServiceV2Path = getNode(doc, "WebServiceV2Path");
            webServiceScheme = getNode(doc, "WebServiceScheme");
            markupsPath = getNode(doc, "MarkupsPath");
            markupLayerRecordsPath = getNode(doc, "MarkupLayerRecordsPath");
            searchTermsPath = getNode(doc, "SearchTermsPath");
            apiKey = getNode(doc, "ApiKey");
            imageStampPath = getNode(doc, "ImageStampPath");
            validImageStampTypes = getNode(doc, "ValidImageStampTypes");

            if (string.IsNullOrEmpty(documentPath))
            {
                documentPath = req.PhysicalApplicationPath;
            }

            if (documentPath.StartsWith("./") || documentPath.StartsWith(".\\"))
            {
                documentPath = System.IO.Path.Combine(req.PhysicalApplicationPath, documentPath);
            }

            if (string.IsNullOrEmpty(webServiceHost))
            {
                webServiceHost = "localhost";
            }

            if (string.IsNullOrEmpty(webServicePort))
            {
                webServicePort = "18681";
            }

            if (string.IsNullOrEmpty(webServicePath))
            {
                webServicePath = "";
            }

            if (string.IsNullOrEmpty(webServiceV2Path))
            {
                webServiceV2Path = "";
            }

            if (string.IsNullOrEmpty(webServiceScheme))
            {
                webServiceScheme = "http";
            }

            if (string.IsNullOrEmpty(markupsPath))
            {
                markupsPath = System.IO.Path.GetTempPath() + Path.DirectorySeparatorChar + ".Markups";
            }

            if (markupsPath.StartsWith("./") || markupsPath.StartsWith(".\\"))
            {
                markupsPath = System.IO.Path.Combine(req.PhysicalApplicationPath, markupsPath);
            }

            if (string.IsNullOrEmpty(markupLayerRecordsPath))
            {
                markupLayerRecordsPath = System.IO.Path.GetTempPath() + Path.DirectorySeparatorChar + ".MarkupLayerRecords";
            }

            if (markupLayerRecordsPath.StartsWith("./") || markupLayerRecordsPath.StartsWith(".\\"))
            {
                markupLayerRecordsPath = System.IO.Path.Combine(req.PhysicalApplicationPath, markupLayerRecordsPath);
            }

            if (string.IsNullOrEmpty(imageStampPath))
            {
                imageStampPath = System.IO.Path.GetTempPath() + Path.DirectorySeparatorChar + ".ImageStamp";
            }

            if (imageStampPath.StartsWith("./") || imageStampPath.StartsWith(".\\"))
            {
                imageStampPath = System.IO.Path.Combine(req.PhysicalApplicationPath, imageStampPath);
            }

            if (string.IsNullOrEmpty(searchTermsPath))
            {
                searchTermsPath = req.PhysicalApplicationPath;
            }

            if (searchTermsPath.StartsWith("./") || searchTermsPath.StartsWith(".\\"))
            {
                searchTermsPath = System.IO.Path.Combine(req.PhysicalApplicationPath, searchTermsPath);
            }

            webServiceUrl = webServiceScheme + "://" + webServiceHost + ":" + webServicePort + "/" + webServicePath;
            webServiceV2Url = webServiceScheme + "://" + webServiceHost + ":" + webServicePort + "/" + webServiceV2Path;
            documentPath = inlineEnvVariables(documentPath);
            markupsPath = inlineEnvVariables(markupsPath);
            imageStampPath = inlineEnvVariables(imageStampPath);
            markupLayerRecordsPath = inlineEnvVariables(markupLayerRecordsPath);

            if (!(documentPath.EndsWith("\\") || documentPath.EndsWith("/")))
            {
                documentPath += Path.DirectorySeparatorChar;
            }

            if (!(markupsPath.EndsWith("\\") || markupsPath.EndsWith("/")))
            {
                markupsPath += Path.DirectorySeparatorChar;
            }

            if (!(imageStampPath.EndsWith("\\") || imageStampPath.EndsWith("/")))
            {
                imageStampPath += Path.DirectorySeparatorChar;
            }

            if (!(searchTermsPath.EndsWith("\\") || searchTermsPath.EndsWith("/")))
            {
                searchTermsPath += Path.DirectorySeparatorChar;
            }

            if (!(markupLayerRecordsPath.EndsWith("\\") || markupLayerRecordsPath.EndsWith("/")))
            {
                markupLayerRecordsPath += Path.DirectorySeparatorChar;
            }

            enableDocumentPath = false;
            Boolean.TryParse(getNode(doc, "EnableDocumentPath"), out enableDocumentPath);
        }

        /// <summary>
        /// Returns a requested xml node value.
        /// </summary>
        /// <param name="doc">Xml document object to retrieve the node.</param>
        /// <param name="name">Node name in the xml document.</param>
        /// <returns>The string value within the node of interest.</returns>
        static string getNode(XmlDocument doc, string name)
        {
            XmlNodeList a = doc.GetElementsByTagName(name);
            if (a != null && a.Count > 0)
            {
                return a[0].InnerText;
            }
            return null;
        }

        /// <summary>
        /// Checks if file path is inside doc or temp folder as specified in origPath.
        /// </summary>
        /// <param name="origPath">File path to check.</param>
        /// <returns>True if the file can be opened in origPath false otherwise.</returns>
        public static bool IsFileSafeToOpen(string origPath)
        {
            if (enableDocumentPath == false)
            {
                return true;
            }
            return IsFolderSafeToOpen(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(origPath)));
        }

        /// <summary>
        /// Checks if folder is inside doc or temp folder as specified in origPath.
        /// </summary>
        /// <param name="origPath">Path to check.</param>
        /// <returns>True if the path can be opened in origPath false otherwise.</returns>
        public static bool IsFolderSafeToOpen(string origPath)
        {
            if (enableDocumentPath == false)
            {
                return true;
            }
            string fullPath = System.IO.Path.GetFullPath(origPath);
            string docPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(DocumentFolder));
            if (fullPath.StartsWith(docPath))
            {
                return true;
            }

            string markupdir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(MarkupFolder));
            if (fullPath.StartsWith(markupdir))
                return true;

            string markupRecordsLayerdir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(MarkupLayerRecordsPath));
            if (fullPath.StartsWith(markupRecordsLayerdir))
                return true;

            return false;
        }


        /// <summary>
        /// Any special xml node values indicating an environmental variable is resolved.
        /// </summary>
        /// <param name="str">The xml string to search for an environmental name.</param>
        /// <returns>The expanded environmental value if it exists or the original string if does not.</returns>
        protected static string inlineEnvVariables(string str)
        {
            Regex pattern = new Regex("\\%([A-Za-z]*)\\%");
            String ret = str;

            MatchCollection mm = pattern.Matches(str);
            foreach (Match m in mm)
            {
                string varName = m.Groups[1].Value;
                string varValue = Environment.GetEnvironmentVariable(varName);
                if (varValue != null)
                {
                    ret = ret.Substring(0, m.Index) +
                          varValue +
                          ret.Substring(m.Index + m.Length);
                }
            }

            return ret;
        }
    }
}