using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using SourceAFIS;

namespace FPTester
{
    public class WebSocketServer
    {
        private readonly ScannerThread _scanner = new();
        private readonly DatabaseTemplateStore _dbStore = new();
        private CancellationTokenSource? _scanCts;

        public async Task StartAsync()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Console.WriteLine("WebSocket server listening on ws://localhost:5000/");

            while (true)
            {
                var context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    var wsContext = await context.AcceptWebSocketAsync(null);
                    _ = HandleConnectionAsync(wsContext.WebSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async Task HandleConnectionAsync(WebSocket webSocket)
        {
            Console.WriteLine("Client connected.");
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                        StopScan();
                    }
                    else
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received: {message}");
                        
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
                                    StopScan();
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
                            Console.WriteLine($"JSON parse error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                StopScan();
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed)
                    webSocket.Dispose();
                Console.WriteLine("Client disconnected.");
            }
        }

        private void StopScan()
        {
            if (_scanCts != null)
            {
                _scanCts.Cancel();
                _scanCts = null;
                Console.WriteLine("Scan cancelled.");
            }
        }

        private async Task StartScanAsync(WebSocket webSocket)
        {
            StopScan(); // Stop any existing scan
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
            StopScan();
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
