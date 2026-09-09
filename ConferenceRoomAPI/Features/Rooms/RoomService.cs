using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomAPI.Features.Rooms;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateRoomResult> CreateAsync(CreateRoomRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        
        var serviceIds = request.ExtraServiceIds.ToHashSet();
        
        var invalidServiceIds = await FindInvalidExtraServiceIdsAsync(serviceIds, ct);
        if (invalidServiceIds.Length > 0)
        {
            var result = new CreateRoomResult { Status = CreateRoomStatus.ExtraServicesNotFound };
            
            foreach (var serviceId in invalidServiceIds)
            {
                result.InvalidExtraServiceIds.Add(serviceId);
            }
            return result;
        }

        var room = new Room
        {
            Name = name,
            Capacity = request.Capacity,
            HourlyRate = request.HourlyRate
        };

        foreach (var serviceId in serviceIds)
        {
            room.ExtraServices.Add(new RoomExtraService
            {
                Room = room,
                ExtraServiceId = serviceId
            });
        }

        _db.Rooms.Add(room);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        // Let the database enforce uniqueness atomically
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return new CreateRoomResult { Status = CreateRoomStatus.NameConflict };
        }

        return new CreateRoomResult
        {
            Status = CreateRoomStatus.Created,
            RoomId = room.Id
        };
    }

    public async Task<DeleteRoomResult> DeleteAsync(int id, CancellationToken ct)
    {
        var room = await _db.Rooms.FindAsync([id], ct);

        if (room == null)
            return DeleteRoomResult.NotFound;

        if (!room.IsActive)
            return DeleteRoomResult.Deleted;

        // Delete only marks the room as inactive
        // this keeps the room in the database so we can have all booking history
        room.IsActive = false;
        
        await _db.SaveChangesAsync(ct);

        return DeleteRoomResult.Deleted;
    }

    private async Task<int[]> FindInvalidExtraServiceIdsAsync(HashSet<int> serviceIds, CancellationToken ct)
    {
        if (serviceIds.Count == 0)
            return [];

        var existingServiceIds = await _db.ExtraServices
            .Where(service => serviceIds.Contains(service.Id))
            .Select(service => service.Id)
            .ToListAsync(ct);

        return serviceIds.Except(existingServiceIds).Order().ToArray();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqliteException
        {
            SqliteErrorCode: SQLitePCL.raw.SQLITE_CONSTRAINT,
            SqliteExtendedErrorCode: SQLitePCL.raw.SQLITE_CONSTRAINT_UNIQUE
        };
    }
}
