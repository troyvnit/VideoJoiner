using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dashboard.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using VideoJoiner.DataAccess;
using VideoJoiner.Models;
using YoutubeExtractor;
using Video = VideoJoiner.DataAccess.Video;

namespace Dashboard.Utility
{
    public static class JoinerUtility
    {
        private static readonly string AppDataPath = (System.Web.HttpContext.Current == null)
            ? System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/")
            : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/");

        private static string _fbAccessToken;
        //private static string _cloudName;
        //private static string _apiKey;
        //private static string _apiSecret;
        private static string _youtubeClientId;
        private static string _youtubeClientSecret;
        private static string _youtubeUser;

        public static void Join(List<Video> videos)
        {
            using (var db = new VideoJoinerContext())
            {
                var settings = db.Settings.ToList();
                _fbAccessToken = settings.FirstOrDefault(s => s.SettingKey == "FacebookAccessToken")?.SettingValue;
                //_cloudName = settings.FirstOrDefault(s => s.SettingKey == "CloudinaryCloudName")?.SettingValue;
                //_apiKey = settings.FirstOrDefault(s => s.SettingKey == "CloudinaryApiKey")?.SettingValue;
                //_apiSecret = settings.FirstOrDefault(s => s.SettingKey == "CloudinaryApiSecret")?.SettingValue;
                _youtubeClientId = settings.FirstOrDefault(s => s.SettingKey == "YoutubeClientId")?.SettingValue;
                _youtubeClientSecret = settings.FirstOrDefault(s => s.SettingKey == "YoutubeClientSecret")?.SettingValue; 
                _youtubeUser = settings.FirstOrDefault(s => s.SettingKey == "YoutubeUser")?.SettingValue;
            }
            foreach (var video in videos)
            {
                var cts = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    try
                    {
                        var originalVideo = "";
                        var watermarkVideo = "";
                        var joinedVideo = "";

                        if (video.SourceLink.ToLower().Contains("youtube"))
                        {
                            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(video.SourceLink);

                            VideoInfo videoInfo = videoInfos
                                .First(info => info.VideoType == VideoType.Mp4);

                            video.VideoTitle = videoInfo.Title;

                            originalVideo = Path.Combine(AppDataPath,
                                $"{videoInfo.Title.GenerateSlug()}.mp4");

                            watermarkVideo = Path.Combine(AppDataPath,
                                $"{videoInfo.Title.GenerateSlug()}_Watermark.mp4");

                            joinedVideo = Path.Combine(AppDataPath,
                                $"{videoInfo.Title.GenerateSlug()}_Joined.mp4");

                            if (video.Progress >= Progress.Downloaded && File.Exists(originalVideo))
                            {
                                await RunProcesses(video, originalVideo, watermarkVideo, joinedVideo);
                                return;
                            }

                            UpdateVideo(video, Progress.Downloading, Status.Handling);

                            if (videoInfo.RequiresDecryption)
                            {
                                DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
                            }

                            var videoDownloader = new VideoDownloader(videoInfo, Path.Combine(AppDataPath, originalVideo));

                            videoDownloader.DownloadProgressChanged +=
                                (sender, a) => System.Console.WriteLine(a.ProgressPercentage);

                            videoDownloader.DownloadFinished += async (sender, eventArgs) =>
                            {
                                UpdateVideo(video, Progress.Downloaded, Status.Handling);
                                await RunProcesses(video, originalVideo, watermarkVideo, joinedVideo);
                                cts.Cancel();
                            };

                            videoDownloader.Execute();
                        }
                        else
                        {
                            using (var webClient = new WebClient())
                            {
                                var fbVideoMatch = Regex.Match(video.SourceLink, "(\\d+)\\/?$");
                                if (fbVideoMatch.Success)
                                {
                                    var fbVideoId = fbVideoMatch.Value.Replace("/", "");

                                    var resultString =
                                        webClient.DownloadString(
                                            $"https://graph.facebook.com/v2.8/{fbVideoId}?fields=source%2Ctitle%2Cdescription&access_token={_fbAccessToken}");

                                    var result = JsonConvert.DeserializeObject<FacebookVideoInfo>(resultString);

                                    if (result.Error == null)
                                    {
                                        video.VideoTitle = result.Title;
                                        video.Description = result.Description;

                                        originalVideo = Path.Combine(AppDataPath,
                                            $"{result.Title.GenerateSlug()}.mp4");

                                        watermarkVideo = Path.Combine(AppDataPath,
                                            $"{result.Title.GenerateSlug()}_Watermark.mp4");

                                        joinedVideo = Path.Combine(AppDataPath,
                                            $"{result.Title.GenerateSlug()}_Joined.mp4");

                                        if (video.Progress >= Progress.Downloaded && File.Exists(originalVideo))
                                        {
                                            await RunProcesses(video, originalVideo, watermarkVideo, joinedVideo);
                                            return;
                                        }

                                        UpdateVideo(video, Progress.Downloading, Status.Handling);

                                        webClient.DownloadFileAsync(new Uri(result.Source), originalVideo);
                                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(async (sender, args) =>
                                            {
                                                UpdateVideo(video, Progress.Downloaded, Status.Handling);
                                                await RunProcesses(video, originalVideo, watermarkVideo, joinedVideo);
                                                cts.Cancel();
                                            });
                                    }
                                    else
                                    {
                                        UpdateVideo(video, video.Progress, Status.Failed, "Can't download this video!");
                                    }
                                }
                                else
                                {
                                    UpdateVideo(video, video.Progress, Status.Failed, "Invalid Facebook link!");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        UpdateVideo(video, video.Progress, Status.Failed, e.InnerException?.Message);
                        cts.Cancel();
                    }
                }, cts.Token);
            }
        }

        private static async Task RunProcesses(Video video, string originalVideo, string watermarkVideo, string joinedVideo)
        {
            if (!(video.Progress >= Progress.Joined && File.Exists(joinedVideo)))
            {
                UpdateVideo(video, Progress.Joining, Status.Handling);
                MakeWatermark(video, originalVideo, watermarkVideo);
                Concat(video, watermarkVideo, joinedVideo);
            }
            //UploadVideo(video, originalVideo, watermarkVideo, joinedVideo);
            await UploadToYoutube(video, originalVideo, watermarkVideo, joinedVideo);
        }

        private static void DeleteVideos(string[] videos)
        {
            foreach (var video in videos)
            {
                try
                {
                    if (File.Exists(video))
                    {
                        File.Delete(video);
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }
        }

        private static void MakeWatermark(Video video, string originalVideo, string watermarkVideo)
        {
            Process ffmpegProcess = new Process();
            try
            {
                DeleteVideos(new[] {watermarkVideo});
                var ffmpeg = Path.Combine(AppDataPath, "ffmpeg.exe");
                var content = Path.Combine(AppDataPath, originalVideo);
                var output = Path.Combine(AppDataPath, watermarkVideo);
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
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
            }
            catch (Exception ex)
            {
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
                UpdateVideo(video, Progress.Downloaded, Status.Failed, ex.InnerException?.Message);
            }
        }

        private static void Concat(Video video, string watermarkVideo, string joinedVideo)
        {
            Process ffmpegProcess = new Process();
            try
            {
                DeleteVideos(new[] {joinedVideo});
                var ffmpeg = Path.Combine(AppDataPath, "ffmpeg.exe");
                var introStart = Path.Combine(AppDataPath, "IntroStart.mp4");
                var introEnd = Path.Combine(AppDataPath, "IntroEnd.mp4");
                var content = Path.Combine(AppDataPath, watermarkVideo);
                var output = Path.Combine(AppDataPath, joinedVideo);
                ProcessStartInfo ffmpeg_StartInfo = new ProcessStartInfo(ffmpeg, $" -i {introStart} -i {content} -i {introEnd} -filter_complex \"[0]scale=1280x720,setdar=16/9[a];[1]scale=1280x720,setdar=16/9[b];[2]scale=1280x720,setdar=16/9[c]; [a][0:a][b][1:a][c][2:a] concat=n=3:v=1:a=1 [v] [a1]\" -map \"[v]\" -map \"[a1]\" {output}");            
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
                UpdateVideo(video, Progress.Joined);
            }
            catch (Exception ex)
            {
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
                UpdateVideo(video, Progress.Downloaded, Status.Failed, ex.InnerException?.Message);
            }
        }

        //private static void UploadVideo(Video video, string originalVideo, string watermarkVideo, string joinedVideo)
        //{
        //    UpdateVideo(video, Progress.Uploading, Status.Handling);

        //    Account account = new Account(_cloudName, _apiKey, _apiSecret);

        //    Cloudinary cloudinary = new Cloudinary(account);

        //    var videoUploadParams = new VideoUploadParams()
        //    {
        //        File = new FileDescription(joinedVideo)
        //    };
        //    try
        //    {
        //        var result = cloudinary.Upload(videoUploadParams);
        //        if (result.Error == null)
        //        {
        //            video.ReuploadedLink = result.Uri.AbsoluteUri;
        //            UpdateVideo(video, Progress.Uploaded, Status.Completed);
        //            DeleteVideos(new[] {originalVideo, watermarkVideo, joinedVideo});
        //        }
        //        else
        //        {
        //            UpdateVideo(video, Progress.Joined, Status.Failed, result.Error.Message);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var message = string.IsNullOrEmpty(ex.Message)
        //            ? "Uploading failed"
        //            : ex.Message;
        //        UpdateVideo(video, Progress.Joined, Status.Failed, message);
        //    }
        //}

        private static async Task UploadToYoutube(Video video, string originalVideo, string watermarkVideo, string joinedVideo)
        {
            UpdateVideo(video, Progress.Uploading, Status.Handling);
            try
            {
                var clientSecrets = new
                {
                    installed = new
                    {
                        client_id = _youtubeClientId,
                        client_secret = _youtubeClientSecret
                    }
                };
                File.WriteAllText(Path.Combine(AppDataPath, "client_secrets.json"), JsonConvert.SerializeObject(clientSecrets));

                UserCredential credential;
                using (var stream = new FileStream(Path.Combine(AppDataPath, "client_secrets.json"), FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        // This OAuth 2.0 access scope allows an application to upload files to the
                        // authenticated user's YouTube channel, but doesn't allow other types of access.
                        new[] { YouTubeService.Scope.YoutubeUpload },
                        _youtubeUser,
                        CancellationToken.None
                    );
                }

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
                });

                var youtubeVideo = new Google.Apis.YouTube.v3.Data.Video();
                youtubeVideo.Snippet = new VideoSnippet();
                youtubeVideo.Snippet.Title = video.VideoTitle;
                youtubeVideo.Snippet.Description = video.Description;
                youtubeVideo.Snippet.Tags = new string[] { "vui.us" };
                //youtubeVideo.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                youtubeVideo.Status = new VideoStatus();
                youtubeVideo.Status.PrivacyStatus = "public"; // or "private" or "public"
                var filePath = joinedVideo; // Replace with path to actual movie file.

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var videosInsertRequest = youtubeService.Videos.Insert(youtubeVideo, "snippet,status", fileStream, "video/*");
                    videosInsertRequest.ProgressChanged += progress =>
                    {
                        switch (progress.Status)
                        {
                            case UploadStatus.Uploading:
                                UpdateVideo(video, Progress.Uploading, Status.Handling, $"{progress.BytesSent} bytes sent.");
                                Console.WriteLine("{0} bytes sent.", progress.BytesSent);
                                break;

                            case UploadStatus.Failed:
                                UpdateVideo(video, Progress.Joined, Status.Failed, progress.Exception.Message);
                                Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                                break;
                        }
                    };
                    videosInsertRequest.ResponseReceived += response =>
                    {
                        video.ReuploadedLink = $"https://www.youtube.com/watch?v={response.Id}";
                        UpdateVideo(video, Progress.Uploaded, Status.Completed);
                        DeleteVideos(new[] { originalVideo, watermarkVideo, joinedVideo });
                        Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
                    };

                    await videosInsertRequest.UploadAsync();
                }
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrEmpty(ex.Message)
                    ? "Uploading failed"
                    : ex.Message;
                UpdateVideo(video, Progress.Joined, Status.Failed, message);
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