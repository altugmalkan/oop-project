using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockECommerce.DAL.Data;
using MockECommerce.DAL.Entities;
using MockECommerce.DAL.Enums;

namespace MockECommerce.WebAPI.Extensions;

public static class DatabaseSeedExtensions
{
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager, logger);

            // Seed admin user
            await SeedAdminUserAsync(userManager, logger);

            // Seed categories
            await SeedCategoriesAsync(context, logger);

            // Seed demo seller and products
            await SeedDemoSellerAndProductsAsync(context, userManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new AppRole { Name = "Admin", Description = "System Administrator" },
            new AppRole { Name = "Seller", Description = "Product Seller" },
            new AppRole { Name = "Customer", Description = "Regular Customer" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully", role.Name);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}': {Errors}", 
                        role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role '{RoleName}' already exists", role.Name);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            
            if (result.Succeeded)
            {
                // Add admin role
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully with email: {Email}", adminEmail);
                    logger.LogInformation("Default admin password is: {Password}", adminPassword);
                    logger.LogWarning("IMPORTANT: Change the default admin password after first login!");
                }
                else
                {
                    logger.LogError("Failed to add Admin role to user: {Errors}", 
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists: {Email}", adminEmail);

            // Ensure admin has Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Added Admin role to existing user: {Email}", adminEmail);
                }
            }
        }
    }

    private static async Task SeedCategoriesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            logger.LogInformation("Categories already exist, skipping seed");
            return;
        }

        var categories = new List<Category>
        {
            new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = "Electronics",
                SeoSlug = "electronics",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = "Fashion",
                SeoSlug = "fashion",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = "Home & Living",
                SeoSlug = "home-living",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = "Sports & Outdoors",
                SeoSlug = "sports-outdoors",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.NewGuid(),
                CategoryName = "Books",
                SeoSlug = "books",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} categories", categories.Count);
    }

    private static async Task SeedDemoSellerAndProductsAsync(AppDbContext context, UserManager<AppUser> userManager, ILogger logger)
    {
        if (await context.Products.AnyAsync())
        {
            logger.LogInformation("Products already exist, skipping seed");
            return;
        }

        // Create demo seller user
        const string sellerEmail = "seller@demo.com";
        const string sellerPassword = "Seller123!";

        var sellerUser = await userManager.FindByEmailAsync(sellerEmail);
        if (sellerUser == null)
        {
            sellerUser = new AppUser
            {
                UserName = sellerEmail,
                Email = sellerEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "Seller",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(sellerUser, sellerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sellerUser, "Seller");
                logger.LogInformation("Demo seller user created: {Email}", sellerEmail);
            }
            else
            {
                logger.LogError("Failed to create demo seller: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Create seller profile
        var sellerProfile = await context.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == sellerUser.Id);
        if (sellerProfile == null)
        {
            sellerProfile = new SellerProfile
            {
                Id = Guid.NewGuid(),
                UserId = sellerUser.Id,
                StoreName = "Demo Store",
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.SellerProfiles.AddAsync(sellerProfile);
            await context.SaveChangesAsync();
            logger.LogInformation("Demo seller profile created: {StoreName}", sellerProfile.StoreName);
        }

        // Get categories
        var electronics = await context.Categories.FirstOrDefaultAsync(c => c.SeoSlug == "electronics");
        var fashion = await context.Categories.FirstOrDefaultAsync(c => c.SeoSlug == "fashion");
        var homeLiving = await context.Categories.FirstOrDefaultAsync(c => c.SeoSlug == "home-living");
        var sports = await context.Categories.FirstOrDefaultAsync(c => c.SeoSlug == "sports-outdoors");

        if (electronics == null || fashion == null || homeLiving == null || sports == null)
        {
            logger.LogError("Required categories not found for product seeding");
            return;
        }

        // Product data with images
        var products = new List<(string Title, string Description, decimal Price, int Stock, Guid CategoryId, string ImageUrl)>
        {
            ("Elegant Winter Coat", "Premium quality winter coat with warm lining, perfect for cold weather.", 1299.99m, 50, fashion.Id,
                "https://cdn.dsmcdn.com/mnresize/400/-/ty1608/prod/QC/20241130/18/e496f645-c425-3155-887b-de8e605f275b/1_org_zoom.jpg"),

            ("Decorative Table Lamp", "Modern decorative lamp for your living room or bedroom.", 349.99m, 100, homeLiving.Id,
                "https://www.senetsepet.com/idea/jz/77/myassets/products/813/202307168-1.jpg?revision=1697143329"),

            ("Wireless Earbuds Pro", "High-quality wireless earbuds with noise cancellation.", 799.99m, 200, electronics.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTFsqb0zE7p47ozmO20_VFc-lZgGXp_nvSvjA&s"),

            ("MacBook Pro 14\"", "Apple MacBook Pro with M3 chip, 16GB RAM, 512GB SSD.", 54999.99m, 25, electronics.Id,
                "https://sm.pcmag.com/pcmag_me/photo/default/macbook-6_hgfm.jpg"),

            ("Smart Watch Series X", "Advanced smartwatch with health monitoring and GPS.", 2499.99m, 75, electronics.Id,
                "https://cdn.dsmcdn.com/mnresize/400/-/ty1720/prod/QC_PREP/20250801/16/4e39c3ff-0d8a-3351-aae1-59360d32b641/1_org_zoom.jpg"),

            ("Designer Handbag", "Luxury designer handbag made from genuine leather.", 1899.99m, 30, fashion.Id,
                "https://cdn.dsmcdn.com/mnresize/420/620/ty1545/product/media/images/ty1547/prod/QC/20240915/14/a68d2478-6386-31fb-a6a9-b1990983bc68/1_org_zoom.jpg"),

            ("Running Shoes Pro", "Professional running shoes with advanced cushioning.", 899.99m, 150, sports.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTefdCtkqKRXa3T61sVRfTlKPLKuVRKKVLXPQ&s"),

            ("Bluetooth Speaker", "Portable Bluetooth speaker with 360-degree sound.", 599.99m, 120, electronics.Id,
                "https://cdn.dsmcdn.com/mnresize/400/-/ty1757/prod/QC_PREP/20250917/10/f8610118-33a4-36e4-9c85-9029d6f3037c/1_org_zoom.jpg"),

            ("Yoga Mat Premium", "Extra thick yoga mat with carrying strap.", 249.99m, 200, sports.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT2ZqUxiO3wzEHhKQsPfRtsFsFpiPd7tuuA6w&s"),

            ("Coffee Maker Deluxe", "Automatic coffee maker with built-in grinder.", 1499.99m, 60, homeLiving.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSyPa3kusgD_ZevIiQ_0d5OtJ8Owr-pREhCUw&s"),

            ("Sunglasses Classic", "Classic style sunglasses with UV protection.", 449.99m, 180, fashion.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTRvQirv3AOaD7VI9zOb3ru6DtLLzw6RDQToA&s"),

            ("Fitness Tracker Band", "Lightweight fitness tracker with heart rate monitor.", 399.99m, 250, sports.Id,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR7ly0ttQR7TQhdrReBbeHxN0TW-pXBSqpZDA&s")
        };

        foreach (var (title, description, price, stock, categoryId, imageUrl) in products)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                SellerId = sellerProfile.Id,
                CategoryId = categoryId,
                Title = title,
                Description = description,
                Price = price,
                Stock = stock,
                Status = ProductStatus.Active,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            var productImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = imageUrl,
                AltText = title,
                IsPrimary = true,
                DisplayOrder = 0
            };

            await context.ProductImages.AddAsync(productImage);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} products with images", products.Count);
    }
}
