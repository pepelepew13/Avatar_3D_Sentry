using System;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Controllers;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class AvatarConfigControllerTests
{
    [Fact]
    public async Task Get_SameEmpresaDifferentCase_ReusesConfiguration()
    {
        await using var context = CreateContext();
        var controller = new AvatarConfigController(context);

        var firstResult = await controller.Get("Sentry", "Principal");
        var firstConfig = AssertIsOk(firstResult);

        var secondResult = await controller.Get("sentry", "principal");
        var secondConfig = AssertIsOk(secondResult);

        Assert.Equal(firstConfig.Id, secondConfig.Id);
        Assert.Equal(firstConfig.Empresa, secondConfig.Empresa);
        Assert.Equal(firstConfig.Sede, secondConfig.Sede);
    }

    private static AvatarConfig AssertIsOk(ActionResult<AvatarConfig> actionResult)
    {
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        return Assert.IsType<AvatarConfig>(okResult.Value);
    }

    private static AvatarContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AvatarContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AvatarContext(options);
    }
}
