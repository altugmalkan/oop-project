using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockECommerce.BusinessLayer.Exceptions;
using MockECommerce.BusinessLayer.Services;
using MockECommerce.DAL.Abstract;
using MockECommerce.DAL.Data;
using MockECommerce.DAL.Entities;
using MockECommerce.DAL.Enums;
using MockECommerce.DtoLayer.ProductDtos;

namespace MockECommerce.BusinessLayer.Managers;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly ICategoryDal _categoryDal;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;

    public ProductManager(IProductDal productDal, ICategoryDal categoryDal, IMapper mapper, AppDbContext context)
    {
        _productDal = productDal;
        _categoryDal = categoryDal;
        _mapper = mapper;
        _context = context;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, Guid userId)
    {
        // Validate category exists
        var category = await _categoryDal.GetByIdAsync(createProductDto.CategoryId);
        if (category == null)
        {
            throw new NotFoundException("Belirtilen kategori bulunamadı", "CATEGORY_NOT_FOUND");
        }

        // Validate user ID
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        // Find or create SellerProfile for the user
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
        
        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı. Lütfen önce satıcı profilinizi oluşturun.", "SELLER_PROFILE_NOT_FOUND");
        }

        // Map DTO to entity
        var product = _mapper.Map<Product>(createProductDto);
        product.Id = Guid.NewGuid();
        product.SellerId = sellerProfile.Id; // Use SellerProfile.Id, not User.Id
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        // Handle images
        if (createProductDto.Images?.Any() == true)
        {
            var images = new List<ProductImage>();
            var hasPrimary = createProductDto.Images.Any(i => i.IsPrimary);
            
            for (int i = 0; i < createProductDto.Images.Count; i++)
            {
                var imageDto = createProductDto.Images[i];
                var image = _mapper.Map<ProductImage>(imageDto);
                image.Id = Guid.NewGuid();
                image.ProductId = product.Id;
                
                // If no image is marked as primary, make the first one primary
                if (!hasPrimary && i == 0)
                {
                    image.IsPrimary = true;
                }
                
                images.Add(image);
            }
            
            product.Images = images;
        }

        await _productDal.CreateAsync(product);
        
        // Get the created product with details
        var createdProduct = await _productDal.GetByIdWithDetailsAsync(product.Id);
        return _mapper.Map<ProductDto>(createdProduct);
    }

    public async Task<BulkCreateProductResultDto> CreateProductsBulkAsync(List<CreateProductDto> products, Guid userId)
    {
        var result = new BulkCreateProductResultDto
        {
            TotalRequested = products.Count
        };

        // Validate user ID
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        // Find SellerProfile for the user
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);

        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı. Lütfen önce satıcı profilinizi oluşturun.", "SELLER_PROFILE_NOT_FOUND");
        }

        // Pre-fetch all categories to validate
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var existingCategories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

        for (int i = 0; i < products.Count; i++)
        {
            var createProductDto = products[i];
            try
            {
                // Validate category exists
                if (!existingCategories.Contains(createProductDto.CategoryId))
                {
                    result.Errors.Add(new BulkCreateErrorDto
                    {
                        Index = i,
                        Title = createProductDto.Title,
                        ErrorMessage = "Belirtilen kategori bulunamadı",
                        ErrorCode = "CATEGORY_NOT_FOUND"
                    });
                    result.FailedCount++;
                    continue;
                }

                // Map DTO to entity
                var product = _mapper.Map<Product>(createProductDto);
                product.Id = Guid.NewGuid();
                product.SellerId = sellerProfile.Id;
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                // Handle images
                if (createProductDto.Images?.Any() == true)
                {
                    var images = new List<ProductImage>();
                    var hasPrimary = createProductDto.Images.Any(img => img.IsPrimary);

                    for (int j = 0; j < createProductDto.Images.Count; j++)
                    {
                        var imageDto = createProductDto.Images[j];
                        var image = _mapper.Map<ProductImage>(imageDto);
                        image.Id = Guid.NewGuid();
                        image.ProductId = product.Id;

                        if (!hasPrimary && j == 0)
                        {
                            image.IsPrimary = true;
                        }

                        images.Add(image);
                    }

                    product.Images = images;
                }

                await _productDal.CreateAsync(product);

                var createdProduct = await _productDal.GetByIdWithDetailsAsync(product.Id);
                result.CreatedProducts.Add(_mapper.Map<ProductDto>(createdProduct));
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkCreateErrorDto
                {
                    Index = i,
                    Title = createProductDto.Title,
                    ErrorMessage = ex.Message,
                    ErrorCode = "CREATION_FAILED"
                });
                result.FailedCount++;
            }
        }

        return result;
    }

    public async Task<ProductDto> CreateProductBySellerIdAsync(CreateProductDto createProductDto, Guid sellerId)
    {
        // Validate category exists
        var category = await _categoryDal.GetByIdAsync(createProductDto.CategoryId);
        if (category == null)
        {
            throw new NotFoundException("Belirtilen kategori bulunamadı", "CATEGORY_NOT_FOUND");
        }

        // Validate seller ID
        if (sellerId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz satıcı bilgisi", "INVALID_SELLER");
        }

        // Verify seller profile exists
        var sellerProfile = await _context.SellerProfiles.FindAsync(sellerId);
        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı.", "SELLER_PROFILE_NOT_FOUND");
        }

        // Map DTO to entity
        var product = _mapper.Map<Product>(createProductDto);
        product.Id = Guid.NewGuid();
        product.SellerId = sellerId; // Use SellerProfile.Id directly
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        // Handle images
        if (createProductDto.Images?.Any() == true)
        {
            var images = new List<ProductImage>();
            var hasPrimary = createProductDto.Images.Any(i => i.IsPrimary);
            
            for (int i = 0; i < createProductDto.Images.Count; i++)
            {
                var imageDto = createProductDto.Images[i];
                var image = _mapper.Map<ProductImage>(imageDto);
                image.Id = Guid.NewGuid();
                image.ProductId = product.Id;
                
                // If no image is marked as primary, make the first one primary
                if (!hasPrimary && i == 0)
                {
                    image.IsPrimary = true;
                }
                
                images.Add(image);
            }
            
            product.Images = images;
        }

        await _productDal.CreateAsync(product);
        
        // Get the created product with details
        var createdProduct = await _productDal.GetByIdWithDetailsAsync(product.Id);
        return _mapper.Map<ProductDto>(createdProduct);
    }

    public async Task<ProductDto> UpdateProductAsync(UpdateProductDto updateProductDto, Guid userId)
    {
        var existingProduct = await _productDal.GetByIdWithDetailsAsync(updateProductDto.Id);
        if (existingProduct == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Validate user ID
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        // Get seller profile from user ID
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı. Lütfen önce satıcı profilinizi oluşturun.", "SELLER_PROFILE_NOT_FOUND");
        }

        // Check if the seller owns this product
        if (existingProduct.SellerId != sellerProfile.Id)
        {
            throw new BusinessException("Bu ürünü güncelleme yetkiniz yok", "UNAUTHORIZED_PRODUCT_UPDATE");
        }

        // Validate category exists if changed
        if (existingProduct.CategoryId != updateProductDto.CategoryId)
        {
            var category = await _categoryDal.GetByIdAsync(updateProductDto.CategoryId);
            if (category == null)
            {
                throw new NotFoundException("Belirtilen kategori bulunamadı", "CATEGORY_NOT_FOUND");
            }
        }

        // Update product properties
        _mapper.Map(updateProductDto, existingProduct);
        existingProduct.UpdatedAt = DateTime.UtcNow;

        // Handle image updates
        if (updateProductDto.Images?.Any() == true)
        {
            await UpdateProductImagesAsync(existingProduct, updateProductDto.Images);
        }

        await _productDal.UpdateAsync(existingProduct);
        
        // Get updated product with details
        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(existingProduct.Id);
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task DeleteProductAsync(Guid id, Guid userId)
    {
        var product = await _productDal.GetByIdAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Validate user ID
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        // Get seller profile from user ID
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı. Lütfen önce satıcı profilinizi oluşturun.", "SELLER_PROFILE_NOT_FOUND");
        }

        // Check if the seller owns this product
        if (product.SellerId != sellerProfile.Id)
        {
            throw new BusinessException("Bu ürünü silme yetkiniz yok", "UNAUTHORIZED_PRODUCT_DELETE");
        }

        await _productDal.DeleteAsync(product);
    }

    public async Task<ProductDto> UpdateProductAsAdminAsync(UpdateProductDto updateProductDto)
    {
        var existingProduct = await _productDal.GetByIdWithDetailsAsync(updateProductDto.Id);
        if (existingProduct == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Admin can update any product without ownership check
        
        // Validate category exists if changed
        if (existingProduct.CategoryId != updateProductDto.CategoryId)
        {
            var category = await _categoryDal.GetByIdAsync(updateProductDto.CategoryId);
            if (category == null)
            {
                throw new NotFoundException("Belirtilen kategori bulunamadı", "CATEGORY_NOT_FOUND");
            }
        }

        // Update product properties
        _mapper.Map(updateProductDto, existingProduct);
        existingProduct.UpdatedAt = DateTime.UtcNow;

        // Handle image updates
        if (updateProductDto.Images?.Any() == true)
        {
            await UpdateProductImagesAsync(existingProduct, updateProductDto.Images);
        }

        await _productDal.UpdateAsync(existingProduct);
        
        // Get updated product with details
        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(existingProduct.Id);
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task DeleteProductAsAdminAsync(Guid id)
    {
        var product = await _productDal.GetByIdAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Admin can delete any product without ownership check
        await _productDal.DeleteAsync(product);
    }

    public async Task<ProductDto> UpdateProductBySellerIdAsync(UpdateProductDto updateProductDto, Guid sellerId)
    {
        var existingProduct = await _productDal.GetByIdWithDetailsAsync(updateProductDto.Id);
        if (existingProduct == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Check if the seller owns this product (using SellerProfile.Id directly)
        if (existingProduct.SellerId != sellerId)
        {
            throw new BusinessException("Bu ürünü güncelleme yetkiniz yok", "UNAUTHORIZED_PRODUCT_UPDATE");
        }

        // Validate category exists if changed
        if (existingProduct.CategoryId != updateProductDto.CategoryId)
        {
            var category = await _categoryDal.GetByIdAsync(updateProductDto.CategoryId);
            if (category == null)
            {
                throw new NotFoundException("Belirtilen kategori bulunamadı", "CATEGORY_NOT_FOUND");
            }
        }

        // Update product properties
        _mapper.Map(updateProductDto, existingProduct);
        existingProduct.UpdatedAt = DateTime.UtcNow;

        // Handle image updates
        if (updateProductDto.Images?.Any() == true)
        {
            await UpdateProductImagesAsync(existingProduct, updateProductDto.Images);
        }

        await _productDal.UpdateAsync(existingProduct);
        
        // Get updated product with details
        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(existingProduct.Id);
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task DeleteProductBySellerIdAsync(Guid id, Guid sellerId)
    {
        var product = await _productDal.GetByIdAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        // Check if the seller owns this product (using SellerProfile.Id directly)
        if (product.SellerId != sellerId)
        {
            throw new BusinessException("Bu ürünü silme yetkiniz yok", "UNAUTHORIZED_PRODUCT_DELETE");
        }

        await _productDal.DeleteAsync(product);
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productDal.GetAllWithDetailsAsync();
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<List<ProductDto>> GetProductsByCategoryIdAsync(Guid categoryId)
    {
        var products = await _productDal.GetByCategoryIdAsync(categoryId);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetProductsBySellerIdAsync(Guid sellerId)
    {
        var products = await _productDal.GetBySellerIdAsync(sellerId);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetProductsByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }
        
        // Get SellerProfile from userId
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
        if (sellerProfile == null)
        {
            throw new BusinessException("Satıcı profili bulunamadı. Lütfen önce satıcı profilinizi oluşturun.", "SELLER_PROFILE_NOT_FOUND");
        }
        
        var products = await _productDal.GetBySellerIdAsync(sellerProfile.Id);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetActiveProductsAsync()
    {
        var products = await _productDal.GetActiveProductsAsync();
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetProductsByStatusAsync(ProductStatus status)
    {
        var products = await _productDal.GetByStatusAsync(status);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<ProductDto>();
        }

        var products = await _productDal.SearchProductsAsync(searchTerm.Trim());
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<List<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
        {
            throw new BusinessException("Geçersiz fiyat aralığı", "INVALID_PRICE_RANGE");
        }

        var products = await _productDal.GetProductsByPriceRangeAsync(minPrice, maxPrice);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<ProductDto> UpdateProductStatusAsync(Guid id, ProductStatus status)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;

        await _productDal.UpdateAsync(product);
        
        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    public async Task<ProductDto> ToggleProductActiveStatusAsync(Guid id)
    {
        var product = await _productDal.GetByIdWithDetailsAsync(id);
        if (product == null)
        {
            throw new NotFoundException("Ürün bulunamadı", "PRODUCT_NOT_FOUND");
        }

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _productDal.UpdateAsync(product);
        
        var updatedProduct = await _productDal.GetByIdWithDetailsAsync(id);
        return _mapper.Map<ProductDto>(updatedProduct);
    }

    private async Task UpdateProductImagesAsync(Product product, List<UpdateProductImageDto> imageDtos)
    {
        // Handle image deletions
        var imagesToDelete = imageDtos.Where(i => i.IsDeleted && i.Id.HasValue).ToList();
        foreach (var imageDto in imagesToDelete)
        {
            var existingImage = product.Images.FirstOrDefault(i => i.Id == imageDto.Id.Value);
            if (existingImage != null)
            {
                product.Images.Remove(existingImage);
            }
        }

        // Handle image updates and additions
        var activeImages = imageDtos.Where(i => !i.IsDeleted).ToList();
        var hasPrimary = activeImages.Any(i => i.IsPrimary);

        for (int i = 0; i < activeImages.Count; i++)
        {
            var imageDto = activeImages[i];
            
            if (imageDto.Id.HasValue)
            {
                // Update existing image
                var existingImage = product.Images.FirstOrDefault(img => img.Id == imageDto.Id.Value);
                if (existingImage != null)
                {
                    _mapper.Map(imageDto, existingImage);
                }
            }
            else
            {
                // Add new image
                var newImage = _mapper.Map<ProductImage>(imageDto);
                newImage.Id = Guid.NewGuid();
                newImage.ProductId = product.Id;
                
                // If no image is marked as primary, make the first one primary
                if (!hasPrimary && i == 0)
                {
                    newImage.IsPrimary = true;
                }
                
                product.Images.Add(newImage);
            }
        }
    }
    
    public async Task<object> CreateSellerProfileAsync(Guid userId, string storeName)
    {
        // Validate input
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        if (string.IsNullOrWhiteSpace(storeName))
        {
            throw new BusinessException("Mağaza adı gereklidir", "INVALID_STORE_NAME");
        }

        // Check if seller profile already exists
        var existingProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
        
        if (existingProfile != null)
        {
            throw new BusinessException("Satıcı profili zaten mevcut", "SELLER_PROFILE_EXISTS");
        }

        // Create new seller profile
        var sellerProfile = new MockECommerce.DAL.Entities.SellerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StoreName = storeName.Trim(),
            IsApproved = true, // Auto-approve for mock system
            CreatedAt = DateTime.UtcNow
        };

        _context.SellerProfiles.Add(sellerProfile);
        await _context.SaveChangesAsync();

        return new
        {
            id = sellerProfile.Id,
            userId = sellerProfile.UserId,
            storeName = sellerProfile.StoreName,
            isApproved = sellerProfile.IsApproved,
            createdAt = sellerProfile.CreatedAt
        };
    }

    public async Task<object?> GetSellerProfileByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);

        if (sellerProfile == null)
        {
            return null;
        }

        return new
        {
            id = sellerProfile.Id,
            userId = sellerProfile.UserId,
            storeName = sellerProfile.StoreName,
            isApproved = sellerProfile.IsApproved,
            createdAt = sellerProfile.CreatedAt
        };
    }

    public async Task<BulkCreateProductResultDto> SeedMockProductsAsync(Guid userId)
    {
        // Validate user ID
        if (userId == Guid.Empty)
        {
            throw new BusinessException("Geçersiz kullanıcı bilgisi", "INVALID_USER");
        }

        // Find or create SellerProfile for the user
        var sellerProfile = await _context.SellerProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);

        if (sellerProfile == null)
        {
            // Auto-create seller profile for seeding
            sellerProfile = new SellerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StoreName = "Mock Store",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.SellerProfiles.Add(sellerProfile);
            await _context.SaveChangesAsync();
        }

        // Get categories
        var categories = await _context.Categories.ToListAsync();
        if (!categories.Any())
        {
            throw new BusinessException("Kategori bulunamadı. Önce kategori oluşturun.", "NO_CATEGORIES");
        }

        var electronics = categories.FirstOrDefault(c => c.SeoSlug == "electronics" || c.CategoryName.Contains("Electron"));
        var fashion = categories.FirstOrDefault(c => c.SeoSlug == "fashion" || c.CategoryName.Contains("Fashion"));
        var homeLiving = categories.FirstOrDefault(c => c.SeoSlug == "home-living" || c.CategoryName.Contains("Home"));
        var sports = categories.FirstOrDefault(c => c.SeoSlug == "sports-outdoors" || c.CategoryName.Contains("Sport"));

        // Fallback to first category if specific ones not found
        var defaultCategory = categories.First();

        var mockProducts = new List<CreateProductDto>
        {
            new CreateProductDto
            {
                CategoryId = (sports ?? defaultCategory).Id,
                Title = "Fitness Tracker Band",
                Description = "Lightweight fitness tracker with heart rate monitor, step counter, and sleep tracking. Perfect for daily workouts.",
                Price = 399.99m,
                Stock = 250,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR7ly0ttQR7TQhdrReBbeHxN0TW-pXBSqpZDA&s", AltText = "Fitness Tracker Band", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (homeLiving ?? defaultCategory).Id,
                Title = "Deluxe Coffee Maker",
                Description = "Automatic coffee maker with built-in grinder, programmable timer, and thermal carafe. Brew perfect coffee every morning.",
                Price = 1499.99m,
                Stock = 60,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSyPa3kusgD_ZevIiQ_0d5OtJ8Owr-pREhCUw&s", AltText = "Deluxe Coffee Maker", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (electronics ?? defaultCategory).Id,
                Title = "Bluetooth Speaker Premium",
                Description = "Portable Bluetooth speaker with 360-degree sound, waterproof design, and 24-hour battery life. Perfect for outdoor adventures.",
                Price = 599.99m,
                Stock = 120,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://cdn.dsmcdn.com/mnresize/400/-/ty1757/prod/QC_PREP/20250917/10/f8610118-33a4-36e4-9c85-9029d6f3037c/1_org_zoom.jpg", AltText = "Bluetooth Speaker Premium", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (fashion ?? defaultCategory).Id,
                Title = "Designer Luxury Handbag",
                Description = "Elegant designer handbag made from genuine leather with gold-plated hardware. Spacious interior with multiple compartments.",
                Price = 1899.99m,
                Stock = 30,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://cdn.dsmcdn.com/mnresize/420/620/ty1545/product/media/images/ty1547/prod/QC/20240915/14/a68d2478-6386-31fb-a6a9-b1990983bc68/1_org_zoom.jpg", AltText = "Designer Luxury Handbag", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (fashion ?? defaultCategory).Id,
                Title = "Elegant Winter Coat",
                Description = "Premium quality winter coat with warm lining, water-resistant exterior, and stylish design. Perfect for cold weather.",
                Price = 1299.99m,
                Stock = 50,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://cdn.dsmcdn.com/mnresize/400/-/ty1608/prod/QC/20241130/18/e496f645-c425-3155-887b-de8e605f275b/1_org_zoom.jpg", AltText = "Elegant Winter Coat", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (electronics ?? defaultCategory).Id,
                Title = "Lenovo IdeaPad Slim 3 Laptop",
                Description = "Lenovo IdeaPad Slim 3 with Intel Celeron N100, 4GB RAM, 128GB SSD, Intel UHD Graphics, 15.6 inch FHD display, Windows 11.",
                Price = 8999.99m,
                Stock = 40,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "http://trendyol.com/lenovo/ideapad-slim-3-intel-celeron-n100-4gb-128gb-intel-uhd-graphics-15-6-fhd-w11-notebook-82xb009gtx-p-941342405", AltText = "Lenovo IdeaPad Slim 3", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (electronics ?? defaultCategory).Id,
                Title = "MacBook Pro 14 inch",
                Description = "Apple MacBook Pro 14 inch with M3 chip, 16GB unified memory, 512GB SSD. Stunning Liquid Retina XDR display.",
                Price = 54999.99m,
                Stock = 25,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://sm.pcmag.com/pcmag_me/photo/default/macbook-6_hgfm.jpg", AltText = "MacBook Pro 14 inch", IsPrimary = true, DisplayOrder = 0 }
                }
            },
            new CreateProductDto
            {
                CategoryId = (homeLiving ?? defaultCategory).Id,
                Title = "Decorative Modern Table Lamp",
                Description = "Modern decorative lamp for your living room or bedroom. Elegant design with adjustable brightness and warm lighting.",
                Price = 349.99m,
                Stock = 100,
                Status = ProductStatus.Active,
                Images = new List<CreateProductImageDto>
                {
                    new CreateProductImageDto { ImageUrl = "https://www.senetsepet.com/idea/jz/77/myassets/products/813/202307168-1.jpg?revision=1697143329", AltText = "Decorative Modern Table Lamp", IsPrimary = true, DisplayOrder = 0 }
                }
            }
        };

        return await CreateProductsBulkAsync(mockProducts, userId);
    }
}
