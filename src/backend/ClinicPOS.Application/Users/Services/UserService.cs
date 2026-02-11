using ClinicPOS.Application.Common.Exceptions;
using ClinicPOS.Application.Users.Dtos;
using ClinicPOS.Domain.Entities;
using ClinicPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicPOS.Application.Users.Services;

public class UserService
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UserService(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            TenantId = _tenant.TenantId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var branchId in request.BranchIds)
        {
            user.UserBranches.Add(new UserBranch { UserId = user.Id, BranchId = branchId });
        }

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> AssignRoleAsync(Guid userId, AssignRoleRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        user.Role = request.Role;
        await _db.SaveChangesAsync(ct);

        return MapToDto(user);
    }

    private static UserDto MapToDto(User u) =>
        new(u.Id, u.Username, u.Role, u.TenantId,
            u.UserBranches.Select(ub => ub.BranchId).ToList());

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        return inner != null && inner.GetType().Name == "PostgresException"
            && inner.GetType().GetProperty("SqlState")?.GetValue(inner)?.ToString() == "23505";
    }
}
