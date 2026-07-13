using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FPTester
{
    /// <summary>
    /// P/Invoke bindings for ftrScanAPI.dll — transcribed exactly from the
    /// official ftrScanAPI.h header (github.com/erikssm/futronics-fingerprint-reader).
    ///
    /// Key corrections vs previous versions:
    ///  1. FTRSCAN_IMAGE_SIZE has THREE int fields (nWidth, nHeight, nImageSize)
    ///     — the missing third field caused AccessViolationException.
    ///  2. ftrScanGetImage buffer is GCHandle-pinned so the GC cannot relocate
    ///     the byte array while the native DLL is writing into it.
    ///  3. ftrScanIsFingerPresent pFrameParameters is passed as IntPtr.Zero
    ///     (the SDK accepts null for this optional parameter).
    /// </summary>
    internal static class FutronicScanAPI
    {
        // Full path — Enrollment Kit 2025 version (works on modern Windows).
        // SDK 4.2's ftrScanAPI.dll is NOT used — it causes device failure.
        private const string DLL =
            @"C:\Users\Dale Barro\Downloads\FS6x_Enrollment_Kit_2025.11.06\" +
            @"FS6x_Enrollment_Kit_2025.11.06\ftrScanAPI.dll";

        // ── Device lifecycle ──────────────────────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ftrScanOpenDevice();

        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern void ftrScanCloseDevice(IntPtr handle);

        // ── Finger presence ───────────────────────────────────────────────────
        // pFrameParameters is optional — pass IntPtr.Zero to skip it.
        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int ftrScanIsFingerPresent(
            IntPtr handle,
            IntPtr pFrameParameters);  // PFTRSCAN_FRAME_PARAMETERS — pass IntPtr.Zero

        // ── Image size query ──────────────────────────────────────────────────
        // FTRSCAN_IMAGE_SIZE has exactly 3 int fields per the official header.
        // Missing nImageSize caused the AccessViolationException in prior builds.
        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int ftrScanGetImageSize(
            IntPtr handle,
            ref FTRSCAN_IMAGE_SIZE pImageSize);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FTRSCAN_IMAGE_SIZE
        {
            public int nWidth;
            public int nHeight;
            public int nImageSize;   // total bytes = nWidth * nHeight
        }

        // ── Image capture ─────────────────────────────────────────────────────
        // pBuffer must be pinned — the DLL writes directly into native memory.
        // We pin via GCHandle in the helper below rather than declaring byte[].
        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern int ftrScanGetImage(
            IntPtr handle,
            int    nDose,
            IntPtr pBuffer);         // pinned byte[] passed as raw pointer

        // ── Error retrieval ───────────────────────────────────────────────────
        // The SDK has its own error function — prefer this over GetLastError().
        [DllImport(DLL, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ftrScanGetLastError();

        public static string LastErrorDescription()
        {
            uint code = ftrScanGetLastError();
            return code switch
            {
                0          => "Success",
                8          => "Not enough memory",
                21         => "Device not ready — replug USB",
                50         => "Not supported",
                87         => "Invalid parameter — wrong buffer size or dose value",
                120        => "Call not implemented",
                1460       => "Timeout — no finger detected",
                1610       => "Bad configuration",
                0x20000001 => "Movable finger — hold still",
                0x20000002 => "No frame captured",
                0x20000003 => "User cancelled",
                0x20000004 => "Hardware incompatible",
                0x20000005 => "Firmware incompatible",
                4306       => "Empty frame — no finger on sensor",
                _          => $"ftrScan error 0x{code:X8}"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Queries the actual image dimensions from the device.
        /// Returns the nImageSize field directly — this is the exact buffer
        /// size the DLL expects for ftrScanGetImage.
        /// </summary>
        public static bool TryGetImageSize(IntPtr handle,
            out int width, out int height, out int totalBytes)
        {
            var sz = new FTRSCAN_IMAGE_SIZE();
            int ok = ftrScanGetImageSize(handle, ref sz);
            if (ok != 0 && sz.nWidth > 0 && sz.nHeight > 0)
            {
                width      = sz.nWidth;
                height     = sz.nHeight;
                // nImageSize is the authoritative buffer size — use it directly.
                // Do NOT compute width*height; they may differ due to padding.
                totalBytes = sz.nImageSize > 0 ? sz.nImageSize : sz.nWidth * sz.nHeight;
                return true;
            }
            width = height = totalBytes = 0;
            return false;
        }

        /// <summary>
        /// Captures one raw 8-bit grayscale image into a pre-allocated buffer.
        /// The buffer is GCHandle-pinned during the call.
        /// Tries dose=4 first (standard Futronic example value), then dose=0.
        /// </summary>
        public static bool CaptureImage(IntPtr handle, byte[] buffer)
        {
            var pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = pin.AddrOfPinnedObject();
                // dose=4 is the value used in Futronic's own C++ examples
                int result = ftrScanGetImage(handle, 4, ptr);
                if (result != 0) return true;
                // fallback: dose=0 (auto)
                result = ftrScanGetImage(handle, 0, ptr);
                return result != 0;
            }
            finally
            {
                pin.Free();
            }
        }

        /// <summary>
        /// Waits up to timeoutMs for a finger on the sensor.
        /// Returns true when detected, false on timeout or cancellation.
        /// </summary>
        public static bool WaitForFinger(IntPtr handle, int timeoutMs,
            CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (ftrScanIsFingerPresent(handle, IntPtr.Zero) != 0)
                    return true;
                Thread.Sleep(120);
            }
            return false;
        }
    }
}
