using System.Diagnostics;
using LinkShorter.Data;
using Microsoft.AspNetCore.Mvc;
using LinkShorter.Models;
using LinkShorter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShorter.Controllers;

public class HomeController (
        AppDbContext context,
        UrlShortenerService shortenerService,
        IMemoryCache cache //TODO? Заменить на Redis
    ) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(string longUrl)
    {
        if (string.IsNullOrEmpty(longUrl) || !Uri.IsWellFormedUriString(longUrl, UriKind.Absolute))
        {
            ModelState.AddModelError("", "Введите корректную ссылку (начиная с http/https)");
            return View("Index", await context.Urls.ToListAsync());
        }
        
        string code;
        do
        {
            code = shortenerService.GenerateCode();
        } while (await context.Urls.AnyAsync(u => u.ShortCode == code));
        
        var newUrl = new ShortUrl
        {
            LongUrl = longUrl,
            ShortCode = code,
            CreatedAt = DateTime.UtcNow,
            ClickCount = 0
        };

        context.Add(newUrl);
        await context.SaveChangesAsync();

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{newUrl.ShortCode}";
        TempData["LastGeneratedUrl"] = shortUrl;
        TempData["LastShortCode"] = newUrl.ShortCode;
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var urlEntry = await context.Urls.FindAsync(id);
        if (urlEntry == null) return NotFound();

        return View(urlEntry);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string longUrl)
    {
        var urlEntry = await context.Urls.FindAsync(id);
        if (urlEntry == null) return NotFound();

        if (string.IsNullOrEmpty(longUrl) || !Uri.IsWellFormedUriString(longUrl, UriKind.Absolute))
        {
            ModelState.AddModelError("", "Введите корректную ссылку (начиная с http/https)");
            return View(urlEntry);
        }

        urlEntry.LongUrl = longUrl;
        cache.Remove(urlEntry.ShortCode);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var urlEntry = await context.Urls.FindAsync(id);
    
        if (urlEntry != null)
        {
            cache.Remove(urlEntry.ShortCode);
            context.Urls.Remove(urlEntry);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    
    [HttpGet("/{code}")]
    public async Task<IActionResult> RedirectTo(string code)
    {
        if (!cache.TryGetValue(code, out string? longUrl))
        {
            var urlEntry = await context.Urls.FirstOrDefaultAsync(u => u.ShortCode == code);
        
            if (urlEntry == null) return NotFound();

            longUrl = urlEntry.LongUrl;
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            cache.Set(code, longUrl, cacheEntryOptions);
        }

        _ = Task.Run(async () => {
            var entry = await context.Urls.FirstOrDefaultAsync(u => u.ShortCode == code);
            if (entry != null) {
                entry.ClickCount++;
                await context.SaveChangesAsync();
            }
        });

        return Redirect(longUrl!);
    }
    
    public async Task<IActionResult> Index()
    {
        var urls = await context.Urls.OrderByDescending(u => u.CreatedAt).ToListAsync();

        return View(urls);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}