using System.ComponentModel.DataAnnotations;

namespace LinkShorter.Models;

public class ShortUrl
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Введите длинную ссылку")]
    [Url(ErrorMessage = "Неверный формат ссылки")]
    public string LongUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ClickCount { get; set; }
}