using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Dashboard.Models;
using VideoJoiner.Controllers;
using VideoJoiner.Hubs;
using VideoJoiner.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using VideoJoiner.DataAccess;

namespace VideoJoiner.Utility
{
    public static class VideoJoinerData
    {
        #region < Private Variable Declaration >
        private static Timer _timer;
        private static readonly Random Random = new Random();
        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            var next = Random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        private static int RandomIntNumberBetween(int minValue, int maxValue)
        {
            return Random.Next(minValue, maxValue); 
        }
        #endregion

        #region < Mock Data Repositories >
        private static Graph GetTokyoData()
        {
            var arr = new Graph();
            arr.name = "Tokyo";
            for (int i = 0; i < 12; i++)
            {
                arr.data.Add(RandomNumberBetween(1, 120));
            }
            return arr;
        }

        private static Graph GetLondonData()
        {
            var arr = new Graph();
            arr.name = "London";
            for (int i = 0; i < 12; i++)
            {
                arr.data.Add(RandomNumberBetween(1, 120));
            }
            return arr;
        }

        private static Graph GetBerlinData()
        {
            var arr = new Graph();
            arr.name = "Berlin";
            for (int i = 0; i < 12; i++)
            {
                arr.data.Add(RandomNumberBetween(1, 120));
            }
            return arr;
        }

        private static Graph GetNewYorkData()
        {
            var arr = new Graph();
            arr.name = "New York";
            for (int i = 0; i < 12; i++)
            {
                arr.data.Add(RandomNumberBetween(1, 120));
            }
            return arr;
        }

        #endregion

        #region < Public Methods >

        public static List<Graph> GetChartDataForVideoJoiner()
        {
            var list = new List<Graph>();
            list.Add(GetTokyoData());
            list.Add(GetLondonData());
            list.Add(GetBerlinData());
            list.Add(GetNewYorkData());

            return list;
        }

        public static CustomerInformations GetCustomerInformations()
        {
            return new CustomerInformations()
            {
                NewlyRegistered = RandomIntNumberBetween(200, 5000).ToString(),
                SubscribedCustomers = RandomIntNumberBetween(200, 3000).ToString(),
                TopRatedCustomers = RandomIntNumberBetween(300, 1500).ToString(),
                PendingToApprove = RandomIntNumberBetween(10, 500).ToString(),
            };
        }

        public static VideoJoinerInfos GetVideoJoinerInfos()
        {
            var videoJoinerInfos = new VideoJoinerInfos();
            using (var db = new VideoJoinerContext())
            {
                var videos = db.Videos;
                videoJoinerInfos.TotalVideos = videos.Count();
                videoJoinerInfos.TotalUnhandledVideos = videos.Count(v => v.Status == Status.Unhandled);
                videoJoinerInfos.TotalFailedVideos = videos.Count(v => v.Status == Status.Failed);
                videoJoinerInfos.TotalCompletedVideos = videos.Count(v => v.Status == Status.Completed);
                videoJoinerInfos.Videos = videos.Where(v => v.Status != Status.Completed).ToList();
            }
            return videoJoinerInfos;
        }
        #endregion
    }
}