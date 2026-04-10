namespace Domain.Entities;

public class OcrImage
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}