using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class TraineeRouteTests
{
    private const string Trainee = "7c1d0e2a-9b3f-4a5c-8d6e-1f2a3b4c5d6e";

    [Fact]
    public void Own_User_Produces_No_Trainee_Fragment()
    {
        TraineeRoute.TraineeQuery(null).Should().BeEmpty();
        TraineeRoute.TraineeQuery("   ").Should().BeEmpty();
        TraineeRoute.ApiUserQuery(null).Should().BeEmpty();
        TraineeRoute.ApiUserParam(null).Should().BeEmpty();
        TraineeRoute.Escape(null).Should().BeEmpty();
    }

    [Fact]
    public void Trainee_Query_Uses_Canonical_TraineeId_Name()
    {
        TraineeRoute.TraineeQuery(Trainee)
            .Should().Be($"?traineeId={Trainee}");
        TraineeRoute.TraineeQuery(Trainee, first: false)
            .Should().Be($"&traineeId={Trainee}");
    }

    [Fact]
    public void Exercises_Api_Family_Maps_Trainee_To_UserId()
    {
        TraineeRoute.ApiUserQuery(Trainee)
            .Should().Be($"?userId={Trainee}");
        TraineeRoute.ApiUserQuery(Trainee, first: false)
            .Should().Be($"&userId={Trainee}");
        TraineeRoute.ApiUserParam(Trainee)
            .Should().Be($"userId={Trainee}");
    }

    [Fact]
    public void Trainee_Values_Are_Escaped()
    {
        TraineeRoute.Escape("a b/c")
            .Should().Be("a%20b%2Fc");
        TraineeRoute.TraineeQuery("a b/c")
            .Should().Be("?traineeId=a%20b%2Fc");
    }

    [Fact]
    public void TypeDetailsRoute_Switches_Between_Library_And_Trainee_Details()
    {
        TraineeRoute.TypeDetailsRoute(21, null)
            .Should().Be("/training/library/21");
        TraineeRoute.TypeDetailsRoute(21, Trainee)
            .Should().Be($"/exercises/types/21/details/{Trainee}");
    }
}
