using DeviantartDownloader.Models;
using DeviantartDownloader.DTOs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using DeviantartDownloader.Models.Enum;

namespace DeviantartDownloader.Service {
    public class DeviantartService {
        public string AccessKey { get; set; } = string.Empty;
        public DateTime? KeyTime { get; set; } = null;
        private HttpClient _httpClient;
        public DeviantartService() {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://www.deviantart.com");
        }
        public DeviantType TypeValidation(DeviantAPIContent result) {
            if (result.video != null) {
                return DeviantType.Video;
            }
            else if (result.excerpt != null) {
                return DeviantType.Literature;
            }
            else {
                return DeviantType.Art;
            }
        }
        public async Task<bool> Authenticate() {
            try {
                if (KeyTime == null || KeyTime < DateTime.Now) {
                    using HttpResponseMessage response = await _httpClient.GetAsync("oauth2/token?client_id=58502&client_secret=54daa2749cd91ed21c28850b0a3be0a8&grant_type=client_credentials");
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var key = JsonSerializer.Deserialize<AuthenticateResponse>(jsonResponse);
                    AccessKey = key.access_token;
                    KeyTime = DateTime.Now.AddHours(1);
                }
                return true;
            }
            catch {
                return false;
            }
        }
        public async Task<ICollection<Folder>> GetFolders(string userName, CancellationTokenSource cts) {

            try {
                if (KeyTime == null || KeyTime < DateTime.Now) {
                    if (!await Authenticate())
                        throw new Exception("Fail autho");
                }
                int? offSet = 0;
                bool hasMore = true;
                List<FolderAPIContent> contents = [];
                while (hasMore) {
                    string request = $"api/v1/oauth2/gallery/folders?username={userName}&limit=50&offset={offSet}&calculate_size=true&filter_empty_folder=true&access_token={AccessKey}";
                    using HttpResponseMessage response = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var key = JsonSerializer.Deserialize<searchFolderResponse>(jsonResponse);
                    if (key.error != null) {
                        MessageBox.Show(key.error_description, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        throw new Exception(key.error_description);
                    }
                    hasMore = key.has_more.Value;
                    offSet = key.has_more.Value ? key.next_offset : 0;
                    contents.AddRange(key.results ?? []);
                }
                List<Folder> folders = contents.Select(o => new Folder() {
                    Id = o.folderid,
                    Name = o.name,
                    Size = o.size
                }).OrderBy(o => o.Name).ToList();
                return folders;
            }
            catch (TaskCanceledException ex) {
                if (ex.CancellationToken == cts.Token) {
                    MessageBox.Show("Opperation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else {
                    MessageBox.Show("Timeout", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return [];
            }
            catch (Exception ex) {
                return [];
            }
        }
        public async Task<ICollection<Deviant>> GetDeviants(string userName, string folderId, CancellationTokenSource cts) {

            try {
                if (KeyTime == null || KeyTime < DateTime.Now) {
                    if (!await Authenticate())
                        throw new Exception("Fail autho");
                }
                int? offSet = 0;
                bool hasMore = true;
                List<DeviantAPIContent> contents = [];
                while (hasMore) {
                    string request = $"api/v1/oauth2/gallery/{folderId}?username={userName}&offset={offSet}&mode=newest&limit=24&access_token={AccessKey}";
                    using HttpResponseMessage response = await _httpClient.GetAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var key = JsonSerializer.Deserialize<SearchDeviantResponse>(jsonResponse);
                    if (key.error != null) {
                        MessageBox.Show(key.error_description, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        throw new Exception(key.error_description);
                    }
                    hasMore = key.has_more.Value;
                    offSet = key.has_more.Value ? key.next_offset : 0;
                    contents.AddRange(key.results ?? []);
                }
                List<Deviant> deviants = contents.Select(o => new Deviant() {
                                Author = new Author() {
                                    Id = o.author.userid,
                                    Username = o.author.username
                                },
                                Content = o.content,
                                Deviationid = o.deviationid,
                                Title = o.title,
                                Url = o.url,
                                Video = o.video,
                                Type = TypeValidation(o),
                            }
                )
                .ToList();
                return deviants;
            }
            catch (TaskCanceledException ex) {
                if (ex.CancellationToken == cts.Token) {
                    MessageBox.Show("Opperation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else {
                    MessageBox.Show("Timeout", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return [];
            }
            catch (Exception ex) {
                return [];
            }
        }
    }
}
