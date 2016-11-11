using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VideoJoiner.DataAccess;

namespace Dashboard.Models
{
    public class VideoJoinerInfos
    {
        public int TotalVideos { get; set; }
        public int TotalUnhandledVideos { get; set; }
        public int TotalFailedVideos { get; set; }
        public int TotalCompletedVideos { get; set; }
        public bool Running { get; set; }

        public List<Video> Videos { get; set; }
    }
}