# ScriptNote

Ghi chú đọc kịch bản trên Windows 10 phiên bản 2004 trở lên và Windows 11.

## Sử dụng

1. Mở `release/ScriptNote.exe`.
2. Gõ hoặc dán kịch bản. Kéo thanh tiêu đề để di chuyển, kéo cạnh/góc để đổi kích thước.
3. Dùng A− / A+ để chỉnh cỡ chữ từ 14–48; cuộn bằng chuột khi kịch bản dài.
4. Đóng bằng × hoặc Alt+F4. Nội dung và vị trí tự lưu tại `%LocalAppData%\ScriptNote\state.json`.

Nếu lưu thất bại, ứng dụng giữ cửa sổ mở để bạn sao chép nội dung. Tệp lưu hỏng được sao lưu với hậu tố `.corrupt-<thời gian>` trước khi ghi tệp mới. Không có kết nối mạng trong ứng dụng.

## Kiểm tra với OBS trước khi quay chính thức

Windows xác nhận `WDA_EXCLUDEFROMCAPTURE` không đồng nghĩa mọi phương thức quay đều hỗ trợ. Xem cả tệp video, không chỉ cửa sổ preview.

1. Trong OBS tạo nguồn **Display Capture** cho màn hình có ghi chú.
2. Mở nội dung chuyển động phía sau ghi chú, chẳng hạn video hoặc trang đang cuộn.
3. Quay thử 15–30 giây; trong lúc quay nhập tiếng Việt, cuộn, di chuyển và đổi kích thước ghi chú.
4. Xem video: ghi chú phải biến mất hoàn toàn, không có ô đen/bóng, nội dung phía sau vẫn đầy đủ.
5. Nếu không đạt, thử phương thức capture khác trong Properties của Display Capture, gồm Windows Graphics Capture nếu OBS cung cấp; ghi lại phương thức đạt yêu cầu.

OBS được phát hiện trên máy: **32.2.2**. Chưa nghiệm thu bằng video OBS thực tế. Không cam kết ẩn khỏi mọi phần mềm quay hay camera bên ngoài. Luôn nổi áp dụng cho cửa sổ thông thường, không bảo đảm với ứng dụng fullscreen độc quyền.

## Build

Yêu cầu .NET 10 SDK trên Windows:

```powershell
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o release
```

Bản phát hành x64 tự chứa, không cần cài .NET. Lần đầu chạy .NET có thể giải nén thư viện native vào thư mục tạm của người dùng.
