using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Security.Cryptography;
using System.Web.Services;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using PccViewer.WebTier.Core;


public partial class _Default : System.Web.UI.Page
{
    public string languageJson = "{}";
    public string searchJson = "{}";
    public String htmlTemplates = String.Empty;
    public String redactionReasons = String.Empty;

    static string root = HttpContext.Current.Server.MapPath(".");
    string languageFileName = root + "/viewer-assets/languages/en-US.json";
    string searchtext = root + "/predefinedsearch.json";
    string redactionReasonFile = root + "/redactionReason.json";
    string templatePath = root + "/viewer-assets/templates";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!this.IsPostBack)
        {
            HttpRequest req = HttpContext.Current.Request;

            JavaScriptSerializer ser = new JavaScriptSerializer();
            string configPath = System.IO.Path.Combine(req.PhysicalApplicationPath, languageFileName);
            if (File.Exists(configPath))
            {
                using (Stream jsonDataStream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (TextReader tr = new StreamReader(jsonDataStream))
                    {
                        languageJson = tr.ReadToEnd();
                        languageJson = languageJson.Replace('\r', ' ');
                        languageJson = languageJson.Replace('\n', ' ');
                        languageJson = languageJson.Replace('\t', ' ');
                    }
                    jsonDataStream.Close();
                }
            }

            configPath = System.IO.Path.Combine(req.PhysicalApplicationPath, searchtext);
            if (File.Exists(configPath))
            {
                using (Stream jsonDataStream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (TextReader tr = new StreamReader(jsonDataStream))
                    {
                        searchJson = tr.ReadToEnd();
                        searchJson = searchJson.Replace('\r', ' ');
                        searchJson = searchJson.Replace('\n', ' ');
                        searchJson = searchJson.Replace('\t', ' ');
                    }
                    jsonDataStream.Close();
                }
            }

            getTemplates(System.IO.Path.Combine(req.PhysicalApplicationPath, templatePath));
            getRedactonReasons(System.IO.Path.Combine(req.PhysicalApplicationPath, redactionReasonFile));
        }
    }

    private static string[] GetFiles(string sourceFolder, string filters, System.IO.SearchOption searchOption)
    {
        return filters.Split('|').SelectMany(filter => System.IO.Directory.GetFiles(sourceFolder, filter, searchOption)).ToArray();
    }

    private void getTemplates(string templatePath)
    {
        string templateData = string.Empty;
        Dictionary<string, String> json = new Dictionary<string, String>();

        //Location where template files are stored
        var templateList = GetFiles(templatePath, "*Template.html", System.IO.SearchOption.TopDirectoryOnly);

        for (int i = 0; i < templateList.Length; i++)
        {
            if (File.Exists(templateList[i]))
            {
                using (Stream jsonDataStream = new FileStream(templateList[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (TextReader tr = new StreamReader(jsonDataStream))
                    {
                        templateData = tr.ReadToEnd();
                        templateData = templateData.Replace('\r', ' ');
                        templateData = templateData.Replace('\n', ' ');
                        templateData = templateData.Replace('\t', ' ');
                        if (templateData.Length > 0)
                        {
                            var regex = new Regex("Template.html", RegexOptions.IgnoreCase);
                            String fileName = regex.Replace(templateList[i], "");
                            json.Add(System.IO.Path.GetFileName(fileName), templateData);
                        }
                    }
                    jsonDataStream.Close();
                }
            }
        }
        //stringify JSON object
        htmlTemplates = toJSON(json);
    }

    private void getRedactonReasons(string filePath) 
    {
        if (File.Exists(filePath))
        {
            using (Stream jsonDataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (TextReader tr = new StreamReader(jsonDataStream))
                {
                    redactionReasons = tr.ReadToEnd();
                    redactionReasons = redactionReasons.Replace('\r', ' ');
                    redactionReasons = redactionReasons.Replace('\n', ' ');
                    redactionReasons = redactionReasons.Replace('\t', ' ');
                }
                jsonDataStream.Close();
            }
        }

        if (redactionReasons == String.Empty)
        {
            redactionReasons = "undefined";
        }
    }

    private string toJSON(Object obj)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        return serializer.Serialize(obj);
    }

}
