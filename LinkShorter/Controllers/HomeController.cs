using System.Diagnostics;
using LinkShorter.Data;
using Microsoft.AspNetCore.Mvc;
using LinkShorter.Models;
using LinkShorter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShorter.Controllers;

public class HomeController (
        ClickBuffer buffer,
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
            return View("Index", await context.Urls.ToListAsync()); ;
        }
        
        var maxRetries = 5;
        var retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            var newUrl = new ShortUrl
            {
                ShortCode = shortenerService.GenerateCode(),
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            };
            
            try
            {
                context.Urls.Add(newUrl);
                await context.SaveChangesAsync();
            
                var shortUrl = $"{Request.Scheme}://{Request.Host}/r/{newUrl.ShortCode}";
                TempData["LastGeneratedUrl"] = shortUrl;
                TempData["LastShortCode"] = newUrl.ShortCode;
        
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                context.Entry(newUrl).State = EntityState.Detached;
                retryCount++;
            }
        }
        throw new Exception("Failed to generate a unique short code after several attempts.");
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
            buffer.Remove(urlEntry.ShortCode);
            context.Urls.Remove(urlEntry);
            await context.SaveChangesAsync();
        }
        
        return RedirectToAction(nameof(Index));
    }

    
    [HttpGet("/r/{code}")]
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

        buffer.AddClick(code);

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