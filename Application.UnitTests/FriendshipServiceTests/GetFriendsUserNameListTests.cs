﻿using Application.Contracts;
using Domain;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.FriendshipServiceTests;

public partial class FriendshipServiceTests
{
    [Fact]
    public void WhenGetFriendsUserNameListIsCalled_ReturnsUsernameList()
    {
        //Arrange
        var userName = "TestUserName";
        var friendUserName = "AnotherUserName";
        var friendship = new Friendship
        {
            UserName = userName,
            FriendUserName = friendUserName,
            AreFriends = true
        };
        var friendships = new List<Friendship> { friendship };

        var friendUserNameList = new List<string> { friendUserName };

        var options = new DbContextOptions<StatusContext>();
        var chatContextMock = new Mock<StatusContext>(options);
        chatContextMock.Setup(db => db.Friendships).ReturnsDbSet(friendships).Verifiable();

        var statusUserServiceMock = new Mock<IStatusUserService>();

        var friendshipService = new FriendshipService(
            chatContextMock.Object,
            statusUserServiceMock.Object
        );
        // Act
        var result = friendshipService.GetFriendsUserNameList(userName);

        // Assert
        result.Should().BeEquivalentTo(friendUserNameList);
        chatContextMock.Verify();
    }
}
