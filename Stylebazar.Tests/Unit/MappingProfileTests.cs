using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Mappings;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Xunit;

namespace Stylebazar.Tests.Unit;

/// <summary>
/// Guards the AutoMapper profile. Building the mapper compiles every CreateMap,
/// so a malformed expression (wrong member, bad type) fails the build test; the
/// RegisterDto→AppUser test pins the hand-written Identity-field projections.
/// </summary>
public class MappingProfileTests
{
    private static IMapper BuildMapper()
    {
        // AutoMapper 14+ (Lucky Penny fork) takes an ILoggerFactory on the config ctor.
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public void Profile_builds_without_throwing()
    {
        var act = () => BuildMapper();
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterDto_maps_to_AppUser_with_normalized_identity_fields()
    {
        var mapper = BuildMapper();
        var dto = new RegisterDto
        {
            FullName = "Test User",
            Email = "User@StyleBazar.AZ",
            Password = "parol1",
            ConfirmPassword = "parol1",
            Role = UserRole.Buyer
        };

        var user = mapper.Map<AppUser>(dto);

        user.UserName.Should().Be("User@StyleBazar.AZ");
        user.Email.Should().Be("User@StyleBazar.AZ");
        user.NormalizedEmail.Should().Be("USER@STYLEBAZAR.AZ");
        user.NormalizedUserName.Should().Be("USER@STYLEBAZAR.AZ");
        user.FullName.Should().Be("Test User");
        user.Id.Should().NotBeNullOrWhiteSpace();
        user.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
    }
}
