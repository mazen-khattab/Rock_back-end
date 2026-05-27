using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class UserMappingExtensions
    {
        public static UserProfileDto ToDto(this User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return new UserProfileDto()
            {
                FirstName = user.Fname ?? string.Empty,
                LastName = user.Lname ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                Governorate = user.Governorate ?? string.Empty,
                City = user.City ?? string.Empty,
                FullAddress = user.FullAddress ?? string.Empty
            };
        }
    }
}
