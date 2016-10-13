using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VideoJoiner.DataAccess;

namespace Dashboard.Models
{
    public class VideoDto
    {
        public int Id { get; set; }
        
        public string VideoTitle { get; set; }
        
        public string SourceLink { get; set; }
        
        public string ReuploadedLink { get; set; }

        public Progress Progress { get; set; }

        public int ProgressPercentage { get; set; }

        public string Note { get; set; }

        public DateTime? CreatedDate { get; set; }

        public bool? IsDeleted { get; set; }
    }
}