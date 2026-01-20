using AvatarSentry.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvatarSentry.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public ActionResult<PagedResult<UserItemDto>> GetUsers([FromQuery] string? empresa, [FromQuery] string? sede, [FromQuery] string? role, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(new PagedResult<UserItemDto> { Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public ActionResult<UserItemDto> GetById([FromRoute] int id)
    {
        return Ok(new UserItemDto { Id = id });
    }

    [HttpPost]
    public ActionResult<UserItemDto> Create([FromBody] UserCreateRequest request)
    {
        return Ok(new UserItemDto());
    }

    [HttpPut("{id:int}")]
    public ActionResult<UserItemDto> Update([FromRoute] int id, [FromBody] UserUpdateRequest request)
    {
        return Ok(new UserItemDto { Id = id });
    }

    [HttpDelete("{id:int}")]
    public ActionResult<object> Delete([FromRoute] int id)
    {
        return Ok(new { Id = id });
    }
}
