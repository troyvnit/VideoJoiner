using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dashboard.Models;
using VideoJoiner.DataAccess;
using YoutubeExtractor;

namespace Dashboard.Utility
{
    public static class JoinerUtility
    {
        private static readonly string AppDataPath = (System.Web.HttpContext.Current == null)
                ? System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/")
                : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/");
        public static void Join(List<Video> videos)
        {
            foreach (var video in videos)
            {
                var cts = new CancellationTokenSource();
                Task.Run(() =>
                {
                    UpdateVideo(video, Progress.Downloading);
                    IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(video.SourceLink);
                    
                    VideoInfo videoInfo = videoInfos
                        .First(info => info.VideoType == VideoType.Mp4);
                    
                    if (videoInfo.RequiresDecryption)
                    {
                        DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
                    }

                    var originalVideo = $"{videoInfo.Title.GenerateSlug()}{videoInfo.VideoExtension}";
                    var watermarkVideo = $"{videoInfo.Title.GenerateSlug()}_Watermark{videoInfo.VideoExtension}";
                    var joinedVideo = $"{videoInfo.Title.GenerateSlug()}_Joined{videoInfo.VideoExtension}";

                    if (File.Exists(Path.Combine(AppDataPath, originalVideo)))
                    {
                        File.Delete(Path.Combine(AppDataPath, originalVideo));
                    }
                    if (File.Exists(Path.Combine(AppDataPath, watermarkVideo)))
                    {
                        File.Delete(Path.Combine(AppDataPath, watermarkVideo));
                    }
                    if (File.Exists(Path.Combine(AppDataPath, joinedVideo)))
                    {
                        File.Delete(Path.Combine(AppDataPath, joinedVideo));
                    }
                    var videoDownloader = new VideoDownloader(videoInfo, Path.Combine(AppDataPath, originalVideo));
                    
                    videoDownloader.DownloadProgressChanged += (sender, a) => System.Console.WriteLine(a.ProgressPercentage);

                    videoDownloader.DownloadFinished += (sender, eventArgs) =>
                    {
                        try
                        {
                            UpdateVideo(video, Progress.Downloaded);
                            UpdateVideo(video, Progress.Joining);
                            MakeWatermark(video, originalVideo, watermarkVideo);
                            Concat(video, watermarkVideo, joinedVideo);
                            UpdateVideo(video, Progress.Joined);
                            UpdateVideo(video, video.Progress, Status.Completed);
                            cts.Cancel();
                        }
                        catch (Exception e)
                        {
                            UpdateVideo(video, video.Progress, Status.Failed, e.InnerException?.Message);
                            cts.Cancel();
                        }
                    };
                    
                    videoDownloader.Execute();
                }, cts.Token);
            }
        }

        private static void MakeWatermark(Video video, string originalVideo, string watermarkVideo)
        {
            var ffmpeg = Path.Combine(AppDataPath, "ffmpeg.exe");
            var content = Path.Combine(AppDataPath, originalVideo);
            var output = Path.Combine(AppDataPath, watermarkVideo);
            Process ffmpegProcess = new Process();
            try
            {
                var logo = Path.Combine(AppDataPath, "vui.png");
                ProcessStartInfo ffmpeg_StartInfo = new ProcessStartInfo(ffmpeg, $" -i {content} -i {logo} -filter_complex \"overlay=main_w-overlay_w-10:10\" {output}");
                ffmpeg_StartInfo.UseShellExecute = false;
                ffmpeg_StartInfo.RedirectStandardError = true;
                ffmpeg_StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo = ffmpeg_StartInfo;
                ffmpeg_StartInfo.CreateNoWindow = true;
                ffmpegProcess.EnableRaisingEvents = true;
                ffmpegProcess.Start();
                ffmpegProcess.OutputDataReceived += (o, args) =>
                {
                    Console.WriteLine($"Output: {args.Data}");
                };
                ffmpegProcess.ErrorDataReceived += (o, args) =>
                {
                    Console.WriteLine($"Error: {args.Data}");
                };
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
                ffmpegProcess.Exited += (sender, args) =>
                {
                    
                };
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
            }
            catch (Exception ex)
            {
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
                UpdateVideo(video, video.Progress, Status.Failed, ex.InnerException?.Message);
            }
        }

        private static void Concat(Video video, string watermarkVideo, string joinedVideo)
        {
            var ffmpeg = Path.Combine(AppDataPath, "ffmpeg.exe");
            var introStart = Path.Combine(AppDataPath, "IntroStart.mp4");
            var introEnd = Path.Combine(AppDataPath, "IntroEnd.mp4");
            var content = Path.Combine(AppDataPath, watermarkVideo);
            var output = Path.Combine(AppDataPath, joinedVideo);
            Process ffmpegProcess = new Process();
            try
            {
                ProcessStartInfo ffmpeg_StartInfo = new ProcessStartInfo(ffmpeg, $" -i {introStart} -i {content} -i {introEnd} -filter_complex \"[0:0] [0:1] [1:0] [1:1] [2:0] [2:1] concat=n=3:v=1:a=1 [v] [a1]\" -map \"[v]\" -map \"[a1]\" {output}");              
                ffmpeg_StartInfo.UseShellExecute = false;
                ffmpeg_StartInfo.RedirectStandardError = true;
                ffmpeg_StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo = ffmpeg_StartInfo;
                ffmpeg_StartInfo.CreateNoWindow = true;
                ffmpegProcess.EnableRaisingEvents = true;
                ffmpegProcess.Start();
                ffmpegProcess.OutputDataReceived += (o, args) =>
                {
                    Console.WriteLine($"Output: {args.Data}");
                };
                ffmpegProcess.ErrorDataReceived += (o, args) =>
                {
                    Console.WriteLine($"Error: {args.Data}");
                };
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
            }
            catch (Exception ex)
            {
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
                UpdateVideo(video, video.Progress, Status.Failed, ex.InnerException?.Message);
            }
        }

        public static string GenerateSlug(this string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return string.Empty;
            }
            var normalizedString = title.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(title.Length);
            foreach (var c in normalizedString.ToLower().ToCharArray())
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    switch (c.ToString())
                    {
                        case "&":
                            stringBuilder.Append("va");
                            break;
                        case "$":
                            stringBuilder.Append("dola");
                            break;
                        case "%":
                            stringBuilder.Append("phan-tram");
                            break;
                        case ".":
                            stringBuilder.Append("-");
                            break;
                        case "/":
                            stringBuilder.Append("-");
                            break;
                        case "\\":
                            stringBuilder.Append("-");
                            break;
                        case "đ":
                            stringBuilder.Append("d");
                            break;
                        default:
                            stringBuilder.Append(c);
                            break;
                    }
                }
            }
            var slug = stringBuilder.ToString();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", " ").Trim();
            slug = Regex.Replace(slug, @"\s", "-");
            return slug;
        }

        private static void UpdateVideo(Video video, Progress progress, Status status = Status.Unhandled, string note = "")
        {
            using (var db = new VideoJoinerContext())
            {
                video.Progress = progress;
                video.Status = status;
                video.Note = note;
                db.Videos.Attach(video);
                db.Entry(video).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}