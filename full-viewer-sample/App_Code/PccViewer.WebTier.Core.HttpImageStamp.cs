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

    /// <summary>
    /// Handles the request from the viewer to acquire image from the web server
    /// in binary format or base64
    /// </summary>
    public class ImageStamp : PccHandler
    {
        string imagePath = String.Empty;
        String imageStampPath = String.Empty;

        public override void ProcessRequest(HttpContext context, Match match)
        {
            //context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            //context.Response.Cache.SetMaxAge(TimeSpan.FromHours(24));

            // Environmental Setup
            PccConfig.LoadConfig("viewer-webtier/pcc.config");
            imagePath = PccConfig.ImageStampFolder;

            try
            {
                string imageFormat = context.Request.QueryString["format"];
                char[] charsToTrim = { '/', '\\' };
                string[] paramslist = match.ToString().Split(charsToTrim);
                string[] validParams = new[] { "base64", "image" };
                String imageStampId = String.Empty;


                var flag = Array.Exists(validParams, element => element == imageFormat.ToLower());

                if (flag)
                {
                    imageStampId = paramslist[2];
                    imageStampPath = GetPath(imageStampId);

                    String fileExtension = Path.GetExtension(imageStampPath);

                    this.ValidateImagefileType(imageStampId);

                    if (imageFormat.ToLower() == "base64")
                    {
                        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                        context.Response.Cache.SetMaxAge(TimeSpan.FromHours(24));

                        context.Response.ContentType = "text/plain";
                        context.Response.Write(ImageToBase64String(imageStampId));
                    }
                    if (imageFormat.ToLower() == "image")
                    {
                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Cache.SetMaxAge(TimeSpan.FromHours(24));
                        context.Response.Cache.SetExpires(DateTime.Now.AddHours(24));
                        context.Response.ClearHeaders();
                        context.Response.ContentType = GetMimeType(fileExtension);
                        context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(imageStampPath).ToString());
                        context.Response.WriteFile(imageStampPath);
                    }
                }
                else
                {
                    throw new Exception("Parameter format requires valid values. Valid values are Base64 and Image");
                }
            }
            catch (Exception e)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Write("Exception Message: " + e.Message);
                context.Response.Write("StackTrace: " + e.StackTrace.ToString());
                //context.Response.Write("Exception InnerException: " + e.InnerException.ToString());
                context.Response.ContentType = "text/plain";
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        //This function will validate if the file format is supported or not
        private void ValidateImagefileType(String ImageStampId)
        {
            //Retrieve the valid image types from the pcc.config file
            String[] ValidImageStampTypes = PccConfig.ValidImageStampTypes.Replace(".", "").Split(',');
            String fullPath = imageStampPath;
            string fileExtension = Path.GetExtension(fullPath).Replace(".", "");

            var flag = Array.Exists(ValidImageStampTypes, element => element == fileExtension.ToLower());

            if (!flag)
            {
                throw new Exception("File extension is not valid for this operation.");
            }
        }

        //Covert the image to Base64 string
        private string ImageToBase64String(String ImageStampId)
        {
            using (Image image = Image.FromFile(imageStampPath))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    StringBuilder sb = new StringBuilder();
                    StringBuilder sb1 = new StringBuilder();

                    String mimeType = String.Empty;

                    // Convert Image to byte[]
                    image.Save(ms, image.RawFormat);
                    byte[] imageBytes = ms.ToArray();

                    mimeType = this.GetMimeType(Path.GetExtension(imageStampPath));

                    sb.AppendFormat("data: {0};base64,{1}", mimeType, Convert.ToBase64String(imageBytes));
                    sb1.AppendFormat("{{\"dataHash\": \"{0}\", \"dataUrl\": \"{1}\"}}", PccViewer.WebTier.Core.Encoder.GetHashString(sb.ToString()), sb.ToString());
                    return sb1.ToString();
                }
            }
        }

        //Decode the ImageStampId and return the fullpath of the ImageStamp
        private String GetPath(String ImageStampId)
        {

            return PccConfig.ImageStampFolder + PccViewer.WebTier.Core.Encoder.DecodeURLString(ImageStampId);

            //String fullPath = PccConfig.ImageStampFolder + PccViewer.WebTier.Core.Encoder.DecodeURLString(ImageStampId);

            //if (!File.Exists(fullPath))
            //{
            //    throw new Exception("Invalid ImageStampId: " + ImageStampId);
            //}

            //return fullPath;

        }

        private String GetMimeType(String FileExtension)
        {
            String mimeType = String.Empty;
            FileExtension = FileExtension.Replace(".", "");

            switch (FileExtension.ToLower())
            {
                case "png":
                    mimeType = "image/png";
                    break;
                case "jpg":
                    mimeType = "image/jpeg";
                    break;
                case "jpeg":
                    mimeType = "image/jpeg";
                    break;
                case "gif":
                    mimeType = "image/gif";
                    break;
                default:
                    throw new Exception("File extension is not valid.");
            }

            return mimeType;
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}