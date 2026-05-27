using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.ExceptionsTypes;
using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class ProductService : Services<Product>, IProductService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IServices<Category> _categoryService;
        private readonly IServices<Language> _langServices;
        private readonly IServices<Size> _sizeService;
        private readonly IServices<Color> _colorService;

        public ProductService(IUnitOfWork unitOfWork, IRepo<Product> productRepo, ILogger<ProductService> logger, IWebHostEnvironment env, IServices<Category> categoryService, IServices<Language> langServices, IServices<Size> sizeService, IServices<Color> colorService) : base(unitOfWork, productRepo, logger)
        {
            _env = env;
            _categoryService = categoryService;
            _langServices = langServices;
            _sizeService = sizeService;
            _colorService = colorService;
        }

        public IQueryable<Product> GetProductsWithIncludes(int languageId)
        {
            _logger.LogDebug("Building product query for language {LanguageId}", languageId);

            var products = Query(filter: p => p.Translations.Any(t => t.LanguageId == languageId), tracked: false)
            .Include(p => p.Category)
                .ThenInclude(c => c.Translations.Where(t => t.LanguageId == languageId))
            .Include(p => p.Translations.Where(t => t.LanguageId == languageId))
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
                    .ThenInclude(i => i.MediaAsset)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Size)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
                    .ThenInclude(c => c.Translations.Where(t => t.LanguageId == languageId));

            return products;
        }

        private IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(p =>
                    p.Category.Translations.Any(t => t.Name.Contains(filter.Category)));
            }

            if (!string.IsNullOrEmpty(filter.Color))
            {
                query = query.Where(p =>
                    p.Variants.Any(v => v.Color.Translations.Any(t => t.Name.Contains(filter.Color))));
            }

            if (!string.IsNullOrEmpty(filter.Size))
            {
                query = query.Where(p =>
                    p.Variants.Any(v => v.Size.Name.Contains(filter.Size)));
            }

            return query;
        }

        public async Task<ApiResponse<(IEnumerable<Product> Items, int TotalCount)>> GetFilteredProductsAsync(int languageId, ProductFilterDto filterDto)
        {
            var query = GetProductsWithIncludes(languageId);

            // Apply filters
            query = ApplyFilters(query, filterDto);

            // Get count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var pageNumber = Math.Max(1, filterDto.PageNumber);
            var pageSize = Math.Clamp(filterDto.PageSize <= 0 ? 12 : filterDto.PageSize, 1, 50);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ApiResponse<(IEnumerable<Product> Items, int TotalCount)>()
            {
                IsSuccess = true,
                Data = (items, totalCount)
            };
        }

        public async Task<ApiResponse<Product>> GetProductById(int id, int langId)
        {
            var productWithIncludes = GetProductsWithIncludes(langId);

            var product = await productWithIncludes.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return new ApiResponse<Product>()
                {
                    IsSuccess = false,
                    Message = "product not found",
                    Data = null
                };
            }

            return new ApiResponse<Product>()
            {
                IsSuccess = true,
                Data = product,
            };
        }

        public async Task<ApiResponse<Product>> CreateProduct(ProductDetailsAdminDto productDto)
        {
            // This list used to store physical file paths of successfully saved files, 
            // so we can delete them if something goes wrong later in the process
            var savedPhysicalFiles = new List<string>();

            try
            {
                // Start transaction
                await _unitOfWork.BeginTransactionAsync();

                // File guards
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                // Resolve upload folder, which is wwwroot
                var webRoot = _env.WebRootPath;
                _logger.LogInformation("Web root path resolved to: {WebRoot}", webRoot);

                // Create wwwroot if it doesn't exist
                if (string.IsNullOrEmpty(webRoot))
                {
                    webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    _logger.LogInformation("webRoot now is: {0}", webRoot);
                }

                var uploadFolder = Path.Combine(webRoot, "Uploads", "Products");
                Directory.CreateDirectory(uploadFolder);

                // Lookup category
                //var category = await _categoryService.GetAsync(c => c.Translations.Any(t => t.Name.ToLower() == productDto.category.ToLower()));
                //if (category == null)
                //    throw new NotFoundException($"Category '{productDto.category}' not found.");

                // Lookup languages (ar / en)
                var ar = await _langServices.GetAsync(l => l.Code.ToLower() == "ar");
                if (ar == null)
                    throw new NotFoundException("Arabic language not found in the system.");

                var en = await _langServices.GetAsync(l => l.Code.ToLower() == "en");
                if (en == null)
                    throw new NotFoundException("English language not found in the system.");

                if (!(await _categoryService.ExistsAsync(c => c.Id == productDto.CategoryId)))
                    throw new NotFoundException($"Category with ID '{productDto.CategoryId}' not found");

                // Start building product entity
                var product = new Product()
                {
                    CategoryId = productDto.CategoryId,
                    Price = productDto.Price,
                    OriginalPrice = productDto.Price,
                    IsDeleted = productDto.IsDeleted,
                    CreatedAt = DateTime.Now
                };

                // Add translations
                product.Translations.Add(new ProductTranslation
                {
                    LanguageId = ar.Id,
                    Name = productDto.NameAr,
                    Description = productDto.DescriptionAr,
                    Slug = productDto.SlugAr,
                    MetaTitle = productDto.MetaTitleAr,
                    MetaDescription = productDto.MetaDescriptionAr
                });

                product.Translations.Add(new ProductTranslation
                {
                    LanguageId = en.Id,
                    Name = productDto.NameEn,
                    Description = productDto.DescriptionEn,
                    Slug = productDto.SlugEn,
                    MetaTitle = productDto.MetaTitleEn,
                    MetaDescription = productDto.MetaDescriptionEn
                });


                var allFiles = productDto.Variants.SelectMany(v => v.Images).Select(i => i.File).ToList();

                foreach (var file in allFiles)
                {
                    if (file == null || file.Length == 0)
                        throw new ValidationException("One or more image files are missing or empty.");

                    if (file.Length > maxFileSize)
                        throw new ValidationException("One or more files exceed the maximum allowed size of 5MB.");

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExt.Contains(ext))
                        throw new ValidationException("One or more files have an invalid file extension.");
                }

                foreach (var vDto in productDto.Variants)
                {
                    // find size
                    if (! (await _sizeService.ExistsAsync(s => s.Id == vDto.SizeId)))
                        throw new NotFoundException($"Size with Id: '{vDto.SizeId}' not found.");

                    // find color
                    if (!(await _colorService.ExistsAsync(c => c.Id == vDto.ColorId)))
                        throw new NotFoundException($"Color with Id: '{vDto.ColorId}' not found.");

                    var variant = new Variant()
                    {
                        SizeId = vDto.SizeId,
                        ColorId = vDto.ColorId,
                        Quantity = vDto.Quantity,
                        Reserved = 0
                    };

                    // images
                    foreach (var imgDto in vDto.Images)
                    {
                        var file = imgDto.File;
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                        var physicalPath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var stream = new FileStream(physicalPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        savedPhysicalFiles.Add(physicalPath);

                        MediaAsset mediaAsset = new()
                        {
                            FileName = uniqueFileName,
                            FilePath = $"/Uploads/Products/{uniqueFileName}",
                            AltText = productDto.SlugEn,
                            MediaType = "image",
                            CreatedAt = DateTime.Now,
                        };

                        variant.Images.Add(new VariantImage
                        {
                            MediaAsset = mediaAsset,
                            SortOrder = imgDto.sortOrder,
                        });
                    }

                    product.Variants.Add(variant);
                }

                // Begin transaction

                // Add product and save
                await AddAsync(product);
                await SaveChangesAsync();

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Product created successfully with ID {ProductId}", product.Id);

                return new()
                {
                    IsSuccess = true,
                    Data = product,
                    Message = "Product created successfully."
                };
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating product: {Message}", ex.Message);

                Cleanup(savedPhysicalFiles);
                return new()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found error while creating product: {Message}", ex.Message);

                Cleanup(savedPhysicalFiles);
                return new()
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                // delete saved files
                Cleanup(savedPhysicalFiles);

                _logger.LogError(ex, "Error occurred while creating product.");

                return new()
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred while creating the product."
                };
            }
            finally
            {
                // rollback transaction if active
                if (!_unitOfWork.IsTransactionFinished())
                {
                    try
                    {
                        await _unitOfWork.RollbackAsync();
                    }
                    catch
                    {
                        _logger.LogError("Failed to rollback transaction after error occurred.");
                    }
                }
            }
        }

        void Cleanup(List<string> savedPhysicalFiles)
        {
            foreach (var path in savedPhysicalFiles)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file at {FilePath} during cleanup.", path);
                }
            }
        }
    }
}
