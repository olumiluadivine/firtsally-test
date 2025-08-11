using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace test.Common;

public abstract class ControllerTestBase
{
    protected static void SetupUserContext(ControllerBase controller, Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    protected static void SetupInvalidUserContext(ControllerBase controller)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    protected static void SetupEmptyUserContext(ControllerBase controller)
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}