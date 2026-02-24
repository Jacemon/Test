using System.ComponentModel.DataAnnotations;

namespace LinkShorter.Models;

public class ShortUrl
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the URL")]
    [Url(ErrorMessage = "Incorrect link format")]
    public string LongUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ClickCount { get; set; }
}