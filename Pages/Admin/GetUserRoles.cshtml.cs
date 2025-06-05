using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace MiniAccountManagementSystem.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class GetUserRolesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public GetUserRolesModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            var roles = await _userManager.GetRolesAsync(user);
            return new JsonResult(roles);
        }
    }
}