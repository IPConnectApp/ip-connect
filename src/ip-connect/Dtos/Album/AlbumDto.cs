namespace ip_connect.DTOs
{
    public class AlbumDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PhotoCount { get; set; }
        public string? CoverPhotoUrl { get; set; }
    }
}