using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

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
            return new CreateRoomResult(invalidServiceIds)
            {
                Status = CreateRoomStatus.InvalidExtraServiceIds
            };
        }

        var room = new Room
        {
            Name = name,
            Capacity = request.Capacity,
            HourlyRate = request.HourlyRate
        };

        foreach (var id in serviceIds)
        {
            room.ExtraServices.Add(new RoomExtraService
            {
                Room = room,
                ExtraServiceId = id
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

    public async Task<IReadOnlyCollection<AvailableRoomResponse>> FindAvailableAsync(
        DateTime startUtc,
        DateTime endUtc,
        int minCapacity,
        CancellationToken ct)
    {
        // Strict comparisons let one booking start exactly when another one ends.
        var rooms = await _db.Rooms
            .AsNoTracking()
            .Where(room => room.IsActive &&
                           room.Capacity >= minCapacity &&
                           !room.Bookings.Any(b => b.StartUtc < endUtc && b.EndUtc > startUtc))
            .Include(room => room.ExtraServices)
            .ThenInclude(service => service.ExtraService)
            .OrderBy(room => room.Id)
            .ToListAsync(ct);

        return rooms.Select(room =>
        {
            var extraServices = room.ExtraServices
                .OrderBy(service => service.ExtraServiceId)
                .Select(service => new AvailableRoomExtraServiceResponse(
                    service.ExtraServiceId,
                    service.ExtraService.Name,
                    service.ExtraService.Price))
                .ToArray();

            return new AvailableRoomResponse(extraServices)
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                HourlyRate = room.HourlyRate
            };
        }).ToArray();
    }

    public async Task<UpdateRoomResult> UpdateAsync(int id, UpdateRoomRequest request, CancellationToken ct)
    {
        var room = await _db.Rooms
            .Include(r => r.ExtraServices)
            .SingleOrDefaultAsync(r => r.Id == id && r.IsActive, ct);

        if (room == null)
            return new UpdateRoomResult { Status = UpdateRoomStatus.NotFound };

        var serviceIdsToAdd = request.ExtraServiceIdsToAdd.ToHashSet();
        var serviceIdsToRemove = request.ExtraServiceIdsToRemove.ToHashSet();
        var requestedServiceIds = serviceIdsToAdd.Concat(serviceIdsToRemove).ToHashSet();

        var invalidServiceIds = await FindInvalidExtraServiceIdsAsync(requestedServiceIds, ct);
        if (invalidServiceIds.Length > 0)
        {
            return new UpdateRoomResult(invalidServiceIds)
            {
                Status = UpdateRoomStatus.InvalidExtraServiceIds
            };
        }

        if (request.Name != null)
        {
            room.Name = request.Name.Trim();
        }

        if (request.Capacity != null)
        {
            room.Capacity = request.Capacity.Value;
        }

        if (request.HourlyRate != null)
        {
            room.HourlyRate = request.HourlyRate.Value;
        }

        // Treat service changes as set operations so retrying the same PATCH has the same outcome.
        foreach (var service in room.ExtraServices
                     .Where(s => serviceIdsToRemove.Contains(s.ExtraServiceId))
                     .ToList())
        {
            room.ExtraServices.Remove(service);
        }

        var existingServiceIds = room.ExtraServices.Select(s => s.ExtraServiceId).ToHashSet();

        foreach (var serviceId in serviceIdsToAdd.Except(existingServiceIds))
        {
            room.ExtraServices.Add(new RoomExtraService
            {
                Room = room,
                ExtraServiceId = serviceId
            });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return new UpdateRoomResult { Status = UpdateRoomStatus.NameConflict };
        }

        return new UpdateRoomResult { Status = UpdateRoomStatus.Updated };
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
            SqliteErrorCode: raw.SQLITE_CONSTRAINT,
            SqliteExtendedErrorCode: raw.SQLITE_CONSTRAINT_UNIQUE
        };
    }
}