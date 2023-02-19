﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StatusApp_Server.Application;
using StatusApp_Server.Domain;
using StatusApp_Server.Infrastructure;

namespace StatusApp_Server.Presentation;

public static class UserRoutes
{
    public static void MapUserRoutes(this WebApplication app)
    {
        app.MapGet(
                "/getUser",
                async Task<Results<Ok<Profile>, NotFound>> (
                    UserManager<User> userManager,
                    HttpContext context
                ) =>
                {
                    var userName = context.User.Identity?.Name ?? throw new ArgumentNullException();
                    User? user = await userManager.FindByNameAsync(userName);
                    if (user is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var profile = new Profile
                    {
                        UserName = user.UserName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Status = user.Status,
                        Online = user.Online
                    };
                    return TypedResults.Ok(profile);
                    //return Results.Ok(user);
                }
            )
            .RequireAuthorization()
            .WithName("GetUser")
            .WithOpenApi();

        app.MapGet(
                "/signin",
                async Task<Results<Ok<Profile>, BadRequest, UnauthorizedHttpResult>> (
                    UserManager<User> userManager,
                    SignInManager<User> signInManager,
                    string userName,
                    string password
                ) =>
                {
                    var user = await userManager.FindByNameAsync(userName);
                    if (user == null)
                    {
                        return TypedResults.BadRequest();
                    }

                    var profile = new Profile
                    {
                        UserName = user.UserName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Status = user.Status,
                        Online = user.Online
                    };

                    var success = await signInManager.PasswordSignInAsync(
                        user,
                        password,
                        true,
                        false
                    );
                    return success.Succeeded
                        ? TypedResults.Ok(profile)
                        : TypedResults.Unauthorized();
                }
            )
            .AllowAnonymous()
            .WithName("SignIn")
            .WithOpenApi();

        app.MapGet(
                "/signOut",
                async Task<Ok> (SignInManager<User> signInManager) =>
                {
                    await signInManager.SignOutAsync();
                    return TypedResults.Ok();
                }
            )
            .RequireAuthorization()
            .WithName("SignOut")
            .WithOpenApi();

        app.MapPut(
                "/createUser",
                async Task<Results<Ok<Profile>, BadRequest<IEnumerable<IdentityError>>>> (
                    UserManager<User> userManager,
                    SignInManager<User> signInManager,
                    string userName,
                    string password,
                    string firstName,
                    string lastName,
                    string email
                ) =>
                {
                    var newUser = new User
                    {
                        UserName = userName,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName
                    };

                    var result = await userManager.CreateAsync(newUser, password);

                    if (!result.Succeeded)
                    {
                        return TypedResults.BadRequest(result.Errors);
                    }

                    var newProfile = new Profile
                    {
                        UserName = userName,
                        FirstName = firstName,
                        LastName = lastName
                    };
                    await signInManager.SignInAsync(newUser, isPersistent: true);
                    return TypedResults.Ok(newProfile);
                }
            )
            .AllowAnonymous()
            .WithName("CreateUser")
            .WithOpenApi();

        app.MapDelete(
                "deleteUser",
                async Task<Results<Ok, BadRequest>> (
                    HttpContext context,
                    UserManager<User> userManager,
                    SignInManager<User> signInManager
                ) =>
                {
                    var userName = context.User.Identity?.Name ?? throw new ArgumentNullException();
                    var targetUser = await userManager.FindByNameAsync(userName);
                    if (targetUser is null)
                    {
                        return TypedResults.BadRequest();
                    }

                    //TODO: Confirm Auth flow
                    await signInManager.SignOutAsync();
                    await userManager.DeleteAsync(targetUser);
                    //TODO: Also delete Friendships
                    return TypedResults.Ok();
                }
            )
            .RequireAuthorization()
            .WithName("DeleteUser")
            .WithOpenApi();

        //TODO: Create separate route for updating Password
        app.MapPatch(
                "updateUser",
                async Task<Results<Ok<Profile>, BadRequest>> (
                    ChatContext db,
                    IHubContext<StatusHub, IStatusClient> hubContext,
                    HttpContext context,
                    UserManager<User> userManager,
                    FriendshipService friendshipService,
                    string? firstName,
                    string? lastName,
                    string? status,
                    bool? online
                ) =>
                {
                    var userName = context.User.Identity?.Name ?? throw new ArgumentNullException();
                    //TODO: Update Friendships too
                    var targetUser = await userManager.FindByNameAsync(userName);
                    if (targetUser is null)
                    {
                        return TypedResults.BadRequest();
                    }

                    targetUser.FirstName = firstName ?? targetUser.FirstName;
                    targetUser.LastName = lastName ?? targetUser.LastName;
                    targetUser.Status = status ?? targetUser.Status;
                    targetUser.Online = online ?? targetUser.Online;
                    await userManager.UpdateAsync(targetUser);

                    var updatedProfile = new Profile
                    {
                        UserName = targetUser.UserName,
                        FirstName = targetUser.FirstName,
                        LastName = targetUser.LastName,
                        Status = targetUser.Status,
                        Online = targetUser.Online,
                    };

                    // Push changes to this user to any of their friends
                    var friendsUserNameList = friendshipService.GetFriendsUserNameList(userName);
                    var usersToNotify = db.Connections
                        .Where(s => friendsUserNameList.Contains(s.UserName))
                        .GroupBy(s => s.UserName)
                        .Select(s => s.First().UserName)
                        .ToList();

                    await hubContext.Clients
                        .Users(usersToNotify)
                        .ReceiveUpdatedUser(updatedProfile);
                    return TypedResults.Ok(updatedProfile);
                }
            )
            .RequireAuthorization()
            .WithName("UpdateUser")
            .WithOpenApi();
    }
}
