using AvatarSentry.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvatarSentry.Api.Controllers;

[ApiController]
[Route("api/avatar-configs")]
[Authorize]
public class AvatarConfigsController : ControllerBase
{
    [HttpGet]
    public ActionResult<PagedResult<AvatarConfigDto>> GetConfigs([FromQuery] string? empresa, [FromQuery] string? sede, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(new PagedResult<AvatarConfigDto> { Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public ActionResult<AvatarConfigDto> GetById([FromRoute] int id)
    {
        return Ok(new AvatarConfigDto { Id = id });
    }

    [HttpPost]
    public ActionResult<AvatarConfigDto> Create([FromBody] AvatarConfigCreateRequest request)
    {
        return Ok(new AvatarConfigDto());
    }

    [HttpPut("{id:int}")]
    public ActionResult<AvatarConfigDto> Update([FromRoute] int id, [FromBody] AvatarConfigUpdateRequest request)
    {
        return Ok(new AvatarConfigDto { Id = id });
    }

    [HttpPatch("{id:int}")]
    public ActionResult<AvatarConfigDto> Patch([FromRoute] int id, [FromBody] AvatarConfigUpdateRequest request)
    {
        return Ok(new AvatarConfigDto { Id = id });
    }

    [HttpDelete("{id:int}")]
    public ActionResult<object> Delete([FromRoute] int id)
    {
        return Ok(new { Id = id });
    }

    [HttpPost("{id:int}/logo")]
    [Consumes("multipart/form-data")]
    public ActionResult<AvatarConfigDto> UploadLogo([FromRoute] int id, [FromForm] AssetUploadRequest request)
    {
        return Ok(new AvatarConfigDto { Id = id });
    }

    [HttpPost("{id:int}/fondo")]
    [Consumes("multipart/form-data")]
    public ActionResult<AvatarConfigDto> UploadFondo([FromRoute] int id, [FromForm] AssetUploadRequest request)
    {
        return Ok(new AvatarConfigDto { Id = id });
    }
}
