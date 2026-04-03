namespace InstagramClone.Api.DTOs;

// Auth DTOs
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public int ExpiresIn { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public bool IsFollowing { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

// Post DTOs
public class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PeoplePresent { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
}

public class PostDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PeoplePresent { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public UserSummaryDto? User { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

public class UploadUrlResponse
{
    public string UploadUrl { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 15;
}

// Comment DTOs
public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto? User { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RatePostRequest
{
    public int Rating { get; set; }
}

public class PostRatingDto
{
    public Guid PostId { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int? CurrentUserRating { get; set; }
}
