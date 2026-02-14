using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public IList<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserViewModel
        {
            public string Id { get; set; } = null!;
            public string UserName { get; set; } = null!;
            public string Email { get; set; } = null!;
        }

        public async Task OnGetAsync()
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(u => u.UserName!.Contains(term) || (u.Email != null && u.Email.Contains(term)));
            }

            var list = new List<UserViewModel>();
            await foreach (var u in query.AsAsyncEnumerable())
            {
                list.Add(new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty
                });
            }

            Users = list;
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                StatusMessage = "User not found.";
                return RedirectToPage();
            }

            // Prevent admin from accidentally deleting themselves
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                StatusMessage = "You cannot delete your own account.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error deleting user.";
                return RedirectToPage();
            }

            StatusMessage = "User deleted.";
            return RedirectToPage();
        }
    }
}