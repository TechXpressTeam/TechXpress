using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechXpress.Data.Models;
using TechXpress.Data.Models.Contexts;

namespace TechXpress.Web.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TechXpressDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

            await context.Database.MigrateAsync();
            await SeedRolesAndAdminAsync(roleManager, userManager, configuration, logger);

            if (await context.Products.AnyAsync())
            {
                return;
            }

            var categories = new[]
            {
                new Category { Name = "Laptops", Description = "Laptop computers", Slug = "laptops", ImageUrl = "/images/laptops/1.jpg", DisplayOrder = 1, IsActive = true, MetaTitle = "Laptops", MetaDescription = "Browse laptops", MetaKeywords = "laptops, computers", Level = 0 },
                new Category { Name = "Mobiles", Description = "Mobile phones", Slug = "mobiles", ImageUrl = "/images/iph.jpeg", DisplayOrder = 2, IsActive = true, MetaTitle = "Mobiles", MetaDescription = "Browse mobile phones", MetaKeywords = "mobiles, phones", Level = 0 },
                new Category { Name = "Televisions", Description = "Smart televisions", Slug = "televisions", ImageUrl = "/images/tvs/1.jpg", DisplayOrder = 3, IsActive = true, MetaTitle = "Televisions", MetaDescription = "Browse televisions", MetaKeywords = "tv, television", Level = 0 },
                new Category { Name = "Keyboards", Description = "Computer keyboards", Slug = "keyboards", ImageUrl = "/images/keyboards/kb627-2.jpg", DisplayOrder = 4, IsActive = true, MetaTitle = "Keyboards", MetaDescription = "Browse keyboards", MetaKeywords = "keyboard, gaming", Level = 0 },
                new Category { Name = "Mice", Description = "Computer mice", Slug = "mice", ImageUrl = "/images/mices/mo708-2-min.jpg", DisplayOrder = 5, IsActive = true, MetaTitle = "Mice", MetaDescription = "Browse computer mice", MetaKeywords = "mouse, gaming", Level = 0 }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var laptop = categories[0];
            var mobile = categories[1];
            var tv = categories[2];
            var keyboard = categories[3];
            var mouse = categories[4];

            context.Products.AddRange(
                new ProductViewModel { Name = "Professional Laptop", Description = "Powerful laptop for work and study", Price = 899.99m, StockQuantity = 15, IsFeatured = true, IsActive = true, SKU = "LAP-001", ImageUrl = "/images/laptops/1.jpg", CategoryId = laptop.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Gaming Laptop", Description = "High-performance laptop for gaming", Price = 1499.99m, StockQuantity = 8, IsFeatured = true, IsActive = true, SKU = "LAP-002", ImageUrl = "/images/laptops/2.jpg", CategoryId = laptop.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Smartphone Pro", Description = "Premium smartphone with advanced camera", Price = 699.99m, StockQuantity = 25, IsFeatured = true, IsActive = true, SKU = "MOB-001", ImageUrl = "/images/iph.jpeg", CategoryId = mobile.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Smartphone Lite", Description = "Affordable everyday smartphone", Price = 299.99m, StockQuantity = 30, IsFeatured = false, IsActive = true, SKU = "MOB-002", ImageUrl = "/images/OIP.jpeg", CategoryId = mobile.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "4K Smart TV", Description = "55-inch 4K smart television", Price = 799.99m, StockQuantity = 12, IsFeatured = true, IsActive = true, SKU = "TV-001", ImageUrl = "/images/tvs/1.jpg", CategoryId = tv.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Full HD TV", Description = "43-inch Full HD television", Price = 449.99m, StockQuantity = 18, IsFeatured = false, IsActive = true, SKU = "TV-002", ImageUrl = "/images/tvs/2.jpg", CategoryId = tv.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Mechanical Keyboard", Description = "RGB mechanical gaming keyboard", Price = 129.99m, StockQuantity = 40, IsFeatured = true, IsActive = true, SKU = "KEY-001", ImageUrl = "/images/keyboards/kb627-2.jpg", CategoryId = keyboard.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Wireless Keyboard", Description = "Compact wireless keyboard", Price = 59.99m, StockQuantity = 35, IsFeatured = false, IsActive = true, SKU = "KEY-002", ImageUrl = "/images/keyboards/kb626-9-min.jpg", CategoryId = keyboard.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Gaming Mouse", Description = "Precision gaming mouse with RGB lighting", Price = 49.99m, StockQuantity = 50, IsFeatured = true, IsActive = true, SKU = "MOU-001", ImageUrl = "/images/mices/mo708-2-min.jpg", CategoryId = mouse.Id, CreatedDate = DateTime.UtcNow },
                new ProductViewModel { Name = "Wireless Mouse", Description = "Silent wireless mouse", Price = 29.99m, StockQuantity = 60, IsFeatured = false, IsActive = true, SKU = "MOU-002", ImageUrl = "/images/mices/mo698-2-min.jpg", CategoryId = mouse.Id, CreatedDate = DateTime.UtcNow }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAndAdminAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            foreach (var roleName in new[] { "Admin", "Customer", "SuperAdmin" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminEmail = configuration["AdminSetup:Email"];
            var adminPassword = configuration["AdminSetup:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Admin account was not created because AdminSetup:Email or AdminSetup:Password is missing.");
                return;
            }

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "TechXpress",
                    LastName = "Administrator"
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                {
                    logger.LogError("Admin account creation failed: {Errors}",
                        string.Join("; ", result.Errors.Select(error => error.Description)));
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
