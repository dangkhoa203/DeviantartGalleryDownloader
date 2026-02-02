using ControlzEx.Standard;
using DeviantartDownloader.DTOs;
using DeviantartDownloader.Exceptions;
using DeviantartDownloader.Extension;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.ViewModels;
using HtmlAgilityPack;
using MahApps.Metro.Controls.Dialogs;
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

namespace DeviantartDownloader.Service {
    public class DeviantartService {
        private string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken {
            get; set;
        }
        public DateTime? KeyTime { get; set; } = null;
        private HttpClient _httpClient;
        public DeviantartService() {
            _httpClient = new HttpClient();
        }
        public bool isGettingKey {
            get;
            set;
        }=false;

        public async Task<bool> GetUserAccessToken(string code) {
            try {
                using HttpResponseMessage response = await _httpClient.GetAsync($"https://www.deviantart.com/oauth2/token?client_id=60309&client_secret=145f67512a11b4bd24380e1acafc8cf1&grant_type=authorization_code&redirect_uri=https://dangkhoa203.github.io/GetDeviantartCode&code={code}");
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response_Authenticate>(jsonResponse);
                AccessToken = result.access_token;
                RefreshToken = result.refresh_token;
                KeyTime = DateTime.Now.AddMinutes(50);
                return true;
            }
            catch {
                return false;
            }
        }
        public async Task<bool> GetAccessToken() {
            try {
                if((KeyTime == null || KeyTime < DateTime.Now) && !isGettingKey) {
                    isGettingKey = true;
                    string URL = RefreshToken != null ?
                        $"https://www.deviantart.com/oauth2/token?client_id=60309&client_secret=145f67512a11b4bd24380e1acafc8cf1&grant_type=refresh_token&refresh_token={RefreshToken}"
                          :
                        "https://www.deviantart.com/oauth2/token?client_id=60309&client_secret=145f67512a11b4bd24380e1acafc8cf1&grant_type=client_credentials";
                    using HttpResponseMessage response = await _httpClient.GetAsync(URL);

                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Response_Authenticate>(jsonResponse);
                    AccessToken = result.access_token;
                    RefreshToken = result.refresh_token;
                    KeyTime = DateTime.Now.AddMinutes(50);
                }
                return true;
            }
            catch {
                RefreshToken = null;
                return false;
            }
            finally {
                isGettingKey=false;
            }
        }
        public async Task<ICollection<GalleryFolder>> GetFolders(string userName, CancellationTokenSource cts, IDialogCoordinator dialogCoordinator, ViewModel view,AppSetting appSetting) {

            try {
                if(!await GetAccessToken())
                    throw new Exception("Fail authenticate");

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
                    if(hasMore && RefreshToken!=null) {
                        await Task.Delay(appSetting.UserKeySearchFolderWaitTime * 1000);
                    }
                }

                List<GalleryFolder> folders = contents
                                                .Select(o => new GalleryFolder(o.folderid, o.name, o.size))
                                                .OrderBy(o => o.Name)
                                                .ToList();
                if(folders.Count > 0) {
                    await dialogCoordinator.ShowMessageAsync(view, "ALERT", "Search completed!", MessageDialogStyle.Affirmative);
                }
                return folders;
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    await dialogCoordinator.ShowMessageAsync(view, "ALERT", "Operation canceled!", MessageDialogStyle.Affirmative);
                }
                else {
                    await dialogCoordinator.ShowMessageAsync(view, "ERROR", "Timeout!", MessageDialogStyle.Affirmative);
                }
                return [];
            }
            catch(Exception ex) {
                await dialogCoordinator.ShowMessageAsync(view, "ERROR", ex.Message, MessageDialogStyle.Affirmative);
                return [];
            }
        }
        public async Task<ICollection<Deviant>> GetDeviants(string userName, string folderId, CancellationTokenSource cts, IDialogCoordinator dialogCoordinator, ViewModel view,AppSetting appSetting) {

            try {
                if(!await GetAccessToken())
                    throw new Exception("Fail authenticate");

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
                    if(hasMore && RefreshToken != null) {
                        await Task.Delay(appSetting.UserKeySearchDeviantWaitTime*1000);
                    }
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
                                    Downloadable = o.is_downloadable ?? false,
                                    Type = TypeValidation(o),
                                    PublishDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(o.published_time)).Date,
                                    ContentLocked = !(o.tier_access == null || o.tier_access == "unlocked")
                                })
                                .OrderBy(o => o.PublishDate)
                                .ToList();
                return deviants;
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    await dialogCoordinator.ShowMessageAsync(view, "ALERT", "Operation canceled!", MessageDialogStyle.Affirmative);
                }
                else {
                    await dialogCoordinator.ShowMessageAsync(view, "ERROR", "Timeout!", MessageDialogStyle.Affirmative);
                }
                return [];
            }
            catch(Exception ex) {
                await dialogCoordinator.ShowMessageAsync(view, "ERROR", ex.Message, MessageDialogStyle.Affirmative);
                return [];
            }
        }
        public async Task DownloadDeviant(DownloadableDeviant content, CancellationTokenSource cts, string destinationPath,AppSetting appSetting, int literatureCount = 2) {
            try {
                await GetAccessToken();
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
                        if(content.Deviant.Downloadable) {
                            string request = $"https://www.deviantart.com/api/v1/oauth2/deviation/download/{content.Deviant.Id}?access_token={AccessToken}";
                            using HttpResponseMessage getDownloadresponse = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                            if(getDownloadresponse.StatusCode == HttpStatusCode.TooManyRequests) {
                                throw new RateLimitException();
                            }
                            var jsonResponse = await getDownloadresponse.Content.ReadAsStringAsync();
                            var downloadContent = JsonSerializer.Deserialize<Response_GetDownloadContent>(jsonResponse);

                            if(downloadContent.error != null) {
                                throw new Exception(downloadContent.error_description);
                            }
                            FileType imgType = GetFileType(content.Deviant.Type, downloadContent.filename);
                            await Task.Delay(appSetting.UserKeyDownloadDeviantWaitTime * 1000);
                            using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, $"[{content.Deviant.PublishDate.Date.ToString("yyyy-MM-dd")}] {GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username} - {content.Deviant.Url.Substring(content.Deviant.Url.Length - 9)}.{imgType.ToString()}"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                                await _httpClient.DownloadAsync(downloadContent.src, file, Speed, Progress, cts.Token);
                            }

                            content.Status = DownloadStatus.Completed;
                            content.Percent = 100;
                        }
                        else {
                            FileType imgType = GetFileType(content.Deviant.Type, content.Deviant.Content.Src);
                            if(imgType == FileType.unknown) {
                                throw new Exception("Unknow File Type");
                            }

                            using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, $"[{content.Deviant.PublishDate.Date.ToString("yyyy-MM-dd")}] {GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username} - {content.Deviant.Url.Substring(content.Deviant.Url.Length - 9)}.{imgType.ToString()}"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                                await _httpClient.DownloadAsync(content.Deviant.Content.Src, file, Speed, Progress, cts.Token);
                            }
                        }
                        content.Percent = 100;
                        content.Status = DownloadStatus.Completed;
                        break;

                    case DeviantType.Video:
                        var video = content.Deviant.Video.OrderByDescending(o => o.FileSize).First();
                        FileType videoType = GetFileType(content.Deviant.Type,video.Src);
                        if(videoType == FileType.unknown) {
                            throw new Exception("Unknow File Type");
                        }

                        using(var file = new FileStream(Path.Combine(destinationPath, content.Deviant.Author.Username, $"[{content.Deviant.PublishDate.Date.ToString("yyyy-MM-dd")}] {GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username} - {content.Deviant.Url.Substring(content.Deviant.Url.Length - 9)}.{videoType.ToString()}"), FileMode.Create, FileAccess.Write, FileShare.None)) {
                            await _httpClient.DownloadAsync(video.Src, file, Speed, Progress, cts.Token);
                        }

                        content.Status = DownloadStatus.Completed;
                        content.Percent = 100;
                        break;

                    case DeviantType.Literature:
                        var handler = new SocketsHttpHandler {
                            UseCookies = false
                        };

                        IProgress<float> progress = Progress;
                        using(var httpClient = new HttpClient(handler)) {
                            var request = new HttpRequestMessage(HttpMethod.Get, content.Deviant.Url);
                            request.Headers.UserAgent.ParseAdd(_userAgent[literatureCount % 10]);
                            if(appSetting.HeaderString != "") {
                                request.Headers.Add("Cookie", appSetting.HeaderString);
                            }
                            progress.Report(0.25f);

                            using(var response = await httpClient.SendAsync(request, cts.Token)) {
                                if(response.StatusCode == HttpStatusCode.Forbidden) {
                                    content.DownloadSpeed = "";
                                    throw new RateLimitException();
                                }
                                progress.Report(0.5f);

                                string htmlContent = await response.Content.ReadAsStringAsync(cts.Token);
                                var htmlDoc = new HtmlDocument();
                                htmlDoc.LoadHtml(htmlContent);

                                var literatureText = htmlDoc.DocumentNode.SelectNodes("//section").ToList();
                                progress.Report(0.75f);

                                string filePath = Path.Combine(destinationPath, content.Deviant.Author.Username, $"[{content.Deviant.PublishDate.Date.ToString("yyyy-MM-dd")}] {GetLegalFileName(content.Deviant.Title)} by {content.Deviant.Author.Username} - {content.Deviant.Url.Substring(content.Deviant.Url.Length - 9)}.html");
                                HtmlNode textContent = literatureText[1].InnerText.Contains("Badge Awards") ? literatureText[2] : literatureText[1];
                                textContent.RemoveChild(textContent.ChildNodes[0], false);
                                await File.WriteAllTextAsync(filePath, CreateHTMLFile(content.Deviant.Title, textContent, appSetting), cts.Token);
                                progress.Report(1);

                                content.Status = DownloadStatus.Completed;
                            }
                        }
                        break;
                }
            }
            catch(TaskCanceledException ex) {
                if(ex.CancellationToken == cts.Token) {
                    content.Status = DownloadStatus.Fail;
                }
                else {
                    content.Status = DownloadStatus.Fail;
                }
            }catch(RateLimitException ex) {
                content.Status = DownloadStatus.Rate_Limited;
            }
            catch(Exception ex) {
                content.Status = DownloadStatus.Fail;
            }
            finally {
                content.DownloadSpeed = "";
            }

        }
        public async Task GetDescriptions(List<DownloadableDeviant> deviants,CancellationTokenSource cts,string destinationPath, AppSetting appSetting,ProgressDialogController progressDialogController=null) {
            try {
                if(!await GetAccessToken())
                    throw new Exception("Fail authenticate");

                progressDialogController.Canceled += (sender, e) => {
                    cts.Cancel();
                };
                progressDialogController.SetMessage("Starting...");
                await Task.Delay(2000);

                int count = (int)Math.Floor(deviants.Count() / (decimal)50) + 1;
                int finishCount = 0;
                List<Content_MetaDataAPI> metaDatas = [];
                for(int i = 0; i < count; i++) {
                    List<DownloadableDeviant> items = deviants.Skip(i * 50).Take(50).ToList();
                    progressDialogController.SetMessage("Getting deviation description...");
                    StringBuilder Query = new StringBuilder();
                    foreach(DownloadableDeviant deviant in items) {
                        Query.Append($"deviationids%5B%5D={deviant.Deviant.Id}&");
                    }
                    string metaDataRequest = $"https://www.deviantart.com/api/v1/oauth2/deviation/metadata?{Query.ToString()}access_token={AccessToken}";
                    using HttpResponseMessage getMetaDataresponse = await _httpClient.GetAsync(metaDataRequest, HttpCompletionOption.ResponseContentRead, cts.Token);
                    if(getMetaDataresponse.StatusCode == HttpStatusCode.TooManyRequests) {
                        throw new RateLimitException();
                    }
                    else {
                        var metaDataJSONResponse = await getMetaDataresponse.Content.ReadAsStringAsync();
                        var metadata = JsonSerializer.Deserialize<Response_SearchMetaData>(metaDataJSONResponse);
                        if(metadata.error != null) {
                            throw new Exception(metadata.error_description);
                        }
                        metaDatas.AddRange(metadata.metadata.ToList());
                    }
                }
                if(appSetting.IgnoreEmptyDescription) {
                    metaDatas = metaDatas.Where(d => d.description.Length > 7).ToList();
                }
                progressDialogController.SetMessage("Saving deviation description...");
                foreach(var data in metaDatas) {
                    var deviant = deviants.FirstOrDefault(d => d.Deviant.Id == data.deviationid);
                    if(!Directory.Exists(Path.Combine(destinationPath, deviant.Deviant.Author.Username))) {
                        Directory.CreateDirectory(Path.Combine(destinationPath, deviant.Deviant.Author.Username));
                    }
                    if(deviant != null) {
                        string filePath = Path.Combine(destinationPath, deviant.Deviant.Author.Username, $"[{deviant.Deviant.PublishDate.Date.ToString("yyyy-MM-dd")}] {GetLegalFileName(deviant.Deviant.Title)} by {deviant.Deviant.Author.Username} - {deviant.Deviant.Url.Substring(deviant.Deviant.Url.Length - 9)} (description).html");
                        await File.WriteAllTextAsync(filePath, CreateDescriptionHTMLFile(deviant.Deviant.Title, data.description, deviant.Deviant.Url, appSetting, deviant.Deviant.Content?.Src, deviant.Deviant.Type), cts.Token);
                    }
                    finishCount++;
                    progressDialogController.SetProgress(Math.Round(((double)finishCount / metaDatas.Count) * 100, 1));
                }
            }
            catch(OperationCanceledException ex) {
                progressDialogController.SetMessage("Canceling...");
                await Task.Delay(2000);
            }catch(Exception ex) {
                progressDialogController.SetMessage("Something went wrong...");
                await Task.Delay(2000);
            }
            finally {
                if(progressDialogController.IsOpen)
                    await progressDialogController.CloseAsync();
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
        private FileType GetFileType(DeviantType type,string url) {
            switch(type) {
                case DeviantType.Art:
                    return ReturnArtFileType(url);
                case DeviantType.Video:
                    return ReturnVideoFileType(url);
                default:
                    return FileType.unknown;
            }
        }
        private FileType ReturnArtFileType(string url) {
            List<FileType> validFileType = [
                FileType.jpg,
                FileType.png,
                FileType.gif,
                FileType.swf,
                FileType.fla,
                FileType.pdf,
                FileType.psd,
                FileType.ico,
                FileType.rar,
                FileType.zip,
                FileType.sevenzip,
                ];

            foreach(FileType item in validFileType) {
                if(url.Contains($".{item.ToString()}")) {
                    return item;
                }
                if(item == FileType.sevenzip) {
                    if(url.Contains($".7z")) {
                        return item;
                    }
                }
            }
          
            return FileType.jpg;
        }
        private FileType ReturnVideoFileType(string url) {
            List<FileType> validFileType = [
                FileType.mp3,
                FileType.mp4,
                FileType.mov,
                FileType.webm,
                FileType.wmv,
                FileType.avi,
                FileType.flv,
                ];

            foreach(FileType item in validFileType) {
                if(url.Contains($".{item.ToString()}")) {
                    return item;
                }
            }
            return FileType.mp4;
        }
        private string CreateHTMLFile(string title, HtmlNode node,AppSetting appSetting) {
            var figureCheck = node.SelectNodes(".//figure")?.ToList();
            if(figureCheck != null) {
                foreach(var f in figureCheck) {
                    f.AddClass("quoTVs");
                    var section = f.SelectSingleNode(".//section");
                    if(section != null) {
                        section.RemoveChild(section.FirstChild, false);
                    }
                }
            }

            return $@"
                    <html>
                    <head>
                        <title>{title}</title>
                          <style>
                              *::selection{{
                                 background-color: #00c787;
                              }}

                              body {{
                                 background-color: #d2decc;
                                 font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                              }}

                              .title {{
                                 text-align: center;
                                 letter-spacing: 0.2em;
                                 font-size: 2.5em;
                              }}

                              .content {{
                                 padding: 10px 25px;
                                 font-size: 1.5em;
                              }}

                              .quoTVs {{
                                 border: 1px solid #a8b2a7;
                                 justify-self: center;
                                 padding:50px 30px;
                                 display: flex;
                                 justify-content: center;
                                 background-color: #dde6d9;
								 font-weight:450;
								 
                                 a {{
                                    font-size: 1em;
                                    color: rgb(0, 0, 0);
                                    text-decoration: none;
                                 }}
                              }}

                             {(appSetting.UseCustomStyle ? appSetting.CustomStyle : "")}
						 </style>
                    </head>
                    <body>
                       <h1 class='title'>{title}</h1>
                       <hr/>
                       <div class='content'>
                            {node.OuterHtml} 
                       </div> 
                    </body>
                    </html>";
        }
        private string CreateDescriptionHTMLFile(string title, string description,string url,AppSetting appSetting, string src="", DeviantType type=DeviantType.Literature) {
            string value = description.Replace("https://www.deviantart.com/users/outgoing?", "");
            value = value.Replace("<a ", "<a target='_blank '");
            return $@"
                    <html>
                    <head>
                        <title>{title}</title>
                          <style>
                              *::selection{{
                                 background-color: #00c787;
                              }}

                              body {{
                                 background-color: #d2decc;
                                 font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                              }}

                              .title {{
                                 text-align: center;
                                 letter-spacing: 0.2em;
                                 font-size: 2.5em;
                              }}

                              .description-content {{
                                 padding: 10px 25px;
                                 font-size: 1.5em;
                                 display:flex;
                                 flex-direction:column;
                              }}

                              .description-image{{
                                display:flex;
                                padding: 0px 5%;
                                justify-content:center;
                                padding-bottom: 50px;
                                border-bottom:2px solid #2b3635;
                                img{{
                                    max-width:100%;
                                }}
                              }}

                              {(appSetting.UseCustomStyle ? appSetting.CustomStyle : "")}
						 </style>
                    </head>
                    <body>
                       <h1 class='title'>{title}</h1>
                       <hr/>
                       <div class='description-content'>
                            {(type==DeviantType.Art ? 
                                $@"
                                   <a href='{url}' target='_blank' class='description-image'>
                                        <img src='{src}' alt='{title}'/>
                                   </a>"
                            :
                            "")}
                            <div class='description-text'> 
                                {value}
                            </div>
                       </div> 
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
        private List<string> _userAgent = [
            "Pinterestbot",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36 OPR/109.0.0.0",
            "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36 Edg/110.0.1587.50",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 11_12; rv:110.0) Gecko/20110101 Firefox/110.0",
            "Mozilla/5.0 (Linux; Android 10; LM-Q730) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 12; M2101K7AG) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36",
            "Mozilla/5.0 (Linux; Android 10; CPH1931) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36"
        ];
    }
}
