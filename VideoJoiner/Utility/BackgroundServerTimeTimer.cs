using VideoJoiner.Hubs;
using VideoJoiner.Utility;
using Microsoft.AspNet.SignalR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json;
using VideoJoiner.DataAccess;

namespace VideoJoiner
{
    public class BackgroundServerTimeTimer : IRegisteredObject
    {
        private readonly Timer _timer;
        private readonly Timer _timer1;
        private readonly IHubContext _hubContext;

        public BackgroundServerTimeTimer()
        {
            HostingEnvironment.RegisterObject(this);
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            _timer = new Timer(OnTimerElapsed, null,TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            _timer1 = new Timer(OnTimerElapsed1, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimerElapsed(object sender)
        {
            _hubContext.Clients.All.monthlyRainFall(VideoJoinerData.GetChartDataForVideoJoiner());
            _hubContext.Clients.All.customerInformations(VideoJoinerData.GetCustomerInformations());
        }
        private void OnTimerElapsed1(object sender)
        {
            var server = new ServerPerformanceUtility();

           _hubContext.Clients.All.serverPerformanceDetails(null);

        }

        public void Stop(bool immediate)
        {
            _timer.Dispose();
            _timer1.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }
    }

}