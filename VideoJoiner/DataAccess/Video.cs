namespace VideoJoiner.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Video")]
    public partial class Video
    {
        public int Id { get; set; }

        [StringLength(500)]
        public string VideoTitle { get; set; }

        [StringLength(500)]
        public string SourceLink { get; set; }

        [StringLength(500)]
        public string ReuploadedLink { get; set; }

        public Progress Progress { get; set; }

        public Status Status { get; set; }

        public string Note { get; set; }

        public DateTime? CreatedDate { get; set; }

        public bool? IsDeleted { get; set; }
    }

    public enum Progress
    {
        Unhandled, Downloading, Downloaded, Joining, Joined, Uploading, Uploaded, Failed
    }

    public enum Status
    {
        Unhandled, Failed, Completed
    }
}
