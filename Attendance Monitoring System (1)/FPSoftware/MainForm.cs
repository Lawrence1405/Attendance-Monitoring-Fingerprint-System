using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        // ── Controls — status bar ─────────────────────────────────────────────
        private Label                lblDevice      = null!;
        private Button               btnConnect     = null!;

        // ── Controls — left panel ─────────────────────────────────────────────
        private FingerprintVisualPanel fpVisual     = null!;
        private ProgressBar          pbQuality      = null!;
        private Label                lblQuality     = null!;
        private Label                lblStage       = null!;

        // ── Controls — tabs ───────────────────────────────────────────────────
        private Button               btnTabScan     = null!;
        private Button               btnTabEnroll   = null!;
        private Button               btnTabMatch    = null!;
        private Button               btnTabIdentify = null!;
        private Button               btnTabImgToTpl = null!;
        private Button               btnTabTplToImg = null!;
        private Panel                pnlTabContent  = null!;

        // ── Tab panels ────────────────────────────────────────────────────────
        private Panel                tabScan        = null!;
        private Panel                tabEnroll      = null!;
        private Panel                tabMatch       = null!;
        private Panel                tabIdentify    = null!;
        private Panel                tabImgToTpl    = null!;
        private Panel                tabTplToImg    = null!;

        // ── Scan tab ──────────────────────────────────────────────────────────
        private Button               btnScanNow     = null!;
        private Button               btnCancelScan  = null!;
        private TextBox              txtScanSaveFolder = null!;   // NEW: custom save folder

        // ── Enroll tab ────────────────────────────────────────────────────────
        private TextBox              txtSlotName       = null!;
        private TextBox              txtNotes          = null!;
        private Button               btnEnrollNow      = null!;
        private Button               btnCancelEnroll   = null!;
        private Panel[]              dots              = null!;
        private Label                lblEnrollStatus   = null!;
        private TextBox              txtEnrollSaveFolder = null!; // template save folder
        private TextBox              txtEnrollImgFolder  = null!; // image save folder (separate)

        // ── 1:1 Match tab ─────────────────────────────────────────────────────
        private ListBox              lstSlots          = null!;
        private Button               btnMatchNow       = null!;
        private Button               btnCancelMatch    = null!;
        private Button               btnDeleteSlot     = null!;
        private Label                lblMatchResult    = null!;
        private TextBox              txtMatchFolder    = null!;   // NEW: custom template folder
        private Button               btnMatchBrowse    = null!;
        private Button               btnMatchReload    = null!;
        private Label                lblMatchFolderNote= null!;

        // ── 1:N Identify tab ──────────────────────────────────────────────────
        private Button               btnIdentifyNow    = null!;
        private Button               btnCancelIdentify = null!;
        private RichTextBox          rtbIdentifyResults= null!;
        private Label                lblIdentifyStatus = null!;
        private TextBox              txtIdentifyFolder = null!;   // NEW: custom template folder
        private Button               btnIdentifyBrowse = null!;
        private Button               btnIdentifyReload = null!;
        private Label                lblIdentifyFolderNote = null!;

        // ── Image → Template tab ──────────────────────────────────────────────
        private TextBox              txtImgPath       = null!;
        private TextBox              txtImgSlotName   = null!;
        private Button               btnBrowseImg     = null!;
        private Button               btnConvertImg    = null!;
        private Label                lblConvertStatus = null!;
        private PictureBox           picPreview       = null!;

        // ── Template → Image tab ──────────────────────────────────────────────
        private ComboBox             cmbTplSlot        = null!;
        private Button               btnRefreshSlots   = null!;
        private TextBox              txtExportFolder   = null!;
        private Button               btnBrowseFolder   = null!;
        private TextBox              txtExportFileName = null!;
        private Button               btnExportImg      = null!;
        private Label                lblExportStatus   = null!;
        private PictureBox           picExportPreview  = null!;

        // ── Log ───────────────────────────────────────────────────────────────
        private RichTextBox          rtbLog         = null!;

        // ══════════════════════════════════════════════════════════════════════
        public MainForm()
        {
            BuildUI();
            WireEvents();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UI Construction
        // ══════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            SuspendLayout();
            Text            = "FS64 Fingerprint Tester";
            Size            = new Size(760, 700);
            MinimumSize     = new Size(720, 660);
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
                "ftrScanAPI (2025 Enrollment Kit)  +  SourceAFIS  •  Scan  •  Enroll  •  Match  •  1:N",
                20, 36, new Font("Segoe UI", 8.5f), C_MUTED));

            // Status bar
            var bar = MkPanel(0, 52, 760, 36, Color.FromArgb(20, 22, 30));
            lblDevice  = Lbl("● Not connected", 14, 10, new Font("Segoe UI", 9f), C_RED);
            btnConnect = MkBtn("Connect Scanner", 600, 4, 148, 28, C_ACCENT);
            bar.Controls.Add(lblDevice);
            bar.Controls.Add(btnConnect);

            // Left column
            var left = MkPanel(16, 100, 200, 460, C_SURFACE);
            var cap  = Lbl("LIVE SENSOR", 0, 12, new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED);
            cap.Width = 200; cap.TextAlign = ContentAlignment.MiddleCenter;
            left.Controls.Add(cap);
            fpVisual = new FingerprintVisualPanel { Location = new Point(20, 34), Size = new Size(160, 200) };
            left.Controls.Add(fpVisual);
            var ql = Lbl("QUALITY", 0, 250, new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED);
            ql.Width = 200; ql.TextAlign = ContentAlignment.MiddleCenter;
            left.Controls.Add(ql);
            pbQuality = new ProgressBar { Location = new Point(20, 270), Size = new Size(160, 18), Minimum = 0, Maximum = 100, Value = 0, Style = ProgressBarStyle.Continuous };
            left.Controls.Add(pbQuality);
            lblQuality = Lbl("—", 20, 292, new Font("Segoe UI Semibold", 10f), C_MUTED);
            left.Controls.Add(lblQuality);
            lblStage = Lbl("Idle", 0, 325, new Font("Segoe UI", 8.5f), C_MUTED);
            lblStage.Width = 200; lblStage.Height = 100; lblStage.TextAlign = ContentAlignment.TopCenter;
            left.Controls.Add(lblStage);

            // Tab bar
            var tabs = MkPanel(232, 100, 512, 44, C_SURFACE);
            btnTabScan      = MkTabBtn("Scan",     4,   4,  78);
            btnTabEnroll    = MkTabBtn("Enroll",  86,   4,  78);
            btnTabMatch     = MkTabBtn("1:1",    168,   4,  60);
            btnTabIdentify  = MkTabBtn("1:N",    232,   4,  60);
            btnTabImgToTpl  = MkTabBtn("Img→Tpl",296,  4,  96);
            btnTabTplToImg  = MkTabBtn("Tpl→Img",396,  4,  96);
            tabs.Controls.AddRange(new Control[] { btnTabScan, btnTabEnroll, btnTabMatch, btnTabIdentify, btnTabImgToTpl, btnTabTplToImg });

            pnlTabContent = MkPanel(232, 144, 512, 420, C_SURFACE);

            BuildTabScan();
            BuildTabEnroll();
            BuildTabMatch();
            BuildTabIdentify();
            BuildTabImgToTpl();
            BuildTabTplToImg();

            Controls.Add(Lbl("ACTIVITY LOG", 16, 578, new Font("Segoe UI", 7.5f, FontStyle.Bold), C_MUTED));
            rtbLog = new RichTextBox { Location = new Point(16, 596), Size = new Size(728, 72), BackColor = C_SURFACE, ForeColor = C_TEXT, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 8f), ScrollBars = RichTextBoxScrollBars.Vertical };
            Controls.Add(rtbLog);

            ResumeLayout();
            ActivateTab(btnTabScan, tabScan);
        }

        // ── Tab builders ──────────────────────────────────────────────────────

        private void BuildTabScan()
        {
            tabScan = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabScan.Controls.Add(Lbl("Scan Test", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabScan.Controls.Add(Lbl("Captures one raw image from the FS64 sensor and saves it as PNG.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            // Save folder picker
            tabScan.Controls.Add(Lbl("Save image to folder", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtScanSaveFolder = MkFolderBox(16, 94, 362, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            var btnScanBrowse = MkBtn("Browse…", 386, 94, 80, 28, C_ACCENT);
            btnScanBrowse.Click += (_, _) => BrowseFolder(txtScanSaveFolder, "Select folder to save scan images");
            tabScan.Controls.AddRange(new Control[] { txtScanSaveFolder, btnScanBrowse });

            btnScanNow    = MkBtn("▶  Start Scan", 16, 136, 140, 38, C_ACCENT);
            btnCancelScan = MkBtn("■  Cancel",     164, 136, 100, 38, C_YELLOW);
            btnCancelScan.Visible = false;
            tabScan.Controls.AddRange(new Control[] { btnScanNow, btnCancelScan });

            tabScan.Controls.Add(Lbl("1. Click Start Scan\n2. Place finger firmly on the sensor\n3. Hold still — PNG saved to the folder above\n4. Quality score appears on the left", 16, 190, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 80));
        }

        private void BuildTabEnroll()
        {
            tabEnroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabEnroll.Controls.Add(Lbl("Enroll Fingerprint", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabEnroll.Controls.Add(Lbl("Captures 3 scans, runs 1:N duplicate check, then saves a SourceAFIS template.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            tabEnroll.Controls.Add(Lbl("Slot name", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtSlotName = new TextBox { Location = new Point(16, 94), Size = new Size(240, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
            tabEnroll.Controls.Add(txtSlotName);

            tabEnroll.Controls.Add(Lbl("Notes (optional)", 16, 130, new Font("Segoe UI", 8.5f), C_MUTED));
            txtNotes = new TextBox { Location = new Point(16, 148), Size = new Size(240, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle };
            tabEnroll.Controls.Add(txtNotes);

            // Template save folder
            tabEnroll.Controls.Add(Lbl("Save template (.fpt) to folder", 16, 184, new Font("Segoe UI", 8.5f), C_MUTED));
            txtEnrollSaveFolder = MkFolderBox(16, 202, 362, _store.DefaultFolder);
            var btnEnrollTplBrowse = MkBtn("Browse…", 386, 202, 80, 28, C_ACCENT);
            btnEnrollTplBrowse.Click += (_, _) => BrowseFolder(txtEnrollSaveFolder, "Select folder to save fingerprint templates (.fpt)");
            tabEnroll.Controls.AddRange(new Control[] { txtEnrollSaveFolder, btnEnrollTplBrowse });

            // Image save folder (separate)
            tabEnroll.Controls.Add(Lbl("Save scan images (.png) to folder", 16, 234, new Font("Segoe UI", 8.5f), C_MUTED));
            txtEnrollImgFolder = MkFolderBox(16, 252, 362, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            var btnEnrollImgBrowse = MkBtn("Browse…", 386, 252, 80, 28, C_ACCENT);
            btnEnrollImgBrowse.Click += (_, _) => BrowseFolder(txtEnrollImgFolder, "Select folder to save scan images (.png)");
            tabEnroll.Controls.AddRange(new Control[] { txtEnrollImgFolder, btnEnrollImgBrowse });

            // Progress dots
            tabEnroll.Controls.Add(Lbl("Scan progress", 16, 288, new Font("Segoe UI", 8.5f), C_MUTED));
            dots = new Panel[3];
            for (int i = 0; i < 3; i++)
            {
                var dot = new Panel { Location = new Point(16 + i * 72, 308), Size = new Size(54, 54), BackColor = C_DOT_OFF };
                dot.Paint += DotPaint;
                dots[i] = dot;
                tabEnroll.Controls.Add(dot);
                tabEnroll.Controls.Add(Lbl($"Scan {i + 1}", 22 + i * 72, 366, new Font("Segoe UI", 8f), C_MUTED));
            }

            lblEnrollStatus = Lbl("Ready", 16, 384, new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 22);
            tabEnroll.Controls.Add(lblEnrollStatus);

            btnEnrollNow    = MkBtn("▶  Start Enroll", 16,  408, 148, 38, C_GREEN);
            btnCancelEnroll = MkBtn("■  Cancel",       172, 408, 100, 38, C_YELLOW);
            btnCancelEnroll.Visible = false;
            tabEnroll.Controls.AddRange(new Control[] { btnEnrollNow, btnCancelEnroll });
        }

        private void BuildTabMatch()
        {
            tabMatch = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabMatch.Controls.Add(Lbl("1:1 Verification", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabMatch.Controls.Add(Lbl("Select a slot then scan to verify it matches.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            // Template folder picker
            tabMatch.Controls.Add(Lbl("Load templates from folder", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtMatchFolder = MkFolderBox(16, 94, 310, _store.DefaultFolder);
            btnMatchBrowse = MkBtn("Browse…", 334, 94, 76, 28, C_ACCENT);
            btnMatchReload = MkBtn("⟳ Load",  418, 94, 70, 28, C_MUTED);
            lblMatchFolderNote = Lbl("(default)", 16, 126, new Font("Segoe UI", 8f), C_MUTED, w: 478, h: 16);
            tabMatch.Controls.AddRange(new Control[] { txtMatchFolder, btnMatchBrowse, btnMatchReload, lblMatchFolderNote });

            tabMatch.Controls.Add(Lbl("Enrolled slots", 16, 148, new Font("Segoe UI", 8.5f), C_MUTED));
            lstSlots = new ListBox { Location = new Point(16, 166), Size = new Size(280, 130), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            tabMatch.Controls.Add(lstSlots);

            btnMatchNow   = MkBtn("▶  Verify Match",  16,  306, 148, 38, C_ACCENT);
            btnCancelMatch= MkBtn("■  Cancel",        172, 306, 100, 38, C_YELLOW);
            btnDeleteSlot = MkBtn("🗑  Delete",        290, 306, 100, 38, C_RED);
            btnCancelMatch.Visible = false;

            lblMatchResult = new Label { Location = new Point(16, 356), Size = new Size(478, 52), Font = new Font("Segoe UI Semibold", 13f), ForeColor = C_MUTED, Text = "", TextAlign = ContentAlignment.MiddleLeft };
            tabMatch.Controls.AddRange(new Control[] { btnMatchNow, btnCancelMatch, btnDeleteSlot, lblMatchResult });
        }

        private void BuildTabIdentify()
        {
            tabIdentify = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabIdentify.Controls.Add(Lbl("1:N  Identify", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabIdentify.Controls.Add(Lbl("Scan once and compare against ALL templates. Every match is listed, best highlighted.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            // Template folder picker
            tabIdentify.Controls.Add(Lbl("Load templates from folder", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtIdentifyFolder = MkFolderBox(16, 94, 310, _store.DefaultFolder);
            btnIdentifyBrowse = MkBtn("Browse…", 334, 94, 76, 28, C_ACCENT);
            btnIdentifyReload = MkBtn("⟳ Load",  418, 94, 70, 28, C_MUTED);
            lblIdentifyFolderNote = Lbl("(default)", 16, 126, new Font("Segoe UI", 8f), C_MUTED, w: 478, h: 16);
            tabIdentify.Controls.AddRange(new Control[] { txtIdentifyFolder, btnIdentifyBrowse, btnIdentifyReload, lblIdentifyFolderNote });

            btnIdentifyNow    = MkBtn("▶  Identify Finger", 16, 150, 170, 38, C_ACCENT);
            btnCancelIdentify = MkBtn("■  Cancel",          194, 150, 100, 38, C_YELLOW);
            btnCancelIdentify.Visible = false;
            tabIdentify.Controls.AddRange(new Control[] { btnIdentifyNow, btnCancelIdentify });

            lblIdentifyStatus = Lbl("Ready — press Identify Finger to scan.", 16, 198, new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 22);
            tabIdentify.Controls.Add(lblIdentifyStatus);

            tabIdentify.Controls.Add(Lbl("Results", 16, 226, new Font("Segoe UI", 8.5f), C_MUTED));
            rtbIdentifyResults = new RichTextBox { Location = new Point(16, 246), Size = new Size(478, 160), BackColor = C_BG, ForeColor = C_TEXT, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };
            tabIdentify.Controls.Add(rtbIdentifyResults);
        }

        private void BuildTabImgToTpl()
        {
            tabImgToTpl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabImgToTpl.Controls.Add(Lbl("Image → Template", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabImgToTpl.Controls.Add(Lbl("Convert a saved fingerprint PNG into a template slot for verification.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            tabImgToTpl.Controls.Add(Lbl("PNG image file", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            txtImgPath = new TextBox { Location = new Point(16, 94), Size = new Size(330, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, Text = "No file selected" };
            btnBrowseImg = MkBtn("Browse…", 354, 94, 90, 28, C_ACCENT);
            tabImgToTpl.Controls.AddRange(new Control[] { txtImgPath, btnBrowseImg });

            tabImgToTpl.Controls.Add(Lbl("Preview", 16, 130, new Font("Segoe UI", 8.5f), C_MUTED));
            picPreview = new PictureBox { Location = new Point(16, 148), Size = new Size(160, 130), BackColor = C_BG, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            tabImgToTpl.Controls.Add(picPreview);

            tabImgToTpl.Controls.Add(Lbl("Save as slot name", 190, 148, new Font("Segoe UI", 8.5f), C_MUTED));
            txtImgSlotName = new TextBox { Location = new Point(190, 166), Size = new Size(264, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. Alice — Right Index (from image)" };
            tabImgToTpl.Controls.Add(txtImgSlotName);
            tabImgToTpl.Controls.Add(Lbl("Converted at 500 dpi using SourceAFIS.", 190, 202, new Font("Segoe UI", 8.5f), C_MUTED, w: 264, h: 22));

            btnConvertImg = MkBtn("▶  Convert to Template", 190, 232, 200, 38, C_GREEN);
            tabImgToTpl.Controls.Add(btnConvertImg);

            lblConvertStatus = Lbl("Select a PNG image to begin.", 16, 280, new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 44);
            tabImgToTpl.Controls.Add(lblConvertStatus);
        }

        private void BuildTabTplToImg()
        {
            tabTplToImg = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            tabTplToImg.Controls.Add(Lbl("Template → Image", 16, 16, new Font("Segoe UI Semibold", 12f), C_TEXT));
            tabTplToImg.Controls.Add(Lbl("Export a stored template as a PNG to any folder you choose.", 16, 46, new Font("Segoe UI", 9f), C_MUTED, w: 478, h: 22));

            tabTplToImg.Controls.Add(Lbl("Template slot", 16, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            cmbTplSlot = new ComboBox { Location = new Point(16, 94), Size = new Size(300, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            btnRefreshSlots = MkBtn("⟳  Refresh", 324, 94, 100, 28, C_ACCENT);
            tabTplToImg.Controls.AddRange(new Control[] { cmbTplSlot, btnRefreshSlots });

            tabTplToImg.Controls.Add(Lbl("Save to folder", 16, 130, new Font("Segoe UI", 8.5f), C_MUTED));
            txtExportFolder = new TextBox { Location = new Point(16, 148), Size = new Size(330, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
            btnBrowseFolder = MkBtn("Browse…", 354, 148, 90, 28, C_ACCENT);
            tabTplToImg.Controls.AddRange(new Control[] { txtExportFolder, btnBrowseFolder });

            tabTplToImg.Controls.Add(Lbl("File name (no extension)", 16, 184, new Font("Segoe UI", 8.5f), C_MUTED));
            txtExportFileName = new TextBox { Location = new Point(16, 202), Size = new Size(280, 28), BackColor = C_BG, ForeColor = C_TEXT, Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. Alice_RightIndex_export" };
            tabTplToImg.Controls.Add(txtExportFileName);
            tabTplToImg.Controls.Add(Lbl(".png", 300, 210, new Font("Segoe UI", 9f), C_MUTED));

            tabTplToImg.Controls.Add(Lbl("Preview", 370, 76, new Font("Segoe UI", 8.5f), C_MUTED));
            picExportPreview = new PictureBox { Location = new Point(370, 94), Size = new Size(120, 150), BackColor = C_BG, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            tabTplToImg.Controls.Add(picExportPreview);

            btnExportImg = MkBtn("💾  Export as PNG", 16, 246, 180, 38, C_GREEN);
            tabTplToImg.Controls.Add(btnExportImg);

            lblExportStatus = Lbl("Select a template slot to begin.", 16, 296, new Font("Segoe UI", 9.5f), C_MUTED, w: 478, h: 70);
            tabTplToImg.Controls.Add(lblExportStatus);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Event Wiring
        // ══════════════════════════════════════════════════════════════════════
        private void WireEvents()
        {
            btnConnect.Click += BtnConnect_Click;

            btnTabScan.Click     += (_, _) => ActivateTab(btnTabScan,     tabScan);
            btnTabEnroll.Click   += (_, _) => ActivateTab(btnTabEnroll,   tabEnroll);
            btnTabMatch.Click    += (_, _) => { RefreshSlotList(); ActivateTab(btnTabMatch, tabMatch); };
            btnTabIdentify.Click += (_, _) => ActivateTab(btnTabIdentify, tabIdentify);
            btnTabImgToTpl.Click += (_, _) => ActivateTab(btnTabImgToTpl, tabImgToTpl);
            btnTabTplToImg.Click += (_, _) => { RefreshExportSlots(); ActivateTab(btnTabTplToImg, tabTplToImg); };

            btnScanNow.Click        += BtnScan_Click;
            btnCancelScan.Click     += (_, _) => CancelOp();
            btnEnrollNow.Click      += BtnEnroll_Click;
            btnCancelEnroll.Click   += (_, _) => CancelOp();
            btnMatchNow.Click       += BtnMatch_Click;
            btnCancelMatch.Click    += (_, _) => CancelOp();
            btnDeleteSlot.Click     += BtnDelete_Click;
            btnIdentifyNow.Click    += BtnIdentify_Click;
            btnCancelIdentify.Click += (_, _) => CancelOp();

            // Match folder
            btnMatchBrowse.Click += (_, _) =>
            {
                BrowseFolder(txtMatchFolder, "Select folder containing templates for 1:1 Match");
                ApplyMatchFolder();
            };
            btnMatchReload.Click += (_, _) => ApplyMatchFolder();

            // Identify folder
            btnIdentifyBrowse.Click += (_, _) =>
            {
                BrowseFolder(txtIdentifyFolder, "Select folder containing templates for 1:N Identify");
                ApplyIdentifyFolder();
            };
            btnIdentifyReload.Click += (_, _) => ApplyIdentifyFolder();

            btnBrowseImg.Click    += BtnBrowseImg_Click;
            btnConvertImg.Click   += BtnConvertImg_Click;
            btnBrowseFolder.Click += BtnBrowseFolder_Click;
            btnRefreshSlots.Click += (_, _) => RefreshExportSlots();
            btnExportImg.Click    += BtnExportImg_Click;

            cmbTplSlot.SelectedIndexChanged += (_, _) =>
            {
                if (cmbTplSlot.SelectedItem is SlotListItem item)
                {
                    txtExportFileName.Text    = $"FP_{string.Concat(item.SlotName.Split(Path.GetInvalidFileNameChars()))}";
                    lblExportStatus.Text      = $"Ready to export '{item.SlotName}'.";
                    lblExportStatus.ForeColor = C_MUTED;
                    picExportPreview.Image    = null;
                }
            };

            FormClosing += (_, _) => { _cts?.Cancel(); _scanner.Dispose(); };
        }

        // ── Folder helpers ────────────────────────────────────────────────────
        private static void BrowseFolder(TextBox target, string description)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                SelectedPath = target.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK) target.Text = dlg.SelectedPath;
        }

        private void ApplyMatchFolder()
        {
            string folder = txtMatchFolder.Text.Trim();
            _store.SetActiveFolder(folder == _store.DefaultFolder ? null : folder);
            _store.ReloadActiveIndex();
            RefreshSlotList();
            bool isDefault = !_store.IsUsingCustomFolder;
            lblMatchFolderNote.Text      = isDefault ? "(default folder)" : $"Custom: {_store.SlotNames().Count} slot(s) found";
            lblMatchFolderNote.ForeColor = isDefault ? C_MUTED : C_ACCENT;
            Log($"1:1 Match folder → {folder}  ({_store.SlotNames().Count} slots)", C_ACCENT);
        }

        private void ApplyIdentifyFolder()
        {
            string folder = txtIdentifyFolder.Text.Trim();
            _store.SetActiveFolder(folder == _store.DefaultFolder ? null : folder);
            _store.ReloadActiveIndex();
            bool isDefault = !_store.IsUsingCustomFolder;
            lblIdentifyFolderNote.Text      = isDefault ? "(default folder)" : $"Custom: {_store.SlotNames().Count} slot(s) found";
            lblIdentifyFolderNote.ForeColor = isDefault ? C_MUTED : C_ACCENT;
            Log($"1:N Identify folder → {folder}  ({_store.SlotNames().Count} slots)", C_ACCENT);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Connect
        // ══════════════════════════════════════════════════════════════════════
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
                    if (ok) { SetDeviceLabel(true); Log($"Scanner ready — {sizeInfo}", C_GREEN); SetStage("Ready"); }
                    else    { SetDeviceLabel(false); Log($"Connect failed: {sizeInfo}", C_RED); SetStage("Connection failed."); }
                }
            }
            catch (Exception ex) { Log($"Connect error: {ex.Message}", C_RED); }
            finally { btnConnect.Enabled = true; }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Scan Test
        // ══════════════════════════════════════════════════════════════════════
        private async void BtnScan_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            _cts = new CancellationTokenSource();
            SetBusy(true, btnScanNow, btnCancelScan);
            UpdateQuality(0);
            try
            {
                var (image, err) = await _scanner.CaptureAsync(CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger on scanner…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing image…")));

                if (image == null) { SetStage("Scan failed."); Log($"Scan failed: {err}", C_RED); }
                else
                {
                    long sum = 0; foreach (byte b in image) sum += b;
                    int q = Math.Clamp((int)(sum / image.Length) / 2, 0, 100);
                    UpdateQuality(q); SetStage("Scan captured ✓");
                    Log($"Scan OK — {image.Length / 1024} KB, brightness ~{q}%", QualityColor(q));

                    string folder = txtScanSaveFolder.Text.Trim();
                    if (string.IsNullOrEmpty(folder)) folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string path = Path.Combine(folder, $"FPScan_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    try { SaveGrayscalePng(image, _scanner.ImageWidth, _scanner.ImageHeight, path); Log($"Image saved → {path}", C_ACCENT); }
                    catch (Exception imgEx) { Log($"Image save failed: {imgEx.Message}", C_YELLOW); }
                }
            }
            catch (OperationCanceledException) { SetStage("Cancelled."); Log("Scan cancelled.", C_MUTED); }
            catch (Exception ex) { SetStage("Scan error."); Log($"Scan error: {ex.Message}", C_RED); }
            finally { SetBusy(false, btnScanNow, btnCancelScan); }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Enroll — 1:N duplicate check FIRST, then 3 captures → save
        // ══════════════════════════════════════════════════════════════════════
        private async void BtnEnroll_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            string slotName = txtSlotName.Text.Trim();
            if (string.IsNullOrEmpty(slotName)) { Log("Enter a slot name.", C_YELLOW); txtSlotName.Focus(); return; }

            // Always write to default store (active folder is for reading only)
            if (_store.ExistsInDefault(slotName))
            {
                if (MessageBox.Show($"Slot '{slotName}' already exists. Overwrite?", "Overwrite?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }

            string saveFolder = txtEnrollSaveFolder.Text.Trim();
            if (string.IsNullOrEmpty(saveFolder)) saveFolder = _store.DefaultFolder;

            string imgFolder = txtEnrollImgFolder.Text.Trim();
            if (string.IsNullOrEmpty(imgFolder)) imgFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _cts = new CancellationTokenSource();
            SetBusy(true, btnEnrollNow, btnCancelEnroll);
            ResetDots(); SetEnrollStatus("Starting enrollment…"); UpdateQuality(0);

            try
            {
                FingerprintTemplate? bestTemplate = null;
                int retries = 0; const int MAX_RETRIES = 5;

                for (int scan = 0; scan < 3; scan++)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    int i = scan;
                    SetEnrollStatus($"Scan {i + 1} of 3 — place finger on the scanner…");

                    var (image, err) = await _scanner.CaptureAsync(CAPTURE_TIMEOUT_MS, _cts.Token,
                        onWaiting:   () => SafeInvoke(() => SetEnrollStatus($"Scan {i + 1}/3 — place finger…")),
                        onCapturing: () => SafeInvoke(() => SetEnrollStatus($"Scan {i + 1}/3 — hold still…")));

                    if (image == null)
                    {
                        if (_cts.Token.IsCancellationRequested) break;
                        retries++;
                        Log($"Scan {i + 1} failed: {err} (retry {retries}/{MAX_RETRIES})", C_YELLOW);
                        if (retries >= MAX_RETRIES) { SetEnrollStatus("Too many failures."); break; }
                        scan--; continue;
                    }
                    retries = 0;

                    try
                    {
                        var tpl = new FingerprintTemplate(new FingerprintImage(
                            _scanner.ImageWidth, _scanner.ImageHeight, image,
                            new FingerprintImageOptions { Dpi = SCAN_DPI }));
                        bestTemplate = tpl;
                        LightDot(i, C_GREEN);
                        long sum = 0; foreach (byte b in image) sum += b;
                        UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));
                        Log($"Scan {i + 1}/3 extracted.", C_GREEN);

                        // Save scan image to chosen IMAGE folder
                        string safeName = string.Concat(slotName.Split(Path.GetInvalidFileNameChars()));
                        string imgPath  = Path.Combine(imgFolder, $"FPEnroll_{safeName}_scan{i + 1}_{DateTime.Now:HHmmss}.png");
                        try { SaveGrayscalePng(image, _scanner.ImageWidth, _scanner.ImageHeight, imgPath); Log($"Image saved → {imgPath}", C_ACCENT); }
                        catch (Exception imgEx) { Log($"Image save failed: {imgEx.Message}", C_YELLOW); }
                    }
                    catch (Exception ex) { LightDot(i, C_RED); Log($"Scan {i + 1} failed: {ex.Message} — retrying…", C_YELLOW); scan--; continue; }

                    if (scan < 2)
                    {
                        SetEnrollStatus("Lift finger, then place again…");
                        await System.Threading.Tasks.Task.Delay(900, _cts.Token).ContinueWith(_ => { });
                    }
                }

                if (_cts.Token.IsCancellationRequested) { SetEnrollStatus("Cancelled."); Log("Enroll cancelled.", C_MUTED); return; }
                if (bestTemplate == null) { SetEnrollStatus("✗ No usable template captured."); return; }

                // ── 1:N DUPLICATE CHECK before saving ─────────────────────────
                SetEnrollStatus("Running duplicate check against existing templates…");
                Log("Duplicate check: comparing against all existing templates…", C_MUTED);

                var matcher   = new FingerprintMatcher(bestTemplate);
                var allSlots  = _store.SlotNamesFromDefault();
                string? dupName  = null;
                double  dupScore = 0;

                foreach (var existingSlot in allSlots)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    byte[]? existingBytes = _store.LoadFromDefault(existingSlot);
                    if (existingBytes == null) continue;
                    try
                    {
                        double score = matcher.Match(new FingerprintTemplate(existingBytes));
                        if (score >= MATCH_THRESHOLD && score > dupScore)
                        {
                            dupScore = score;
                            dupName  = existingSlot;
                        }
                    }
                    catch { }
                }

                if (dupName != null)
                {
                    // Duplicate found — warn user, let them decide
                    var choice = MessageBox.Show(
                        $"⚠ Duplicate fingerprint detected!\n\n" +
                        $"This finger already matches slot '{dupName}'\n" +
                        $"(similarity score: {dupScore:F1}, threshold: {MATCH_THRESHOLD})\n\n" +
                        "Save anyway as a new slot?",
                        "Duplicate Detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (choice != DialogResult.Yes)
                    {
                        SetEnrollStatus($"✗ Enroll cancelled — duplicate of '{dupName}'.");
                        Log($"Enroll cancelled: duplicate of '{dupName}' score {dupScore:F1}", C_YELLOW);
                        return;
                    }
                    Log($"User chose to save despite duplicate of '{dupName}' score {dupScore:F1}", C_YELLOW);
                }
                else
                {
                    Log("Duplicate check passed — no existing match found.", C_GREEN);
                }

                // ── Save template ──────────────────────────────────────────────
                byte[] bytes = bestTemplate.ToByteArray();
                bool saved;

                if (saveFolder == _store.DefaultFolder)
                    saved = _store.Save(slotName, bytes, txtNotes.Text.Trim());
                else
                    saved = _store.SaveToFolder(saveFolder, slotName, bytes, txtNotes.Text.Trim());

                if (saved)
                {
                    SetEnrollStatus($"✓ Enrolled '{slotName}'  ({bytes.Length} bytes)");
                    Log($"Enrolled '{slotName}'  {bytes.Length} bytes → template: {saveFolder}", C_GREEN);
                    Log($"  images saved to: {imgFolder}", C_ACCENT);
                    txtSlotName.Clear(); txtNotes.Clear();
                }
                else
                {
                    SetEnrollStatus("Captured but disk save failed.");
                    Log("Save failed — check disk permissions.", C_RED);
                }
            }
            catch (OperationCanceledException) { SetEnrollStatus("Cancelled."); Log("Enroll cancelled.", C_MUTED); }
            catch (Exception ex) { SetEnrollStatus("Error — see log."); Log($"Enroll error: {ex.Message}", C_RED); }
            finally { SetBusy(false, btnEnrollNow, btnCancelEnroll); }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  1:1 Match
        // ══════════════════════════════════════════════════════════════════════
        private async void BtnMatch_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            if (lstSlots.SelectedItem is not SlotListItem sel) { Log("Select a slot first.", C_YELLOW); return; }
            byte[]? stored = _store.Load(sel.SlotName);
            if (stored == null) { Log("Could not load template.", C_RED); return; }

            _cts = new CancellationTokenSource();
            SetBusy(true, btnMatchNow, btnCancelMatch);
            lblMatchResult.Text = ""; lblMatchResult.ForeColor = C_MUTED; UpdateQuality(0);

            try
            {
                var (image, err) = await _scanner.CaptureAsync(CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger to verify…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing…")));

                if (image == null) { SetStage("Capture failed."); Log($"Match failed: {err}", C_RED); }
                else
                {
                    var probe  = new FingerprintTemplate(new FingerprintImage(_scanner.ImageWidth, _scanner.ImageHeight, image, new FingerprintImageOptions { Dpi = SCAN_DPI }));
                    double score = new FingerprintMatcher(probe).Match(new FingerprintTemplate(stored));
                    bool   match = score >= MATCH_THRESHOLD;

                    long sum = 0; foreach (byte b in image) sum += b;
                    UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));

                    lblMatchResult.Text      = match ? $"✓  MATCH   '{sel.SlotName}'   score {score:F1}" : $"✗  NO MATCH   '{sel.SlotName}'   score {score:F1}";
                    lblMatchResult.ForeColor = match ? C_GREEN : C_RED;
                    SetStage(match ? "Match confirmed ✓" : "No match ✗");
                    Log($"{(match ? "MATCH ✓" : "NO MATCH")}  '{sel.SlotName}'  score {score:F1} / threshold {MATCH_THRESHOLD}", match ? C_GREEN : C_RED);
                }
            }
            catch (OperationCanceledException) { SetStage("Cancelled."); Log("Match cancelled.", C_MUTED); }
            catch (Exception ex) { SetStage("Error."); Log($"Match error: {ex.Message}", C_RED); }
            finally { SetBusy(false, btnMatchNow, btnCancelMatch); }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  1:N Identify
        // ══════════════════════════════════════════════════════════════════════
        private async void BtnIdentify_Click(object? s, EventArgs e)
        {
            if (!GuardDevice()) return;
            var slotNames = _store.SlotNames();
            if (slotNames.Count == 0) { Log("No templates in selected folder.", C_YELLOW); lblIdentifyStatus.Text = "No templates found in selected folder."; lblIdentifyStatus.ForeColor = C_YELLOW; return; }

            _cts = new CancellationTokenSource();
            SetBusy(true, btnIdentifyNow, btnCancelIdentify);
            lblIdentifyStatus.Text = "Place finger on scanner…"; lblIdentifyStatus.ForeColor = C_MUTED;
            rtbIdentifyResults.Clear(); UpdateQuality(0);

            try
            {
                var (image, err) = await _scanner.CaptureAsync(CAPTURE_TIMEOUT_MS, _cts.Token,
                    onWaiting:   () => SafeInvoke(() => SetStage("Place finger to identify…")),
                    onCapturing: () => SafeInvoke(() => SetStage("Capturing…")));

                if (image == null) { SetStage("Capture failed."); Log($"Identify failed: {err}", C_RED); lblIdentifyStatus.Text = $"Capture failed: {err}"; lblIdentifyStatus.ForeColor = C_RED; return; }

                long sum = 0; foreach (byte b in image) sum += b;
                UpdateQuality(Math.Clamp((int)(sum / image.Length) / 2, 0, 100));

                var probe   = new FingerprintTemplate(new FingerprintImage(_scanner.ImageWidth, _scanner.ImageHeight, image, new FingerprintImageOptions { Dpi = SCAN_DPI }));
                var matcher = new FingerprintMatcher(probe);

                lblIdentifyStatus.Text = $"Comparing against {slotNames.Count} template(s)…";
                var results = new List<(string Name, double Score)>();

                foreach (var name in slotNames)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    byte[]? tplBytes = _store.Load(name);
                    if (tplBytes == null) continue;
                    try { double score = matcher.Match(new FingerprintTemplate(tplBytes)); results.Add((name, score)); }
                    catch (Exception ex) { Log($"1:N could not match '{name}': {ex.Message}", C_YELLOW); }
                }

                if (_cts.Token.IsCancellationRequested) { SetStage("Cancelled."); Log("1:N cancelled.", C_MUTED); return; }

                results.Sort((a, b2) => b2.Score.CompareTo(a.Score));
                rtbIdentifyResults.Clear();

                var matches = results.Where(r => r.Score >= MATCH_THRESHOLD).ToList();
                if (matches.Count == 0)
                {
                    lblIdentifyStatus.Text = $"✗  No matches in {slotNames.Count} template(s).";
                    lblIdentifyStatus.ForeColor = C_RED; SetStage("No match ✗");
                    Log($"1:N — no match (0/{slotNames.Count} above threshold {MATCH_THRESHOLD})", C_RED);
                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.AppendText($"Scanned against {slotNames.Count} template(s). Threshold: {MATCH_THRESHOLD}\n\nNo templates matched.\n\nTop scores:\n");
                    foreach (var (name, score) in results.Take(5))
                    {
                        rtbIdentifyResults.SelectionColor = C_MUTED;
                        rtbIdentifyResults.AppendText($"  {name,-30} {score,8:F1}\n");
                    }
                }
                else
                {
                    var best = matches[0];
                    lblIdentifyStatus.Text = $"✓  Best: '{best.Name}'  score {best.Score:F1}  ({matches.Count} match{(matches.Count > 1 ? "es" : "")})";
                    lblIdentifyStatus.ForeColor = C_GREEN; SetStage("Identified ✓");
                    Log($"1:N BEST: '{best.Name}' score {best.Score:F1}  ({matches.Count}/{slotNames.Count} matched)", C_GREEN);

                    rtbIdentifyResults.SelectionFont  = new Font("Consolas", 9f, FontStyle.Bold);
                    rtbIdentifyResults.SelectionColor = C_TEXT;
                    rtbIdentifyResults.AppendText($"{"#",-4} {"Slot Name",-30} {"Score",8}  {"Status"}\n");
                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.AppendText(new string('─', 58) + "\n");

                    // Show all results (matches highlighted, non-matches dimmed)
                    for (int i = 0; i < results.Count; i++)
                    {
                        var (name, score) = results[i];
                        bool isMatch = score >= MATCH_THRESHOLD;
                        bool isBest  = (i == 0 && isMatch);
                        rtbIdentifyResults.SelectionFont  = new Font("Consolas", 9f, isBest ? FontStyle.Bold : FontStyle.Regular);
                        rtbIdentifyResults.SelectionColor = isBest ? C_GREEN : isMatch ? C_TEXT : C_MUTED;
                        string tag = isBest ? "★ BEST" : isMatch ? "  match" : "  —";
                        rtbIdentifyResults.AppendText($"{i + 1,-4} {name,-30} {score,8:F1}  {tag}\n");
                    }

                    rtbIdentifyResults.SelectionFont  = new Font("Consolas", 9f, FontStyle.Regular);
                    rtbIdentifyResults.SelectionColor = C_MUTED;
                    rtbIdentifyResults.AppendText($"\n{matches.Count} of {slotNames.Count} matched  (threshold ≥ {MATCH_THRESHOLD})\n");
                }
                rtbIdentifyResults.SelectionStart = 0; rtbIdentifyResults.ScrollToCaret();
            }
            catch (OperationCanceledException) { SetStage("Cancelled."); Log("1:N cancelled.", C_MUTED); }
            catch (Exception ex) { SetStage("Error."); Log($"1:N error: {ex.Message}", C_RED); lblIdentifyStatus.Text = "Error — see log."; lblIdentifyStatus.ForeColor = C_RED; }
            finally { SetBusy(false, btnIdentifyNow, btnCancelIdentify); }
        }

        private void BtnDelete_Click(object? s, EventArgs e)
        {
            if (lstSlots.SelectedItem is not SlotListItem sel) return;
            if (MessageBox.Show($"Delete '{sel.SlotName}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _store.Delete(sel.SlotName); RefreshSlotList(); Log($"Deleted '{sel.SlotName}'.", C_MUTED);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Image → Template
        // ══════════════════════════════════════════════════════════════════════
        private void BtnBrowseImg_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Title = "Select fingerprint PNG", Filter = "PNG images (*.png)|*.png|All files (*.*)|*.*", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            txtImgPath.Text = dlg.FileName;
            try
            {
                picPreview.Image = Image.FromFile(dlg.FileName);
                using var tmp = new Bitmap(dlg.FileName);
                lblConvertStatus.Text = $"Loaded: {tmp.Width}×{tmp.Height} px"; lblConvertStatus.ForeColor = C_MUTED;
                string baseName = Regex.Replace(Path.GetFileNameWithoutExtension(dlg.FileName), @"_\d{6}$", "");
                if (string.IsNullOrEmpty(txtImgSlotName.Text)) txtImgSlotName.Text = baseName;
            }
            catch (Exception ex) { lblConvertStatus.Text = $"Load error: {ex.Message}"; lblConvertStatus.ForeColor = C_RED; }
        }

        private void BtnConvertImg_Click(object? s, EventArgs e)
        {
            if (txtImgPath.Text == "No file selected") { lblConvertStatus.Text = "Select a PNG first."; lblConvertStatus.ForeColor = C_YELLOW; return; }
            string slotName = txtImgSlotName.Text.Trim();
            if (string.IsNullOrEmpty(slotName)) { lblConvertStatus.Text = "Enter a slot name."; lblConvertStatus.ForeColor = C_YELLOW; return; }
            if (_store.ExistsInDefault(slotName) && MessageBox.Show($"Slot '{slotName}' exists. Overwrite?", "Overwrite?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                btnConvertImg.Enabled = false; lblConvertStatus.Text = "Converting…"; lblConvertStatus.ForeColor = C_MUTED;
                using var bmp = new Bitmap(txtImgPath.Text);
                int w = bmp.Width, h = bmp.Height; var pixels = new byte[w * h];
                var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try { unsafe { byte* src = (byte*)bd.Scan0; for (int i = 0; i < w * h; i++) { byte g2 = (byte)((src[i*4]+src[i*4+1]+src[i*4+2])/3); pixels[i] = (byte)(255-g2); } } } finally { bmp.UnlockBits(bd); }
                var tpl = new FingerprintTemplate(new FingerprintImage(w, h, pixels, new FingerprintImageOptions { Dpi = SCAN_DPI }));
                byte[] bytes = tpl.ToByteArray();
                if (_store.Save(slotName, bytes, $"from image: {Path.GetFileName(txtImgPath.Text)}"))
                {
                    lblConvertStatus.Text = $"✓ Saved as '{slotName}'  ({bytes.Length} bytes)"; lblConvertStatus.ForeColor = C_GREEN;
                    Log($"Img→Tpl: '{slotName}'  {bytes.Length} bytes", C_GREEN);
                    txtImgSlotName.Clear(); txtImgPath.Text = "No file selected"; picPreview.Image = null;
                }
                else { lblConvertStatus.Text = "Save failed."; lblConvertStatus.ForeColor = C_RED; }
            }
            catch (Exception ex) { lblConvertStatus.Text = $"✗ {ex.Message}"; lblConvertStatus.ForeColor = C_RED; }
            finally { btnConvertImg.Enabled = true; }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Template → Image
        // ══════════════════════════════════════════════════════════════════════
        private void RefreshExportSlots()
        {
            cmbTplSlot.Items.Clear();
            foreach (var name in _store.SlotNames())
                cmbTplSlot.Items.Add(new SlotListItem(name, $"{name}  [{_store.All[name].EnrolledAt:MM/dd HH:mm}]"));
            if (cmbTplSlot.Items.Count > 0) cmbTplSlot.SelectedIndex = 0;
        }

        private void BtnBrowseFolder_Click(object? s, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = "Select folder to save fingerprint image", UseDescriptionForTitle = true, SelectedPath = txtExportFolder.Text };
            if (dlg.ShowDialog() == DialogResult.OK) txtExportFolder.Text = dlg.SelectedPath;
        }

        private void BtnExportImg_Click(object? s, EventArgs e)
        {
            if (cmbTplSlot.SelectedItem is not SlotListItem sel) { lblExportStatus.Text = "Select a slot first."; lblExportStatus.ForeColor = C_YELLOW; return; }
            string folder = txtExportFolder.Text.Trim();
            if (!Directory.Exists(folder)) { lblExportStatus.Text = "Select a valid folder."; lblExportStatus.ForeColor = C_YELLOW; return; }
            string fileName = Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(txtExportFileName.Text.Trim()) ? $"FP_{sel.SlotName}" : txtExportFileName.Text.Trim());
            string outputPath = Path.Combine(folder, fileName + ".png");
            try
            {
                btnExportImg.Enabled = false; lblExportStatus.Text = "Exporting…"; lblExportStatus.ForeColor = C_MUTED;
                byte[]? tplBytes = _store.Load(sel.SlotName);
                if (tplBytes == null) { lblExportStatus.Text = "Could not load template."; lblExportStatus.ForeColor = C_RED; return; }
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string safe    = string.Concat(sel.SlotName.Split(Path.GetInvalidFileNameChars()));
                string[] cands = Directory.GetFiles(desktop, $"FPEnroll_{safe}_scan1*.png");
                if (cands.Length == 0) cands = Directory.GetFiles(desktop, $"FPEnroll_{safe}*.png");
                if (cands.Length > 0)
                {
                    File.Copy(cands[0], outputPath, overwrite: true);
                    lblExportStatus.Text = $"✓ Exported original scan:\n{outputPath}"; lblExportStatus.ForeColor = C_GREEN;
                    Log($"Tpl→Img: '{sel.SlotName}' → {outputPath}", C_GREEN);
                }
                else
                {
                    ExportTemplatePlaceholder(sel.SlotName, tplBytes, outputPath);
                    lblExportStatus.Text = $"✓ Placeholder exported:\n{outputPath}"; lblExportStatus.ForeColor = C_YELLOW;
                    Log($"Tpl→Img: placeholder for '{sel.SlotName}' → {outputPath}", C_YELLOW);
                }
                try { picExportPreview.Image = Image.FromFile(outputPath); } catch { }
            }
            catch (Exception ex) { lblExportStatus.Text = $"✗ {ex.Message}"; lblExportStatus.ForeColor = C_RED; }
            finally { btnExportImg.Enabled = true; }
        }

        private static void ExportTemplatePlaceholder(string slotName, byte[] tplBytes, string path)
        {
            const int W = 480, H = 640;
            using var bmp = new Bitmap(W, H);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Color.White); g.SmoothingMode = SmoothingMode.AntiAlias;
            var pen = new Pen(Color.FromArgb(180, 180, 180), 1.5f);
            int cx = W/2, cy = H/2-40;
            for (int i = 1; i <= 12; i++) g.DrawArc(pen, cx-i*18, cy-i*22+30, i*36, i*44, 200, 140);
            var sf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(slotName, new Font("Segoe UI Semibold", 14f), Brushes.Black, new RectangleF(20, 20, W-40, 60), sf);
            g.DrawString($"Template: {tplBytes.Length} bytes\nExported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n(placeholder — no original scan found)", new Font("Segoe UI", 9f), Brushes.Gray, new RectangleF(20, H-110, W-40, 90), sf);
            g.DrawRectangle(new Pen(Color.LightGray, 2f), 1, 1, W-2, H-2);
            bmp.Save(path, ImageFormat.Png);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════════════════════
        private void CancelOp() { _cts?.Cancel(); SetStage("Cancelling…"); Log("Cancelled.", C_MUTED); }

        private bool GuardDevice()
        {
            if (_scanner.IsOpen) return true;
            Log("Scanner not connected. Click Connect Scanner.", C_YELLOW); return false;
        }

        private void ActivateTab(Button tab, Panel content)
        {
            pnlTabContent.Controls.Clear();
            pnlTabContent.Controls.Add(content);
            foreach (var b in new[] { btnTabScan, btnTabEnroll, btnTabMatch, btnTabIdentify, btnTabImgToTpl, btnTabTplToImg })
            {
                b.BackColor = b == tab ? C_ACCENT : C_BG;
                b.ForeColor = b == tab ? C_BG     : C_MUTED;
            }
        }

        private void RefreshSlotList()
        {
            lstSlots.Items.Clear();
            foreach (var name in _store.SlotNames())
                lstSlots.Items.Add(new SlotListItem(name, $"{name}  [{_store.All[name].EnrolledAt:MM/dd HH:mm}]"));
        }

        private void SetDeviceLabel(bool on) { if (InvokeRequired) { Invoke(() => SetDeviceLabel(on)); return; } lblDevice.Text = on ? "● Scanner ready" : "● Not connected"; lblDevice.ForeColor = on ? C_GREEN : C_RED; btnConnect.Text = on ? "Disconnect" : "Connect Scanner"; }
        private void SetBusy(bool busy, Button primary, Button cancel) { if (InvokeRequired) { Invoke(() => SetBusy(busy, primary, cancel)); return; } primary.Enabled = !busy; cancel.Visible = busy; btnConnect.Enabled = !busy; Cursor = busy ? Cursors.WaitCursor : Cursors.Default; }
        private void UpdateQuality(int q) { if (InvokeRequired) { Invoke(() => UpdateQuality(q)); return; } pbQuality.Value = Math.Clamp(q, 0, 100); lblQuality.Text = q == 0 ? "—" : $"{q}%"; lblQuality.ForeColor = QualityColor(q); fpVisual.Quality = q; fpVisual.Invalidate(); }
        private void SetStage(string msg) { if (InvokeRequired) { Invoke(() => SetStage(msg)); return; } lblStage.Text = msg; }
        private void SetEnrollStatus(string msg) { if (InvokeRequired) { Invoke(() => SetEnrollStatus(msg)); return; } lblEnrollStatus.Text = msg; }
        private void LightDot(int i, Color c) { if (InvokeRequired) { Invoke(() => LightDot(i, c)); return; } dots[i].BackColor = c; dots[i].Invalidate(); }
        private void ResetDots() { if (InvokeRequired) { Invoke(ResetDots); return; } foreach (var d in dots) { d.BackColor = C_DOT_OFF; d.Invalidate(); } }
        private static Color QualityColor(int q) => q >= 65 ? C_GREEN : q >= 40 ? C_YELLOW : C_RED;
        private void Log(string msg, Color? c = null) { if (rtbLog.InvokeRequired) { rtbLog.Invoke(() => Log(msg, c)); return; } rtbLog.SelectionStart = rtbLog.TextLength; rtbLog.SelectionColor = c ?? C_TEXT; rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  {msg}\n"); rtbLog.ScrollToCaret(); }
        private void SafeInvoke(Action a) { if (InvokeRequired) Invoke(a); else a(); }

        private static void SaveGrayscalePng(byte[] pixels, int width, int height, string path)
        {
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { unsafe { byte* dst = (byte*)data.Scan0; for (int i = 0; i < pixels.Length && i < width*height; i++) { byte v = (byte)(255-pixels[i]); int o = i*4; dst[o]=v; dst[o+1]=v; dst[o+2]=v; dst[o+3]=255; } } } finally { bmp.UnlockBits(data); }
            bmp.Save(path, ImageFormat.Png);
        }

        private static void DotPaint(object? s, PaintEventArgs e)
        {
            if (s is not Panel p) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(new SolidBrush(p.BackColor), 2, 2, p.Width-4, p.Height-4);
            e.Graphics.DrawEllipse(new Pen(Color.FromArgb(80,90,110), 1.5f), 2, 2, p.Width-5, p.Height-5);
        }

        // ── Control factories ─────────────────────────────────────────────────
        private Panel MkPanel(int x, int y, int w, int h, Color bg)
        { var p = new Panel { Location=new Point(x,y), Size=new Size(w,h), BackColor=bg }; Controls.Add(p); return p; }

        private static Label Lbl(string text, int x, int y, Font? font=null, Color? color=null, int w=0, int h=0)
        { var l = new Label { Text=text, Location=new Point(x,y), AutoSize=(w==0), Font=font??SystemFonts.DefaultFont, ForeColor=color??Color.White }; if(w>0)l.Width=w; if(h>0)l.Height=h; return l; }

        private static Button MkBtn(string t, int x, int y, int w, int h, Color bg) =>
            new() { Text=t, Location=new Point(x,y), Size=new Size(w,h), BackColor=bg, ForeColor=bg.GetBrightness()>0.5f?Color.FromArgb(15,17,23):Color.White, FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI Semibold",9.5f), Cursor=Cursors.Hand };

        private static Button MkTabBtn(string t, int x, int y, int w) =>
            new() { Text=t, Location=new Point(x,y), Size=new Size(w,36), BackColor=Color.FromArgb(15,17,23), ForeColor=Color.FromArgb(120,130,150), FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI Semibold",9.5f), Cursor=Cursors.Hand };

        private static TextBox MkFolderBox(int x, int y, int w, string defaultText) =>
            new() { Location=new Point(x,y), Size=new Size(w,28), BackColor=Color.FromArgb(15,17,23), ForeColor=Color.FromArgb(220,225,235), Font=new Font("Segoe UI",9f), BorderStyle=BorderStyle.FixedSingle, ReadOnly=true, Text=defaultText };
    }

    // ── Support classes ───────────────────────────────────────────────────────
    internal class FingerprintVisualPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Quality { get; set; } = 0;
        public FingerprintVisualPanel() { DoubleBuffered = true; BackColor = Color.FromArgb(15, 17, 23); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int cx = Width/2, cy = Height/2, r = Math.Min(Width,Height)/2-8;
            Color ring = Quality==0?Color.FromArgb(50,55,70):Quality>=65?Color.FromArgb(72,199,142):Quality>=40?Color.FromArgb(246,194,62):Color.FromArgb(240,100,90);
            g.DrawEllipse(new Pen(ring,3f), cx-r, cy-r, r*2, r*2);
            g.FillEllipse(new SolidBrush(Color.FromArgb(26,29,38)), cx-r+6, cy-r+6, (r-6)*2, (r-6)*2);
            if (Quality==0) { var rp=new Pen(Color.FromArgb(45,50,65),1.2f); for(int i=1;i<=5;i++) g.DrawArc(rp,cx-i*10,cy-i*10+10,i*20,i*20,200,140); g.DrawString("PLACE\nFINGER",new Font("Segoe UI",7.5f),new SolidBrush(Color.FromArgb(70,80,95)),new RectangleF(cx-28,cy-14,56,30),new StringFormat{Alignment=StringAlignment.Center}); }
            else { g.DrawString($"{Quality}%",new Font("Segoe UI Semibold",18f),new SolidBrush(ring),new RectangleF(cx-35,cy-18,70,28),new StringFormat{Alignment=StringAlignment.Center}); g.DrawString("quality",new Font("Segoe UI",7.5f),new SolidBrush(Color.FromArgb(120,130,150)),new RectangleF(cx-30,cy+12,60,16),new StringFormat{Alignment=StringAlignment.Center}); }
        }
    }

    internal class SlotListItem
    {
        public string SlotName { get; }
        private readonly string _display;
        public SlotListItem(string name, string display) { SlotName=name; _display=display; }
        public override string ToString() => _display;
    }
}
