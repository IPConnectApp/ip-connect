using ip_connect.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ip_connect.Data.Configurations
{
    public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> builder)
        {
            builder.HasOne(p => p.Album)
                .WithMany(a => a.Photos)
                .HasForeignKey(p => p.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);


            builder.Property(p => p.PhotoUrl)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(p => p.UploadedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(p => p.AlbumId);
            builder.HasIndex(p => p.UserId);
        }
    }
}