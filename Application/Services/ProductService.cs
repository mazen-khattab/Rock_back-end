using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Application.Responses;
using Core.Entities;
using Core.Enum;
using Core.ExceptionsTypes;
using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Services
{
    public class ProductService : Services<Product>, IProductService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IServices<Category> _categoryService;
        private readonly IServices<Language> _langServices;
        private readonly IServices<Size> _sizeService;
        private readonly IServices<Color> _colorService;
        private readonly IServices<MediaAsset> _mediaAssetService;

        public ProductService(
            IUnitOfWork unitOfWork,
            IRepo<Product> productRepo,
            ILogger<ProductService> logger,
            IWebHostEnvironment env,
            IServices<Category> categoryService,
            IServices<Language> langServices,
            IServices<Size> sizeService,
            IServices<Color> colorService,
            IServices<MediaAsset> mediaAssetService) : base(unitOfWork, productRepo, logger)
        {
            _env = env;
            _categoryService = categoryService;
            _langServices = langServices;
            _sizeService = sizeService;
            _colorService = colorService;
            _mediaAssetService = mediaAssetService;
        }

        private async Task<int> GetLanguageId(string langCode)
        {
            _logger.LogInformation("Retrieving language ID for language code: {LangCode}", langCode);
            int langId = await _langServices.Query(l => l.Code == langCode).Select(l => l.Id).FirstOrDefaultAsync();

            if (langId == 0)
            {
                _logger.LogWarning("Language code {LangCode} not found. Defaulting to language ID 1.", langCode);
                return 1; // Default to Arabic if not found
            }

            _logger.LogInformation("Found language ID {LangId} for language code {LangCode}", langId, langCode);
            return langId;
        }

        #region GetAllProducts
        public async Task<ApiResponse<(IEnumerable<ProductsDto> Items, int TotalCount)>> GetAllProducts(ProductFilterDto filterDto, string langCode, string baseUrl)
        {
            _logger.LogInformation("GetAllProducts called with LangCode: {LangCode}, BaseUrl: {BaseUrl}, PageNumber: {PageNumber}, PageSize: {PageSize}", langCode, baseUrl, filterDto.PageNumber, filterDto.PageSize);

            var langId = await GetLanguageId(langCode);

            var query = GetProductsWithIncludes(langId, p => p.IsActive == true);
            _logger.LogDebug("Base query created with IsActive filter");

            var (items, totalCount) = await GetPaginatedProducts(query, filterDto, langId);

            var itemsDto = items.ToDtoList(langId, baseUrl);
            _logger.LogDebug("Mapped {ItemCount} products to DTOs with LanguageId: {LangId}", itemsDto.Count(), langId);

            return new ApiResponse<(IEnumerable<ProductsDto> Items, int TotalCount)>()
            {
                IsSuccess = true,
                Data = (itemsDto, totalCount)
            };
        }
        #endregion

        #region GetAllAdminProducts
        public async Task<ApiResponse<(IEnumerable<ProductsAdminDto> Items, int TotalCount)>> GetAllAdminProducts(ProductFilterDto filterDto, string langCode, string baseUrl)
        {
            _logger.LogInformation("GetAllAdminProducts called with LangCode: {LangCode}, BaseUrl: {BaseUrl}, PageNumber: {PageNumber}, PageSize: {PageSize}", langCode, baseUrl, filterDto.PageNumber, filterDto.PageSize);

            var langId = await GetLanguageId(langCode);

            var query = GetProductsWithIncludes(langId);
            _logger.LogDebug("Base query created with IsActive filter");

            var (items, totalCount) = await GetPaginatedProducts(query, filterDto, langId);

            var itemsDto = items.ToDtoAdminList(langId, baseUrl);
            _logger.LogDebug("Mapped {ItemCount} products to AdminDTOs with LanguageId: {LangId}", itemsDto.Count(), langId);

            return new ApiResponse<(IEnumerable<ProductsAdminDto> Items, int TotalCount)>()
            {
                IsSuccess = true,
                Data = (itemsDto, totalCount)
            };
        }
        #endregion

        #region GetProductById
        public async Task<ApiResponse<ProductDetailsDto>> GetProductById(int id, string langCode, string baseUrl)
        {
            _logger.LogInformation("GetProductById service called with Id: {Id}, LangCode: {LangCode}", id, langCode);

            int langId = await GetLanguageId(langCode);
            _logger.LogDebug("Resolved LanguageId: {LangId} for LangCode: {LangCode}", langId, langCode);

            var productWithIncludes = GetProductsWithIncludes(langId);

            var product = await productWithIncludes.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Product not found for Id: {Id}, LangCode: {LangCode}", id, langCode);

                return new ApiResponse<ProductDetailsDto>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "product not found",
                    Data = null
                };
            }

            _logger.LogDebug("Product found for Id: {Id}, proceeding to DTO mapping", id);

            var productDto = product.ToDetailsDto(langId, baseUrl);

            _logger.LogInformation("Successfully retrieved product details for Id: {Id}, LangCode: {LangCode}", id, langCode);

            return new ApiResponse<ProductDetailsDto>()
            {
                IsSuccess = true,
                Data = productDto,
            };
        }
        #endregion

        #region GetAdminProductbyId
        public async Task<ApiResponse<ProductDetailsAdminDto>> GetAdminProductbyId(int id, string baseUrl)
        {
            _logger.LogInformation("GetAdminProductbyId service called with Id: {Id}", id);
            _logger.LogDebug("BaseUrl: {BaseUrl}", baseUrl);

            var productWithIncludes = GetAdminProductByIdWithIncludes(id);

            var product = await productWithIncludes.FirstOrDefaultAsync();

            if (product == null)
            {
                _logger.LogWarning("Admin product not found for Id: {Id}", id);

                return new ApiResponse<ProductDetailsAdminDto>()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "product not found",
                    Data = null
                };
            }

            _logger.LogDebug("Admin product found for Id: {Id}, proceeding to DTO mapping", id);

            var adminProductDto = product.ToAdminDetailsDto(baseUrl);

            _logger.LogInformation("Successfully retrieved admin product details for Id: {Id}", id);

            return new ApiResponse<ProductDetailsAdminDto>()
            {
                IsSuccess = true,
                Data = adminProductDto,
            };
        }
        #endregion

        #region CreateProduct
        public async Task<ApiResponse<ProductDetailsAdminDto>> CreateProduct(ProductDetailsAdminDto productDto, string baseUrl)
        {
            _logger.LogInformation("CreateProduct service called");
            _logger.LogDebug("ProductDto: {@ProductDto}", productDto);

            // This list used to store physical file paths of successfully saved files, 
            // so we can delete them if something goes wrong later in the process
            var savedPhysicalFiles = new List<string>();

            try
            {
                _logger.LogDebug("Starting product creation process");
                // File guards
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                string uploadFolder = GetOrCreateUploaderFolder();

                // Lookup ar language
                Language ar = await _langServices.GetAsync(l => l.Code.ToLower() == "ar", false) ?? throw new NotFoundException("Arabic language not found in the system.");

                // Lookup en language
                Language en = await _langServices.GetAsync(l => l.Code.ToLower() == "en", false) ?? throw new NotFoundException("English language not found in the system.");

                if (!(await _categoryService.ExistsAsync(c => c.Id == productDto.CategoryId)))
                    throw new NotFoundException($"Category with ID '{productDto.CategoryId}' not found");

                // Start building product entity
                var product = new Product();
                product.FromDetialsAdminDto(productDto);

                // Add translations
                product.Translations.Add(new ProductTranslation()
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
                    if (!(await _sizeService.ExistsAsync(s => s.Id == vDto.SizeId)))
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
                        var file = imgDto.File!;

                        MediaAsset mediaAsset = CreateMediaAsset(file, uploadFolder, savedPhysicalFiles);

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
                Add(product);
                await SaveChangesAsync();

                _logger.LogInformation("Product created successfully with ID {ProductId}", product.Id);

                var createdProductDto = product.ToAdminDetailsDto(baseUrl);

                return new()
                {
                    IsSuccess = true,
                    Data = createdProductDto,
                    Message = "Product created successfully."
                };
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating product: {Message}", ex.Message);

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = ex.Message
                };
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found error while creating product: {Message}", ex.Message);

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product.");

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ServerError,
                    Message = "An unexpected error occurred while creating the product."
                };
            }
        }
        #endregion

        #region UpdateProduct
        public async Task<ApiResponse<ProductDetailsAdminDto>> UpdateProduct(int id, ProductDetailsAdminDto productDto, string baseUrl)
        {
            _logger.LogInformation("UpdateProduct service called");
            _logger.LogDebug("ProductDto: {@ProductDto}", productDto);

            // Step 1: Initialize File Tracker
            var savedPhysicalFiles = new List<string>();

            try
            {
                _logger.LogDebug("Starting product updating process");

                // Step 3: Get Upload Context
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                const long maxFileSize = 5 * 1024 * 1024; // 5MB

                string uploadFolder = GetOrCreateUploaderFolder();

                // Step 4: Fetch Complete Aggregation (Crucial)
                var product = await GetAdminProductByIdWithIncludes(id)
                    .FirstOrDefaultAsync();

                // Step 5: Validate Product Existence
                if (product == null)
                    throw new NotFoundException($"Product with ID '{id}' not found");
                _logger.LogInformation("Beginning update of product ID: {ProductId}", id);

                // Lookup AR language
                Language ar = await _langServices.GetAsync(l => l.Code.ToLower() == "ar", false) ?? throw new NotFoundException("Arabic language not found in the system.");
                // Lookup EN language
                Language en = await _langServices.GetAsync(l => l.Code.ToLower() == "en", false) ?? throw new NotFoundException("English language not found in the system.");

                // Validate category
                if (!(await _categoryService.ExistsAsync(c => c.Id == productDto.CategoryId)))
                    throw new NotFoundException($"Category with ID '{productDto.CategoryId}' not found");

                // Step 6: Update Product Translations
                var arTranslation = product.Translations.FirstOrDefault(t => t.LanguageId == ar.Id);
                var enTranslation = product.Translations.FirstOrDefault(t => t.LanguageId == en.Id);

                if (arTranslation != null)
                    arTranslation.FromDetailsAdminDto(productDto, 1);

                if (enTranslation != null)
                    enTranslation.FromDetailsAdminDto(productDto, 2);

                // Update basic product properties
                product.FromDetialsAdminDto(productDto);

                // Step 7: Validate New Files Only
                var allNewImages = productDto.Variants
                    .SelectMany(v => v.Images)
                    .Where(i => i.File != null)
                    .ToList();

                foreach (var imgDto in allNewImages)
                {
                    var file = imgDto.File!;

                    if (file.Length > maxFileSize)
                        throw new ValidationException("One or more files exceed the maximum allowed size of 5MB.");

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExt.Contains(ext))
                        throw new ValidationException("One or more files have an invalid file extension.");
                }

                // Step 8: Synchronize Variants (Orphan Removal)
                var incomingVariantIds = productDto.Variants
                    //.Where(v => v.Id.HasValue)
                    .Select(v => v.Id)
                    .ToList();

                // Identify variants to remove (those that exist in DB but not in incoming DTO)
                var variantsToRemove = product.Variants
                    .Where(v => !incomingVariantIds.Contains(v.Id))
                    .ToList();

                foreach (var variantToRemove in variantsToRemove)
                {
                    _logger.LogInformation("Removing orphaned variant ID: {VariantId}", variantToRemove.Id);
                    product.Variants.Remove(variantToRemove);
                }

                // Step 9: Process Variants (Loop 1)
                foreach (var vDto in productDto.Variants)
                {
                    var existingVariant = product.Variants.FirstOrDefault(v => v.Id == vDto.Id);

                    if (existingVariant != null)
                    {
                        // Validate Size and Color exist
                        if (!(await _sizeService.ExistsAsync(s => s.Id == vDto.SizeId)))
                            throw new NotFoundException($"Size with Id: '{vDto.SizeId}' not found.");

                        if (!(await _colorService.ExistsAsync(c => c.Id == vDto.ColorId)))
                            throw new NotFoundException($"Color with Id: '{vDto.ColorId}' not found.");

                        // Update variant properties
                        existingVariant.SizeId = vDto.SizeId;
                        existingVariant.ColorId = vDto.ColorId;
                        existingVariant.Quantity = vDto.Quantity;

                        // Step 10: Synchronize and Process Variant Images (Loop 2 - Nested)
                        SyncVariantImages(existingVariant, vDto, uploadFolder, allowedExt, maxFileSize, savedPhysicalFiles);
                    }
                    else
                    {
                        // New Variant - Create
                        if (!(await _sizeService.ExistsAsync(s => s.Id == vDto.SizeId)))
                            throw new NotFoundException($"Size with Id: '{vDto.SizeId}' not found.");

                        if (!(await _colorService.ExistsAsync(c => c.Id == vDto.ColorId)))
                            throw new NotFoundException($"Color with Id: '{vDto.ColorId}' not found.");

                        var newVariant = new Variant()
                        {
                            SizeId = vDto.SizeId,
                            ColorId = vDto.ColorId,
                            Quantity = vDto.Quantity,
                            Reserved = 0
                        };

                        // Process images for new variant
                        SyncVariantImages(newVariant, vDto, uploadFolder, allowedExt, maxFileSize, savedPhysicalFiles);

                        product.Variants.Add(newVariant);
                    }
                }

                // Step 11: Save and Commit Changes
                await SaveChangesAsync();

                _logger.LogInformation("Product ID: {ProductId} updated successfully", id);

                var updatedProductDto = product.ToAdminDetailsDto(baseUrl);

                return new()
                {
                    IsSuccess = true,
                    Data = updatedProductDto,
                    Message = "Product updated successfully."
                };
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating product ID {ProductId}: {Message}", id, ex.Message);

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ValidationError,
                    Message = ex.Message
                };
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found error while updating product ID {ProductId}: {Message}", id, ex.Message);

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating product ID: {ProductId}", id);

                await CleanUpPhysicalFiles(savedPhysicalFiles);

                return new()
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.ServerError,
                    Message = "An unexpected error occurred while updating the product."
                };
            }
        }
        #endregion

        #region DeleteProduct
        public async Task<ApiResponse<int>> DeleteProduct(int id)
        {
            _logger.LogInformation("DeleteProduct requested for ID: {ProductId}", id);

            // Fetch the product including related entities if needed
            //var product1 = await GetAdminProductByIdWithIncludes(id).FirstOrDefaultAsync();
            var product = await GetByIdAsync(id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID: {ProductId} not found for deletion.", id);
                return new ApiResponse<int>
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.NotFound,
                    Message = "Product not found."
                };
            }

            _logger.LogInformation("Deleting product ID: {ProductId}", id);

            Delete(product);
            await SaveChangesAsync();

            _logger.LogInformation("Product ID: {ProductId} deleted successfully.", id);

            return new ApiResponse<int>
            {
                IsSuccess = true,
                Data = id,
                Message = "Product deleted successfully."
            };
        }
        #endregion

        #region Helper Methods
        private IQueryable<Product> GetProductsWithIncludes(int languageId, Expression<Func<Product, bool>>? filter = null, bool ignoreQueryFilters = false)
        {
            _logger.LogInformation("Building product query for language {LanguageId}", languageId);

            var products = Query(filter, false, ignoreQueryFilters)
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

        private async Task<(List<Product> Items, int totalCount)> GetPaginatedProducts(IQueryable<Product> query, ProductFilterDto filterDto, int langId)
        {
            // Apply filters
            query = ApplyFilters(query, filterDto);
            _logger.LogDebug("Filters applied to query");

            // Get count
            int totalCount = await query.CountAsync();
            _logger.LogInformation("Total products count after filters: {TotalCount}", totalCount);

            // Apply pagination
            var pageNumber = Math.Max(1, filterDto.PageNumber);
            var pageSize = Math.Clamp(filterDto.PageSize <= 0 ? 12 : filterDto.PageSize, 1, 50);
            _logger.LogDebug("Pagination applied - PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        private IQueryable<Product> GetAdminProductByIdWithIncludes(int id)
        {
            _logger.LogDebug("Building product query for admin");

            var products = Query(p => p.Id == id, tracked: true, ignoreQueryFilters: true)
                .Include(p => p.Translations)
                    .ThenInclude(t => t.Language)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)
                        .ThenInclude(i => i.MediaAsset);

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

        private string GetOrCreateUploaderFolder()
        {
            // Resolve upload folder, which is wwwroot
            var webRoot = _env.WebRootPath;
            _logger.LogInformation("Web root path resolved to: {WebRoot}", webRoot);

            // Create wwwroot path if it doesn't exist
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                _logger.LogInformation("webRoot now is: {0}", webRoot);
            }

            var uploadFolder = Path.Combine(webRoot, "Uploads", "Products");

            // Create upload folder if it doesn't exist
            Directory.CreateDirectory(uploadFolder);

            return uploadFolder;
        }

        private void SyncVariantImages(Variant variant, VariantsDetailsAdminDto vDto, string uploadFolder, string[] allowedExt, long maxFileSize, List<string> savedPhysicalFiles)
        {
            _logger.LogInformation("Sync images for variant ID: {VariantId}", variant.Id);

            // Identify images to remove (orphan removal)
            var incomingImageIds = vDto.Images
                //.Where(img => img.Id.HasValue)
                .Select(img => img.Id)
                .ToList();

            var imagesToRemove = variant.Images
                .Where(img => !incomingImageIds.Contains(img.Id))
                .ToList();

            // Remove the images from DB those not exist in the dto
            foreach (var imageToRemove in imagesToRemove)
            {
                _logger.LogInformation("Removing image ID: {ImageId} from variant", imageToRemove.Id);

                try
                {
                    // Delete physical file
                    if (imageToRemove.MediaAsset != null && !string.IsNullOrEmpty(imageToRemove.MediaAsset.FilePath))
                    {
                        var fileName = imageToRemove.MediaAsset.FileName;
                        _logger.LogInformation("The file name is: {0}", fileName);

                        var physicalPath = Path.Combine(uploadFolder, fileName);
                        _logger.LogInformation("The physical path is: {0}", physicalPath);

                        if (File.Exists(physicalPath))
                        {
                            File.Delete(physicalPath);
                            _logger.LogInformation("Deleted physical file: {FilePath}", physicalPath);
                            _mediaAssetService.Delete(imageToRemove.MediaAsset);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting physical file for image ID: {ImageId}", imageToRemove.Id);
                }

                variant.Images.Remove(imageToRemove);
            }

            // Process incoming images
            foreach (var imgDto in vDto.Images)
            {
                if (imgDto.File != null)
                {
                    // Scenario A: New Image Uploaded
                    var file = imgDto.File;

                    MediaAsset mediaAsset = CreateMediaAsset(file, uploadFolder, savedPhysicalFiles);

                    variant.Images.Add(new VariantImage
                    {
                        MediaAsset = mediaAsset,
                        SortOrder = imgDto.sortOrder,
                    });

                    _logger.LogInformation("Added new image: {FileName} to variant", mediaAsset.FilePath);
                }
                else if (!string.IsNullOrEmpty(imgDto.ImageUrl))
                {
                    // Scenario B: Existing Image / Order Changed
                    var existingImage = variant.Images.FirstOrDefault(img => img.Id == imgDto.Id);
                    if (existingImage != null && existingImage.SortOrder != imgDto.sortOrder)
                    {
                        _logger.LogInformation("Updating sort order for image ID: {ImageId} from {OldOrder} to {NewOrder}",
                            imgDto.Id, existingImage.SortOrder, imgDto.sortOrder);

                        existingImage.SortOrder = imgDto.sortOrder;
                    }
                }
            }
        }

        private MediaAsset CreateMediaAsset(IFormFile file, string uploadFolder, List<string> savedPhysicalFiles)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant(); // (e.g. ".jpg")

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var physicalPath = Path.Combine(uploadFolder, uniqueFileName); // (e.g. "F:\\Mazen\\Rock\\Rock_back-end\\API\\wwwroot\\Uploads\\Products\\f13fee9b-3dc9-4541-b3f3-8c518e34830b.jpg")

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                file.CopyToAsync(stream).Wait();
            }

            savedPhysicalFiles.Add(physicalPath);

            MediaAsset mediaAsset = new()
            {
                FileName = uniqueFileName,
                FilePath = $"/Uploads/Products/{uniqueFileName}",
                AltText = uniqueFileName,
                MediaType = "image",
            };

            return mediaAsset;
        }

        private async Task CleanUpPhysicalFiles(List<string> savedPhysicalFiles)
        {
            // Cleanup physical files
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
        #endregion
    }
}
