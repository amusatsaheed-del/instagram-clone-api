using InstagramClone.Api.Data;
using InstagramClone.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly InstagramContext _context;

    public UsersController(InstagramContext context)
    {
        _context = context;
    }

    [HttpGet("{id}/profile")]
    public async Task<ActionResult<UserDto>> GetProfile(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        if (user == null)
        {
            return NotFound();
        }

        var followerCount = await _context.Followers.CountAsync(f => f.FolloweeId == id);
        var followingCount = await _context.Followers.CountAsync(f => f.FollowerId == id);
        var postCount = await _context.Posts.CountAsync(p => p.UserId == id && p.IsActive);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            PostCount = postCount,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpGet("{id}/followers")]
    public async Task<ActionResult<List<UserDto>>> GetFollowers(Guid id)
    {
        var followers = await _context.Followers
            .AsNoTracking()
            .Where(f => f.FolloweeId == id)
            .Include(f => f.FollowerUser)
            .Select(f => new UserDto
            {
                Id = f.FollowerUser!.Id,
                Email = f.FollowerUser.Email,
                Username = f.FollowerUser.Username,
                Bio = f.FollowerUser.Bio,
                AvatarUrl = f.FollowerUser.AvatarUrl,
                Role = f.FollowerUser.Role,
                CreatedAt = f.FollowerUser.CreatedAt
            })
            .ToListAsync();

        return Ok(followers);
    }
}
