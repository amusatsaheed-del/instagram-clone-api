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

        ConfigureSharedModel(modelBuilder);
        ConfigureCosmosModel(modelBuilder);
    }

    private static void ConfigureSharedModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Bio).HasMaxLength(500);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).HasMaxLength(120).IsRequired();
            entity.Property(p => p.Caption).HasMaxLength(2200);
            entity.Property(p => p.Location).HasMaxLength(255);
            entity.Property(p => p.PeoplePresent).HasMaxLength(500);
            entity.Property(p => p.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(p => p.BlobPath).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).HasMaxLength(500).IsRequired();
        });
    }

    private static void ConfigureCosmosModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasManualThroughput(400);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToContainer("Users");
            entity.HasPartitionKey(u => u.Id);
            entity.Ignore(u => u.Posts);
            entity.Ignore(u => u.Comments);
            entity.Ignore(u => u.Likes);
            entity.Ignore(u => u.Ratings);
            entity.Ignore(u => u.Followers);
            entity.Ignore(u => u.Following);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToContainer("Posts");
            entity.HasPartitionKey(p => p.UserId);
            entity.Ignore(p => p.User);
            entity.Ignore(p => p.Comments);
            entity.Ignore(p => p.Likes);
            entity.Ignore(p => p.Ratings);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToContainer("Comments");
            entity.HasPartitionKey(c => c.PostId);
            entity.Ignore(c => c.Post);
            entity.Ignore(c => c.User);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.ToContainer("Likes");
            entity.HasKey(l => l.Id);
            entity.HasPartitionKey(l => l.PostId);
            entity.Ignore(l => l.User);
            entity.Ignore(l => l.Post);
        });

        modelBuilder.Entity<MediaRating>(entity =>
        {
            entity.ToContainer("MediaRatings");
            entity.HasKey(r => r.Id);
            entity.HasPartitionKey(r => r.PostId);
            entity.Property(r => r.RatingValue).IsRequired();
            entity.Ignore(r => r.User);
            entity.Ignore(r => r.Post);
        });

        modelBuilder.Entity<Follower>(entity =>
        {
            entity.ToContainer("Followers");
            entity.HasKey(f => f.Id);
            entity.HasPartitionKey(f => f.FolloweeId);
            entity.Ignore(f => f.FollowerUser);
            entity.Ignore(f => f.FolloweeUser);
        });
    }
}
