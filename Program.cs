using System.Text.Json;
using System.Text.Json.Nodes;

// ================================================================
//  KQXS Crawler v2 - Dùng API của xoso.me (ổn định, miễn phí)
//  .NET 8
// ================================================================

Console.WriteLine("=== KQXS Crawler bắt đầu ===");
Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

var crawler = new KqxsCrawler();
await crawler.RunAsync();

Console.WriteLine("=== Hoàn thành! ===");

public class KqxsCrawler
{
    private readonly HttpClient _http;
    private readonly string _outputDir;

    public KqxsCrawler()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.Timeout = TimeSpan.FromSeconds(30);

        _outputDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(_outputDir);
    }

    public async Task RunAsync()
    {
        var today = DateTime.Now;
        var allResults = new List<KqxsResult>();

        // 3 nguồn API thử lần lượt
        var apis = new[]
        {
            "https://xsapi.com.vn",
            "https://api.xoso.net",
            "https://sxmb.net/api",
        };

        Console.WriteLine("\n--- Crawl Miền Bắc ---");
        await TryCrawlMB(allResults, today, apis);

        Console.WriteLine("\n--- Crawl Miền Trung ---");
        await TryCrawlMT(allResults, today);

        Console.WriteLine("\n--- Crawl Miền Nam ---");
        await TryCrawlMN(allResults, today);

        await SaveJsonAsync(allResults, today);
        Console.WriteLine($"\nTổng cộng: {allResults.Count} tỉnh/thành");
    }

    private async Task TryCrawlMB(List<KqxsResult> results, DateTime date, string[] apis)
    {
        // API miền Bắc: api.xoso.net/api/xsmb.js
        try
        {
            var url = $"https://api.xoso.net/api/xsmb.js";
            var json = await _http.GetStringAsync(url);
            var node = JsonNode.Parse(json);
            var data = node?["data"]?["t"];
            if (data != null)
            {
                var result = new KqxsResult
                {
                    ProvinceCode = "xsmb",
                    ProvinceName = "Miền Bắc",
                    Region = "MB",
                    DrawDate = date.ToString("yyyy-MM-dd"),
                    DrawDateDisplay = date.ToString("dd/MM/yyyy"),
                    DayOfWeek = GetDayOfWeek(date.DayOfWeek),
                    Prizes = new Dictionary<string, List<string>>()
                };

                var prizeNames = new[] { "Giải ĐB", "Giải Nhất", "Giải Nhì", "Giải Ba", "Giải Tư", "Giải Năm", "Giải Sáu", "Giải Bảy" };
                var prizeKeys = new[] { "db", "g1", "g2", "g3", "g4", "g5", "g6", "g7" };

                for (int i = 0; i < prizeKeys.Length; i++)
                {
                    var val = data[prizeKeys[i]];
                    if (val == null) continue;
                    var nums = new List<string>();
                    if (val is JsonArray arr)
                        nums = arr.Select(x => x?.ToString() ?? "").Where(x => x != "").ToList();
                    else
                        nums = new List<string> { val.ToString() };
                    if (nums.Any()) result.Prizes[prizeNames[i]] = nums;
                }

                if (result.Prizes.Any())
                {
                    results.Add(result);
                    Console.WriteLine("  ✓ Miền Bắc");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ API 1 lỗi: {ex.Message}");
        }

        // Fallback: sxmb.net
        try
        {
            var url = "https://sxmb.net/api/xsmb-hom-nay";
            var json = await _http.GetStringAsync(url);
            ParseGenericApi(json, "xsmb", "Miền Bắc", "MB", date, results);
            Console.WriteLine("  ✓ Miền Bắc (fallback)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Tất cả API MB đều lỗi: {ex.Message}");
        }
    }

    private async Task TryCrawlMT(List<KqxsResult> results, DateTime date)
    {
        try
        {
            var url = $"https://api.xoso.net/api/xsmt.js";
            var json = await _http.GetStringAsync(url);
            ParseXosoNetApi(json, "MT", date, results);
            Console.WriteLine($"  ✓ Miền Trung ({results.Count(r => r.Region == "MT")} tỉnh)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {ex.Message}");
        }
    }

    private async Task TryCrawlMN(List<KqxsResult> results, DateTime date)
    {
        try
        {
            var url = $"https://api.xoso.net/api/xsmn.js";
            var json = await _http.GetStringAsync(url);
            ParseXosoNetApi(json, "MN", date, results);
            Console.WriteLine($"  ✓ Miền Nam ({results.Count(r => r.Region == "MN")} tỉnh)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {ex.Message}");
        }
    }

    private void ParseXosoNetApi(string json, string region, DateTime date, List<KqxsResult> results)
    {
        var node = JsonNode.Parse(json);
        var items = node?["data"]?.AsArray();
        if (items == null) return;

        var prizeNames = new[] { "Giải ĐB", "Giải Nhất", "Giải Nhì", "Giải Ba", "Giải Tư", "Giải Năm", "Giải Sáu", "Giải Bảy", "Giải Tám" };
        var prizeKeys = new[] { "db", "g1", "g2", "g3", "g4", "g5", "g6", "g7", "g8" };

        foreach (var item in items)
        {
            if (item == null) continue;
            var t = item["t"];
            if (t == null) continue;

            var result = new KqxsResult
            {
                ProvinceCode = item["id"]?.ToString() ?? "",
                ProvinceName = item["n"]?.ToString() ?? "",
                Region = region,
                DrawDate = date.ToString("yyyy-MM-dd"),
                DrawDateDisplay = date.ToString("dd/MM/yyyy"),
                DayOfWeek = GetDayOfWeek(date.DayOfWeek),
                Prizes = new Dictionary<string, List<string>>()
            };

            for (int i = 0; i < prizeKeys.Length; i++)
            {
                var val = t[prizeKeys[i]];
                if (val == null) continue;
                var nums = new List<string>();
                if (val is JsonArray arr)
                    nums = arr.Select(x => x?.ToString() ?? "").Where(x => x != "").ToList();
                else
                    nums = new List<string> { val.ToString() };
                if (nums.Any()) result.Prizes[prizeNames[i]] = nums;
            }

            if (result.Prizes.Any()) results.Add(result);
        }
    }

    private void ParseGenericApi(string json, string code, string name, string region, DateTime date, List<KqxsResult> results)
    {
        var node = JsonNode.Parse(json);
        var result = new KqxsResult
        {
            ProvinceCode = code,
            ProvinceName = name,
            Region = region,
            DrawDate = date.ToString("yyyy-MM-dd"),
            DrawDateDisplay = date.ToString("dd/MM/yyyy"),
            DayOfWeek = GetDayOfWeek(date.DayOfWeek),
            Prizes = new Dictionary<string, List<string>>()
        };

        var prizes = node?["prizes"] ?? node?["data"]?["prizes"];
        if (prizes != null)
        {
            foreach (var prop in prizes.AsObject())
            {
                var nums = prop.Value?.AsArray()
                    .Select(n => n?.ToString() ?? "")
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList() ?? new();
                if (nums.Any()) result.Prizes[prop.Key] = nums;
            }
        }

        if (result.Prizes.Any()) results.Add(result);
    }

    private async Task SaveJsonAsync(List<KqxsResult> results, DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var output = new
        {
            date = dateStr,
            dateDisplay = date.ToString("dd/MM/yyyy"),
            dayOfWeek = GetDayOfWeek(date.DayOfWeek),
            updatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            mienBac = results.Where(r => r.Region == "MB").ToList(),
            mienTrung = results.Where(r => r.Region == "MT").ToList(),
            mienNam = results.Where(r => r.Region == "MN").ToList(),
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(output, options);

        await File.WriteAllTextAsync(Path.Combine(_outputDir, $"kqxs-{dateStr}.json"), json);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "latest.json"), json);

        // Cập nhật index
        var indexPath = Path.Combine(_outputDir, "index.json");
        List<string> index = new();
        if (File.Exists(indexPath))
        {
            var existing = await File.ReadAllTextAsync(indexPath);
            index = JsonSerializer.Deserialize<List<string>>(existing) ?? new();
        }
        if (!index.Contains(dateStr)) index.Insert(0, dateStr);
        index = index.Take(90).ToList();
        await File.WriteAllTextAsync(indexPath, JsonSerializer.Serialize(index, options));

        Console.WriteLine($"\nĐã lưu → data/kqxs-{dateStr}.json");
        Console.WriteLine("Đã lưu → data/latest.json");
    }

    private string GetDayOfWeek(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        DayOfWeek.Sunday => "Chủ Nhật",
        _ => ""
    };
}

public class KqxsResult
{
    public string ProvinceCode { get; set; } = "";
    public string ProvinceName { get; set; } = "";
    public string Region { get; set; } = "";
    public string DrawDate { get; set; } = "";
    public string DrawDateDisplay { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public Dictionary<string, List<string>> Prizes { get; set; } = new();
}