using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TechXpress.Data.Models;
using TechXpress.Data.Models.Contexts;
using TechXpress.Services.GenericServices; // Import your generic service interface
using TechXpress.Services.ShoppingCartsService; // Import your shopping cart service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechXpress.Web.Areas.Admin.Models;

namespace TechXpress.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomerManagementController : Controller
    {
        private readonly IGenericService<ApplicationUser> _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly TechXpressDbContext _context;

        // Constructor injection of the services
        public CustomerManagementController(IGenericService<ApplicationUser> userService,
            UserManager<ApplicationUser> userManager,
            IShoppingCartService shoppingCartService,
            TechXpressDbContext context)
        {
            _userService = userService;
            _shoppingCartService = shoppingCartService;
            _userManager = userManager;
            _context = context;
        }

        // GET: Admin/CustomerManagement/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync(); // Assuming you have a generic service to get all users
            return View(users); // View will display the list of customers
        }

        // GET: Admin/CustomerManagement/Create
        public IActionResult Create()
        {
            return View(new CustomerCreateViewModel());
        }

        // POST: Admin/CustomerManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                PostalCode = model.PostalCode,
                Country = model.Country,
                AccountCreated = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                if (roleResult.Succeeded)
                {
                    TempData["SuccessMessage"] = "Customer created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Admin/CustomerManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/CustomerManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser user)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.GetByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                // Update user properties
                var existingEmail = existingUser.Email;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                if (string.Equals(existingUser.UserName, existingEmail, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser.UserName = user.Email;
                }
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.AddressLine1 = user.AddressLine1;
                existingUser.AddressLine2 = user.AddressLine2;
                existingUser.City = user.City;
                existingUser.State = user.State;
                existingUser.PostalCode = user.PostalCode;
                existingUser.Country = user.Country;

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (updateResult.Succeeded)
                {
                    TempData["SuccessMessage"] = "Customer updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(user);
            }
            return View(user);
        }

        // GET: Admin/CustomerManagement/Details/{userId}
        public async Task<IActionResult> Details(string Id)
        {
            var user = await _userService.GetByIdAsync(Id);

            if (user == null)
            {
                return NotFound();
            }
            return View(user); // Display detailed information about the user
        }

        // GET: Admin/CustomerManagement/Cart/{userId}
        public async Task<IActionResult> Cart(string Id)
        {
            var cart = await _shoppingCartService.GetCartByUserIdAsync(Id);
            if (cart == null)
            {
                return NotFound();
            }
            return View(cart); // Display the user's shopping cart
        }

        // GET: Admin/CustomerManagement/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/CustomerManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete the account you are currently using.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var hasOrderHistory = await _context.Orders.AnyAsync(order => order.UserId == id)
                || await _context.Payments.AnyAsync(payment => payment.UserId == id)
                || await _context.Reviews.AnyAsync(review => review.UserId == id);
            if (hasOrderHistory)
            {
                TempData["ErrorMessage"] = "This account cannot be deleted because it has order, payment, or review history.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var cart = await _context.ShoppingCarts.SingleOrDefaultAsync(shoppingCart => shoppingCart.UserId == id);
            if (cart != null)
            {
                _context.ShoppingCarts.Remove(cart);
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }

            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/CustomerManagement/Manage/{userId}
        public async Task<IActionResult> Manage(string Id)
        {
            var user = await _userService.GetByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user); // Show user management options, such as updating details
        }

    }
}
