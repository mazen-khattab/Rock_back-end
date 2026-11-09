using Application.DTOs;
using Core.Entities;

namespace Application.Mapping
{
    public static class AccountMappingExtensions
    {
        public static IQueryable<ManagedAccountDto> ProjectToManagedAccount(this IQueryable<User> users)
        {
            // Never project password hashes, security stamps, tokens or Identity internals.
            return users.Select(user => new ManagedAccountDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.Fname,
                LastName = user.Lname,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Governorate = user.Governorate,
                City = user.City,
                FullAddress = user.FullAddress,
                Roles = user.UserRoles.Where(ur => !ur.IsDeleted && !ur.Role.IsDeleted)
                    .OrderBy(ur => ur.Role.Name).Select(ur => ur.Role.Name!).ToList(),
                OrderCount = user.Orders.Count(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        public static void ApplyManagedProfile(this User user, AccountProfileRequestDto dto)
        {
            string email = dto.Email.Trim();
            // Email-based usernames follow an email change; custom seeded usernames are preserved.
            if (string.IsNullOrEmpty(user.UserName) || string.Equals(user.UserName, user.Email, StringComparison.OrdinalIgnoreCase))
                user.UserName = email;
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                user.EmailConfirmed = false;
            if (user.PhoneNumber != dto.Phone.Trim())
                user.PhoneNumberConfirmed = false;

            user.Fname = dto.FirstName.Trim();
            user.Lname = dto.LastName.Trim();
            user.Email = email;
            user.PhoneNumber = dto.Phone.Trim();
            user.Governorate = dto.Governorate?.Trim() ?? string.Empty;
            user.City = dto.City?.Trim() ?? string.Empty;
            user.FullAddress = dto.FullAddress?.Trim() ?? string.Empty;
        }
    }
}
