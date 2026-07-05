using Microsoft.Extensions.Options;
using Moq;
using TeamFlow.Core.DTOs.Workspace;
using TeamFlow.Core.Enums;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Helpers;
using Xunit;
using System.Threading.Tasks;

namespace TeamFlow.Tests.Workspace;

public class WorkspaceServiceTests
{
    private readonly WorkspaceService _workspaceService;
    private readonly Core.Entities.ApplicationUser _owner;
    private readonly Core.Entities.ApplicationUser _member;

    public WorkspaceServiceTests()
    {
        var context = TestDbContextFactory.Create();
        var userManager = TestDbContextFactory.CreateUserManager(context);

        _owner = TestDbContextFactory.MakeUser("Owner", "User", "owner@test.com");
        _member = TestDbContextFactory.MakeUser("Member", "User", "member@test.com");

        context.Users.AddRange(_owner, _member);
        context.SaveChanges();

        _workspaceService = new WorkspaceService(context, userManager);
    }

    [Fact]
    public async Task Create_Workspace_OwnerBecomesOwner()
    {
        var request = new CreateWorkspaceRequest("My Workspace", null, null);

        var result = await _workspaceService.CreateAsync(_owner.Id, request);

        Assert.NotNull(result);
        Assert.Equal("My Workspace", result.Name);

        var role = await _workspaceService.GetUserRoleAsync(result.Id, _owner.Id);
        Assert.Equal(WorkspaceRole.Owner, role);
    }

    [Fact]
    public async Task GetById_NonMember_ThrowsUnauthorized()
    {
        var request = new CreateWorkspaceRequest("Private Workspace", null, null);
        var workspace = await _workspaceService.CreateAsync(_owner.Id, request);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _workspaceService.GetByIdAsync(workspace.Id, _member.Id));
    }

    [Fact]
    public async Task InviteMember_ValidEmail_MemberAdded()
    {
        var ws = await _workspaceService.CreateAsync(_owner.Id,
            new CreateWorkspaceRequest("Team", null, null));

        var result = await _workspaceService.InviteMemberAsync(ws.Id, _owner.Id,
            new InviteMemberRequest(_member.Email!, WorkspaceRole.Developer));

        Assert.Equal(_member.Id, result.UserId);
        Assert.Equal(WorkspaceRole.Developer, result.Role);
    }

    [Fact]
    public async Task InviteMember_DuplicateMember_ThrowsException()
    {
        var ws = await _workspaceService.CreateAsync(_owner.Id,
            new CreateWorkspaceRequest("Team", null, null));

        await _workspaceService.InviteMemberAsync(ws.Id, _owner.Id,
            new InviteMemberRequest(_member.Email!, WorkspaceRole.Developer));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _workspaceService.InviteMemberAsync(ws.Id, _owner.Id,
                new InviteMemberRequest(_member.Email!, WorkspaceRole.Developer)));
    }

    [Fact]
    public async Task DeleteWorkspace_ByNonOwner_ThrowsUnauthorized()
    {
        var ws = await _workspaceService.CreateAsync(_owner.Id,
            new CreateWorkspaceRequest("Team", null, null));

        await _workspaceService.InviteMemberAsync(ws.Id, _owner.Id,
            new InviteMemberRequest(_member.Email!, WorkspaceRole.Admin));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _workspaceService.DeleteAsync(ws.Id, _member.Id));
    }
}