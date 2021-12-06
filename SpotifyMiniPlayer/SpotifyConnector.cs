using CredentialManagement;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SpotifyMiniPlayer
{
    public class SpotifyConnector
    {
        private static string _credentialManagerTarget = "SpotifyMiniPlayer";
        private static string _redirectUri = "http://localhost:5050/callback";

        private string _clientId;
        private string _clientSecret;
        private static HttpListener _httpListener;
        private static string _returnUrl = string.Empty;

        public SpotifyConnector()
        {
            var credential = new Credential { Target = _credentialManagerTarget };
            credential.Load();
            _clientId = credential.Username;
            _clientSecret = credential.Password;
        }

        public Token GetToken()
        {
            var client = new RestClient("https://accounts.spotify.com/api/token");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_clientId + ":" + _clientSecret));
            var request = new RestRequest(Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", $"Basic {auth}");
            request.AddParameter("grant_type", "client_credentials");

            var response = client.Execute(request);
            var content = response.Content;
            return JsonConvert.DeserializeObject<Token>(content);
        }

        public Token GetAccessToken(string code)
        {
            var client = new RestClient("https://accounts.spotify.com/api/token");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_clientId + ":" + _clientSecret));
            var request = new RestRequest(Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", $"Basic {auth}");
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", $"{code}");
            request.AddParameter("redirect_uri", _redirectUri);
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);

            var response = client.Execute(request);
            var content = response.Content;
            return JsonConvert.DeserializeObject<Token>(content);
        }

        public void AuthorizationAsync()
        {
            var scope = "user-read-currently-playing";

            var authorizationLink = "https://accounts.spotify.com/authorize";
            authorizationLink += "?response_type=code";
            authorizationLink += "&client_id=" + _clientId;
            authorizationLink += "&scope=" + scope;
            authorizationLink += "&redirect_uri=" + _redirectUri;

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:5050/");
            _httpListener.Start();

            var process = new Process();
            process.StartInfo = new ProcessStartInfo(authorizationLink);
            process.Start();

            ResponseThread();
            _httpListener.Stop();
            GetSong(GetAccessToken(_returnUrl));
        }

        public void GetSong(Token accessToken)
        {
            var client = new RestClient("https://api.spotify.com/v1/me/player/currently-playing");

            var request = new RestRequest(Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", $"Bearer {accessToken.access_token}");

            var response = client.Execute(request);
            var content = response.Content;
            var currentSong = JsonConvert.DeserializeObject<CurrentSong>(content);
        }

        private void ResponseThread()
        {
            var callbackSite = Encoding.UTF8.GetBytes(Properties.Resources.Callback);

            HttpListenerContext context = _httpListener.GetContext();
            _returnUrl = context.Request.Url.Query.Substring(6);
            context.Response.KeepAlive = false;
            context.Response.OutputStream.Write(callbackSite, 0, callbackSite.Length);
            context.Response.Close();
            return;
        }
    }
}