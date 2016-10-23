using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Dashboard.Models;
using Dashboard.Utility;
using VideoJoiner.Utility;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using VideoJoiner.DataAccess;

namespace VideoJoiner.Hubs
{
    [HubName("MyHub")]
    public class MyHub : Hub
    {
        private CancellationTokenSource _cts;

        public async Task StartVideoJoiner()
        {
            try
            {
                _cts = new CancellationTokenSource();

                await Task.Run(async () =>
                 {
                     using (var db = new VideoJoinerContext())
                     {
                         var settings = await db.Settings.ToListAsync();
                         var totalSetting = settings.FirstOrDefault(s => s.SettingKey == "TotalVideosPerSession");
                         var take = totalSetting != null ? int.Parse(totalSetting.SettingValue) : 2;
                         var skip = 0;
                         while (!_cts.IsCancellationRequested)
                         {
                             if (!db.Videos.Any(v => v.Status == Status.Handling))
                             {
                                 var videos = await db.Videos.Where(v => v.Status != Status.Completed).OrderBy(v => v.Status).Take(take).ToListAsync();
                                 if (videos.Any())
                                 {
                                     skip += take;
                                     JoinerUtility.Join(videos);
                                 }
                             }
                             await Task.Delay(2000);
                         }
                     }
                 }, _cts.Token);
            }
            catch (Exception e)
            {
                _cts?.Cancel();
            }
        }

        public void StopVideoJoiner()
        {
            _cts?.Cancel();
            JoinerUtility.StopJoin();
        }
    }
}