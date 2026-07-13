using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SourceAFIS;

namespace FPTester
{
    internal class WebSocketServer
    {
        private readonly ScannerThread _scanner;
        private readonly DatabaseTemplateStore _dbStore = new();
        private CancellationTokenSource? _scanCts;

        public WebSocketServer(ScannerThread scanner)
        {
            _scanner = scanner;
        }

        private static void Log(string msg)
        {
            try
            {
                string logPath = Path.Combine(
                    AppContext.BaseDirectory, "ws_log.txt");
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            }
            catch { /* best-effort logging */ }
        }

        public async Task StartAsync()
        {
            Log("StartAsync called — using TcpListener");

            TcpListener tcp;
            try
            {
                tcp = new TcpListener(IPAddress.Loopback, 5000);
                tcp.Start();
                Log($"TcpListener started on port 5000");
            }
            catch (Exception ex)
            {
                Log($"Failed to start TcpListener: {ex.GetType().Name}: {ex.Message}");
                return;
            }

            try
            {
                while (true)
                {
                    var client = await tcp.AcceptTcpClientAsync();
                    _ = HandleTcpClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Log($"Listen loop error: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private async Task HandleTcpClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                try
                {
                    // Read the HTTP upgrade request
                    var buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!request.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase))
                    {
                        // Not a WebSocket request — send 400
                        var badReq = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 400 Bad Request\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(badReq);
                        return;
                    }

                    // Extract Sec-WebSocket-Key
                    string? wsKey = null;
                    foreach (var line in request.Split("\r\n"))
                    {
                        if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                        {
                            wsKey = line.Substring(line.IndexOf(':') + 1).Trim();
                            break;
                        }
                    }

                    if (wsKey == null)
                    {
                        var badReq = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 400 Bad Request\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(badReq);
                        return;
                    }

                    // Compute accept key per RFC 6455
                    string acceptKey = Convert.ToBase64String(
                        SHA1.HashData(
                            Encoding.UTF8.GetBytes(wsKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

                    // Send upgrade response
                    string response =
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Connection: Upgrade\r\n" +
                        $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(response));

                    // Now use the .NET WebSocket class over this stream
                    var ws = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
                    {
                        IsServer = true,
                        KeepAliveInterval = TimeSpan.FromSeconds(30)
                    });

                    Log("WebSocket client connected");
                    await HandleConnectionAsync(ws);
                }
                catch (Exception ex)
                {
                    Log($"TCP client error: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private async Task HandleConnectionAsync(WebSocket webSocket)
        {
            Log("Client connected.");
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                        StopAllScans();
                    }
                    else
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Log($"Received: {message}");
                        
                        try {
                            var doc = JsonDocument.Parse(message);
                            if (doc.RootElement.TryGetProperty("action", out var actionElement))
                            {
                                string action = actionElement.GetString() ?? "";
                                if (action == "start_scan")
                                {
                                    _ = StartScanAsync(webSocket);
                                }
                                else if (action == "stop_scan")
                                {
                                    StopAllScans();
                                }
                                else if (action == "enroll_fingerprint")
                                {
                                    if (doc.RootElement.TryGetProperty("clientId", out var clientElement))
                                        _ = EnrollFingerprintAsync(webSocket, clientElement.GetString() ?? "");
                                }
                                else if (action == "remove_fingerprint")
                                {
                                    if (doc.RootElement.TryGetProperty("clientId", out var clientElement))
                                        _ = RemoveFingerprintAsync(webSocket, clientElement.GetString() ?? "");
                                }
                            }
                        } catch (Exception ex) {
                            Log($"JSON parse error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"WebSocket error: {ex.Message}");
                StopAllScans();
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed)
                    webSocket.Dispose();
                Log("Client disconnected.");
            }
        }

        public void StopAllScans()
        {
            if (_scanCts != null)
            {
                _scanCts.Cancel();
                _scanCts = null;
                Log("Scan cancelled by GUI or socket closure.");
            }
        }

        private async Task StartScanAsync(WebSocket webSocket)
        {
            StopAllScans(); // Stop any existing scan
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            try
            {
                if (!_scanner.IsOpen)
                {
                    await SendMessageAsync(webSocket, new { status = "connecting" });
                    var (ok, msg) = await _scanner.OpenAsync();
                    if (!ok)
                    {
                        await SendMessageAsync(webSocket, new { success = false, error_type = "scanner_not_connected", error = "Scanner not connected: " + msg });
                        return;
                    }
                }

                await SendMessageAsync(webSocket, new { status = "scanner_ready" });
                await SendMessageAsync(webSocket, new { status = "scanning" });

                byte[]? image = null;
                while (!token.IsCancellationRequested)
                {
                    var (img, err) = await _scanner.CaptureAsync(15000, token, () => {}, () => {});
                    if (img != null)
                    {
                        image = img;
                        break;
                    }
                    // If timeout, just loop again since we want continuous scanning until cancel
                }
                
                if (token.IsCancellationRequested || image == null) return;

                await SendMessageAsync(webSocket, new { status = "processing" });

                // Extract template
                var template = new FingerprintTemplate(new FingerprintImage(_scanner.ImageWidth, _scanner.ImageHeight, image, new FingerprintImageOptions { Dpi = 500 }));
                var matcher = new FingerprintMatcher(template);

                // Fetch all templates from DB
                var dbTemplates = _dbStore.GetAllTemplates();
                string? bestMatchId = null;
                double bestScore = 0;
                double threshold = 40.0;

                foreach (var kvp in dbTemplates)
                {
                    if (token.IsCancellationRequested) return;
                    try
                    {
                        var storedTemplate = new FingerprintTemplate(kvp.Value);
                        double score = matcher.Match(storedTemplate);
                        if (score >= threshold && score > bestScore)
                        {
                            bestScore = score;
                            bestMatchId = kvp.Key;
                        }
                    }
                    catch { /* Ignore corrupted templates */ }
                }

                if (bestMatchId != null)
                {
                    await SendMessageAsync(webSocket, new { success = true, match = true, clientId = bestMatchId, score = bestScore });
                }
                else
                {
                    await SendMessageAsync(webSocket, new { success = true, match = false });
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled cleanly
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    await SendMessageAsync(webSocket, new { success = false, error = ex.Message });
            }
        }

        private async Task EnrollFingerprintAsync(WebSocket webSocket, string clientId)
        {
            StopAllScans();
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            try
            {
                if (!_scanner.IsOpen)
                {
                    await SendMessageAsync(webSocket, new { status = "connecting" });
                    var (ok, msg) = await _scanner.OpenAsync();
                    if (!ok)
                    {
                        await SendMessageAsync(webSocket, new { success = false, error_type = "scanner_not_connected", error = "Scanner not connected: " + msg });
                        return;
                    }
                }

                await SendMessageAsync(webSocket, new { status = "scanner_ready" });
                await SendMessageAsync(webSocket, new { status = "scanning" });

                byte[]? image = null;
                while (!token.IsCancellationRequested)
                {
                    var (img, err) = await _scanner.CaptureAsync(15000, token, () => {}, () => {});
                    if (img != null)
                    {
                        image = img;
                        break;
                    }
                }

                if (token.IsCancellationRequested || image == null) return;

                await SendMessageAsync(webSocket, new { status = "processing" });

                var template = new FingerprintTemplate(new FingerprintImage(_scanner.ImageWidth, _scanner.ImageHeight, image, new FingerprintImageOptions { Dpi = 500 }));
                byte[] templateBytes = template.ToByteArray();

                bool saved = _dbStore.SaveTemplate(clientId, templateBytes);

                if (saved)
                {
                    await SendMessageAsync(webSocket, new { success = true, action = "enroll_fingerprint" });
                }
                else
                {
                    await SendMessageAsync(webSocket, new { success = false, error = "Failed to save template to database" });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    await SendMessageAsync(webSocket, new { success = false, error = ex.Message });
            }
        }

        private async Task RemoveFingerprintAsync(WebSocket webSocket, string clientId)
        {
            try
            {
                bool removed = _dbStore.RemoveTemplate(clientId);
                if (removed)
                {
                    await SendMessageAsync(webSocket, new { success = true, action = "remove_fingerprint" });
                }
                else
                {
                    await SendMessageAsync(webSocket, new { success = false, error = "Failed to remove template from database" });
                }
            }
            catch (Exception ex)
            {
                await SendMessageAsync(webSocket, new { success = false, error = ex.Message });
            }
        }

        private async Task SendMessageAsync(WebSocket webSocket, object data)
        {
            if (webSocket.State != WebSocketState.Open) return;
            string json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
