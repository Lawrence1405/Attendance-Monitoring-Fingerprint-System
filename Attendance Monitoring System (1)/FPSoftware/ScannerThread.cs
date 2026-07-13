using System;
using System.Threading;
using System.Threading.Tasks;

namespace FPTester
{
    /// <summary>
    /// Dedicated STA thread that owns the ftrScanAPI.dll device handle for its
    /// entire lifetime. All SDK calls are marshalled onto this thread via a
    /// BlockingCollection queue so the handle is always used from the thread
    /// that created it (required by the Futronic USB driver).
    /// </summary>
    internal class ScannerThread : IDisposable
    {
        private readonly Thread          _thread;
        private readonly BlockingChannel _channel = new();
        private IntPtr                   _device  = IntPtr.Zero;
        private bool                     _disposed;

        public bool IsOpen     => _device != IntPtr.Zero;
        public int  ImageWidth  { get; private set; } = 480;
        public int  ImageHeight { get; private set; } = 640;
        public int  ImageBytes  { get; private set; } = 480 * 640;

        public ScannerThread()
        {
            _thread = new Thread(ThreadLoop)
            {
                IsBackground = true,
                Name         = "ScannerSTA"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public Task<(bool ok, string error)> OpenAsync() =>
            Run(() =>
            {
                if (_device != IntPtr.Zero)
                    return (true, "");

                _device = FutronicScanAPI.ftrScanOpenDevice();
                if (_device == IntPtr.Zero)
                    return (false, FutronicScanAPI.LastErrorDescription());

                bool sizeOk = FutronicScanAPI.TryGetImageSize(_device,
                    out int w, out int h, out int bytes);

                ImageWidth  = w;
                ImageHeight = h;
                ImageBytes  = bytes;

                string sizeMsg = sizeOk
                    ? $"{w}×{h} ({bytes} bytes)"
                    : $"query failed — using fallback {w}×{h} ({bytes} bytes)";

                return (true, sizeMsg);
            });

        public Task CloseAsync() =>
            Run(() =>
            {
                if (_device == IntPtr.Zero) return;
                FutronicScanAPI.ftrScanCloseDevice(_device);
                _device = IntPtr.Zero;
            });

        /// <summary>
        /// Waits for a finger then captures one raw grayscale image.
        /// onWaiting / onCapturing fire on the scanner thread — use SafeInvoke to touch UI.
        /// </summary>
        public Task<(byte[]? image, string error)> CaptureAsync(
            int               timeoutMs,
            CancellationToken ct,
            Action?           onWaiting   = null,
            Action?           onCapturing = null) =>
            Run(() =>
            {
                onWaiting?.Invoke();

                if (!FutronicScanAPI.WaitForFinger(_device, timeoutMs, ct))
                    return ((byte[]?)null, "No finger detected — timed out.");

                onCapturing?.Invoke();

                var buf = new byte[ImageBytes];
                if (!FutronicScanAPI.CaptureImage(_device, buf))
                    return (null, FutronicScanAPI.LastErrorDescription());

                return (buf, "");
            });

        // ── Thread plumbing ───────────────────────────────────────────────────

        private void ThreadLoop()
        {
            foreach (var work in _channel.Consume())
            {
                try   { work(); }
                catch { /* exceptions are carried by the individual TaskCompletionSource */ }
            }
        }

        private Task<T> Run<T>(Func<T> fn)
        {
            var tcs = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _channel.Post(() =>
            {
                try   { tcs.SetResult(fn()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        private Task Run(Action fn)
        {
            var tcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _channel.Post(() =>
            {
                try   { fn(); tcs.SetResult(); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Run(() =>
            {
                if (_device == IntPtr.Zero) return;
                FutronicScanAPI.ftrScanCloseDevice(_device);
                _device = IntPtr.Zero;
            }).Wait(2000);
            _channel.Complete();
            _thread.Join(2000);
        }
    }

    internal sealed class BlockingChannel
    {
        private readonly System.Collections.Concurrent.BlockingCollection<Action>
            _q = new(boundedCapacity: 64);

        public void Post(Action a) => _q.Add(a);
        public void Complete()     => _q.CompleteAdding();
        public System.Collections.Generic.IEnumerable<Action> Consume() =>
            _q.GetConsumingEnumerable();
    }
}
