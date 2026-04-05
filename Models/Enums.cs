namespace InstagramClone.Api.Models;

public class Like
{
    public string Id { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string CreateDocumentId(Guid userId, Guid postId) => $"{userId:N}:{postId:N}";

    // Navigation properties
    public User? User { get; set; }
    public Post? Post { get; set; }
}

public class Follower
{
    public string Id { get; set; } = string.Empty;
    public Guid FollowerId { get; set; } // User who follows
    public Guid FolloweeId { get; set; } // User being followed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string CreateDocumentId(Guid followerId, Guid followeeId) => $"{followerId:N}:{followeeId:N}";

    // Navigation properties
    public User? FollowerUser { get; set; }
    public User? FolloweeUser { get; set; }
}

public class MediaRating
{
    public string Id { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public int RatingValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string CreateDocumentId(Guid userId, Guid postId) => $"{userId:N}:{postId:N}";

    // Navigation properties
    public User? User { get; set; }
    public Post? Post { get; set; }
}
