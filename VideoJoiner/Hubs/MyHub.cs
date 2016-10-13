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
        public async Task StartVideoJoiner()
        {
            var cts = new CancellationTokenSource();
            try
            {
                await Task.Run(async () =>
                 {
                     using (var db = new VideoJoinerContext())
                     {
                         var videos = await db.Videos.ToListAsync();
                         JoinerUtility.Join(videos);
                     }
                 }, cts.Token);
            }
            catch (Exception e)
            {
                cts.Cancel();
            }
        }
    }
}