using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using SourceAFIS;

namespace FPTester
{
    internal class MainForm : Form
    {
        // ── Services ─────────────────────────────────────────────────────────
        private readonly LocalTemplateStore _store   = new();
        private readonly ScannerThread      _scanner = new();
        private CancellationTokenSource?    _cts;

        private const int    CAPTURE_TIMEOUT_MS = 15000;
        private const int    SCAN_DPI           = 500;
        private const double MATCH_THRESHOLD    = 40.0;

        // ── Palette ──────────────────────────────────────────────────────────
        private static readonly Color C_BG      = Color.FromArgb(15,  17,  23);
        private static readonly Color C_SURFACE = Color.FromArgb(26,  29,  38);
        private static readonly Color C_ACCENT  = Color.FromArgb(99, 179, 237);
        private static readonly Color C_GREEN   = Color.FromArgb(72, 199, 142);
        private static readonly Color C_RED     = Color.FromArgb(240, 100,  90);
        private static readonly Color C_YELLOW  = Color.FromArgb(246, 194,  62);
        private static readonly Color C_TEXT    = Color.FromArgb(220, 225, 235);
        private static readonly Color C_MUTED   = Color.FromArgb(120, 130, 150);
        private static readonly Color C_DOT_OFF = Color.FromArgb(40,  45,  58);

        // ── Controls ─────────────────────────────────────────────────────────
        private Label                lblDevice      = null!;
        private Button               btnConnect     = null!;
        private FingerprintVisualPanel fpVisual     = null!;
        private ProgressBar          pbQuality      = null!;
        private Label                lblQuality     = null!;
        private Label                lblStage       = null!;
        private Button               btnTabScan     = null!;
        private Button               btnTabEnroll   = null!;
        private Button               btnTabMatch    = null!;
        private Panel                pnlTabContent  = null!;
        private Panel                tabScan        = null!;
        private Panel                tabEnroll      = null!;
        private Panel                tabMatch       = null!;
        private Button               btnScanNow     = null!;
        private Button               btnCancelScan  = null!;
        private TextBox              txtSlotName    = null!;
        private TextBox              txtNotes       = null!;
        private Button               btnEnrollNow   = null!;
        private Button               btnCancelEnroll= null!;
        private Panel[]              dots           = null!;
        private Label                lblEnrollStatus= null!;
        private ListBox              lstSlots       = null!;
        private Button               btnMatchNow    = null!;
        private Button               btnCancelMatch = null!;
        private Button               btnDeleteSlot  = null!;
        private Label                lblMatchResult = null!;
        private RichTextBox          rtbLog         = null!;

        // ═════════════════════════════════════════════════════════════════════
        public MainForm()
        {
            BuildUI();
            WireEvents();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  UI Construction
        // ═════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            SuspendLayout();
            Text            = "FS64 Fingerprint Tester";
            Size            = new Size(760, 680);
            MinimumSize     = new Size(720, 640);
            BackColor       = C_BG;
            ForeColor       = C_TEXT;
            Font            = new Font("Segoe UI", 9.5f);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            // Header
            var hdr = MkPanel(0, 0, 760, 52, C_SURFACE);
            hdr.Controls.Add(Lbl("FS64  Fingerprint Test Utility", 18, 14,
                new Font("Segoe UI Semibold", 14f), C_TEXT));
            hdr.Controls.Add(Lbl(
                "ftrScanAPI (2025 Enrollment Kit)  +  SourceAFIS  •  Scan  •  Enroll  •  Match",
                20, 36, new Font("Segoe UI", 8.5f), C_MUTED));

            // Status bar
            var bar = MkPanel(0, 52, 760, 36, Color.FromArgb(20, 22, 30));
            lblDevice  = Lbl("● Not connected", 14, 10, new Font("Segoe UI", 9f), C_RED);
            btnConnect = MkBtn("Connect Scanner", 600, 4, 148, 28, C_ACCENT);
            bar.Controls.Add(lblDevice);
            bar.Controls.Add(btnConnect);

            // Left column: visualiser + quality + stage
            var left = MkPanel(16, 100, 200, 440, C_SURFACE);
            var cap  = Lbl("LIVE SENSOR", 0, 12,
                new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED);
            cap.Width = 200; cap.TextAlign = ContentAlignment.MiddleCenter;
            left.Controls.Add(cap);

            fpVisual = new FingerprintVisualPanel
                { Location = new Point(20, 34), Size = new Size(160, 200) };
            left.Controls.Add(fpVisual);

            var ql = Lbl("QUALITY", 0, 250,
                new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED);
            ql.Width = 200; ql.TextAlign = ContentAlignment.MiddleCenter;
            left.Controls.Add(ql);

            pbQuality = new ProgressBar
            {
                Location = new Point(20, 270), Size = new Size(160, 18),
                Minimum  = 0, Maximum = 100, Value = 0,
                Style    = ProgressBarStyle.Continuous
            };
            left.Controls.Add(pbQuality);

            lblQuality = Lbl("—", 20, 292,
                new Font("Segoe UI Semibold", 10f), C_MUTED);
            left.Controls.Add(lblQuality);

            lblStage = Lbl("Idle", 0, 325,
                new Font("Segoe UI", 8.5f), C_MUTED);
            lblStage.Width = 200; lblStage.Height = 80;
            lblStage.TextAlign = ContentAlignment.TopCenter;
            left.Controls.Add(lblStage);

            // Tab bar
            var tabs = MkPanel(232, 100, 512, 44, C_SURFACE);
            btnTabScan   = MkTabBtn("Scan Test",  4,  4, 160);
            btnTabEnroll = MkTabBtn("Enroll",   168,  4, 160);
            btnTabMatch  = MkTabBtn("Match",    332,  4, 160);
            tabs.Controls.AddRange(new Control[]
                { btnTabScan, btnTabEnroll, btnTabMatch });

            pnlTabContent = MkPanel(232, 144, 512, 400, C_SURFACE);

            BuildTabScan();
            BuildTabEnroll();
            BuildTabMatch();

            // Log
            Controls.Add(Lbl("ACTIVITY LOG", 16, 556,
                new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED));
            rtbLog = new RichTextBox
            {
                Location    = new Point(16, 574),
                Size        = new Size(728, 72),
                BackColor   = C_SURFACE, ForeColor = C_TEXT,
                ReadOnly    = true, BorderStyle = BorderStyle.None,
                Font        = new Font("Consolas", 8f),
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(rtbLog);

            ResumeLayout();
            ActivateTab(btnTabScan, tabScan);
        }

        private void BuildTabScan()
        {
            tabScan = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabScan.Controls.Add(Lbl("Scan Test", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabScan.Controls.Add(Lbl(
                "Captures one raw image from the FS64 sensor.\n" +
                "Nothing is saved — use this to confirm the hardware works.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED,
                w: 478, h: 36));

            btnScanNow    = MkBtn("▶  Start Scan", 16, 100, 140, 38, C_ACCENT);
            btnCancelScan = MkBtn("■  Cancel",     164, 100, 100, 38, C_YELLOW);
            btnCancelScan.Visible = false;
            tabScan.Controls.AddRange(new Control[] { btnScanNow, btnCancelScan });

            tabScan.Controls.Add(Lbl(
                "How to use:\n" +
                "  1. Click Start Scan\n" +
                "  2. The scanner light will turn on — place your finger firmly\n" +
                "  3. Hold still for 1–2 seconds until the image is captured\n" +
                "  4. Quality score appears in the left panel",
                16, 155, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 100));
        }

        private void BuildTabEnroll()
        {
            tabEnroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabEnroll.Controls.Add(Lbl("Enroll Fingerprint", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabEnroll.Controls.Add(Lbl(
                "Captures 3 scans and saves a SourceAFIS template to disk.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            tabEnroll.Controls.Add(Lbl("Slot name (e.g. Alice — Right Index)",
                16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtSlotName = new TextBox
            {
                Location    = new Point(16, 94), Size = new Size(280, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            tabEnroll.Controls.Add(txtSlotName);

            tabEnroll.Controls.Add(Lbl("Notes (optional)",
                16, 130, new Font("Segoe UI", 8.5f), C_MUTED));
            txtNotes = new TextBox
            {
                Location    = new Point(16, 148), Size = new Size(280, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            tabEnroll.Controls.Add(txtNotes);

            // 3 scan progress dots
            tabEnroll.Controls.Add(Lbl("Scan progress",
                16, 188, new Font("Segoe UI", 8.5f), C_MUTED));
            dots = new Panel[3];
            for (int i = 0; i < 3; i++)
            {
                var dot = new Panel
                {
                    Location  = new Point(16 + i * 72, 208),
                    Size      = new Size(54, 54),
                    BackColor = C_DOT_OFF
                };
                dot.Paint += DotPaint;
                dots[i]   = dot;
                tabEnroll.Controls.Add(dot);
                tabEnroll.Controls.Add(Lbl($"Scan {i + 1}",
                    22 + i * 72, 266,
                    new Font("Segoe UI", 8f), C_MUTED));
            }

            lblEnrollStatus = Lbl("Ready", 16, 292,
                new Font("Segoe UI", 9.5f), C_MUTED, w: 460, h: 22);
            tabEnroll.Controls.Add(lblEnrollStatus);

            btnEnrollNow    = MkBtn("▶  Start Enroll", 16,  326, 148, 38, C_GREEN);
            btnCancelEnroll = MkBtn("■  Cancel",       172, 326, 100, 38, C_YELLOW);
            btnCancelEnroll.Visible = false;
            tabEnroll.Controls.AddRange(new Control[]
                { btnEnrollNow, btnCancelEnroll });
        }

        private void BuildTabMatch()
        {
            tabMatch = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabMatch.Controls.Add(Lbl("Match / Verify", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabMatch.Controls.Add(Lbl(
                "Select a slot then scan to verify whether the finger matches.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));
            tabMatch.Controls.Add(Lbl("Enrolled slots",
                16, 76, new Font("Segoe UI", 8.5f), C_MUTED));

            lstSlots = new ListBox
            {
                Location    = new Point(16, 96), Size = new Size(280, 150),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            tabMatch.Controls.Add(lstSlots);

            btnMatchNow   = MkBtn("▶  Verify Match", 16,  256, 148, 38, C_ACCENT);
            btnCancelMatch= MkBtn("■  Cancel",       172, 256, 100, 38, C_YELLOW);
            btnDeleteSlot = MkBtn("🗑  Delete Slot",  280, 256, 120, 38, C_RED);
            btnCancelMatch.Visible = false;

            lblMatchResult = new Label
            {
                Location  = new Point(16, 306), Size = new Size(478, 52),
                Font      = new Font("Segoe UI Semibold", 13f),
                ForeColor = C_MUTED, Text = "",
                TextAlign = ContentAlignment.MiddleLeft
            };
            tabMatch.Controls.AddRange(new Control[]
                { btnMatchNow, btnCancelMatch, btnDeleteSlot, lblMatchResult });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Event wiring
        // ═════════════════════════════════════════════════════════════════════
        private void WireEvents()
        {
            btnConnect.Click += BtnConnect_Click;

            btnTabScan.Click   += (_, _) => ActivateTab(btnTabScan,   tabScan);
            btnTabEnroll.Click += (_, _) => ActivateTab(btnTabEnroll, tabEnroll);
            btnTabMatch.Click  += (_, _) =>
            {
                RefreshSlotList();
                ActivateTab(btnTabMatch, tabMatch);
            };

            btnScanNow.Click     += BtnScan_Click;
            btnCancelScan.Click  += (_, _) => CancelOp();
            btnEnrollNow.Click   += BtnEnroll_Click;
            btnCancelEnroll.Click+= (_, _) => CancelOp();
            btnMatchNow.Click    += BtnMatch_Click;
            btnCancelMatch.Click += (_, _) => CancelOp();
            btnDeleteSlot.Click  += BtnDelete_Click;

            FormClosing += (_, e) =>
            {
                _cts?.Cancel();
                // Synchronously close the device on the scanner thread before
                // the form is destroyed — avoids use-after-free crash on exit.
                // Dispose() calls CloseAsync().Wait() internally so this is safe.
                _scanner.Dispose();
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Connect / Disconnect
        // ═════════════════════════════════════════════════════════════════════
        private async void BtnConnect_Click(object? s, EventArgs e)
        {
            btnConnect.Enabled = false;
            try
            {
                if (_scanner.IsOpen)
                {
                    await _scanner.CloseAsync();
                    SetDeviceLabel(false);
                    Log("Scanner disconnected.", C_MUTED);
                }
                else
                {
                    SetStage("Connecting…");
                    var (ok, sizeInfo) = await _scanner.OpenAsync();
                    if (ok)
                    {
                        SetDeviceLabel(true);
                        Log($"Scanner ready — image: {sizeInfo}", C_GREEN);
                        SetStage("Ready");
                    }
                    else
                    {
                        SetDeviceLabel(false);
                        Log($"Connect failed: {sizeInfo}", C_RED);
                        SetStage("Connection failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Connect error: {ex.GetType().Name}: {ex.Message}", C_RED);
                SetStage("Error — see log.");
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Scan Test
        // ═════════════════════════════════════════════════════════════════════
        private async void BtnScan_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            _cts = new CancellationTokenSource();
            SetBusy(true, btnScanNow, btnCancelScan);
            UpdateQuality(0);
            try
            {
                var (image, err) = await _scanner.CaptureAsync(
                    CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger on scanner…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing image…")));

                if (image == null)
                {
                    SetStage($"Scan failed: {err}");
                    Log($"Scan failed: {err}", C_RED);
                }
                else
                {
                    long sum = 0;
                    foreach (byte b in image) sum += b;
                    int quality = Math.Clamp((int)(sum / image.Length) / 2, 0, 100);
                    UpdateQuality(quality);
                    SetStage("Scan captured ✓");
                    Log($"Scan OK — {image.Length / 1024} KB raw, brightness ~{quality}%",
                        QualityColor(quality));

                    // Save raw grayscale image as PNG to the Desktop
                    // so you can visually verify it captured a real fingerprint.
                    try
                    {
                        string desktop = Environment.GetFolderPath(
                            Environment.SpecialFolder.Desktop);
                        string path = System.IO.Path.Combine(desktop,
                            $"FPScan_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                        SaveGrayscalePng(image, _scanner.ImageWidth,
                            _scanner.ImageHeight, path);
                        Log($"Image saved → Desktop\\{System.IO.Path.GetFileName(path)}",
                            C_ACCENT);
                    }
                    catch (Exception imgEx)
                    {
                        Log($"Image save failed: {imgEx.Message}", C_YELLOW);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStage("Cancelled.");
                Log("Scan cancelled.", C_MUTED);
            }
            catch (Exception ex)
            {
                SetStage("Scan error — see log.");
                Log($"Scan error: {ex.GetType().Name}: {ex.Message}", C_RED);
            }
            finally
            {
                SetBusy(false, btnScanNow, btnCancelScan);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Enroll — 3 captures → SourceAFIS template → disk
        // ═════════════════════════════════════════════════════════════════════
        private async void BtnEnroll_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;

            string slotName = txtSlotName.Text.Trim();
            if (string.IsNullOrEmpty(slotName))
            {
                Log("Enter a slot name first.", C_YELLOW);
                txtSlotName.Focus();
                return;
            }
            if (_store.Exists(slotName))
            {
                var r = MessageBox.Show($"Slot '{slotName}' already exists. Overwrite?",
                    "Overwrite?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            _cts = new CancellationTokenSource();
            SetBusy(true, btnEnrollNow, btnCancelEnroll);
            ResetDots();
            SetEnrollStatus("Starting enrollment…");
            UpdateQuality(0);

            try
            {
                FingerprintTemplate? bestTemplate = null;
                string errorMsg = "";
                int retries = 0;
                const int MAX_RETRIES = 5;

                for (int scan = 0; scan < 3; scan++)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    int i = scan;
                    SetEnrollStatus($"Scan {i + 1} of 3 — place finger on the scanner…");

                    var (image, err) = await _scanner.CaptureAsync(
                        CAPTURE_TIMEOUT_MS, _cts.Token,
                        onWaiting:   () => SafeInvoke(() =>
                            SetEnrollStatus($"Scan {i + 1} of 3 — place finger on the scanner…")),
                        onCapturing: () => SafeInvoke(() =>
                            SetEnrollStatus($"Scan {i + 1} of 3 — hold still…")));

                    if (image == null)
                    {
                        if (_cts.Token.IsCancellationRequested) break;
                        retries++;
                        Log($"Scan {i + 1} capture failed: {err} (retry {retries}/{MAX_RETRIES})", C_YELLOW);
                        if (retries >= MAX_RETRIES)
                        {
                            errorMsg = $"Too many failures: {err}";
                            break;
                        }
                        scan--;
                        continue;
                    }
                    retries = 0; // reset retry counter on success

                    try
                    {
                        var fpImg    = new FingerprintImage(
                            _scanner.ImageWidth, _scanner.ImageHeight, image,
                            new FingerprintImageOptions { Dpi = SCAN_DPI });
                        var template = new FingerprintTemplate(fpImg);
                        bestTemplate = template;

                        LightDot(i, C_GREEN);
                        long sum = 0;
                        foreach (byte b in image) sum += b;
                        UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));
                        Log($"Scan {i + 1}/3 extracted successfully.", C_GREEN);
                    }
                    catch (Exception ex)
                    {
                        Log($"Scan {i + 1} feature extraction failed: {ex.Message} — retrying…",
                            C_YELLOW);
                        LightDot(i, C_RED);
                        scan--;
                        continue;
                    }

                    if (scan < 2)
                    {
                        SetEnrollStatus("Lift finger, then place again for next scan…");
                        await System.Threading.Tasks.Task.Delay(900, _cts.Token)
                            .ContinueWith(_ => { });
                    }
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    SetEnrollStatus("Enrollment cancelled.");
                    Log("Enrollment cancelled by user.", C_MUTED);
                }
                else if (bestTemplate != null)
                {
                    byte[] bytes = bestTemplate.ToByteArray();
                    bool   saved = _store.Save(slotName, bytes, txtNotes.Text.Trim());
                    if (saved)
                    {
                        SetEnrollStatus($"✓ Enrolled '{slotName}'  ({bytes.Length} bytes)");
                        Log($"Enrolled '{slotName}'  template {bytes.Length} bytes", C_GREEN);
                        txtSlotName.Clear();
                        txtNotes.Clear();
                    }
                    else
                    {
                        SetEnrollStatus("Captured but disk save failed.");
                        Log("Save failed — check disk permissions.", C_RED);
                    }
                }
                else
                {
                    SetEnrollStatus($"✗ Enrollment failed: {errorMsg}");
                    Log($"Enrollment failed: {errorMsg}", C_RED);
                }
            }
            catch (OperationCanceledException)
            {
                SetEnrollStatus("Enrollment cancelled.");
                Log("Enrollment cancelled.", C_MUTED);
            }
            catch (Exception ex)
            {
                SetEnrollStatus("Enrollment error — see log.");
                Log($"Enrollment error: {ex.GetType().Name}: {ex.Message}", C_RED);
            }
            finally
            {
                SetBusy(false, btnEnrollNow, btnCancelEnroll);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Match — live scan vs stored SourceAFIS template
        // ═════════════════════════════════════════════════════════════════════
        private async void BtnMatch_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            if (lstSlots.SelectedItem == null)
            {
                Log("Select a slot from the list first.", C_YELLOW); return;
            }

            if (lstSlots.SelectedItem is not SlotListItem selectedSlot)
            {
                Log("Select a slot from the list first.", C_YELLOW); return;
            }

            string  slotName = selectedSlot.SlotName;
            byte[]? stored   = _store.Load(slotName);
            if (stored == null) { Log("Could not load template.", C_RED); return; }

            _cts = new CancellationTokenSource();
            SetBusy(true, btnMatchNow, btnCancelMatch);
            lblMatchResult.Text      = "";
            lblMatchResult.ForeColor = C_MUTED;
            UpdateQuality(0);

            try
            {
                var (image, err) = await _scanner.CaptureAsync(
                    CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger to verify…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing…")));

                if (image == null)
                {
                    SetStage("Capture failed.");
                    Log($"Match capture failed: {err}", C_RED);
                }
                else
                {
                    var fpImg     = new FingerprintImage(
                        _scanner.ImageWidth, _scanner.ImageHeight, image,
                        new FingerprintImageOptions { Dpi = SCAN_DPI });
                    var probe     = new FingerprintTemplate(fpImg);
                    var candidate = new FingerprintTemplate(stored);
                    double score  = new FingerprintMatcher(probe).Match(candidate);
                    bool   match  = score >= MATCH_THRESHOLD;

                    long sum = 0;
                    foreach (byte b in image) sum += b;
                    UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));

                    if (match)
                    {
                        lblMatchResult.Text      = $"✓  MATCH   '{slotName}'   score {score:F1}";
                        lblMatchResult.ForeColor = C_GREEN;
                        SetStage("Match confirmed ✓");
                        Log($"MATCH ✓  '{slotName}'  score {score:F1} / threshold {MATCH_THRESHOLD}",
                            C_GREEN);
                    }
                    else
                    {
                        lblMatchResult.Text      = $"✗  NO MATCH   '{slotName}'   score {score:F1}";
                        lblMatchResult.ForeColor = C_RED;
                        SetStage("No match ✗");
                        Log($"NO MATCH  '{slotName}'  score {score:F1} / threshold {MATCH_THRESHOLD}",
                            C_RED);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStage("Cancelled.");
                Log("Match cancelled.", C_MUTED);
            }
            catch (Exception ex)
            {
                SetStage("Match error — see log.");
                Log($"Match error: {ex.GetType().Name}: {ex.Message}", C_RED);
            }
            finally
            {
                SetBusy(false, btnMatchNow, btnCancelMatch);
            }
        }

        private void BtnDelete_Click(object? s, EventArgs e)
        {
            if (lstSlots.SelectedItem is not SlotListItem selectedSlot) return;
            string name = selectedSlot.SlotName;
            if (MessageBox.Show($"Delete slot '{name}'?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes) return;
            _store.Delete(name);
            RefreshSlotList();
            Log($"Deleted slot '{name}'.", C_MUTED);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Helpers
        // ═════════════════════════════════════════════════════════════════════
        private void CancelOp()
        {
            _cts?.Cancel();
            SetStage("Cancelling…");
            Log("Operation cancelled.", C_MUTED);
        }

        private bool GuardDevice()
        {
            if (_scanner.IsOpen) return true;
            Log("Scanner not connected. Click Connect Scanner first.", C_YELLOW);
            return false;
        }

        private void ActivateTab(Button tab, Panel content)
        {
            pnlTabContent.Controls.Clear();
            pnlTabContent.Controls.Add(content);
            foreach (var b in new[] { btnTabScan, btnTabEnroll, btnTabMatch })
            {
                b.BackColor = b == tab ? C_ACCENT : C_BG;
                b.ForeColor = b == tab ? C_BG     : C_MUTED;
            }
        }

        private void RefreshSlotList()
        {
            lstSlots.Items.Clear();
            foreach (var name in _store.SlotNames())
            {
                // Store the real slot name in Tag — display string has date appended
                // so we cannot use .ToString() directly for storage lookups.
                var item = new SlotListItem(name,
                    $"{name}  [{_store.All[name].EnrolledAt:MM/dd HH:mm}]");
                lstSlots.Items.Add(item);
            }
        }

        private void SetDeviceLabel(bool on)
        {
            if (InvokeRequired) { Invoke(() => SetDeviceLabel(on)); return; }
            lblDevice.Text      = on ? "● Scanner ready" : "● Not connected";
            lblDevice.ForeColor = on ? C_GREEN : C_RED;
            btnConnect.Text     = on ? "Disconnect"      : "Connect Scanner";
        }

        private void SetBusy(bool busy, Button primary, Button cancel)
        {
            if (InvokeRequired) { Invoke(() => SetBusy(busy, primary, cancel)); return; }
            primary.Enabled    = !busy;
            cancel.Visible     = busy;
            btnConnect.Enabled = !busy;
            Cursor             = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void UpdateQuality(int q)
        {
            if (InvokeRequired) { Invoke(() => UpdateQuality(q)); return; }
            pbQuality.Value      = Math.Clamp(q, 0, 100);
            lblQuality.Text      = q == 0 ? "—" : $"{q}%";
            lblQuality.ForeColor = QualityColor(q);
            fpVisual.Quality     = q;
            fpVisual.Invalidate();
        }

        private void SetStage(string msg)
        {
            if (InvokeRequired) { Invoke(() => SetStage(msg)); return; }
            lblStage.Text = msg;
        }

        private void SetEnrollStatus(string msg)
        {
            if (InvokeRequired) { Invoke(() => SetEnrollStatus(msg)); return; }
            lblEnrollStatus.Text = msg;
        }

        private void LightDot(int i, Color c)
        {
            if (InvokeRequired) { Invoke(() => LightDot(i, c)); return; }
            dots[i].BackColor = c;
            dots[i].Invalidate();
        }

        private void ResetDots()
        {
            if (InvokeRequired) { Invoke(ResetDots); return; }
            foreach (var d in dots) { d.BackColor = C_DOT_OFF; d.Invalidate(); }
        }

        private static Color QualityColor(int q) =>
            q >= 65 ? C_GREEN : q >= 40 ? C_YELLOW : C_RED;

        private void Log(string msg, Color? c = null)
        {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(() => Log(msg, c)); return; }
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionColor  = c ?? C_TEXT;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  {msg}\n");
            rtbLog.ScrollToCaret();
        }

        private void SafeInvoke(Action a)
        {
            if (InvokeRequired) Invoke(a); else a();
        }

        /// <summary>
        /// Converts a raw 8-bit grayscale byte array to a PNG file.
        /// Each byte is one pixel (0=black, 255=white).
        /// </summary>
        private static void SaveGrayscalePng(byte[] pixels, int width, int height, string path)
        {
            using var bmp = new System.Drawing.Bitmap(width, height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* dst = (byte*)data.Scan0;
                    for (int i = 0; i < pixels.Length && i < width * height; i++)
                    {
                        byte v = pixels[i];
                        int  o = i * 4;
                        dst[o + 0] = v; // B
                        dst[o + 1] = v; // G
                        dst[o + 2] = v; // R
                        dst[o + 3] = 255; // A
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }

        private static void DotPaint(object? s, PaintEventArgs e)
        {
            if (s is not Panel p) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillEllipse(new SolidBrush(p.BackColor), 2, 2, p.Width-4, p.Height-4);
            g.DrawEllipse(new Pen(Color.FromArgb(80,90,110), 1.5f),
                2, 2, p.Width-5, p.Height-5);
        }

        // ── Control factories ─────────────────────────────────────────────────
        private Panel MkPanel(int x, int y, int w, int h, Color bg)
        {
            var p = new Panel
                { Location=new Point(x,y), Size=new Size(w,h), BackColor=bg };
            Controls.Add(p);
            return p;
        }

        private static Label Lbl(string text, int x, int y,
            Font? font=null, Color? color=null, int w=0, int h=0)
        {
            var l = new Label
            {
                Text      = text, Location = new Point(x, y),
                AutoSize  = (w == 0),
                Font      = font ?? SystemFonts.DefaultFont,
                ForeColor = color ?? Color.White
            };
            if (w > 0) l.Width  = w;
            if (h > 0) l.Height = h;
            return l;
        }

        private static Button MkBtn(string t, int x, int y, int w, int h, Color bg) =>
            new() { Text=t, Location=new Point(x,y), Size=new Size(w,h),
                    BackColor=bg,
                    ForeColor=bg.GetBrightness()>0.5f
                        ?Color.FromArgb(15,17,23):Color.White,
                    FlatStyle=FlatStyle.Flat,
                    Font=new Font("Segoe UI Semibold",9.5f), Cursor=Cursors.Hand };

        private static Button MkTabBtn(string t, int x, int y, int w) =>
            new() { Text=t, Location=new Point(x,y), Size=new Size(w,36),
                    BackColor=Color.FromArgb(15,17,23),
                    ForeColor=Color.FromArgb(120,130,150),
                    FlatStyle=FlatStyle.Flat,
                    Font=new Font("Segoe UI Semibold",9.5f), Cursor=Cursors.Hand };
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Fingerprint visual panel
    // ═════════════════════════════════════════════════════════════════════════
    internal class FingerprintVisualPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Quality { get; set; } = 0;

        public FingerprintVisualPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(15, 17, 23);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int cx = Width/2, cy = Height/2;
            int r  = Math.Min(Width, Height)/2 - 8;

            Color ring = Quality == 0 ? Color.FromArgb(50,55,70)
                : Quality >= 65 ? Color.FromArgb(72,199,142)
                : Quality >= 40 ? Color.FromArgb(246,194,62)
                : Color.FromArgb(240,100,90);

            g.DrawEllipse(new Pen(ring, 3f), cx-r, cy-r, r*2, r*2);
            g.FillEllipse(new SolidBrush(Color.FromArgb(26,29,38)),
                cx-r+6, cy-r+6, (r-6)*2, (r-6)*2);

            if (Quality == 0)
            {
                var rp = new Pen(Color.FromArgb(45,50,65), 1.2f);
                for (int i = 1; i <= 5; i++)
                    g.DrawArc(rp, cx-i*10, cy-i*10+10, i*20, i*20, 200, 140);
                g.DrawString("PLACE\nFINGER",
                    new Font("Segoe UI", 7.5f),
                    new SolidBrush(Color.FromArgb(70,80,95)),
                    new RectangleF(cx-28, cy-14, 56, 30),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
            else
            {
                g.DrawString($"{Quality}%",
                    new Font("Segoe UI Semibold", 18f),
                    new SolidBrush(ring),
                    new RectangleF(cx-35, cy-18, 70, 28),
                    new StringFormat { Alignment = StringAlignment.Center });
                g.DrawString("quality",
                    new Font("Segoe UI", 7.5f),
                    new SolidBrush(Color.FromArgb(120,130,150)),
                    new RectangleF(cx-30, cy+12, 60, 16),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }
    }

    // ── ListBox item that keeps the real slot name separate from display text ──
    internal class SlotListItem
    {
        public string SlotName    { get; }
        private readonly string _display;
        public SlotListItem(string slotName, string display)
        {
            SlotName = slotName;
            _display = display;
        }
        public override string ToString() => _display;
    }
}
