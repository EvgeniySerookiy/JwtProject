using JwtProject.Data;
using JwtProject.Entities;
using JwtProject.Models;
using JwtProject.Models.Item;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JwtProject.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly UserDbContext _context;

    public WorkItemsController(UserDbContext context)
    {
        _context = context;
    }

    private bool IsValidWorkItemStatus(string status)
    {
        var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "New", "InProgress", "Completed", "Cancelled"
        };
        return allowedStatuses.Contains(status);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkItemDto>>> GetWorkItems([FromQuery] WorkItemFilter filter)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

        IQueryable<WorkItem> query = _context.WorkItems.Include(wi => wi.CreatedBy);

        if (currentUserRole != "Admin")
        {
            query = query.Where(wi => wi.CreatedById.ToString() == currentUserId);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            if (!IsValidWorkItemStatus(filter.Status))
            {
                return BadRequest($"Invalid status value '{filter.Status}'. Allowed values are: New, InProgress, Completed, Cancelled.");
            }
            query = query.Where(wi => wi.Status.ToLower() == filter.Status.ToLower());
        }

        if (filter.CreatedById.HasValue)
        {
            if (currentUserRole == "Admin")
            {
                query = query.Where(wi => wi.CreatedById == filter.CreatedById.Value);
            }
            else
            {
                return Forbid("You are not allowed to filter by other users' work items.");
            }
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(wi => wi.Title.Contains(filter.Search) || wi.Description.Contains(filter.Search));
        }

        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            var sortDirection = filter.SortBy.EndsWith(" desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            var sortByField = filter.SortBy.Replace(" desc", "", StringComparison.OrdinalIgnoreCase).Replace(" asc", "", StringComparison.OrdinalIgnoreCase);

            query = sortByField.ToLower() switch
            {
                "title" => sortDirection == "asc" ? query.OrderBy(wi => wi.Title) : query.OrderByDescending(wi => wi.Title),
                "createdat" => sortDirection == "asc" ? query.OrderBy(wi => wi.CreatedAt) : query.OrderByDescending(wi => wi.CreatedAt),
                "status" => sortDirection == "asc" ? query.OrderBy(wi => wi.Status) : query.OrderByDescending(wi => wi.Status),
                _ => query.OrderByDescending(wi => wi.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(wi => wi.CreatedAt);
        }

        var totalRecords = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
        
        var itemDtos = items.Select(wi => new WorkItemDto(
            wi.Id,
            wi.Title,
            wi.Description,
            wi.Status,
            wi.CreatedAt,
            wi.CreatedBy.Username
        )).ToList();

        Response.Headers.Add("X-Pagination-Total-Count", totalRecords.ToString());
        Response.Headers.Add("X-Pagination-Page-Size", filter.PageSize.ToString());
        Response.Headers.Add("X-Pagination-Current-Page", filter.PageNumber.ToString());
        Response.Headers.Add("X-Pagination-Total-Pages", ((int)Math.Ceiling((double)totalRecords / filter.PageSize)).ToString());

        return Ok(itemDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkItemDto>> GetWorkItem(Guid id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var workItem = await _context.WorkItems.Include(wi => wi.CreatedBy).FirstOrDefaultAsync(wi => wi.Id == id);

        if (workItem == null) return NotFound();

        if (currentUserRole == "Admin" || workItem.CreatedById.ToString() == currentUserId)
        {
            return new WorkItemDto(
                workItem.Id,
                workItem.Title,
                workItem.Description,
                workItem.Status,
                workItem.CreatedAt,
                workItem.CreatedBy.Username
            );
        }

        return Forbid("You are not authorized to view this work item.");
    }

    [HttpPost]
    public async Task<ActionResult<WorkItemDto>> CreateWorkItem(CreateWorkItemDto createDto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (currentUserId == null) return Unauthorized();

        Guid assignedToUserId = Guid.Parse(currentUserId);

        if (createDto.AssignToUserId.HasValue)
        {
            if (currentUserRole != "Admin")
            {
                return Forbid("Only administrators can assign work items to other users.");
            }
            assignedToUserId = createDto.AssignToUserId.Value;
            if (!await _context.Users.AnyAsync(u => u.Id == assignedToUserId))
            {
                return BadRequest($"User with ID {assignedToUserId} not found.");
            }
        }
        
        if (!IsValidWorkItemStatus(createDto.Status))
        {
            return BadRequest($"Invalid Status value '{createDto.Status}'. Allowed values are: New, InProgress, Completed, Cancelled.");
        }

        var workItem = new WorkItem
        {
            Id = Guid.NewGuid(),
            Title = createDto.Title,
            Description = createDto.Description,
            Status = createDto.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedById = assignedToUserId
        };

        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var assignedByUser = await _context.Users.FindAsync(workItem.CreatedById);
        var returnWorkItemDto = new WorkItemDto(
            workItem.Id,
            workItem.Title,
            workItem.Description,
            workItem.Status,
            workItem.CreatedAt,
            assignedByUser?.Username ?? "Unknown"
        );

        return CreatedAtAction(nameof(GetWorkItem), new { id = workItem.Id }, returnWorkItemDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkItem(Guid id, UpdateWorkItemDto updateDto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var workItem = await _context.WorkItems.FindAsync(id);
        if (workItem == null) return NotFound();

        if (currentUserRole != "Admin" && workItem.CreatedById.ToString() != currentUserId)
        {
            return Forbid("You are not authorized to update this work item.");
        }
        
        if (updateDto.Title != null) workItem.Title = updateDto.Title;
        if (updateDto.Description != null) workItem.Description = updateDto.Description;
        if (updateDto.Status != null)
        {
            if (!IsValidWorkItemStatus(updateDto.Status))
            {
                return BadRequest($"Invalid Status value '{updateDto.Status}'. Allowed values are: New, InProgress, Completed, Cancelled.");
            }
            workItem.Status = updateDto.Status;
        }

        if (updateDto.AssignToUserId.HasValue)
        {
            if (currentUserRole != "Admin")
            {
                return Forbid("Only administrators can reassign work items.");
            }
            if (!await _context.Users.AnyAsync(u => u.Id == updateDto.AssignToUserId.Value))
            {
                return BadRequest($"User with ID {updateDto.AssignToUserId.Value} not found.");
            }
            workItem.CreatedById = updateDto.AssignToUserId.Value;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.WorkItems.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteWorkItem(Guid id)
    {
        var workItem = await _context.WorkItems.FindAsync(id);
        if (workItem == null) return NotFound();

        _context.WorkItems.Remove(workItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}