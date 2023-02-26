﻿using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using StatusApp_Server.Application;
using StatusApp_Server.Application.Contracts;
using StatusApp_Server.Domain;
using StatusApp_Server.Infrastructure;
using Xunit;

namespace StatusApp_Server.Tests.Application.FriendshipServiceTests;

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

        var options = new DbContextOptions<ChatContext>();
        var chatContextMock = new Mock<ChatContext>(options);
        chatContextMock.Setup(db => db.Friendships).ReturnsDbSet(friendships).Verifiable();

        var userServiceMock = new Mock<IUserService>();

        var friendshipService = new FriendshipService(
            chatContextMock.Object,
            userServiceMock.Object
        );
        // Act
        var result = friendshipService.GetFriendsUserNameList(userName);

        // Assert
        result.Should().BeEquivalentTo(friendUserNameList);
        chatContextMock.Verify();
    }
}
