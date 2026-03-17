# KQXS Crawler + Website
## Hệ thống kết quả xổ số 3 miền tự động - .NET 8

---

## Cấu trúc thư mục

```
KqxsCrawler/
├── Program.cs              ← C# Crawler chính
├── KqxsCrawler.csproj      ← Project file
└── docs/                   ← Thư mục GitHub Pages
    ├── index.html          ← Website hiển thị
    └── data/               ← JSON data (do crawler tạo ra)
        ├── latest.json     ← Kết quả mới nhất
        ├── index.json      ← Danh sách các ngày
        └── kqxs-YYYY-MM-DD.json
```

---

## Bước 1: Cài đặt

```bash
# Cài dependency
cd KqxsCrawler
dotnet restore

# Build thử
dotnet build
```

---

## Bước 2: Chạy crawler lần đầu

```bash
dotnet run
```

Sau khi chạy, kiểm tra thư mục `docs/data/` sẽ có file `latest.json`.

---

## Bước 3: Deploy lên GitHub Pages (Miễn phí)

1. Tạo tài khoản tại https://github.com
2. Tạo repository mới, tên ví dụ: `kqxs-hom-nay`
3. Upload toàn bộ thư mục `docs/` lên repo
4. Vào **Settings → Pages → Source: Deploy from branch → Branch: main → Folder: /docs**
5. Sau ~2 phút, website sẽ có tại: `https://tenban.github.io/kqxs-hom-nay`

---

## Bước 4: Tự động chạy mỗi ngày (Windows Task Scheduler)

1. Mở **Task Scheduler** (tìm trong Start Menu)
2. Chọn **Create Basic Task**
3. Name: `KQXS Crawler Daily`
4. Trigger: **Daily** → Đặt giờ **19:30** (sau khi xổ số xong)
5. Action: **Start a program**
   - Program: `dotnet`
   - Arguments: `run --project C:\path\to\KqxsCrawler\KqxsCrawler.csproj`
6. Finish

**Sau đó cần tự động commit lên GitHub:**

Tạo file `auto-push.bat` cùng thư mục:
```bat
@echo off
cd /d C:\path\to\KqxsCrawler
dotnet run
cd docs
git add -A
git commit -m "Update KQXS %date%"
git push origin main
```

Trong Task Scheduler, trỏ vào file `.bat` này thay vì `dotnet run`.

---

## Bước 5: Đăng ký Google AdSense

1. Vào https://adsense.google.com
2. Thêm website của bạn (ví dụ: `tenban.github.io`)
3. Copy đoạn code AdSense vào `docs/index.html` (phần comment đã đánh dấu sẵn)
4. Chờ Google duyệt (~1-2 tuần)

---

## Tips tăng traffic SEO

- Đặt tên domain riêng (mua tại tenmien.vn ~50k/năm)
- Tạo thêm trang: `/lich-xo-so`, `/thong-ke`, `/xsmb`, `/xsmn`
- Chia sẻ kết quả lên các group Facebook xổ số mỗi ngày
- Đăng ký Google Search Console để Google index nhanh hơn
