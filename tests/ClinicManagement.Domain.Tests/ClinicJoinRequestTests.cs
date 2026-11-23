using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class ClinicJoinRequestTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static ClinicJoinRequest NewRequest() =>
        ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void NewRequest_IsPendingAndUndecided()
    {
        var request = NewRequest();

        Assert.Equal(ClinicJoinRequestStatus.PENDING, request.Status);
        Assert.Null(request.DecidedAtUtc);
    }

    [Theory]
    [InlineData(ClinicJoinRequestStatus.APPROVED)]
    [InlineData(ClinicJoinRequestStatus.REJECTED)]
    [InlineData(ClinicJoinRequestStatus.CANCELLED)]
    public void PendingRequest_CanBeDecidedOnce(ClinicJoinRequestStatus outcome)
    {
        var request = NewRequest();

        switch (outcome)
        {
            case ClinicJoinRequestStatus.APPROVED: request.Approve(Now); break;
            case ClinicJoinRequestStatus.REJECTED: request.Reject(Now); break;
            default: request.Cancel(Now); break;
        }

        Assert.Equal(outcome, request.Status);
        Assert.Equal(Now, request.DecidedAtUtc);
        Assert.Throws<InvalidStatusTransitionException>(() => request.Approve(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => request.Reject(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => request.Cancel(Now));
    }

    [Fact]
    public void Create_RejectsEmptyIds()
    {
        Assert.Equal(
            "providerId",
            Assert.Throws<DomainValidationException>(
                () => ClinicJoinRequest.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid())).PropertyName);
        Assert.Equal(
            "clinicId",
            Assert.Throws<DomainValidationException>(
                () => ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty)).PropertyName);
    }
}
