//----------------------------------------------------------------------
// <copyright file="Encoder.cs" company="Accusoft Corporation">
// CopyrightÂ© 1996-2015 Accusoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------

namespace PccViewer.WebTier.Core
{
    using System;
    using System.ComponentModel;
    using System.Text;
    using System.Security.Cryptography;

    /// <summary>
    /// This class provides encoding and decoding of the DocumentIdentifier string.
    /// Internal API.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Encoder
    {
        /// <summary>
        /// Encoded the Docuemntidentifier string
        /// </summary>
        /// <param name="input">DocumentIdentifier to be encoded.</param>
        /// <returns>Encoded string</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string EncodeURLString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string str;

            byte[] b = new byte[input.Length * 2];

            for (int i = 0; i < input.Length; i++)
            {
                b[i * 2] = (byte)((int)input[i] % 256);
                b[(i * 2) + 1] = (byte)((int)input[i] / 256);
            }

            str = System.Web.HttpServerUtility.UrlTokenEncode(b);

            return str;
        }

        /// <summary>
        /// Internal API.
        /// Decodes the encoded DocumentIdentifier.
        /// </summary>
        /// <param name="conv_str">Encoded string</param>
        /// <returns>encoded document Identifier</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string DecodeURLString(string conv_str)
        {
            if (string.IsNullOrEmpty(conv_str))
            {
                return string.Empty;
            }

            byte[] conv = System.Web.HttpServerUtility.UrlTokenDecode(conv_str);

            System.Text.StringBuilder sb = new System.Text.StringBuilder(conv.Length / 2);

            for (int i = 0; i < conv.Length - 1; i += 2)
            {
                sb.Append((char)((int)conv[i] + (256 * (int)conv[i + 1])));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a computed SHA1 hash of the full document path. Used for associating 
        /// annotation files to their original documents.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetHashString(string input)
        {
            HashAlgorithm hashAlg = new SHA1Cng();
            byte[] hash = hashAlg.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash);
        }

        /// <summary>
        /// Encodes the provided string.
        /// Internal API.
        /// </summary>
        /// <param name="s"> string to be encoded</param>
        /// <returns>Formatted string suitable for the client.</returns>
        internal static string EncodeJsString(string s)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\"");

            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;

                    case '\\':
                        sb.Append("\\\\");
                        break;

                    case '\b':
                        sb.Append("\\b");
                        break;

                    case '\f':
                        sb.Append("\\f");
                        break;

                    case '\n':
                        sb.Append("\\n");
                        break;

                    case '\r':
                        sb.Append("\\r");
                        break;

                    case '\t':
                        sb.Append("\\t");
                        break;

                    default:
                        int i = (int)c;
                        
                        if (i < 32 || i > 127)
                        {
                            sb.AppendFormat("\\u{0:X04}", i);
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append("\"");

            return sb.ToString();
        }

        /// <summary>
        /// Base 64 Array to raw data byte array
        /// </summary>
        /// <param name="encodedArray">Base 64 encoded array</param>
        /// <returns>Unencoded array of bytes</returns>
        internal static byte[] DecodeBase64Array(byte[] encodedArray)
        {
            string encodedStr = System.Text.ASCIIEncoding.ASCII.GetString(encodedArray);
            byte[] unencodedArray = Convert.FromBase64String(encodedStr);

            return unencodedArray;
        }

        /// <summary>
        /// Converts base 64 string back to its normal form.
        /// </summary>
        /// <param name="conv_str">Base 64 data string.</param>
        /// <returns>Byte array of unencoded data.</returns>
        internal static byte[] DecodeBase64String(string conv_str)
        {
            if (string.IsNullOrEmpty(conv_str))
            {
                return null;
            }

            byte[] data = Convert.FromBase64String(conv_str);
            return data;
        }

        /// <summary>
        /// Converts data to base 64.
        /// </summary>
        /// <param name="inputArray">Byte array of data to be encoded to base 64.</param>
        /// <returns>Base 64 string.</returns>
        internal static string EncodeBase64String(byte[] inputArray)
        {
            string str = Convert.ToBase64String(inputArray);
            return str;
        }
    }
}
