using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InstagramClone.Api.DTOs;
using InstagramClone.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InstagramClone.Api.Models;

namespace InstagramClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly ILogger<PostsController> _logger;
    private readonly InstagramContext _context;

    public PostsController(ILogger<PostsController> logger, InstagramContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet("feed")]
    public async Task<ActionResult<List<PostDto>>> GetFeed(int page = 1, int pageSize = 20)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(await BuildPostDtosAsync(posts, currentUserId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting feed: {ex.Message}");
            return StatusCode(500, "Error retrieving feed");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<PostDto>>> Search(
        [FromQuery] string? query,
        [FromQuery] string? location,
        [FromQuery] string? person,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var postsQuery = _context.Posts
                .AsNoTracking()
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLower();
                postsQuery = postsQuery.Where(p =>
                    p.Title.ToLower().Contains(q) ||
                    p.Caption.ToLower().Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim().ToLower();
                postsQuery = postsQuery.Where(p => p.Location != null && p.Location.ToLower().Contains(loc));
            }

            if (!string.IsNullOrWhiteSpace(person))
            {
                var tagged = person.Trim().ToLower();
                postsQuery = postsQuery.Where(p => p.PeoplePresent != null && p.PeoplePresent.ToLower().Contains(tagged));
            }

            var posts = await postsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(await BuildPostDtosAsync(posts, currentUserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts");
            return StatusCode(500, "Error searching posts");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Creator")]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required");
            }

            if (string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest("ImageUrl is required");
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive);
            if (user == null)
            {
                return Unauthorized();
            }

            if (!string.Equals(user.Role, "Creator", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Title = request.Title.Trim(),
                Caption = request.Caption,
                Location = request.Location,
                PeoplePresent = request.PeoplePresent,
                ImageUrl = request.ImageUrl,
                BlobPath = string.IsNullOrWhiteSpace(request.BlobPath) ? "local" : request.BlobPath,
                LikeCount = 0,
                CommentCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Creator post created by user {userId}", userId.Value);

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                Title = post.Title,
                Caption = post.Caption,
                Location = post.Location,
                PeoplePresent = post.PeoplePresent,
                ImageUrl = post.ImageUrl,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount,
                CreatedAt = post.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating post: {ex.Message}");
            return StatusCode(500, "Error creating post");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PostDto>> GetPost(Guid id)
    {
        try
        {
            var post = await _context.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (post == null)
                return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == post.UserId)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            return Ok(new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                Title = post.Title,
                Caption = post.Caption,
                Location = post.Location,
                PeoplePresent = post.PeoplePresent,
                ImageUrl = post.ImageUrl,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount,
                CreatedAt = post.CreatedAt,
                User = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting post: {ex.Message}");
            return StatusCode(500, "Error retrieving post");
        }
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(Guid id)
    {
        try
        {
            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.PostId == id && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var userIds = comments.Select(c => c.UserId).Distinct().ToList();
            var users = userIds.Count == 0
                ? new Dictionary<Guid, UserSummaryDto>()
                : await _context.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new UserSummaryDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        AvatarUrl = u.AvatarUrl
                    })
                    .ToDictionaryAsync(u => u.Id);

            var commentDtos = comments.Select(comment => new CommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                UserId = comment.UserId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                User = users.GetValueOrDefault(comment.UserId)
            }).ToList();

            return Ok(commentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for post {postId}", id);
            return StatusCode(500, "Error retrieving comments");
        }
    }

    [HttpPost("{id}/comments")]
    [Authorize(Roles = "Consumer")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Comment content is required");
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (post == null)
            {
                return NotFound("Post not found");
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = id,
                UserId = userId.Value,
                Content = request.Content.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            post.CommentCount += 1;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);

            return Ok(new CommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                UserId = comment.UserId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                User = user == null ? null : new UserSummaryDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to post {postId}", id);
            return StatusCode(500, "Error adding comment");
        }
    }

    [HttpPost("{id}/ratings")]
    [Authorize(Roles = "Consumer")]
    public async Task<ActionResult<PostRatingDto>> RatePost(Guid id, [FromBody] RatePostRequest request)
    {
        try
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5");
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (post == null)
            {
                return NotFound("Post not found");
            }

            var existingRating = await _context.MediaRatings
                .FirstOrDefaultAsync(r => r.PostId == id && r.UserId == userId.Value);

            if (existingRating == null)
            {
                existingRating = new MediaRating
                {
                    Id = MediaRating.CreateDocumentId(userId.Value, id),
                    UserId = userId.Value,
                    PostId = id,
                    RatingValue = request.Rating,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MediaRatings.Add(existingRating);
            }
            else
            {
                existingRating.RatingValue = request.Rating;
            }

            await _context.SaveChangesAsync();

            var aggregate = await _context.MediaRatings
                .Where(r => r.PostId == id)
                .GroupBy(r => r.PostId)
                .Select(g => new
                {
                    Average = g.Average(x => x.RatingValue),
                    Count = g.Count()
                })
                .FirstAsync();

            post.AverageRating = Math.Round((decimal)aggregate.Average, 2);
            post.RatingCount = aggregate.Count;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new PostRatingDto
            {
                PostId = id,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount,
                CurrentUserRating = request.Rating
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating post {postId}", id);
            return StatusCode(500, "Error rating post");
        }
    }

    [HttpGet("{id}/ratings")]
    public async Task<ActionResult<PostRatingDto>> GetRatings(Guid id)
    {
        try
        {
            var post = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (post == null)
            {
                return NotFound("Post not found");
            }

            int? currentUserRating = null;
            var currentUserId = GetCurrentUserId();

            if (currentUserId.HasValue)
            {
                currentUserRating = await _context.MediaRatings
                    .Where(r => r.PostId == id && r.UserId == currentUserId.Value)
                    .Select(r => (int?)r.RatingValue)
                    .FirstOrDefaultAsync();
            }

            return Ok(new PostRatingDto
            {
                PostId = id,
                AverageRating = post.AverageRating,
                RatingCount = post.RatingCount,
                CurrentUserRating = currentUserRating
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ratings for post {postId}", id);
            return StatusCode(500, "Error retrieving ratings");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var parsedId) ? parsedId : null;
    }

    private async Task<List<PostDto>> BuildPostDtosAsync(List<Post> posts, Guid? currentUserId)
    {
        if (posts.Count == 0)
        {
            return new List<PostDto>();
        }

        var userIds = posts.Select(p => p.UserId).Distinct().ToList();
        var postIds = posts.Select(p => p.Id).ToList();

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Username = u.Username,
                AvatarUrl = u.AvatarUrl
            })
            .ToDictionaryAsync(u => u.Id);

        var likedPostIds = new HashSet<Guid>();

        if (currentUserId.HasValue)
        {
            likedPostIds = (await _context.Likes
                    .AsNoTracking()
                    .Where(l => l.UserId == currentUserId.Value && postIds.Contains(l.PostId))
                    .Select(l => l.PostId)
                    .ToListAsync())
                .ToHashSet();
        }

        return posts.Select(post => new PostDto
        {
            Id = post.Id,
            UserId = post.UserId,
            Title = post.Title,
            Caption = post.Caption,
            Location = post.Location,
            PeoplePresent = post.PeoplePresent,
            ImageUrl = post.ImageUrl,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            AverageRating = post.AverageRating,
            RatingCount = post.RatingCount,
            IsLikedByCurrentUser = likedPostIds.Contains(post.Id),
            CreatedAt = post.CreatedAt,
            User = users.GetValueOrDefault(post.UserId)
        }).ToList();
    }
}
