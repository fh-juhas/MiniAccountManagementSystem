using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MiniAccountManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniAccountManagementSystem.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class ManageRolesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public ManageRolesModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public IList<IdentityUser> Users { get; set; }
        public IList<IdentityRole> Roles { get; set; }
        public IList<UserModulePermission> Permissions { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users.ToListAsync();
            Roles = await _roleManager.Roles.ToListAsync();
            Permissions = new List<UserModulePermission>();

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT * FROM UserModulePermissions", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Permissions.Add(new UserModulePermission
                                {
                                    PermissionId = (int)reader["PermissionId"],
                                    RoleName = reader["RoleName"].ToString(),
                                    ModuleName = reader["ModuleName"].ToString(),
                                    CanView = (bool)reader["CanView"],
                                    CanCreate = (bool)reader["CanCreate"],
                                    CanEdit = (bool)reader["CanEdit"],
                                    CanDelete = (bool)reader["CanDelete"],
                                    CreatedBy = reader["CreatedBy"].ToString(),
                                    CreatedDate = (DateTime)reader["CreatedDate"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Error loading permissions: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string role)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                {
                    return BadRequest("User ID and Role are required");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID '{userId}' not found.");
                }

                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    var result = await _userManager.AddToRoleAsync(user, role);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error assigning role: {ex.Message}");
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdatePermissionAsync(int permissionId, bool canView = false, bool canCreate = false, bool canEdit = false, bool canDelete = false)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("UPDATE UserModulePermissions SET CanView = @CanView, CanCreate = @CanCreate, CanEdit = @CanEdit, CanDelete = @CanDelete WHERE PermissionId = @PermissionId", connection))
                    {
                        command.Parameters.AddWithValue("@PermissionId", permissionId);
                        command.Parameters.AddWithValue("@CanView", canView);
                        command.Parameters.AddWithValue("@CanCreate", canCreate);
                        command.Parameters.AddWithValue("@CanEdit", canEdit);
                        command.Parameters.AddWithValue("@CanDelete", canDelete);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating permission: {ex.Message}");
                return Page();
            }

            return RedirectToPage();
        }
    }
}