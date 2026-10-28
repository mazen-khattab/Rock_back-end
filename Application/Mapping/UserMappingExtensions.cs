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
            ArgumentNullException.ThrowIfNull(user);

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

        public static User FromDto(this User user, UserProfileDto dto)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(dto);

            user.Fname = dto.FirstName;
            user.Lname = dto.LastName;
            user.PhoneNumber = dto.Phone;
            user.Governorate = dto.Governorate;
            user.City = dto.City;
            user.FullAddress = dto.FullAddress;
            user.Email = dto.Email;

            return user;
        }
    }
}
