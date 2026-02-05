namespace ip_connect.DTOs
{
    public class AlbumDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        //This will be populated later when we add Photos
        public int PhotoCount { get; set; }

        //This will be populated later when we add cover photos
        public string? CoverPhotoUrl { get; set; }
    }
}