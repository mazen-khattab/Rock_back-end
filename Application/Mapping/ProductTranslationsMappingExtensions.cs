using Application.DTOs;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Mapping
{
    public static class ProductTranslationsMappingExtensions
    {
        public static ProductTranslation FromDetailsAdminDto(this ProductTranslation translation, ProductDetailsAdminDto dto, int landId)
        {
            ArgumentNullException.ThrowIfNull(translation);
            ArgumentNullException.ThrowIfNull(dto);


            if (landId == 1)
            {
                translation.Name = dto.NameAr;
                translation.Description = dto.DescriptionAr;
                translation.Slug = dto.SlugAr;
                translation.MetaDescription = dto.MetaDescriptionAr;
                translation.MetaTitle = dto.MetaTitleAr;
            }
            else if (landId == 2)
            {
                translation.Name = dto.NameEn;
                translation.Description = dto.DescriptionEn;
                translation.Slug = dto.SlugEn;
                translation.MetaDescription = dto.MetaDescriptionEn;
                translation.MetaTitle = dto.MetaTitleEn;
            }

            return translation;
        }
    }
}
