using VideoJoiner.Hubs;
using VideoJoiner.Utility;
using Microsoft.AspNet.SignalR;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
        private readonly Timer _sendEmailtTimer;
        private readonly IHubContext _hubContext;

        public BackgroundServerTimeTimer()
        {
            HostingEnvironment.RegisterObject(this);
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            _timer = new Timer(OnTimerElapsed, null,TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            _sendEmailtTimer = new Timer(OnSendEmailTimerElapsed, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        }

        private void OnTimerElapsed(object sender)
        {
            _hubContext.Clients.All.monthlyRainFall(VideoJoinerData.GetChartDataForVideoJoiner());
            _hubContext.Clients.All.customerInformations(VideoJoinerData.GetCustomerInformations());
            _hubContext.Clients.All.videoJoinerInfos(VideoJoinerData.GetVideoJoinerInfos());
        }
        private void OnSendEmailTimerElapsed(object sender)
        {
            try
            {
                var videoJoinerInfos = VideoJoinerData.GetVideoJoinerInfos();
                using (var db = new VideoJoinerContext())
                {
                    var settings = db.Settings.ToList();

                    MailMessage message = new System.Net.Mail.MailMessage();
                    string fromEmail = settings.FirstOrDefault(s => s.SettingKey == "SendEmail_FromEmail")?.SettingValue;
                    string password = settings.FirstOrDefault(s => s.SettingKey == "SendEmail_Password")?.SettingValue;
                    string toEmail = settings.FirstOrDefault(s => s.SettingKey == "SendEmail_ToEmail")?.SettingValue;
                    if (fromEmail != null)
                    {
                        message.From = new MailAddress(fromEmail);
                        if (toEmail != null) message.To.Add(toEmail);
                        message.Subject =
                            settings.FirstOrDefault(s => s.SettingKey == "SendEmail_Subject")?.SettingValue;
                        var body = "";
                        body += $"Total videos: {videoJoinerInfos.TotalVideos}. ";
                        body += $"Total completed videos: {videoJoinerInfos.TotalCompletedVideos}. ";
                        body += $"Total failed videos: {videoJoinerInfos.TotalFailedVideos}.";
                        body += $"Total unhandled videos: {videoJoinerInfos.TotalUnhandledVideos}. ";
                        message.Body = body;
                        message.IsBodyHtml = true;
                        message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                        var host = settings.FirstOrDefault(s => s.SettingKey == "SendEmail_SmtpHost")?.SettingValue;
                        int port = 587;
                        var portSetting =
                            settings.FirstOrDefault(s => s.SettingKey == "SendEmail_SmtpPort")?.SettingValue;
                        if (portSetting != null)
                        {
                            port = int.Parse(portSetting);
                        }
                        using (SmtpClient smtpClient = new SmtpClient(host, port))
                        {
                            smtpClient.EnableSsl = true;
                            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                            smtpClient.UseDefaultCredentials = false;
                            smtpClient.Credentials = new NetworkCredential(fromEmail, password);

                            smtpClient.Send(message.From.ToString(), message.To.ToString(), message.Subject,
                                message.Body);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        public void Stop(bool immediate)
        {
            _timer.Dispose();
            _sendEmailtTimer.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }
    }

}