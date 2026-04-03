using Microsoft.EntityFrameworkCore;
using InstagramClone.Api.Models;

namespace InstagramClone.Api.Data;

public class InstagramContext : DbContext
{
    public InstagramContext(DbContextOptions<InstagramContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Like> Likes { get; set; } = null!;
    public DbSet<Follower> Followers { get; set; } = null!;
    public DbSet<MediaRating> MediaRatings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Bio).HasMaxLength(500);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired().HasDefaultValue("Consumer");

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Role);
        });

        // Post configuration
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).HasMaxLength(120).IsRequired();
            entity.Property(p => p.Caption).HasMaxLength(2200);
            entity.Property(p => p.Location).HasMaxLength(255);
            entity.Property(p => p.PeoplePresent).HasMaxLength(500);
            entity.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(p => p.BlobPath).HasMaxLength(500).IsRequired();
            entity.Property(p => p.AverageRating).HasPrecision(5, 2);

            entity.HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => new { p.IsActive, p.CreatedAt }).IsDescending(false, true);
            entity.HasIndex(p => p.Title);
            entity.HasIndex(p => p.Location);
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).HasMaxLength(500).IsRequired();

            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.PostId);
            entity.HasIndex(c => c.UserId);
        });

        // Like configuration (composite key)
        modelBuilder.Entity<Like>(entity =>
        {
            entity.ToTable("Likes");
            entity.HasKey(l => new { l.UserId, l.PostId });

            entity.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.PostId);
        });

        // MediaRating configuration (composite key)
        modelBuilder.Entity<MediaRating>(entity =>
        {
            entity.ToTable("MediaRatings");
            entity.HasKey(r => new { r.UserId, r.PostId });
            entity.Property(r => r.RatingValue).IsRequired();

            entity.HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Post)
                .WithMany(p => p.Ratings)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.PostId);
        });

        // Follower configuration (composite key)
        modelBuilder.Entity<Follower>(entity =>
        {
            entity.ToTable("Followers");
            entity.HasKey(f => new { f.FollowerId, f.FolloweeId });

            entity.HasOne(f => f.FollowerUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.FolloweeUser)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(f => f.FolloweeId);
        });
    }
}
