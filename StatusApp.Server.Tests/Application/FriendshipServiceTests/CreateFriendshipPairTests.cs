﻿using Microsoft.EntityFrameworkCore;
using StatusApp.Server.Application;
using StatusApp.Server.Application.Contracts;
using StatusApp.Server.Domain;
using StatusApp.Server.Infrastructure;

namespace StatusApp.Server.Tests.Application.FriendshipServiceTests;

public partial class FriendshipServiceTests
{
    [Fact]
    public async Task WhenCreateFriendshipPairIsCalled_ReturnsFriendship()
    {
        //Arrange
        var userName = "TestUserName";
        var friendUserName = "AnotherUserName";

        var user = new StatusUser
        {
            UserName = userName,
            FirstName = "AName",
            LastName = "LastName",
            Status = "My Status",
            Online = false
        };

        var friendUser = new StatusUser
        {
            UserName = friendUserName,
            FirstName = "FriendsName",
            LastName = "FriendsLastName",
            Status = "Another Status",
            Online = true
        };

        var expectedFriendship = new Friendship
        {
            UserName = user.UserName!,
            FriendUserName = friendUser.UserName!,
            Accepted = true,
            AreFriends = false,
            FriendFirstName = friendUser.FirstName,
            FriendLastName = friendUser.LastName,
        };

        var options = new DbContextOptions<StatusContext>();
        var chatContextMock = new Mock<StatusContext>(options);
        chatContextMock.Setup(db => db.Friendships).ReturnsDbSet(new List<Friendship>());

        var identityUserServiceMock = new Mock<IIdentityUserService>();
        var statusUserServiceMock = new Mock<IStatusUserService>();

        var friendshipService = new FriendshipService(
            chatContextMock.Object,
            identityUserServiceMock.Object,
            statusUserServiceMock.Object
        );
        // Act
        var result = await friendshipService.CreateFriendshipPair(user, friendUser);

        // Assert
        result.Should().BeEquivalentTo(expectedFriendship);
    }
}
