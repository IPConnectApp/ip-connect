using ip_connect.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ip_connect.Data.Configurations
{
    public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            // Конфигурация на връзката с Album (One-to-Many)
            builder.HasOne(p => p.Album)
                .WithMany() // Ако по-късно добавиш ICollection<Photo> в Album.cs, сложи го тук: .WithMany(a => a.Photos)
                .HasForeignKey(p => p.AlbumId)
                .OnDelete(DeleteBehavior.Cascade); // Ако се изтрие албум, се трият и снимките в него

            // Конфигурация на връзката с User
            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Предпазва от случайно изтриване на потребител със снимки

            // Ограничения и настройки на полетата
            builder.Property(p => p.PhotoUrl)
                .IsRequired()
                .HasMaxLength(2048); // Стандартна дължина за URL

            builder.Property(p => p.UploadedAt)
                .HasDefaultValueSql("GETUTCDATE()"); // Автоматично задаване на дата на сървърно ниво

            // Индекси за по-бързо търсене
            builder.HasIndex(p => p.AlbumId);
            builder.HasIndex(p => p.UserId);
        }
    }
}
