using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MiniAccountManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniAccountManagementSystem.Pages.Accounts
{
    [Authorize(Policy = "AccountManagement")]
    public class ChartOfAccountsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ChartOfAccountsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public Account NewAccount { get; set; }
        public IList<Account> Accounts { get; set; }

        public async Task OnGetAsync()
        {
            NewAccount = new Account { IsActive = true };
            Accounts = new List<Account>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_GetAccountHierarchy", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Accounts.Add(new Account
                            {
                                AccountId = (int)reader["AccountId"],
                                AccountCode = reader["AccountCode"].ToString(),
                                AccountName = reader["AccountName"].ToString(),
                                ParentAccountId = reader["ParentAccountId"] != DBNull.Value ? (int?)reader["ParentAccountId"] : null,
                                AccountType = reader["AccountType"].ToString(),
                                IsActive = (bool)reader["IsActive"]
                            });
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_ManageChartOfAccounts", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Action", "Create");
                    command.Parameters.AddWithValue("@AccountCode", NewAccount.AccountCode);
                    command.Parameters.AddWithValue("@AccountName", NewAccount.AccountName);
                    command.Parameters.AddWithValue("@ParentAccountId", NewAccount.ParentAccountId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AccountType", NewAccount.AccountType);
                    command.Parameters.AddWithValue("@IsActive", NewAccount.IsActive);
                    command.Parameters.AddWithValue("@UserName", User.Identity.Name);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int accountId, string accountCode, string accountName, int? parentAccountId, string accountType, bool isActive)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_ManageChartOfAccounts", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Action", "Update");
                    command.Parameters.AddWithValue("@AccountId", accountId);
                    command.Parameters.AddWithValue("@AccountCode", accountCode);
                    command.Parameters.AddWithValue("@AccountName", accountName);
                    command.Parameters.AddWithValue("@ParentAccountId", parentAccountId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AccountType", accountType);
                    command.Parameters.AddWithValue("@IsActive", isActive);
                    command.Parameters.AddWithValue("@UserName", User.Identity.Name);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int accountId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_ManageChartOfAccounts", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Action", "Delete");
                    command.Parameters.AddWithValue("@AccountId", accountId);
                    command.Parameters.AddWithValue("@UserName", User.Identity.Name);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToPage();
        }
    }
}