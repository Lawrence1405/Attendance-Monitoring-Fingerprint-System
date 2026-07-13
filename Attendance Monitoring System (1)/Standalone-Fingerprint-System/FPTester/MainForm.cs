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
        private readonly WebSocketServer    _wsServer;
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

        // ── Template → Image tab ──────────────────────────────────────────────
        private Panel      tabTplToImg       = null!;
        private ComboBox   cmbTplSlot        = null!;
        private Button     btnRefreshSlots   = null!;
        private TextBox    txtExportFolder   = null!;
        private Button     btnBrowseFolder   = null!;
        private TextBox    txtExportFileName = null!;
        private Button     btnExportImg      = null!;
        private Label      lblExportStatus   = null!;
        private PictureBox picExportPreview  = null!;
        private Button     btnTabTplToImg    = null!;

        // ── Image → Template tab ──────────────────────────────────────────────
        private Panel      tabImgToTpl      = null!;
        private TextBox    txtImgPath       = null!;
        private TextBox    txtImgSlotName   = null!;
        private Button     btnBrowseImg     = null!;
        private Button     btnConvertImg    = null!;
        private Label      lblConvertStatus = null!;
        private PictureBox picPreview       = null!;
        private Button     btnTabImgToTpl   = null!;

        // ── 1:N Identify tab ──────────────────────────────────────────────────
        private Panel      tabIdentify        = null!;
        private Button     btnTabIdentify     = null!;
        private Button     btnIdentifyNow     = null!;
        private Button     btnCancelIdentify  = null!;
        private RichTextBox rtbIdentifyResults = null!;
        private Label      lblIdentifyStatus  = null!;

        // ═════════════════════════════════════════════════════════════════════
        public MainForm()
        {
            BuildUI();
            WireEvents();
            _wsServer = new WebSocketServer(_scanner);
            // Start the WebSocket server in background — catch any errors
            // so they don't crash the GUI application
            Task.Run(async () =>
            {
                try { await _wsServer.StartAsync(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[FPTester] WebSocket server error: {ex.Message}");
                }
            });
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
            btnTabScan      = MkTabBtn("Scan",        4,   4,  78);
            btnTabEnroll    = MkTabBtn("Enroll",     86,   4,  78);
            btnTabMatch     = MkTabBtn("Match",     168,   4,  78);
            btnTabIdentify  = MkTabBtn("1:N",       250,   4,  60);
            btnTabImgToTpl  = MkTabBtn("Img→Tpl",  314,   4,  96);
            btnTabTplToImg  = MkTabBtn("Tpl→Img",  414,   4,  96);
            tabs.Controls.AddRange(new Control[]
                { btnTabScan, btnTabEnroll, btnTabMatch, btnTabIdentify,
                  btnTabImgToTpl, btnTabTplToImg });

            pnlTabContent = MkPanel(232, 144, 512, 400, C_SURFACE);

            BuildTabScan();
            BuildTabEnroll();
            BuildTabMatch();
            BuildTabImgToTpl();
            BuildTabTplToImg();
            BuildTabIdentify();

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
        //  Tab: Image → Template
        // ═════════════════════════════════════════════════════════════════════
        private void BuildTabImgToTpl()
        {
            tabImgToTpl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabImgToTpl.Controls.Add(Lbl("Image → Template", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabImgToTpl.Controls.Add(Lbl(
                "Convert a saved fingerprint PNG into a template slot for verification.\n" +
                "Works with images saved from Scan Test or Enrollment.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 36));

            // Image file picker
            tabImgToTpl.Controls.Add(Lbl("PNG image file",
                16, 90, new Font("Segoe UI", 8.5f), C_MUTED));
            txtImgPath = new TextBox
            {
                Location    = new Point(16, 108), Size = new Size(330, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = true,
                Text        = "No file selected"
            };
            btnBrowseImg = MkBtn("Browse…", 354, 108, 90, 28, C_ACCENT);
            tabImgToTpl.Controls.Add(txtImgPath);
            tabImgToTpl.Controls.Add(btnBrowseImg);

            // Preview
            tabImgToTpl.Controls.Add(Lbl("Preview",
                16, 144, new Font("Segoe UI", 8.5f), C_MUTED));
            picPreview = new PictureBox
            {
                Location  = new Point(16, 162),
                Size      = new Size(160, 130),
                BackColor = C_BG,
                SizeMode  = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            tabImgToTpl.Controls.Add(picPreview);

            // Slot name and convert
            tabImgToTpl.Controls.Add(Lbl("Save as slot name",
                190, 162, new Font("Segoe UI", 8.5f), C_MUTED));
            txtImgSlotName = new TextBox
            {
                Location    = new Point(190, 180), Size = new Size(260, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. Alice — Right Index (from image)"
            };
            tabImgToTpl.Controls.Add(txtImgSlotName);

            tabImgToTpl.Controls.Add(Lbl(
                "The image will be read as a grayscale fingerprint\n" +
                "at 500 dpi and converted to a SourceAFIS template.",
                190, 216, new Font("Segoe UI", 8.5f), C_MUTED, w: 260, h: 44));

            btnConvertImg = MkBtn("▶  Convert to Template", 190, 268, 200, 38, C_GREEN);
            tabImgToTpl.Controls.Add(btnConvertImg);

            lblConvertStatus = Lbl("Select a PNG image to begin.", 16, 310,
                new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 44);
            tabImgToTpl.Controls.Add(lblConvertStatus);
        }
        // ═════════════════════════════════════════════════════════════════════
        //  Tab: Template → Image
        // ═════════════════════════════════════════════════════════════════════
        private void BuildTabTplToImg()
        {
            tabTplToImg = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabTplToImg.Controls.Add(Lbl("Template → Image", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabTplToImg.Controls.Add(Lbl(
                "Reconstruct a fingerprint image from a stored template slot\n" +
                "and save it as a PNG to any folder you choose.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 36));

            // Slot picker
            tabTplToImg.Controls.Add(Lbl("Template slot",
                16, 90, new Font("Segoe UI", 8.5f), C_MUTED));
            cmbTplSlot = new ComboBox
            {
                Location      = new Point(16, 108), Size = new Size(300, 28),
                BackColor     = C_BG, ForeColor = C_TEXT,
                Font          = new Font("Segoe UI", 9.5f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat
            };
            btnRefreshSlots = MkBtn("⟳  Refresh", 324, 108, 100, 28, C_ACCENT);
            tabTplToImg.Controls.Add(cmbTplSlot);
            tabTplToImg.Controls.Add(btnRefreshSlots);

            // Output folder
            tabTplToImg.Controls.Add(Lbl("Save to folder",
                16, 146, new Font("Segoe UI", 8.5f), C_MUTED));
            txtExportFolder = new TextBox
            {
                Location    = new Point(16, 164), Size = new Size(330, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly    = true,
                Text        = Environment.GetFolderPath(
                                  Environment.SpecialFolder.Desktop)
            };
            btnBrowseFolder = MkBtn("Browse…", 354, 164, 90, 28, C_ACCENT);
            tabTplToImg.Controls.Add(txtExportFolder);
            tabTplToImg.Controls.Add(btnBrowseFolder);

            // Output filename
            tabTplToImg.Controls.Add(Lbl("File name (without extension)",
                16, 200, new Font("Segoe UI", 8.5f), C_MUTED));
            txtExportFileName = new TextBox
            {
                Location    = new Point(16, 218), Size = new Size(280, 28),
                BackColor   = C_BG, ForeColor = C_TEXT,
                Font        = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. Alice_RightIndex_reconstructed"
            };
            tabTplToImg.Controls.Add(txtExportFileName);
            tabTplToImg.Controls.Add(Lbl(".png", 302, 226,
                new Font("Segoe UI", 9f), C_MUTED));

            // Preview area
            tabTplToImg.Controls.Add(Lbl("Preview (reconstructed)",
                370, 90, new Font("Segoe UI", 8.5f), C_MUTED));
            picExportPreview = new PictureBox
            {
                Location    = new Point(370, 108),
                Size        = new Size(120, 150),
                BackColor   = C_BG,
                SizeMode    = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            tabTplToImg.Controls.Add(picExportPreview);

            // Export button
            btnExportImg = MkBtn("💾  Export as PNG", 16, 262, 180, 38, C_GREEN);
            tabTplToImg.Controls.Add(btnExportImg);

            lblExportStatus = Lbl("Select a template slot to begin.", 16, 312,
                new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 60);
            tabTplToImg.Controls.Add(lblExportStatus);
        }

        private void WireEvents()
        {
            btnConnect.Click += BtnConnect_Click;

            btnTabScan.Click   += (_, _) => ActivateTab(btnTabScan,      tabScan);
            btnTabEnroll.Click += (_, _) => ActivateTab(btnTabEnroll,    tabEnroll);
            btnTabMatch.Click  += (_, _) =>
            {
                RefreshSlotList();
                ActivateTab(btnTabMatch, tabMatch);
            };
            btnTabIdentify.Click += (_, _) => ActivateTab(btnTabIdentify, tabIdentify);
            btnTabImgToTpl.Click += (_, _) => ActivateTab(btnTabImgToTpl, tabImgToTpl);
            btnTabTplToImg.Click += (_, _) =>
            {
                RefreshExportSlots();
                ActivateTab(btnTabTplToImg, tabTplToImg);
            };

            btnScanNow.Click     += BtnScan_Click;
            btnCancelScan.Click  += (_, _) => CancelOp();
            btnEnrollNow.Click   += BtnEnroll_Click;
            btnCancelEnroll.Click+= (_, _) => CancelOp();
            btnMatchNow.Click    += BtnMatch_Click;
            btnCancelMatch.Click += (_, _) => CancelOp();
            btnDeleteSlot.Click  += BtnDelete_Click;
            btnIdentifyNow.Click    += BtnIdentify_Click;
            btnCancelIdentify.Click += (_, _) => CancelOp();
            btnBrowseImg.Click   += BtnBrowseImg_Click;
            btnConvertImg.Click  += BtnConvertImg_Click;
            btnBrowseFolder.Click+= BtnBrowseFolder_Click;
            btnRefreshSlots.Click+= (_, _) => RefreshExportSlots();
            btnExportImg.Click   += BtnExportImg_Click;
            cmbTplSlot.SelectedIndexChanged += (_, _) =>
            {
                // Auto-fill filename from slot name when selection changes
                if (cmbTplSlot.SelectedItem is SlotListItem item)
                {
                    string safe = string.Concat(
                        item.SlotName.Split(System.IO.Path.GetInvalidFileNameChars()));
                    txtExportFileName.Text = $"FP_{safe}";
                    lblExportStatus.Text      = $"Ready to export '{item.SlotName}'.";
                    lblExportStatus.ForeColor = C_MUTED;
                    picExportPreview.Image    = null;
                }
            };

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
                bool uiThinksItsConnected = (btnConnect.Text == "Disconnect");

                if (_scanner.IsOpen && uiThinksItsConnected)
                {
                    // The user explicitly wants to disconnect
                    // We must tell the WebSocket server to stop scanning first, 
                    // otherwise CloseAsync might queue behind a 15-second CaptureAsync block!
                    _wsServer.StopAllScans(); 
                    await _scanner.CloseAsync();
                    SetDeviceLabel(false);
                    Log("Scanner disconnected.", C_MUTED);
                }
                else if (_scanner.IsOpen && !uiThinksItsConnected)
                {
                    // The WebSocket server already opened it in the background!
                    // Just update the GUI to reflect the true state.
                    SetDeviceLabel(true);
                    Log("Scanner is already connected by the web service.", C_GREEN);
                    SetStage("Ready");
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

                        // Save each scan as a PNG so you can visually verify capture quality
                        try
                        {
                            string desktop = Environment.GetFolderPath(
                                Environment.SpecialFolder.Desktop);
                            string safeName = string.Concat(slotName.Split(
                                System.IO.Path.GetInvalidFileNameChars()));
                            string path = System.IO.Path.Combine(desktop,
                                $"FPEnroll_{safeName}_scan{i + 1}_{DateTime.Now:HHmmss}.png");
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
        //  Tab: 1:N Identify — UI builder
        // ═════════════════════════════════════════════════════════════════════
        private void BuildTabIdentify()
        {
            tabIdentify = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            tabIdentify.Controls.Add(Lbl("1:N  Identify", 16, 16,
                new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabIdentify.Controls.Add(Lbl(
                "Scan a finger once and compare it against ALL enrolled templates.\n" +
                "Every match above the threshold is listed, best match highlighted.",
                16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 36));

            btnIdentifyNow    = MkBtn("▶  Identify Finger", 16, 100, 170, 38, C_ACCENT);
            btnCancelIdentify = MkBtn("■  Cancel",          194, 100, 100, 38, C_YELLOW);
            btnCancelIdentify.Visible = false;
            tabIdentify.Controls.AddRange(new Control[] { btnIdentifyNow, btnCancelIdentify });

            lblIdentifyStatus = Lbl("Ready — press Identify Finger to scan.", 16, 148,
                new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 22);
            tabIdentify.Controls.Add(lblIdentifyStatus);

            tabIdentify.Controls.Add(Lbl("Results",
                16, 176, new Font("Segoe UI", 8.5f), C_MUTED));
            rtbIdentifyResults = new RichTextBox
            {
                Location    = new Point(16, 196),
                Size        = new Size(478, 180),
                BackColor   = C_BG,
                ForeColor   = C_TEXT,
                ReadOnly    = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Consolas", 9f),
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };
            tabIdentify.Controls.Add(rtbIdentifyResults);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  1:N Identify — scan once, match against all stored templates
        // ═════════════════════════════════════════════════════════════════════
        private async void BtnIdentify_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;

            var slotNames = _store.SlotNames();
            if (slotNames.Count == 0)
            {
                Log("No enrolled templates — enroll at least one fingerprint first.", C_YELLOW);
                lblIdentifyStatus.Text      = "No templates enrolled.";
                lblIdentifyStatus.ForeColor = C_YELLOW;
                return;
            }

            _cts = new CancellationTokenSource();
            SetBusy(true, btnIdentifyNow, btnCancelIdentify);
            lblIdentifyStatus.Text      = "Place finger on scanner…";
            lblIdentifyStatus.ForeColor = C_MUTED;
            rtbIdentifyResults.Clear();
            UpdateQuality(0);

            try
            {
                // --- Capture ---
                var (image, err) = await _scanner.CaptureAsync(
                    CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger to identify…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing…")));

                if (image == null)
                {
                    SetStage("Capture failed.");
                    Log($"Identify capture failed: {err}", C_RED);
                    lblIdentifyStatus.Text      = $"Capture failed: {err}";
                    lblIdentifyStatus.ForeColor = C_RED;
                    return;
                }

                // Quality feedback
                long sum = 0;
                foreach (byte b in image) sum += b;
                UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));

                // Build probe template from the scanned image
                var fpImg = new FingerprintImage(
                    _scanner.ImageWidth, _scanner.ImageHeight, image,
                    new FingerprintImageOptions { Dpi = SCAN_DPI });
                var probe   = new FingerprintTemplate(fpImg);
                var matcher = new FingerprintMatcher(probe);

                // --- Match against every stored template ---
                lblIdentifyStatus.Text = $"Comparing against {slotNames.Count} template(s)…";
                var results = new System.Collections.Generic.List<(string Name, double Score)>();

                foreach (var name in slotNames)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    byte[]? tplBytes = _store.Load(name);
                    if (tplBytes == null) continue;

                    try
                    {
                        var candidate = new FingerprintTemplate(tplBytes);
                        double score  = matcher.Match(candidate);
                        if (score >= MATCH_THRESHOLD)
                            results.Add((name, score));
                    }
                    catch (Exception ex)
                    {
                        Log($"1:N — could not match '{name}': {ex.Message}", C_YELLOW);
                    }
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    SetStage("Cancelled.");
                    Log("1:N identify cancelled.", C_MUTED);
                    lblIdentifyStatus.Text      = "Cancelled.";
                    lblIdentifyStatus.ForeColor = C_MUTED;
                    return;
                }

                // --- Sort by score descending and display ---
                results.Sort((a, b) => b.Score.CompareTo(a.Score));

                rtbIdentifyResults.Clear();

                if (results.Count == 0)
                {
                    lblIdentifyStatus.Text      =
                        $"✗  No matches found in {slotNames.Count} template(s).";
                    lblIdentifyStatus.ForeColor = C_RED;
                    SetStage("No match ✗");
                    Log($"1:N identify — no match (0/{slotNames.Count} above threshold {MATCH_THRESHOLD})",
                        C_RED);

                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.AppendText(
                        $"Scanned against {slotNames.Count} enrolled template(s).\n" +
                        $"Threshold: {MATCH_THRESHOLD}\n\n" +
                        "No templates matched the scanned finger.\n");
                }
                else
                {
                    var best = results[0];
                    lblIdentifyStatus.Text      =
                        $"✓  Best match: '{best.Name}'  score {best.Score:F1}   " +
                        $"({results.Count} match{(results.Count > 1 ? "es" : "")} found)";
                    lblIdentifyStatus.ForeColor = C_GREEN;
                    SetStage("Identified ✓");
                    Log($"1:N identify — BEST: '{best.Name}' score {best.Score:F1}  " +
                        $"({results.Count}/{slotNames.Count} matched, threshold {MATCH_THRESHOLD})",
                        C_GREEN);

                    // Header
                    rtbIdentifyResults.SelectionFont  =
                        new Font("Consolas", 9f, FontStyle.Bold);
                    rtbIdentifyResults.SelectionColor = C_TEXT;
                    rtbIdentifyResults.AppendText(
                        $"{"#",-4} {"Slot Name",-30} {"Score",8}  {"Status"}\n");
                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.AppendText(new string('─', 60) + "\n");

                    for (int i = 0; i < results.Count; i++)
                    {
                        var (name, score) = results[i];
                        bool isBest = (i == 0);

                        rtbIdentifyResults.SelectionFont =
                            new Font("Consolas", 9f,
                                isBest ? FontStyle.Bold : FontStyle.Regular);
                        rtbIdentifyResults.SelectionColor =
                            isBest ? C_GREEN : C_TEXT;

                        string tag = isBest ? "★ BEST" : "  match";
                        rtbIdentifyResults.AppendText(
                            $"{i + 1,-4} {name,-30} {score,8:F1}  {tag}\n");
                    }

                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.SelectionFont  =
                        new Font("Consolas", 9f, FontStyle.Regular);
                    rtbIdentifyResults.AppendText(
                        $"\n{results.Count} of {slotNames.Count} template(s) matched  " +
                        $"(threshold ≥ {MATCH_THRESHOLD})\n");
                }

                rtbIdentifyResults.SelectionStart = 0;
                rtbIdentifyResults.ScrollToCaret();
            }
            catch (OperationCanceledException)
            {
                SetStage("Cancelled.");
                Log("1:N identify cancelled.", C_MUTED);
            }
            catch (Exception ex)
            {
                SetStage("Identify error — see log.");
                Log($"1:N identify error: {ex.GetType().Name}: {ex.Message}", C_RED);
                lblIdentifyStatus.Text      = "Error — see activity log.";
                lblIdentifyStatus.ForeColor = C_RED;
            }
            finally
            {
                SetBusy(false, btnIdentifyNow, btnCancelIdentify);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Image → Template handlers
        // ═════════════════════════════════════════════════════════════════════
        private void BtnBrowseImg_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title       = "Select fingerprint PNG image",
                Filter      = "PNG images (*.png)|*.png|All files (*.*)|*.*",
                FilterIndex = 1
            };

            // Default to Desktop where scan images are saved
            dlg.InitialDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);

            if (dlg.ShowDialog() != DialogResult.OK) return;

            txtImgPath.Text = dlg.FileName;

            // Show preview and auto-fill slot name from filename
            try
            {
                var img = Image.FromFile(dlg.FileName);
                picPreview.Image = img;
                lblConvertStatus.Text = $"Image loaded: {img.Width}×{img.Height} px";
                lblConvertStatus.ForeColor = C_MUTED;

                // Auto-fill slot name from filename (strip extension and timestamp suffix)
                string baseName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                // Remove trailing _HHmmss timestamp if present (e.g. FPScan_20260701_092437)
                baseName = System.Text.RegularExpressions.Regex.Replace(
                    baseName, @"_\d{6}$", "");
                if (string.IsNullOrEmpty(txtImgSlotName.Text))
                    txtImgSlotName.Text = baseName;
            }
            catch (Exception ex)
            {
                lblConvertStatus.Text      = $"Could not load image: {ex.Message}";
                lblConvertStatus.ForeColor = C_RED;
            }
        }

        private void BtnConvertImg_Click(object? s, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtImgPath.Text) ||
                txtImgPath.Text == "No file selected")
            {
                lblConvertStatus.Text      = "Please select a PNG image first.";
                lblConvertStatus.ForeColor = C_YELLOW;
                return;
            }

            string slotName = txtImgSlotName.Text.Trim();
            if (string.IsNullOrEmpty(slotName))
            {
                lblConvertStatus.Text      = "Enter a slot name first.";
                lblConvertStatus.ForeColor = C_YELLOW;
                txtImgSlotName.Focus();
                return;
            }

            if (_store.Exists(slotName))
            {
                var r = MessageBox.Show(
                    $"Slot '{slotName}' already exists. Overwrite?",
                    "Overwrite?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            try
            {
                btnConvertImg.Enabled = false;
                lblConvertStatus.ForeColor = C_MUTED;
                lblConvertStatus.Text = "Converting…";

                // Load PNG and convert each pixel to 8-bit grayscale.
                // The saved images are already grayscale-inverted (white bg, black ridges)
                // so we invert back to the raw sensor format (dark ridges = lower values)
                // that SourceAFIS expects.
                using var bmp = new System.Drawing.Bitmap(txtImgPath.Text);
                int w = bmp.Width, h = bmp.Height;
                var pixels = new byte[w * h];

                var bmpData = bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, w, h),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        byte* src = (byte*)bmpData.Scan0;
                        for (int i = 0; i < w * h; i++)
                        {
                            // Average R+G+B then invert back to sensor format
                            int b2 = src[i * 4 + 0];
                            int g  = src[i * 4 + 1];
                            int r2 = src[i * 4 + 2];
                            byte gray = (byte)((r2 + g + b2) / 3);
                            pixels[i] = (byte)(255 - gray); // re-invert for SourceAFIS
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                // Extract SourceAFIS template
                var fpImg    = new FingerprintImage(w, h, pixels,
                    new FingerprintImageOptions { Dpi = SCAN_DPI });
                var template = new FingerprintTemplate(fpImg);
                byte[] templateBytes = template.ToByteArray();

                // Save to slot store
                bool saved = _store.Save(slotName, templateBytes, $"from image: {System.IO.Path.GetFileName(txtImgPath.Text)}");
                if (saved)
                {
                    lblConvertStatus.Text      = $"✓ Saved as slot '{slotName}'  ({templateBytes.Length} bytes)";
                    lblConvertStatus.ForeColor = C_GREEN;
                    Log($"Image → Template: '{slotName}'  {templateBytes.Length} bytes from {System.IO.Path.GetFileName(txtImgPath.Text)}", C_GREEN);
                    txtImgSlotName.Clear();
                    txtImgPath.Text  = "No file selected";
                    picPreview.Image = null;
                }
                else
                {
                    lblConvertStatus.Text      = "Converted but disk save failed.";
                    lblConvertStatus.ForeColor = C_RED;
                }
            }
            catch (Exception ex)
            {
                lblConvertStatus.Text      = $"✗ Conversion failed: {ex.Message}";
                lblConvertStatus.ForeColor = C_RED;
                Log($"Image → Template failed: {ex.Message}", C_RED);
            }
            finally
            {
                btnConvertImg.Enabled = true;
            }
        }
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
            foreach (var b in new[]
                { btnTabScan, btnTabEnroll, btnTabMatch, btnTabIdentify,
                  btnTabImgToTpl, btnTabTplToImg })
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

        // ═════════════════════════════════════════════════════════════════════
        //  Template → Image handlers
        // ═════════════════════════════════════════════════════════════════════
        private void RefreshExportSlots()
        {
            cmbTplSlot.Items.Clear();
            foreach (var name in _store.SlotNames())
            {
                var entry = _store.All[name];
                cmbTplSlot.Items.Add(
                    new SlotListItem(name,
                        $"{name}  [{entry.EnrolledAt:MM/dd HH:mm}]"));
            }
            if (cmbTplSlot.Items.Count > 0)
                cmbTplSlot.SelectedIndex = 0;
        }

        private void BtnBrowseFolder_Click(object? s, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description            = "Select folder to save the fingerprint image",
                UseDescriptionForTitle = true,
                SelectedPath           = txtExportFolder.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtExportFolder.Text = dlg.SelectedPath;
        }

        private void BtnExportImg_Click(object? s, EventArgs e)
        {
            if (cmbTplSlot.SelectedItem is not SlotListItem selectedSlot)
            {
                lblExportStatus.Text      = "Select a template slot first.";
                lblExportStatus.ForeColor = C_YELLOW;
                return;
            }

            string folder = txtExportFolder.Text.Trim();
            if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder))
            {
                lblExportStatus.Text      = "Select a valid output folder first.";
                lblExportStatus.ForeColor = C_YELLOW;
                return;
            }

            string fileName = txtExportFileName.Text.Trim();
            if (string.IsNullOrEmpty(fileName))
                fileName = $"FP_{selectedSlot.SlotName}";
            fileName   = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string outputPath = System.IO.Path.Combine(folder, fileName + ".png");

            try
            {
                btnExportImg.Enabled      = false;
                lblExportStatus.Text      = "Looking for original scan image…";
                lblExportStatus.ForeColor = C_MUTED;

                byte[]? templateBytes = _store.Load(selectedSlot.SlotName);
                if (templateBytes == null)
                {
                    lblExportStatus.Text      = "Could not load template from disk.";
                    lblExportStatus.ForeColor = C_RED;
                    return;
                }

                // SourceAFIS templates store minutiae (feature points), not raw pixels,
                // so there is no lossless pixel reconstruction from the template alone.
                // Strategy: look for the original scan PNG saved to the Desktop when
                // this slot was enrolled. If found, copy it to the chosen folder.
                // If not, generate a labeled placeholder image.
                string desktop     = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string safeName    = string.Concat(selectedSlot.SlotName
                    .Split(System.IO.Path.GetInvalidFileNameChars()));

                // Search Desktop for any enrollment scan PNG matching this slot
                string[] candidates = System.IO.Directory.GetFiles(
                    desktop, $"FPEnroll_{safeName}_scan1*.png");

                // Also check if the user stored images in a subfolder named after the slot
                if (candidates.Length == 0)
                    candidates = System.IO.Directory.GetFiles(
                        desktop, $"FPEnroll_{safeName}*.png");

                if (candidates.Length > 0)
                {
                    System.IO.File.Copy(candidates[0], outputPath, overwrite: true);
                    lblExportStatus.Text =
                        $"✓ Exported original scan image:\n{outputPath}";
                    lblExportStatus.ForeColor = C_GREEN;
                    Log($"Tpl→Img: '{selectedSlot.SlotName}' → {outputPath}", C_GREEN);
                }
                else
                {
                    ExportTemplatePlaceholder(selectedSlot.SlotName,
                        templateBytes, outputPath);
                    lblExportStatus.Text =
                        $"✓ Placeholder exported:\n{outputPath}\n" +
                        "(No original scan PNG found on Desktop — use " +
                        "Scan Test to capture a real image first.)";
                    lblExportStatus.ForeColor = C_YELLOW;
                    Log($"Tpl→Img: no scan found for '{selectedSlot.SlotName}', placeholder saved", C_YELLOW);
                }

                // Show preview in the tab
                try { picExportPreview.Image = Image.FromFile(outputPath); }
                catch { /* non-fatal */ }
            }
            catch (Exception ex)
            {
                lblExportStatus.Text      = $"✗ Export failed: {ex.Message}";
                lblExportStatus.ForeColor = C_RED;
                Log($"Tpl→Img failed: {ex.Message}", C_RED);
            }
            finally
            {
                btnExportImg.Enabled = true;
            }
        }

        private static void ExportTemplatePlaceholder(
            string slotName, byte[] templateBytes, string outputPath)
        {
            const int W = 480, H = 640;
            using var bmp = new System.Drawing.Bitmap(W, H);
            using var g   = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Decorative ridge arcs
            var ridgePen = new System.Drawing.Pen(
                System.Drawing.Color.FromArgb(180, 180, 180), 1.5f);
            int cx = W / 2, cy = H / 2 - 40;
            for (int i = 1; i <= 12; i++)
            {
                int rx = i * 18, ry = i * 22;
                g.DrawArc(ridgePen, cx - rx, cy - ry + 30, rx * 2, ry * 2, 200, 140);
            }

            g.DrawString(slotName,
                new System.Drawing.Font("Segoe UI Semibold", 14f),
                System.Drawing.Brushes.Black,
                new System.Drawing.RectangleF(20, 20, W - 40, 60),
                new System.Drawing.StringFormat
                    { Alignment = System.Drawing.StringAlignment.Center });

            g.DrawString(
                $"Template: {templateBytes.Length} bytes\n" +
                $"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                "(placeholder — original scan not found on Desktop)",
                new System.Drawing.Font("Segoe UI", 9f),
                System.Drawing.Brushes.Gray,
                new System.Drawing.RectangleF(20, H - 110, W - 40, 90),
                new System.Drawing.StringFormat
                    { Alignment = System.Drawing.StringAlignment.Center });

            g.DrawRectangle(new System.Drawing.Pen(
                System.Drawing.Color.LightGray, 2f), 1, 1, W - 2, H - 2);

            bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
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
                        byte v = (byte)(255 - pixels[i]); // invert: white bg, black ridges
                        int  o = i * 4;
                        dst[o + 0] = v;   // B
                        dst[o + 1] = v;   // G
                        dst[o + 2] = v;   // R
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
