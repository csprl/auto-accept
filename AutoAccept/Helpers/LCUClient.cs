using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoAccept.Helpers
{
    class LCUClient
    {
        private static readonly string _username = "riot";
        private static readonly bool _secure = true;
        private static readonly HttpClientHandler _httpClientHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true };

        private readonly HttpClient _httpClient = new HttpClient(_httpClientHandler);
        private readonly ClientWebSocket _ws = new ClientWebSocket();
        private readonly int _port;
        private readonly string _baseUrl;

        public Action OnConnected;
        public Action OnDisconnected;
        public Action<string> OnMessage;
        public Action OnReadyCheck;

        public LCUClient(int port, string password)
        {
            // Disable certificate validation
            _ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            // Set credentials
            _ws.Options.Credentials = new NetworkCredential(_username, password);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{password}")));

            // Set keep-alive and subprotocol
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _ws.Options.AddSubProtocol("wamp");

            _port = port;
            _baseUrl = $"{(_secure ? "https" : "http")}://127.0.0.1:{_port}";
        }

        public async Task Connect()
        {
            // Open WebSocket connection
            await _ws.ConnectAsync(new Uri($"{(_secure ? "wss" : "ws")}://127.0.0.1:{_port}/"), CancellationToken.None);
            OnConnected?.Invoke();

            // Subscribe to api events
            await Subscribe("OnJsonApiEvent");

            // Receive messages
            var buffer = new byte[1024];
            while (_ws.State == WebSocketState.Open)
            {
                var message = new StringBuilder();

                // Read message
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    else
                    {
                        message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                } while (!result.EndOfMessage);

                // Finished reading a message
                var msg = message.ToString();
                OnMessage?.Invoke(msg);

                // Parse and handle WAMP messages
                HandleMessage(msg);
            }

            OnDisconnected?.Invoke();
        }

        public async Task Subscribe(string topic)
        {
            // Build payload
            var payload = $"[5, \"{topic}\"]"; // very lazy

            await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task AcceptReadyCheck()
        {
            var response = await _httpClient.PostAsync(_baseUrl + "/lol-matchmaking/v1/ready-check/accept", null);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to accept ready check");
            }
        }

        private void HandleMessage(string message)
        {
            // Parse WAMP message, proper way would be to write a custom json parser but oh well
            if (!message.StartsWith("[") || !message.EndsWith("]")) return;

            // Parse message type
            var messageType = int.Parse(message.Substring(1, message.IndexOf(',') - 1));
            
            // Handle events
            if (messageType == 8) // EVENT
            {
                // Detect ready check
                if (message.Contains("OnJsonApiEvent") && message.Contains("AFK_CHECK") && message.Contains("ackRequired"))
                {
                    OnReadyCheck?.Invoke();
                }
            }
        }
    }
}
