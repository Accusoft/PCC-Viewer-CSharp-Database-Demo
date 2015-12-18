using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Pcc
{
    /// <summary>
    ///     Obtains information from a configuration file (i.e.,"pcc.config").
    /// </summary>
    public static class PccConfig
    {
        private static FileSystemWatcher _watcher;
        private static string _fullName;

        /// <summary>
        ///     Full path of the pcc.config file
        /// </summary>
        public static string FullName
        {
            get
            {
                return _fullName;
            }
            set
            {
                _fullName = value;
                LoadConfig();
                WatchPhysicalFile();
            }
        }

        /// <summary>
        ///     Gets the string path for where the document folder resides.
        /// </summary>
        public static string DocumentFolder { get; private set; }

        /// <summary>
        ///     Gets the scheme of the PrizmApplicationServices
        /// </summary>
        public static string PrizmApplicationServicesScheme { get; private set; }

        /// <summary>
        ///     Gets the host of the PrizmApplicationServices
        /// </summary>
        public static string PrizmApplicationServicesHost { get; private set; }

        /// <summary>
        ///     Gets the port number of the PrizmApplicationServices
        /// </summary>
        public static int PrizmApplicationServicesPort { get; private set; }

        /// <summary>
        ///     Gets the entire address of the PrizmApplicationServices
        /// </summary>
        public static string WebTierAddress
        {
            get { return string.Format("{0}://{1}:{2}", PrizmApplicationServicesScheme, PrizmApplicationServicesHost, PrizmApplicationServicesPort); }
        }

        /// <summary>
        ///     Returns a requested xml node value.
        /// </summary>
        /// <param name="doc">Xml document object to retrieve the node.</param>
        /// <param name="name">Node name in the xml document.</param>
        /// <returns>The string value within the node of interest.</returns>
        private static string GetNode(XmlDocument doc, string name)
        {
            var a = doc.GetElementsByTagName(name);
            return a.Count > 0 ? a[0].InnerText : null;
        }

        /// <summary>
        ///     Reads the pcc.config file for local conditions are locations.
        /// </summary>
        /// <param name="configPath">The xml file name to read.</param>
        private static void LoadConfig()
        {
            var doc = new XmlDocument();

            doc.Load(FullName);

            DocumentFolder = GetNode(doc, "DocumentPath");
            PrizmApplicationServicesScheme = GetNode(doc, "PrizmApplicationServicesScheme");
            PrizmApplicationServicesHost = GetNode(doc, "PrizmApplicationServicesHost");
            PrizmApplicationServicesPort = Convert.ToInt32(GetNode(doc, "PrizmApplicationServicesPort"));

            if(!string.IsNullOrEmpty(DocumentFolder)){
                DocumentFolder = InlineEnvVariables(DocumentFolder);

                if (!(DocumentFolder.EndsWith("\\") || DocumentFolder.EndsWith("/")))
                {
                    DocumentFolder += Path.DirectorySeparatorChar;
                }
            }            
        }

        private static void WatchPhysicalFile()
        {
            var fileInfo = new FileInfo(FullName);
            // If there is a watcher open, dispose of it
            if (_watcher != null)
            {
                _watcher.Dispose();
            }


            // Create a new FileSystemWatcher to watch for the pcc.config file
            _watcher = new FileSystemWatcher
            {
                Path = fileInfo.DirectoryName,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                               | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = fileInfo.Name
            };

            // Add event handlers
            _watcher.Changed += OnPccConfigFileChanged;
            _watcher.Created += OnPccConfigFileChanged;

            // Begin watching
            _watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private static void OnPccConfigFileChanged(object source, FileSystemEventArgs e)
        {
            LoadConfig();
        }

        /// <summary>
        ///     Any special xml node values indicating an environmental variable is resolved.
        /// </summary>
        /// <param name="str">The xml string to search for an environmental name.</param>
        /// <returns>The expanded environmental value if it exists or the original string if does not.</returns>
        private static string InlineEnvVariables(string str)
        {
            var pattern = new Regex("\\%([A-Za-z]*)\\%");
            var ret = str;

            var mm = pattern.Matches(str);
            foreach (Match m in mm)
            {
                var varName = m.Groups[1].Value;
                var varValue = Environment.GetEnvironmentVariable(varName);
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