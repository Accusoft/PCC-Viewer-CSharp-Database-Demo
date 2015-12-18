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

public partial class _Default : System.Web.UI.Page
{
    public string languageJson = "{}";
    public string searchJson = "{}";
    string fileName = "language.json";
    string searchtext = "predefinedsearch.json";
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!this.IsPostBack)
        {
            HttpRequest req = HttpContext.Current.Request;
            JavaScriptSerializer ser = new JavaScriptSerializer();
            string configPath = System.IO.Path.Combine(req.PhysicalApplicationPath, fileName);
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
                        searchJson = searchJson.Replace('\r',' ');
                        searchJson = searchJson.Replace('\n', ' ');
                        searchJson = searchJson.Replace('\t', ' ');
                     }
                    jsonDataStream.Close();
                }
            }
       }
    }
}