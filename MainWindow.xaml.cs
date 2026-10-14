using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScriptNote;

public partial class MainWindow : Window
{
    private readonly string statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScriptNote", "state.json");
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool ready;
    private bool preserveUnreadableFile;
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowDisplayAffinity(IntPtr window, out uint affinity);

    public MainWindow()
    {
        InitializeComponent();
        Restore();
        UpdateFontLabel();
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); Save(); };
        SourceInitialized += (_, _) => EnableCaptureExclusion();
        Loaded += (_, _) => { EnsureVisible(); ready = true; Editor.Focus(); };
        LocationChanged += (_, _) => ScheduleSave();
        SizeChanged += (_, _) => ScheduleSave();
        Closing += OnClosing;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += DisplayChanged;
        Closed += (_, _) => Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= DisplayChanged;
    }

    private void EnableCaptureExclusion()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (!SetWindowDisplayAffinity(handle, 0x11))
        {
            CaptureStatus.Text = $"Không bật được ẩn khỏi quay: {new Win32Exception(Marshal.GetLastWin32Error()).Message}";
            CaptureStatus.Foreground = Brushes.Firebrick;
            return;
        }
        if (!GetWindowDisplayAffinity(handle, out var affinity) || affinity != 0x11)
        {
            CaptureStatus.Text = "Windows chưa xác nhận chế độ ẩn khỏi quay. Hãy kiểm tra bản quay.";
            CaptureStatus.Foreground = Brushes.Firebrick;
            return;
        }
        CaptureStatus.Text = "Đã bật ẩn khỏi quay · Cần quay thử OBS";
    }

    private void Restore()
    {
        if (!File.Exists(statePath)) return;
        try
        {
            var state = JsonSerializer.Deserialize<NoteState>(File.ReadAllText(statePath)) ?? throw new InvalidDataException("Tệp lưu trống.");
            Editor.Text = state.Text ?? "";
            Editor.FontSize = double.IsFinite(state.FontSize) ? Math.Clamp(state.FontSize, 14, 48) : 22;
            Width = double.IsFinite(state.Width) ? Math.Clamp(state.Width, MinWidth, 4000) : 400;
            Height = double.IsFinite(state.Height) ? Math.Clamp(state.Height, MinHeight, 4000) : 480;
            if (double.IsFinite(state.Left) && double.IsFinite(state.Top)) { Left = state.Left; Top = state.Top; }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            ShowSaveError("Không đọc được ghi chú đã lưu. Tệp cũ vẫn được giữ. " + e.Message);
            // Preserve the unreadable file before any later successful save replaces it.
            try { File.Copy(statePath, statePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), false); }
            catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException)
            { preserveUnreadableFile = true; ShowSaveError("Không đọc hoặc sao lưu được tệp cũ. " + backupError.Message); }
        }
    }

    private void EnsureVisible()
    {
        var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        var dpi = VisualTreeHelper.GetDpi(this);
        var work = screen.WorkingArea;
        double left = work.Left / dpi.DpiScaleX, top = work.Top / dpi.DpiScaleY;
        double width = work.Width / dpi.DpiScaleX, height = work.Height / dpi.DpiScaleY;
        Width = Math.Min(Width, width); Height = Math.Min(Height, height);
        if (!double.IsFinite(Left)) Left = left + 30;
        if (!double.IsFinite(Top)) Top = top + 30;
        Left = Math.Clamp(Left, left, Math.Max(left, left + width - Width));
        Top = Math.Clamp(Top, top, Math.Max(top, top + height - Height));
    }

    private void ScheduleSave() { if (!ready) return; saveTimer.Stop(); saveTimer.Start(); }
    private void DisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(new Action(() => { EnsureVisible(); ScheduleSave(); }));
    private void Save()
    {
        if (preserveUnreadableFile) { ShowSaveError("Tệp cũ chưa sao lưu được. Hãy sao chép kịch bản và xử lý tệp state.json trước khi đóng."); return; }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            var state = new NoteState { Text = Editor.Text, FontSize = Editor.FontSize, Left = Left, Top = Top, Width = Width, Height = Height };
            File.WriteAllText(statePath + ".tmp", JsonSerializer.Serialize(state));
            File.Move(statePath + ".tmp", statePath, true);
            SaveStatus.Visibility = Visibility.Collapsed;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { ShowSaveError("Không lưu được. Hãy sao chép kịch bản để giữ nội dung. " + e.Message); }
    }
    private void ShowSaveError(string message) { SaveStatus.Text = message; SaveStatus.Visibility = Visibility.Visible; }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        saveTimer.Stop(); Save();
        if (SaveStatus.Visibility == Visibility.Visible)
        {
            e.Cancel = true;
            Editor.Focus();
        }
    }
    private void UpdateFontLabel() => FontLabel.Text = Editor.FontSize.ToString("0");
    private void ChangeFont(double delta) { Editor.FontSize = Math.Clamp(Editor.FontSize + delta, 14, 48); UpdateFontLabel(); ScheduleSave(); }
    private void Smaller(object sender, RoutedEventArgs e) => ChangeFont(-2);
    private void Larger(object sender, RoutedEventArgs e) => ChangeFont(2);
    private void ContentChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ScheduleSave();
    private void DragHeader(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void CloseNote(object sender, RoutedEventArgs e) => Close();
    private void MinimizeNote(object sender, RoutedEventArgs e) { Save(); WindowState = WindowState.Minimized; }
}

public sealed class NoteState
{
    public string? Text { get; set; } = "";
    public double FontSize { get; set; } = 22;
    public double Left { get; set; } = 30;
    public double Top { get; set; } = 30;
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 480;
}
