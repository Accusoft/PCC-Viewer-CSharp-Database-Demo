namespace Pcc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Data.Entity;

    /// <summary>
    /// Summary description for DocumentModel
    /// </summary>
    public class DocumentModel
    {
        public class Document
        {
            public int DocumentID { get; set; }
            public string ExternalId { get; set; }
        }
    }
}