namespace PccViewer.WebTier.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Data.Entity;

    /// <summary>
    /// Summary description for AnnotationModel
    /// </summary>
    public class AnnotationModel
    {
        public class AnnotationContext : DbContext
        {
            public AnnotationContext()
                : base("AnnotationTest1")
            {
            }
            public DbSet<DocumentModel.Document> Documents { get; set; }
            public DbSet<Annotation> Annotations { get; set; }
            //public DbSet<MarkModel.Mark> Marks { get; set; }
        }

        public class Annotation
        {
            public int AnnotationID { get; set; }
            public string UserName { get; set; }
            public string AnnotationText { get; set; }

            // Foreign key for a Document
            public int DocumentID { get; set; }
            public virtual DocumentModel.Document Document { get; set; }
        }
    } 
}