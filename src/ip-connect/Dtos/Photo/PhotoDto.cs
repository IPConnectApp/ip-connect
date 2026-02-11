namespace ip_connect.Dtos.Photo
{
    public class PhotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
