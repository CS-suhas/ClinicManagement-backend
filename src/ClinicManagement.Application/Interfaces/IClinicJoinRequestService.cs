using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.ClinicJoinRequests.Interfaces;

public interface IClinicJoinRequestService
{
    Task<Guid> RequestToJoinAsync(Guid providerId, Guid clinicId);
    Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId);
    Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(Guid clinicId, ClinicJoinRequestStatus? status);
    Task ApproveAsync(Guid requestId);
    Task RejectAsync(Guid requestId);
    Task CancelAsync(Guid requestId);
}
