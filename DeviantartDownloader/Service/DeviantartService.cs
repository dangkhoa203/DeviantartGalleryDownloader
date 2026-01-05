using DeviantartDownloader.DTOs;
using DeviantartDownloader.Extension;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DeviantartDownloader.Service {
    public class DeviantartService {
        private string AccessToken { get; set; } = string.Empty;
        private DateTime? KeyTime { get; set; } = null;
        private HttpClient _httpClient;
        public DeviantartService() {
            _httpClient = new HttpClient();
        }

        public async Task<bool> GetAccessToken() {
            try {
                if(KeyTime == null || KeyTime < DateTime.Now) {
                    using HttpResponseMessage response = await _httpClient.GetAsync("https://www.deviantart.com/oauth2/token?client_id=58502&client_secret=54daa2749cd91ed21c28850b0a3be0a8&grant_type=client_credentials");
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Response_Authenticate>(jsonResponse);
                    AccessToken = result.access_token;
                    KeyTime = DateTime.Now.AddHours(1);
                }
                return true;
            }
            catch {
                return false;
            }
        }
        public async Task<ICollection<GalleryFolder>> GetFolders(string userName, CancellationTokenSource cts) {

            try {
                if(!await GetAccessToken())
                    throw new Exception("Fail autho");

                int? offSet = 0;
                bool hasMore = true;

                List<Content_FolderAPI> contents = [];
                while(hasMore) {
                    string request = $"https://www.deviantart.com/api/v1/oauth2/gallery/folders?username={userName}&limit=50&offset={offSet}&calculate_size=true&filter_empty_folder=true&access_token={AccessToken}";
                    using HttpResponseMessage response = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<Response_SearchFolder>(jsonResponse);
                    if(result.error != null) {
                        throw new Exception(result.error_description);
                    }

                    hasMore = result.has_more ?? false;
                    offSet = result.next_offset;
                    contents.AddRange(result.results ?? []);
                }

                List<GalleryFolder> folders = contents
                                                .Select(o => new GalleryFolder(o.folderid, o.name, o.size))
                                                .OrderBy(o => o.Name)
                                                .ToList();
                if(folders.Count > 0) {
                    MessageBox.Show("Search completed", "Action completed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                return folders;
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    MessageBox.Show("Operation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else {
                    MessageBox.Show("Timeout", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return [];
            }
            catch(Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
        }
        public async Task<ICollection<Deviant>> GetDeviants(string userName, string folderId, CancellationTokenSource cts) {

            try {
                if(!await GetAccessToken())
                    throw new Exception("Fail autho");

                int? offSet = 0;
                bool hasMore = true;
                List<Content_DeviantAPI> contents = [];

                while(hasMore) {
                    string request = $"https://www.deviantart.com/api/v1/oauth2/gallery/{folderId}?username={userName}&offset={offSet}&mode=newest&mature_content=true&limit=24&access_token={AccessToken}";
                    using HttpResponseMessage response = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Response_SearchDeviant>(jsonResponse);
                    if(result.error != null) {

                        throw new Exception(result.error_description);
                    }
                    hasMore = result.has_more ?? false;
                    offSet = result.next_offset;
                    contents.AddRange(result.results ?? []);
                }

                List<Deviant> deviants = contents
                                .Select(o => new Deviant() {
                                    Author = new Author() {
                                        Id = o.author.userid,
                                        Username = o.author.username
                                    },
                                    Content = o.content != null ? new MediaContent() {
                                        Src = o.content.src,
                                        FileSize = o.content.filesize
                                    }
                                                                        : null,
                                    Id = o.deviationid,
                                    Title = o.title,
                                    Url = o.url,
                                    Video = o.videos != null ? o.videos
                                                                         .Select(o => new MediaContent() {
                                                                             Src = o.src,
                                                                             Quality = o.quality,
                                                                             FileSize = o.filesize
                                                                         })
                                                                         .ToList()
                                                                     : null,
                                    Donwloadable = o.is_downloadable ?? false,
                                    Type = TypeValidation(o)
                                })
                                .ToList();
                return deviants;
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    MessageBox.Show("Operation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else {
                    MessageBox.Show("Timeout", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return [];
            }
            catch(Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
        }
        public async Task DonwloadDeviant(DownloadableDeviant content, CancellationTokenSource cts, string destinationPath, string HeaderString = "") {
            try {
                content.Percent = 0;
                content.Status = DownloadStatus.Waiting;
                var Progress = new Progress<float>(percent => {
                    content.Percent = percent * 100;
                });
                var Speed = new Progress<string>(speed => {
                    content.DownloadSpeed = speed;
                });

                content.Status = DownloadStatus.Downloading;
                if(!Directory.Exists(Path.Combine(destinationPath, content.Deviant.Author.Username))) {
                    Directory.CreateDirectory(Path.Combine(destinationPath, content.Deviant.Author.Username));
                }

                switch(content.Deviant.Type) {
                    case DeviantType.Art:
                        if(content.Deviant.Donwloadable) {
                            string request = $"https://www.deviantart.com/api/v1/oauth2/deviation/download/{content.Deviant.Id}?access_token={AccessToken}";
                            using HttpResponseMessage getDownloadresponse = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                            var jsonResponse = await getDownloadresponse.Content.ReadAsStringAsync();
                            var downloadContent = JsonSerializer.Deserialize<Response_GetDonwloadContent>(jsonResponse);

                            if(downloadContent.error != null) {
                                MessageBox.Show(downloadContent.error_description, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                throw new Exception(downloadContent.error_description);
                            }

                            using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, downloadContent.filename), FileMode.Create, FileAccess.Write, FileShare.None)) {
                                await _httpClient.DownloadAsync(downloadContent.src, file, Speed, Progress, cts.Token);
                            }

                            content.Status = DownloadStatus.Completed;
                        }
                        else {
                            FileType imgType = GetFileType(content.Deviant.Content.Src);
                            if(imgType == FileType.unknown) {
                                throw new Exception("Unknow File Type");
                            }

                            using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, $"{GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username}.{imgType.ToString()}"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                                await _httpClient.DownloadAsync(content.Deviant.Content.Src, file, Speed, Progress, cts.Token);
                            }

                            content.Status = DownloadStatus.Completed;
                        }
                        break;

                    case DeviantType.Video:
                        var video = content.Deviant.Video.OrderByDescending(o => o.FileSize).First();
                        FileType videoType = GetFileType(video.Src);
                        if(videoType == FileType.unknown) {
                            throw new Exception("Unknow File Type");
                        }

                        using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, $"{GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username}.{videoType.ToString()}"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                            await _httpClient.DownloadAsync(video.Src, file, Speed, Progress, cts.Token);
                        }

                        content.Status = DownloadStatus.Completed;
                        break;

                    case DeviantType.Literature:
                        var handler = new SocketsHttpHandler {
                            UseCookies = false
                        };

                        IProgress<float> progress = Progress;
                        using(var httpClient = new HttpClient(handler)) {
                            var request = new HttpRequestMessage(HttpMethod.Get, content.Deviant.Url);
                            if(HeaderString != "") {
                                request.Headers.Add("Cookie", HeaderString);
                            }
                            progress.Report(0.25f);

                            

                            using(var response = await httpClient.SendAsync(request, cts.Token)) {
                                await Task.Delay(TimeSpan.FromSeconds(1.5));
                                response.EnsureSuccessStatusCode();
                                progress.Report(0.5f);

                                string htmlContent = await response.Content.ReadAsStringAsync(cts.Token);
                                var htmlDoc = new HtmlDocument();
                                htmlDoc.LoadHtml(htmlContent);
                                

                                var node = htmlDoc.DocumentNode.SelectNodes("//section").ToList();
                                progress.Report(0.75f);

                                string filePath = Path.Combine(destinationPath, content.Deviant.Author.Username, $"{GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username}.html");
                                HtmlNode textContent = node[1].InnerText.Contains("Badge Awards") ? node[2] : node[1];
                                textContent.RemoveChild(textContent.ChildNodes[0], false);
                                await File.WriteAllTextAsync(filePath, CreateHTMLFile(content.Deviant.Title,textContent.OuterHtml), cts.Token);
                                progress.Report(1);

                                content.Status = DownloadStatus.Completed;
                            }
                            
                            
                        }
                        break;
                }
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    content.Status = DownloadStatus.Canceled;
                }
                else {
                    content.Status = DownloadStatus.Fail;
                }
            }
            catch(Exception ex) {
                content.Status = DownloadStatus.Fail;
            }
            finally {
                content.DownloadSpeed = "";
            }

        }
        private DeviantType TypeValidation(Content_DeviantAPI result) {
            if(result.videos != null) {
                return DeviantType.Video;
            }
            else if(result.excerpt != null) {
                return DeviantType.Literature;
            }
            else {
                return DeviantType.Art;
            }
        }
        private FileType GetFileType(string url) {
            if(url.Contains(".jpg")) {
                return FileType.jpg;
            }
            else if(url.Contains(".png")) {
                return FileType.png;
            }
            else if(url.Contains(".gif")) {
                return FileType.gif;
            }
            else if(url.Contains(".mp4")) {
                return FileType.mp4;
            }
            else if(url.Contains(".mp3")) {
                return FileType.mp3;
            }
            return FileType.unknown;
        }
        private string CreateHTMLFile(string title,string outerHTML) {
            return $@"
                    <html>
                    <head>
                        <title>{title}</title>
                          <style>
                              body {{
                                 background-color: #d2decc;
                              }}

                              p {{
                                 font-size: 1.5em;
                                 font-weight: 450;
                                 font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                              }}

                              p::selection {{
                                 background-color: #00c787;
                              }}

                              .quoTVs {{
                                 border: 1px solid #a8b2a7;
                                 justify-self: center;
                                 padding: 20px;
                                 display: flex;
                                 justify-content: center;
                                 background-color: #dde6d9;

                                 a {{
                                    font-size: 1.2em;
                                    color: rgb(0, 0, 0);
                                    text-decoration: none;
                                 }}
                              }}

                              .title {{
                                 text-align: center;
                                 letter-spacing: 0.2em;
                              }}

                              .content {{
                                 padding: 10px 25px;
                              }}
						 </style>
                    </head>
                    <body>
                       <h1 class={"'title'"}>{title}</h1>
                       <hr/>
                       <div class={"'content'"}>{outerHTML} </div> 
                       
                    </body>
                    </html>";
        }
        private List<char> charsToReplace = ['*', '<', '>', '?', '|', '/', '\\', '"', ':'];
        private string GetLegalFileName(string title) {
            var legalFileName = title.Trim();
            foreach(char c in charsToReplace) {
                legalFileName = legalFileName.Replace(c, ' ');
            }
            return legalFileName;
        }
    }
}
