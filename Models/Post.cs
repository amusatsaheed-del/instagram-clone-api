namespace InstagramClone.Api.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PeoplePresent { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty; // Azure Blob path
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<MediaRating> Ratings { get; set; } = new List<MediaRating>();
}
