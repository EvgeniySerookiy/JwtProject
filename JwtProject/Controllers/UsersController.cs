using JwtProject.Data;
using JwtProject.Models;
using JwtProject.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtProject.Entities;

namespace JwtProject.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _context;

    public UsersController(UserDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] PaginationFilter filter)
    {
        IQueryable<User> query = _context.Users;

        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(u => u.Username.Contains(filter.Search) || u.Email.Contains(filter.Search));
        }

        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            var sortDirection = filter.SortBy.EndsWith(" desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            var sortByField = filter.SortBy.Replace(" desc", "", StringComparison.OrdinalIgnoreCase).Replace(" asc", "", StringComparison.OrdinalIgnoreCase);

            query = sortByField.ToLower() switch
            {
                "username" => sortDirection == "asc" ? query.OrderBy(u => u.Username) : query.OrderByDescending(u => u.Username),
                "email" => sortDirection == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "role" => sortDirection == "asc" ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
                _ => query.OrderBy(u => u.Username)
            };
        }
        else
        {
            query = query.OrderBy(u => u.Username);
        }

        var totalRecords = await query.CountAsync();
        var userDtos = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.Role
            ))
            .ToListAsync();

        Response.Headers.Add("X-Pagination-Total-Count", totalRecords.ToString());
        Response.Headers.Add("X-Pagination-Page-Size", filter.PageSize.ToString());
        Response.Headers.Add("X-Pagination-Current-Page", filter.PageNumber.ToString());
        Response.Headers.Add("X-Pagination-Total-Pages", ((int)Math.Ceiling((double)totalRecords / filter.PageSize)).ToString());

        return Ok(userDtos);
    }
}