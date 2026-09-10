namespace ConferenceRoomAPI.Features.Rooms;

public interface IRoomService
{
    Task<CreateRoomResult> CreateAsync(CreateRoomRequest request, CancellationToken ct);
    Task<UpdateRoomResult> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct);
    Task<DeleteRoomResult> DeleteAsync(int id, CancellationToken ct);
}
