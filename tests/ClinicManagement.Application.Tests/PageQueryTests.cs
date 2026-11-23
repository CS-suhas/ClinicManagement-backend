using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class PageQueryTests
{
    [Fact]
    public void GetOffset_UsesOneBasedPageAndRequestedPageSize()
    {
        var query = new PageQuery("smith", Page: 3, PageSize: 25);

        Assert.Equal(50, query.GetOffset());
        Assert.Equal("smith", query.NormalizedSearch);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public void GetOffset_RejectsInvalidOrUnrepresentablePage(int page, int pageSize)
    {
        Assert.Throws<DomainValidationException>(
            () => new PageQuery(Page: page, PageSize: pageSize).GetOffset());
    }

    [Fact]
    public void AppointmentSearchQuery_RejectsReversedDateRange()
    {
        var query = new AppointmentSearchQuery(
            From: new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Throws<DomainValidationException>(() => query.GetOffset());
    }

    [Fact]
    public void AppointmentSearchQuery_RejectsDateRangeWhereFromEqualsTo()
    {
        var instant = new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<DomainValidationException>(() => new AppointmentSearchQuery(From: instant, To: instant).GetOffset());
    }

    [Fact]
    public void AppointmentSearchQuery_AcceptsOpenEndedOrOrderedDateRange()
    {
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        new AppointmentSearchQuery(From: from).GetOffset();
        new AppointmentSearchQuery(From: from, To: from.AddSeconds(1)).GetOffset();
    }

    [Fact]
    public void AppointmentSearchQuery_RejectsEmptyOrTooManyProviderIds()
    {
        Assert.Throws<DomainValidationException>(
            () => new AppointmentSearchQuery(ProviderIds: [Guid.Empty]).GetOffset());
        Assert.Throws<DomainValidationException>(
            () => new AppointmentSearchQuery(ProviderIds: Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray()).GetOffset());
    }
}
