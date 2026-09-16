using CvManagementSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Middleware;

public class BlockedUserMiddleware
{
    private readonly RequestDelegate _next;

    public BlockedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        UserManager<Models.ApplicationUser> userManager,
        SignInManager<Models.ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = userManager.GetUserId(context.User);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var isBlocked = await dbContext.Users
                    .AsNoTracking()
                    .Where(x => x.Id == userId)
                    .Select(x => x.IsBlocked)
                    .FirstOrDefaultAsync();

                if (isBlocked)
                {
                    await signInManager.SignOutAsync();

                    var returnUrl =
                        context.Request.Path +
                        context.Request.QueryString;

                    var loginUrl =
                        "/Identity/Account/Login?blocked=true&returnUrl=" +
                        Uri.EscapeDataString(returnUrl);

                    context.Response.Redirect(loginUrl);

                    return;
                }
            }
        }

        await _next(context);
    }
}