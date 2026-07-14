using com.strava.v3.api.Activities;
using com.strava.v3.api.Clients;
using com.strava.v3.api.Upload;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Serilog;
using StravaTools.Utilities.Browser;
using StravaTools.Utilities.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StravaTools.Helpers
{
    public static class StravaHelper
    {
        static readonly TimeSpan offset_before_default = TimeSpan.FromMinutes(10);
        static readonly TimeSpan offset_after_default = TimeSpan.FromMinutes(10);

        static readonly string baseUrl = "https://www.strava.com";
        static readonly string baseAPIUrl = "https://www.strava.com/api/v3";

        const string strava_tokens_name = "strava_tokens.json";
        const string strava_cookies_name = "strava_cookies.json";
        public static FileInfo tokens_fi = null;
        public static FileInfo cookies_fi = null;

        public static DirectoryInfo original_backup_dir = null;
        public static DirectoryInfo modified_backup_dir = null;

        public static CookieParam[] strava_cookies = null;

        public static string ClientID;
        public static string ClientSecret;
        public static Uri RedirectLocalhostURI = new Uri("https://localhost/exchange_token");

        public static StravaAccessTokens Tokens;

        public static void InitializeFilesAndDirectories(DirectoryInfo localappdata_dir)
        {
            cookies_fi = ConfigurationUtilities.GetLocalAppDataFile(localappdata_dir, strava_cookies_name);
            cookies_fi.Refresh();
            tokens_fi = ConfigurationUtilities.GetLocalAppDataFile(localappdata_dir, strava_tokens_name);
            tokens_fi.Refresh();

            DirectoryInfo backup_dir = ConfigurationUtilities.GetOrCreateSubdir(localappdata_dir, "Backups");
            original_backup_dir = ConfigurationUtilities.GetOrCreateSubdir(backup_dir, "Original");
            modified_backup_dir = ConfigurationUtilities.GetOrCreateSubdir(backup_dir, "Modified");
        }

        public static async Task InitializeAppCredentials(string client_id, string client_secret, CancellationToken token = default)
        {
            ClientID = client_id;
            ClientSecret = client_secret;

            strava_cookies = LoadLocalCookies(cookies_fi);
            if (strava_cookies != null
                && ((strava_cookies.Length > 0
                && ShouldUpdateForExpiry(strava_cookies)) || strava_cookies.Length == 0))
            {
                CookieParam[] latest_cookies = await CaptureNewCookies(strava_cookies, token);
                UpdateLocalCookies(latest_cookies, ref strava_cookies, cookies_fi);
            }
            Tokens = LoadLocalTokens(tokens_fi);
        }

        public static DownloadedFit GetDownloadedFitPair(FileInfo original)
        {
            return new DownloadedFit()
            {
                Backup = original,
                Main = new FileInfo(Path.Combine(modified_backup_dir.FullName, original.Name))
            };
        }

        public static async Task<bool> Authenticate(CancellationTokenSource cancellation_source)
        {
            string new_tokens_json = "";

            if (!tokens_fi.Exists)
            {
                Log.Information("Tokens file does not exist. Will create...");
                try
                {
                    string authorization_code =
                        await RequestAuthorizationCode(ClientID, RedirectLocalhostURI, strava_cookies, cancellation_source.Token);
                    if (authorization_code == "error")
                    {
                        cancellation_source.Cancel();
                    }
                    cancellation_source.Token.ThrowIfCancellationRequested();
                    new_tokens_json = await RequestTokens(ClientID, ClientSecret, authorization_code, cancellation_source.Token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
            else
            {
                cancellation_source.Token.ThrowIfCancellationRequested();
                try
                {
                    if (Tokens.ExpiryDate < DateTime.UtcNow)
                    {//Refresh tokens
                        Log.Information($"Tokens expired. Will refresh");
                        cancellation_source.Token.ThrowIfCancellationRequested();
                        new_tokens_json = await RefreshTokens(Tokens, ClientID, ClientSecret
                            , cancellation_source.Token);
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (OperationCanceledException)
                {

                }
            }
            //Either after creating a new token file or refreshing them, we come here. Here on out, we write/update the file on disk
            if (!string.IsNullOrEmpty(new_tokens_json))
            {
                StravaAccessTokens new_tokens = UpdateTokens(tokens_fi, new_tokens_json, update_io: true);
                if (IsNotEmpty(new_tokens))
                {
                    Log.Information($"Updated cached tokens");
                    Tokens = new_tokens;
                    return true;
                }
            }

            Log.Error("Could not request or refresh tokens from Strava server. Exiting...");
            return false;
        }

        public static async Task<ActivitySummary> FindActivity(this StravaClient client, DateTime time_created, TimeSpan offset_before = default, TimeSpan offset_after = default)
        {
            DateTime after = time_created.ToUniversalTime();
            after = after.Subtract((offset_before != TimeSpan.Zero) ? offset_before : offset_before_default);
            DateTime before = time_created.ToUniversalTime();
            before = before.Add((offset_after != TimeSpan.Zero) ? offset_after : offset_after_default);
            try
            {
                List<ActivitySummary> activities = await client.Activities.GetActivitiesAsync(after: after, before: before);
                if (activities.Count == 1)
                {
                    return activities[0];
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        public struct StravaAccessTokens
        {
            public StravaAccessTokens()
            {
            }

            public StravaAccessTokens(string at, string rt, long exp_tsp)
            {
                AccessToken = at;
                RefreshToken = rt;
                ExpiryDate = CommonUtilities.GetTimestamp(exp_tsp);
            }

            public string AccessToken { get; set; } = "";
            public string RefreshToken { get; set; } = "";
            public DateTime ExpiryDate { get; set; } = DateTime.MinValue;
        }

        public static StravaAccessTokens LoadLocalTokens(FileInfo _tokens_fi)
        {
            if (_tokens_fi.Exists)
            {
                string tokens_json = File.ReadAllText(_tokens_fi.FullName);
                JObject j = JObject.Parse(tokens_json);
                StravaAccessTokens tokens = new StravaAccessTokens()
                {
                    AccessToken = j["access_token"].Value<string>(),
                    RefreshToken = j["refresh_token"].Value<string>(),
                    ExpiryDate = CommonUtilities.GetTimestamp(j["expires_at"].Value<long>())
                };
                return tokens;
            }

            return new StravaAccessTokens();
        }

        public static StravaAccessTokens GetLatestTokens(string json)
        {
            JObject j = JObject.Parse(json);
            StravaAccessTokens tokens = new StravaAccessTokens()
            {
                AccessToken = j["access_token"].Value<string>(),
                RefreshToken = j["refresh_token"].Value<string>(),
                ExpiryDate = CommonUtilities.GetTimestamp(j["expires_at"].Value<long>())
            };
            return tokens;
        }

        public static StravaAccessTokens UpdateTokens(FileInfo _tokens_fi, string json, bool update_io = false)
        {
            JObject j = JObject.Parse(json);
            StravaAccessTokens new_tokens = new StravaAccessTokens()
            {
                AccessToken = j["access_token"].Value<string>(),
                RefreshToken = j["refresh_token"].Value<string>(),
                ExpiryDate = CommonUtilities.GetTimestamp(j["expires_at"].Value<long>())
            };
            if (update_io)
            {
                File.WriteAllText(tokens_fi.FullName, json);
            }
            return new_tokens;
        }

        public static Url ConstructAuthorizeUri(string client_id, Uri redirect_uri, params string[] scopes)
        {
            Url url = new Url(baseUrl);
            url.AppendPathSegments("oauth", "authorize");
            url.AppendQueryParam("client_id", client_id);
            url.AppendQueryParam("response_type", "code");
            url.AppendQueryParam("redirect_uri", redirect_uri.AbsoluteUri, isEncoded: false);
            url.AppendQueryParam("approval_prompt", "force");
            string scopes_param = string.Join(',', scopes);
            url.AppendQueryParam("scope", scopes_param, isEncoded: true);
            return url;
        }

        public static Url ConstructStravaUri(string endpoint, params KeyValuePair<string, string>[] query_params)
        {
            Url url = new Url(baseUrl);
            url.AppendPathSegment(endpoint);
            foreach (KeyValuePair<string, string> pair in query_params)
            {
                url.AppendQueryParam(pair.Key, pair.Value);
            }
            return url;
        }

        public static Url ConstructRelativeUri(string endpoint, params KeyValuePair<string, string>[] query_params)
        {
            Url url = new Url(endpoint);
            foreach (KeyValuePair<string, string> pair in query_params)
            {
                url.AppendQueryParam(pair.Key, pair.Value);
            }
            return url;
        }

        public static Uri ConstructStravav3Uri(params KeyValuePair<string, string>[] query_params)
        {
            Uri uri = new Uri(baseAPIUrl);
            foreach (KeyValuePair<string, string> pair in query_params)
            {
                uri.AppendQueryParam(pair.Key, pair.Value);
            }
            return uri;
        }

        public static CookieParam[] LoadLocalCookies(FileInfo _cookies_fi)
        {
            if (_cookies_fi.Exists)
            {
                string cookie_json = File.ReadAllText(_cookies_fi.FullName);
                return JsonConvert.DeserializeObject<CookieParam[]>(cookie_json);
            }
            return null;
        }

        private static bool ShouldUpdateForDifferenceOrExpiry(CookieParam[] prev, CookieParam[] next)
        {
            if (prev.Length != next.Length)
            {
                return true;
            }
            else
            {
                foreach (CookieParam param in prev)
                {
                    DateTime dt = CommonUtilities.GetTimestamp((long)param.Expires);
                    if (dt < DateTime.UtcNow)
                    {
                        return true;
                    }
                }
                foreach (CookieParam param in next)
                {
                    foreach (CookieParam p in prev)
                    {
                        if (p.Name == param.Name
                            && (p.Value != param.Value
                            || p.Expires < param.Expires))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool ShouldUpdateForExpiry(CookieParam[] cookies)
        {
            foreach (CookieParam param in cookies)
            {
                if (param.Expires == -1) continue;
                DateTime dt = CommonUtilities.GetTimestamp((long)param.Expires);
                if (dt < DateTime.UtcNow)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateLocalCookies(CookieParam[] latest, ref CookieParam[] cached, FileInfo _cookies_fi)
        {
            if (cached == null
                || ShouldUpdateForDifferenceOrExpiry(cached, latest))
            {
                string strava_cookies_json = JsonConvert.SerializeObject(latest);
                File.WriteAllText(_cookies_fi.FullName, strava_cookies_json);
                cached = latest;
            }
        }

        public static async Task<CookieParam[]> CaptureNewCookies(CookieParam[] cookie_params, CancellationToken token = default)
        {
            Log.Information("Getting latest Strava cookies");
            Url dashboard = ConstructStravaUri("dashboard");

            IBrowser browser = null;
            try
            {
                token.ThrowIfCancellationRequested();
                browser = await BrowserUtilities.LaunchOrConnect(headless: false, full_viewport: true);
                var pages = await browser.PagesAsync();
                var page = pages[0];

                bool location_found = false;

                //Backup
                page.Response += async (sender, response) =>
                {
                    //Url res_uri = new Url(response.Response.Url);
                    //if (res_uri.Authority.Contains("strava.com"))
                    if (response.Response.Url.Contains("strava.com/dashboard/feed")
                    && response.Response.Status == HttpStatusCode.OK)
                    {
                        location_found = true;
                    }
                };

                token.ThrowIfCancellationRequested();
                {//Grab authorization code
                    if (cookie_params != null)
                    {
                        await page.SetCookieAsync(cookie_params);
                    }

                    await page.GoToAsync(dashboard.ToString());

                    token.ThrowIfCancellationRequested();
                    while (!location_found)
                    {

                    }
                    //await page.WaitForNavigationAsync(new NavigationOptions()
                    //{
                    //    WaitUntil = new WaitUntilNavigation[]
                    //    {
                    //        WaitUntilNavigation.Networkidle2
                    //    }
                    //});
                    //await Task.Delay(5000);
                    token.ThrowIfCancellationRequested();

                    CookieParam[] captured_cookie_params = await page.GetCookiesAsync(
                    //new string[]
                    //{
                    //    "www.strava.com",
                    //    ".strava.com"
                    //}
                    );
                    return captured_cookie_params;
                }
            }
            catch (OperationCanceledException)
            {
                Log.Error("User cancelled.");
            }
            //catch (Exception ex)
            //{
            //    Log.Error(ex.ToString());
            //}
            finally
            {
                await BrowserUtilities.ShutdownBrowser(browser);
                Log.Information("Cleanup complete. Exiting.");
            }
            return null;
        }

        public static async Task<string> RequestAuthorizationCode(string client_id, Uri redirect_uri, CookieParam[] cookie_params, CancellationToken token = default)
        {
            Log.Information("Requesting Authorization Code from Strava Server");
            Url authorization_uri = ConstructAuthorizeUri(
                    client_id
                    , redirect_uri
                    , new string[]
                    {
                        "read_all",
                        "profile:read_all",
                        "profile:write",
                        "activity:read_all",
                        "activity:write"
                    }
                );
            string authorization_code = "";

            Log.Information("Loading locally accessible cookies for Strava.com");
            IBrowser browser = null;
            try
            {
                token.ThrowIfCancellationRequested();
                browser = await BrowserUtilities.LaunchOrConnect(headless: false, full_viewport: true);
                var pages = await browser.PagesAsync();
                var page = pages[0];

                //page.SetRequestInterceptionAsync(true);

                token.ThrowIfCancellationRequested();
                {//Grab authorization code

                    page.Request += async (sender, request) =>
                    {
                        try
                        {
                            Url res_uri = new Url(request.Request.Url);
                            if (res_uri.Authority == "localhost")
                            {
                                if (res_uri.QueryParams.Contains("error")
                                    && (string)res_uri.QueryParams.FirstOrDefault("error") == "access_denied")
                                {

                                    authorization_code = "error";
                                }
                                else if (res_uri.QueryParams.Contains("code"))
                                {
                                    authorization_code = (string)res_uri.QueryParams.FirstOrDefault("code");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                    };

                    //Backup
                    page.Response += async (sender, response) =>
                    {
                        Url res_uri = new Url(response.Response.Url);
                        if (res_uri.Authority.Contains("strava.com")
                        && res_uri.PathSegments.Contains("accept_application"))
                        {
                            bool success = response.Response.Headers.TryGetValue("location", out string val);
                            if (success)
                            {
                                Url location = new Url(val);
                                if (location.QueryParams.Contains("code"))
                                {
                                    authorization_code = (string)location.QueryParams.FirstOrDefault("code");
                                }
                            }
                        }
                    };

                    if (cookie_params != null)
                    {
                        await page.SetCookieAsync(cookie_params);
                    }

                    await page.GoToAsync(authorization_uri.ToString());

                    token.ThrowIfCancellationRequested();
                    while (string.IsNullOrEmpty(authorization_code))
                    {
                        await Task.Delay(5000);
                        token.ThrowIfCancellationRequested();
                    }
                    token.ThrowIfCancellationRequested();

                    if (authorization_code == "error")
                    {
                        return authorization_code;
                    }
                }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Log.Error("User cancelled.");
            }
            //catch (Exception ex)
            //{
            //    Log.Error(ex.ToString());
            //}
            finally
            {
                await BrowserUtilities.ShutdownBrowser(browser);
                Log.Information("Cleanup complete. Exiting.");
            }
            Log.Information($"Authorization code acquired: {authorization_code}");
            return authorization_code;
        }

        public static async Task<string> RequestTokens(string client_id, string client_secret, string authorization_code, CancellationToken token = default)
        {
            Log.Information("Requesting access and refresh tokens from Strava server");
            token.ThrowIfCancellationRequested();
            HttpClient sharedClient = new()
            {
                BaseAddress = new Uri(baseUrl),
            };

            try
            {
                Url request_token_uri = ConstructRelativeUri("oauth/token"
                    , new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string,string>("client_id", client_id),
                        new KeyValuePair<string,string>("client_secret", client_secret),
                        new KeyValuePair<string,string>("grant_type", "authorization_code"),
                        new KeyValuePair<string,string>("code", authorization_code)
                    }
                );
                HttpResponseMessage tokens_response = await sharedClient.PostAsync(request_token_uri.ToString(), null);
                tokens_response.EnsureSuccessStatusCode();
                string jsonResponse = await tokens_response.Content.ReadAsStringAsync();
                tokens_response.Dispose();
                return jsonResponse;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                sharedClient.Dispose();
            }
            return null;
        }

        public static async Task<string> RefreshTokens(StravaAccessTokens tokens, string client_id, string client_secret, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            HttpClient sharedClient = new()
            {
                BaseAddress = new Uri(baseUrl),
            };

            try
            {
                Url refresh_token_uri = ConstructRelativeUri("oauth/token"
                    , new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string,string>("client_id", client_id),
                        new KeyValuePair<string,string>("client_secret", client_secret),
                        new KeyValuePair<string,string>("grant_type", "refresh_token"),
                        new KeyValuePair<string,string>("refresh_token", tokens.RefreshToken)
                    }
                );
                HttpResponseMessage tokens_response = await sharedClient.PostAsync(refresh_token_uri.ToString(), null);
                tokens_response.EnsureSuccessStatusCode();
                string jsonResponse = await tokens_response.Content.ReadAsStringAsync();
                //Console.WriteLine($"{jsonResponse}\n");
                Log.Information($"New Access tokens: {jsonResponse}");
                token.ThrowIfCancellationRequested();
                tokens_response.Dispose();
                return jsonResponse;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                sharedClient.Dispose();
            }

            return null;
        }

        //public static async Task RefreshCookies(CookieParam[] CancellationToken token = default)
        //{
        //    token.ThrowIfCancellationRequested();

        //    IBrowser browser = await BrowserUtilities.LaunchOrConnect(headless: false, full_viewport: true);

        //    var pages = await browser.PagesAsync();
        //    var page = pages[0];

        //    Url strava_dashboard = new Url(baseUrl);
        //    strava_dashboard.AppendPathSegment("dashboard");

        //    try
        //    {
        //        if (strava_cookie_params != null
        //            && strava_cookie_params.Length > 0)
        //        {
        //            await page.SetCookieAsync(strava_cookie_params);
        //        }

        //        await page.GoToAsync(strava_dashboard);
        //        try
        //        {
        //            await page.WaitForResponseAsync(strava_dashboard, new WaitForOptions()
        //            {
        //                Timeout = 5
        //            });
        //        }
        //        catch (TimeoutException) { }

        //        CookieParam[] captured_cookie_params = await page.GetCookiesAsync(
        //            new string[]
        //            {
        //                "www.strava.com",
        //                baseUrl,
        //                ".strava.com",
        //                "*.strava.com",
        //                strava_dashboard.ToString()
        //            }
        //        );
        //        UpdateLocalCookies(captured_cookie_params);

        //        token.ThrowIfCancellationRequested();
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        Log.Error("User cancelled.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.ToString());
        //    }
        //    finally
        //    {
        //        await BrowserUtilities.ShutdownBrowser(browser);
        //    }
        //}

        public static async Task<byte[]> DownloadOriginalFit2(string activity_id, FileInfo dl, CancellationToken token = default)
        {
            byte[] bytes = null;
            Url download_original_uri = ConstructStravaUri(
                    $"activities/{activity_id}/export_original"
                );

            CookieParam[] cookie_params = strava_cookies;

            //if (cookie_params == null
            //        || cookie_params.Length == 0)
            //{
            //    await RefreshCookies(token);
            //}

            token.ThrowIfCancellationRequested();

            IBrowser browser = await BrowserUtilities.LaunchOrConnect(headless: false);
            try
            {
                var pages = await browser.PagesAsync();
                var page = pages[0];

                await page.SetRequestInterceptionAsync(true);

                // allows regular downloads for a headless mode
                await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = dl.DirectoryName });

                token.ThrowIfCancellationRequested();
                {
                    // sending POST with GoToAsync
                    page.Request += async (s, requestArgs) =>
                    {
                        foreach (IRequest red in requestArgs.Request.RedirectChain)
                        {
                            string url = red.Url;
                            string initiator_url = red.Initiator.Url;
                        }

                        Url res_uri = new Url(requestArgs.Request.Url);
                        if (res_uri.Authority.Contains("strava.com")
                        && res_uri.PathSegments.Contains("export_original"))
                        {
                            foreach (IRequest red in requestArgs.Request.RedirectChain)
                            {
                                string url = red.Url;
                                string initiator_url = red.Initiator.Url;
                            }

                            await requestArgs.Request.ContinueAsync();
                            //string name = response.Response.Headers["content-disposition"];
                        }

                        //YourContinueRequestHandler(requestArgs, HttpMethod.Post, data);
                    };

                    ////Backup
                    //page.Response += async (sender, response) =>
                    //{
                    //    Url res_uri = new Url(response.Response.Url);
                    //    if (res_uri.Authority.Contains("strava.com")
                    //    && res_uri.PathSegments.Contains("export_original"))
                    //    {
                    //        string name = response.Response.Headers["content-disposition"];
                    //    }
                    //};

                    if (cookie_params != null
                        && cookie_params.Length > 0)
                    {
                        await page.SetCookieAsync(cookie_params);
                    }

                    try
                    {
                        await page.GoToAsync(download_original_uri.ToString());
                    }
                    catch (Exception)
                    {
                        //Rename the download file to what we want.
                    }
                }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Log.Error("User cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                await BrowserUtilities.ShutdownBrowser(browser);
                Log.Information("Cleanup complete. Exiting.");
            }

            return bytes;
        }

        public struct DownloadedFit
        {
            public FileInfo Backup { get; set; }
            public FileInfo Main { get; set; }
        }

        public static string GetFilenameFromSummary(ActivitySummary summary)
        {
            DateTime local_dt = DateTime.Parse(summary.StartDate);
            string filename = string.Format($"{local_dt.ToString("yyyy_MM_dd_hh_mm")}_{summary.Name}.fit");
            return filename;
        }

        private static readonly string[] StravaHtmlDateTimeFormats =
        {
            "h:mm tt 'on' dddd, MMMM d, yyyy",
            "h:mm tt 'on' dddd, MMMM dd, yyyy"
        };

        public static DateTime GetDateTimeFromActivityPage(string dt_str)
        {
            return DateTime.ParseExact(
                dt_str.Trim(' '),
                StravaHtmlDateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None
            );
        }

        public static string GetFilenameFromHtmlInput(string activity_name, DateTime activity_start)
        {
            string filename = string.Format($"{activity_start.ToString("yyyy_MM_dd_hh_mm")}_{activity_name}.fit");
            return filename;
        }

        public static async Task<string> GetActivityInBrowser(long activity_id, CancellationToken token = default)
        {
            Url activity_uri = ConstructStravaUri(
                    $"activities/{activity_id}"
                );

            CookieParam[] cookie_params = strava_cookies;

            //if (cookie_params == null
            //        || cookie_params.Length == 0
            //        || ShouldUpdateForExpiry(cookie_params))
            //{
            //    await RefreshCookies(token);
            //}

            token.ThrowIfCancellationRequested();

            string html_activity_content = "";
            //IBrowser browser = null;
            //string csrfValue = "";
            //try
            //{
            //    token.ThrowIfCancellationRequested();
            //    browser = await BrowserUtilities.LaunchOrConnect(headless: true, full_viewport: true);
            //    var pages = await browser.PagesAsync();
            //    var page = pages[0];

            //    token.ThrowIfCancellationRequested();

            //    if (cookie_params != null)
            //    {
            //        await page.SetCookieAsync(cookie_params);
            //    }

            //    await page.GoToAsync(activity_uri.ToString());

            //    try
            //    {
            //        IElementHandle element = await page.QuerySelectorAsync("meta[name=\"csrf-token\"]");
            //        IJSHandle csrf = await element.GetPropertyAsync("content");
            //        csrfValue = await csrf.JsonValueAsync<string>();
            //    }
            //    catch (TimeoutException) { }

            //    token.ThrowIfCancellationRequested();
            //}
            //catch (OperationCanceledException)
            //{
            //    Log.Error("User cancelled.");
            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex.ToString());
            //}
            //finally
            //{
            //    await BrowserUtilities.ShutdownBrowser(browser);
            //    Log.Debug("Cleanup complete. Exiting.");
            //}

            //if (!string.IsNullOrEmpty(csrfValue))
            {
                //string payload = $"_method=delete&authenticity_token={csrfValue}";


                // Create an HttpClientHandler object and set to use default credentials
                HttpClientHandler handler = new HttpClientHandler();
                //handler.AllowAutoRedirect = false;
                handler.UseCookies = true;
                string cookie_header = "";
                {//Setting cookies on handler and creating the cookie header string
                    for (int i = 0; i < cookie_params.Length; i++)
                    {
                        CookieParam cp = cookie_params[i];
                        if (i > 0)
                        {
                            cookie_header += ";";
                        }
                        cookie_header += $"{cp.Name}={cp.Value}";
                        handler.CookieContainer.Add(new Cookie()
                        {
                            Domain = cp.Domain,
                            Path = cp.Path,
                            Expires = CommonUtilities.GetTimestamp((long)cp.Expires),
                            HttpOnly = cp.HttpOnly.Value,
                            Value = cp.Value,
                            Secure = cp.SourceScheme.Value == CookieSourceScheme.Secure,
                            Name = cp.Name,
                            Expired = CommonUtilities.GetTimestamp((long)cp.Expires) < DateTime.UtcNow,
                            Version = 1
                        });
                    }
                    cookie_header.Remove(cookie_header.Length - 1);
                }

                // Create an HttpClient object
                HttpClient sharedClient = new(handler)
                {
                    BaseAddress = new Uri(baseUrl),
                };

                sharedClient.DefaultRequestHeaders.Remove("User-Agent");

                try
                {
                    HttpRequestMessage activity_get_request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri($"activities/{activity_id}", UriKind.Relative),
                        Method = HttpMethod.Post,
                    };

                    activity_get_request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    activity_get_request.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                    activity_get_request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                    activity_get_request.Headers.Add("Connection", "keep-alive");
                    activity_get_request.Headers.Add("Cookie", cookie_header);
                    //delete_request.Headers.Add("Content-Length", formContent.);
                    //delete_request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    activity_get_request.Headers.Add("Host", "www.strava.com");
                    //fit_file_request.Headers.Add("Authorization", $"Bearer {WorkContext.AccessToken}");
                    activity_get_request.Headers.Add("Origin", "https://www.strava.com");
                    activity_get_request.Headers.Add("Priority", "u=0, i");
                    activity_get_request.Headers.Add("Sec-Fetch-Dest", "document");
                    activity_get_request.Headers.Add("Sec-Fetch-Mode", "navigate");
                    activity_get_request.Headers.Add("Sec-Fetch-Site", "same-origin");
                    activity_get_request.Headers.Add("Sec-Fetch-User", "?1");
                    activity_get_request.Headers.Add("Upgrade-Insecure-Requests", "1");
                    activity_get_request.Headers.Add("TE", "trailers");
                    activity_get_request.Headers.Add("Referer", Url.Combine(baseUrl, "athlete/training"));
                    activity_get_request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:148.0) Gecko/20100101 Firefox/148.0");

                    HttpResponseMessage activity_get_response = await sharedClient.SendAsync(activity_get_request
                        , HttpCompletionOption.ResponseContentRead
                        , token);
                    activity_get_response.EnsureSuccessStatusCode();

                    byte[] bytes = await activity_get_response.Content.ReadAsByteArrayAsync(token);

                    html_activity_content = Encoding.UTF8.GetString(bytes);

                    //Console.WriteLine($"{jsonResponse}\n");
                    token.ThrowIfCancellationRequested();
                    //Console.WriteLine($"Access:{WorkContext.AccessToken}   Refresh:{WorkContext.RefreshToken} Expires:{WorkContext.TokensExpiryDate.ToLocalTime().ToString()}"); 
                    activity_get_response.Dispose();


                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
                finally
                {
                    sharedClient.Dispose();
                }

                return html_activity_content;
            }
        }

        public struct ActivityInfo
        {
            public string ActivityName {get;set;}
            public DateTime ActivityStart { get; set; }
        }

        public static async Task<ActivityInfo> GetActivityInBrowser2(long activity_id, CancellationToken token = default)
        {
            Url activity_uri = ConstructStravaUri(
                    $"activities/{activity_id}"
                );

            CookieParam[] cookie_params = strava_cookies;

            //if (cookie_params == null
            //        || cookie_params.Length == 0
            //        || ShouldUpdateForExpiry(cookie_params))
            //{
            //    await RefreshCookies(token);
            //}

            token.ThrowIfCancellationRequested();
           
            IBrowser browser = null;

            string activity_name = "";
            DateTime activity_date = DateTime.MinValue;

            try
            {
                token.ThrowIfCancellationRequested();
                browser = await BrowserUtilities.LaunchOrConnect(headless: true, full_viewport: true);
                var pages = await browser.PagesAsync();
                var page = pages[0];

                token.ThrowIfCancellationRequested();

                if (cookie_params != null)
                {
                    await page.SetCookieAsync(cookie_params);
                }

                await page.GoToAsync(activity_uri.ToString());

                try
                {
                    {
                        IElementHandle element = await page.QuerySelectorAsync("time");
                        string dt_str = await element.EvaluateFunctionAsync<string>("el => el.innerText");
                        activity_date = GetDateTimeFromActivityPage(dt_str);
                    }
                    {
                        IElementHandle element = await page.QuerySelectorAsync("h1[class=\"text-title1 marginless activity-name\"]");
                        activity_name = await element.EvaluateFunctionAsync<string>("el => el.innerText");
                    }
                }
                catch (TimeoutException) { }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Log.Error("User cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                await BrowserUtilities.ShutdownBrowser(browser);
                Log.Debug("Cleanup complete. Exiting.");
            }

            return new ActivityInfo()
            {
                ActivityName = activity_name,
                ActivityStart = activity_date
            };
        }

        public static async Task<DownloadedFit> DownloadOriginalFit(long activity_id, CancellationToken token = default)
        {
            //filename
            //id
            //startdate

            //< time >
            //8:29 PM on Saturday, July 11, 2026
            //</ time >
            //< span class='location'>Heraklion Municipal Unit, Region of Crete</span>
            //<h1 class='text-title1 marginless activity-name'>Evening Walk</h1>


            ActivityInfo acinfo = await GetActivityInBrowser2(activity_id, token);

            string filename = GetFilenameFromHtmlInput(acinfo.ActivityName, acinfo.ActivityStart);

            DateTime time_created = acinfo.ActivityStart;

            byte[] bytes = null;
            Url download_original_uri = ConstructRelativeUri(
                    $"activities/{activity_id}/export_original"
                );

            CookieParam[] cookie_params = strava_cookies;

            //if (cookie_params == null
            //        || cookie_params.Length == 0
            //        || ShouldUpdateForExpiry(cookie_params))
            //{
            //    await RefreshCookies(token);
            //}

            token.ThrowIfCancellationRequested();

            // Create an HttpClientHandler object and set to use default credentials
            HttpClientHandler handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;
            handler.UseCookies = true;
            string cookie_header = "";
            {//Setting cookies on handler and creating the cookie header string
                for (int i = 0; i < cookie_params.Length; i++)
                {
                    CookieParam cp = cookie_params[i];
                    if (i > 0)
                    {
                        cookie_header += ";";
                    }
                    cookie_header += $"{cp.Name}={cp.Value}";
                    handler.CookieContainer.Add(new Cookie()
                    {
                        Domain = cp.Domain,
                        Path = cp.Path,
                        Expires = CommonUtilities.GetTimestamp((long)cp.Expires),
                        HttpOnly = cp.HttpOnly.Value,
                        Value = cp.Value,
                        Secure = cp.SourceScheme.Value == CookieSourceScheme.Secure,
                        Name = cp.Name,
                        Expired = CommonUtilities.GetTimestamp((long)cp.Expires) < DateTime.UtcNow,
                        Version = 1
                    });
                }
                cookie_header.Remove(cookie_header.Length - 1);
            }

            // Create an HttpClient object
            HttpClient sharedClient = new(handler)
            {
                BaseAddress = new Uri(baseUrl),
            };

            sharedClient.DefaultRequestHeaders.Remove("User-Agent");

            try
            {
                HttpRequestMessage fit_file_request = new HttpRequestMessage()
                {
                    RequestUri = download_original_uri.ToUri(),
                    Method = HttpMethod.Get,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };


                fit_file_request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9");
                fit_file_request.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                fit_file_request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                fit_file_request.Headers.Add("Connection", "keep-alive");
                fit_file_request.Headers.Add("Cookie", cookie_header);
                fit_file_request.Headers.Add("Host", "www.strava.com");
                //fit_file_request.Headers.Add("Authorization", $"Bearer {WorkContext.AccessToken}");
                //fit_file_request.Headers.Add("Origin", "https://www.strava.com");
                fit_file_request.Headers.Add("Priority", "u=0, i");
                fit_file_request.Headers.Add("Sec-Fetch-Dest", "document");
                fit_file_request.Headers.Add("Sec-Fetch-Mode", "navigate");
                fit_file_request.Headers.Add("Sec-Fetch-Site", "same-origin");
                fit_file_request.Headers.Add("Sec-Fetch-User", "?1");
                fit_file_request.Headers.Add("Upgrade-Insecure-Requests", "1");
                fit_file_request.Headers.Add("Referer", Url.Combine(baseUrl, download_original_uri.RemovePathSegment()));
                fit_file_request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:148.0) Gecko/20100101 Firefox/148.0");

                HttpResponseMessage fit_file_response = await sharedClient.SendAsync(fit_file_request
                    , HttpCompletionOption.ResponseContentRead
                    , token);
                fit_file_response.EnsureSuccessStatusCode();
                bytes = await fit_file_response.Content.ReadAsByteArrayAsync(token);

                //Console.WriteLine($"{jsonResponse}\n");
                token.ThrowIfCancellationRequested();
                //Console.WriteLine($"Access:{WorkContext.AccessToken}   Refresh:{WorkContext.RefreshToken} Expires:{WorkContext.TokensExpiryDate.ToLocalTime().ToString()}"); 
                fit_file_response.Dispose();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                sharedClient.Dispose();
            }

            if (bytes != null
                && bytes.Length > 0)
            {
                DownloadedFit files = new DownloadedFit();

                {//Backup
                    files.Backup = new FileInfo(Path.Combine(original_backup_dir.FullName, filename));
                    await File.WriteAllBytesAsync(files.Backup.FullName, bytes, token);
                    files.Backup.CreationTime = time_created;
                    files.Backup.LastWriteTime = time_created;
                    files.Backup.Refresh();
                }

                {//Actual
                    files.Main = new FileInfo(Path.Combine(modified_backup_dir.FullName, filename));
                    await File.WriteAllBytesAsync(files.Main.FullName, bytes, token);
                    files.Main.CreationTime = time_created;
                    files.Main.Refresh();
                }

                return files;
            }

            return new DownloadedFit()
            {
                Backup = null,
                Main = null
            };
        }

        public static async Task DeleteActivityInBrowser(Activity activity, CancellationToken token = default)
        {
            Url delete_uri = ConstructStravaUri(
                    $"activities/{activity.Id}"
                );

            CookieParam[] cookie_params = strava_cookies;

            //if (cookie_params == null
            //        || cookie_params.Length == 0
            //        || ShouldUpdateForExpiry(cookie_params))
            //{
            //    await RefreshCookies(token);
            //}

            token.ThrowIfCancellationRequested();


            IBrowser browser = null;
            string csrfValue = "";
            try
            {
                token.ThrowIfCancellationRequested();
                browser = await BrowserUtilities.LaunchOrConnect(headless: true, full_viewport: true);
                var pages = await browser.PagesAsync();
                var page = pages[0];

                token.ThrowIfCancellationRequested();

                if (cookie_params != null)
                {
                    await page.SetCookieAsync(cookie_params);
                }

                await page.GoToAsync(delete_uri.ToString());

                try
                {
                    IElementHandle element = await page.QuerySelectorAsync("meta[name=\"csrf-token\"]");
                    IJSHandle csrf = await element.GetPropertyAsync("content");
                    csrfValue = await csrf.JsonValueAsync<string>();
                }
                catch (TimeoutException) { }

                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Log.Error("User cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                await BrowserUtilities.ShutdownBrowser(browser);
                Log.Debug("Cleanup complete. Exiting.");
            }

            if (!string.IsNullOrEmpty(csrfValue))
            {
                string payload = $"_method=delete&authenticity_token={csrfValue}";


                // Create an HttpClientHandler object and set to use default credentials
                HttpClientHandler handler = new HttpClientHandler();
                //handler.AllowAutoRedirect = false;
                handler.UseCookies = true;
                string cookie_header = "";
                {//Setting cookies on handler and creating the cookie header string
                    for (int i = 0; i < cookie_params.Length; i++)
                    {
                        CookieParam cp = cookie_params[i];
                        if (i > 0)
                        {
                            cookie_header += ";";
                        }
                        cookie_header += $"{cp.Name}={cp.Value}";
                        handler.CookieContainer.Add(new Cookie()
                        {
                            Domain = cp.Domain,
                            Path = cp.Path,
                            Expires = CommonUtilities.GetTimestamp((long)cp.Expires),
                            HttpOnly = cp.HttpOnly.Value,
                            Value = cp.Value,
                            Secure = cp.SourceScheme.Value == CookieSourceScheme.Secure,
                            Name = cp.Name,
                            Expired = CommonUtilities.GetTimestamp((long)cp.Expires) < DateTime.UtcNow,
                            Version = 1
                        });
                    }
                    cookie_header.Remove(cookie_header.Length - 1);
                }

                // Create an HttpClient object
                HttpClient sharedClient = new(handler)
                {
                    BaseAddress = new Uri(baseUrl),
                };

                sharedClient.DefaultRequestHeaders.Remove("User-Agent");

                try
                {
                    HttpRequestMessage delete_request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri($"activities/{activity.Id}", UriKind.Relative),
                        Method = HttpMethod.Post,
                    };

                    var formContent = new FormUrlEncodedContent(new[]
{
                        new KeyValuePair<string, string>("_method", "delete"),
                        new KeyValuePair<string, string>("authenticity_token", csrfValue)
                    });

                    delete_request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    delete_request.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                    delete_request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                    delete_request.Headers.Add("Connection", "keep-alive");
                    delete_request.Headers.Add("Cookie", cookie_header);
                    //delete_request.Headers.Add("Content-Length", formContent.);
                    //delete_request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    delete_request.Headers.Add("Host", "www.strava.com");
                    //fit_file_request.Headers.Add("Authorization", $"Bearer {WorkContext.AccessToken}");
                    delete_request.Headers.Add("Origin", "https://www.strava.com");
                    delete_request.Headers.Add("Priority", "u=0, i");
                    delete_request.Headers.Add("Sec-Fetch-Dest", "document");
                    delete_request.Headers.Add("Sec-Fetch-Mode", "navigate");
                    delete_request.Headers.Add("Sec-Fetch-Site", "same-origin");
                    delete_request.Headers.Add("Sec-Fetch-User", "?1");
                    delete_request.Headers.Add("Upgrade-Insecure-Requests", "1");
                    delete_request.Headers.Add("TE", "trailers");
                    delete_request.Headers.Add("Referer", Url.Combine(baseUrl, delete_uri));
                    delete_request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:148.0) Gecko/20100101 Firefox/148.0");



                    delete_request.Content = formContent;

                    HttpResponseMessage delete_response = await sharedClient.SendAsync(delete_request
                        , HttpCompletionOption.ResponseContentRead
                        , token);
                    delete_response.EnsureSuccessStatusCode();

                    //Console.WriteLine($"{jsonResponse}\n");
                    token.ThrowIfCancellationRequested();
                    //Console.WriteLine($"Access:{WorkContext.AccessToken}   Refresh:{WorkContext.RefreshToken} Expires:{WorkContext.TokensExpiryDate.ToLocalTime().ToString()}"); 
                    delete_response.Dispose();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
                finally
                {
                    sharedClient.Dispose();
                }
            }
        }

        public static async Task DeleteActivity(StravaClient client, long activity_id, CancellationToken token = default)
        {
            try
            {
                Log.Debug($"Looking up activity #{activity_id}");
                Activity existing_activity = await client.Activities.GetActivityAsync(activity_id.ToString(), false);

                if (existing_activity != null)
                {
                    Log.Debug($"Found activity #{activity_id}");
                    Log.Information($"Deleting activity #{activity_id}");
                    await DeleteActivityInBrowser(existing_activity, token);
                    await Task.Delay(2000);
                    Log.Information($"Verifying activity #{activity_id} was deleted");
                    int tries = 5;
                    bool deleted = false;
                    while (tries-- >= 0)
                    {
                        try
                        {
                            Activity activity = await client.Activities.GetActivityAsync(activity_id.ToString(), false);
                            if (activity == null)
                            {
                                deleted = true;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            deleted = true;
                            break;
                        }
                        await Task.Delay(2000); //Make sure the activity has been deleted
                    }
                    if (deleted)
                    {
                        Log.Information($"Activity {activity_id} was safely deleted.");
                    }
                    else
                    {
                        Log.Error($"Activity {activity_id} could not be  deleted.");
                    }
                }
                else
                {
                    Log.Error($"Could not find activity with ID: {activity_id}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error($"Exception#DeleteActivity: " + e.Message);
            }
        }

        public static bool IsNotEmpty(StravaAccessTokens new_tokens)
        {
            return !string.IsNullOrEmpty(new_tokens.AccessToken)
                && !string.IsNullOrEmpty(new_tokens.RefreshToken)
                && new_tokens.ExpiryDate != DateTime.MinValue;
        }

        public static void ListActivities(StravaClient client, DateTime RangeStart, DateTime RangeEnd, int amount = 5, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                List<ActivitySummary> summaries = client.Activities.GetActivities(RangeStart, RangeEnd, 1, amount);
                token.ThrowIfCancellationRequested();
                if (summaries.Count > 0)
                {
                    Log.Information($"Activity ID\tActivity Name\tTimeCreated(Local)\tDistance\tDuration");
                    foreach (ActivitySummary summary in summaries)
                    {
                        Log.Information($"{summary.Id}\t{summary.Name}\t{summary.StartDateLocal.ToString()}\t{CommonUtilities.GetDistanceInKm(summary.Distance)}\t{CommonUtilities.GetTotalSecondsInDuration(summary.ElapsedTime)}");
                        token.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error($"Exception#ListActivities: " + e.Message);
            }
        }

        public static async Task UploadActivity(StravaClient client, FileInfo file, CancellationToken token = default)
        {
            Log.Information($"Uploading activity file");
            int tries = 5;
            while (tries-- > 0)
            {
                try
                {
                    UploadStatus status = await client.Uploads.UploadActivityAsync(file.FullName, DataFormat.Fit, com.strava.v3.api.Activities.ActivityType.Walk);
                    while (status.CurrentStatus == CurrentUploadStatus.Processing)
                    {
                        Log.Information("Awaiting processing from Strava server");
                        await Task.Delay(5000);
                        status = await client.Uploads.CheckUploadStatusAsync(status.Id.ToString());
                    }
                    if (status.CurrentStatus == CurrentUploadStatus.Ready)
                    {
                        Log.Information($"Upload is finished and ready.");
                        break;
                    }
                    else if (status.CurrentStatus == CurrentUploadStatus.Error)
                    {
                        Log.Error($"Upload had an error: {status.Error}");
                        if (!status.Error.Contains("duplicate"))
                        {
                            //Problem with file most likely
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Log.Error($"Exception#UploadActivity: " + e.Message);
                }
            }
        }

        public static async Task DownloadActivityRange(StravaClient client, DateTime start, DateTime end, CancellationToken token = default)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Downloading activities ");
            if (start != DateTime.MinValue)
            {
                sb.Append(start.ToString());
            }
            if (end != DateTime.MaxValue)
            {
                sb.Append($" up to {end.ToString()}");
            }
            Log.Information(sb.ToString());
            sb.Clear();
            long current_activity_id = 0;
            try
            {
                {//Find Activity
                    token.ThrowIfCancellationRequested();
                    int page_index = 1;
                    List<ActivitySummary> summaries = client.Activities.GetActivities(start, end, page_index++, 20);
                    while (summaries.Count > 0)
                    {
                        token.ThrowIfCancellationRequested();
                        for (int i = 0; i < summaries.Count; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            ActivitySummary summary = summaries[i];
                            current_activity_id = summary.Id;
                            Log.Information($"Downloading original fit file for activity #{summary.Id}");
                            DownloadedFit f = await DownloadOriginalFit(current_activity_id, token);
                        }
                        token.ThrowIfCancellationRequested();
                        summaries = client.Activities.GetActivities(start, end, page_index, 20);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occurred when trying to find the activity or download the activity's original fit file with id #{current_activity_id}. Message: " + ex.Message);
            }
        }

        public static async Task<DownloadedFit> DownloadActivity(StravaClient client, long activity_id, CancellationToken token = default)
        {
            Log.Information($"Downloading activity {activity_id}");
            try
            {
                {//Find Activity
                    token.ThrowIfCancellationRequested();
                    DownloadedFit f = await DownloadOriginalFit(activity_id, token);
                    //Log.Information($"Looking up activity #{activity_id}");
                    //Activity existing_activity = await client.Activities.GetActivityAsync(activity_id.ToString(), false);
                    //token.ThrowIfCancellationRequested();
                    //if (existing_activity != null)
                    //{
                    //    Log.Information($"Found activity #{activity_id}");

                    //    Log.Information($"Downloading original fit file for activity #{activity_id}");
                        
                    //    return f;
                    //}
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occurred when trying to find the activity or download the activity's original fit file with id #{activity_id}. Message: " + ex.Message);
            }

            return new DownloadedFit()
            {
                Main = null,
                Backup = null
            };
        }

        public static async Task FixRemoteActivity(StravaClient client, long activity_id, int steps, Action<FileInfo, int, CancellationTokenSource> ModificationCallback, CancellationTokenSource cancellation_source = default)
        {
            Log.Information($"Fixing activity {activity_id}");
            try
            {
                cancellation_source.Token.ThrowIfCancellationRequested();
                DownloadedFit files = await DownloadActivity(client, activity_id, cancellation_source.Token);
                if (!files.Main.Exists)
                {
                    files.Backup.CopyTo(files.Main.FullName);
                }
                cancellation_source.Token.ThrowIfCancellationRequested();
                ModificationCallback(files.Main, steps, cancellation_source);
                cancellation_source.Token.ThrowIfCancellationRequested();
                await DeleteActivity(client, activity_id, cancellation_source.Token);
                cancellation_source.Token.ThrowIfCancellationRequested();
                Log.Information("Waiting for 4sec to allow system to flush the delete.");
                await Task.Delay(4000);
                cancellation_source.Token.ThrowIfCancellationRequested();
                await UploadActivity(client, files.Main, cancellation_source.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in FixRemoteActivity Message: " + ex.Message);
            }
        }
    }
}
