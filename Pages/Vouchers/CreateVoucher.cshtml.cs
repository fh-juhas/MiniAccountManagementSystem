using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MiniAccountManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MiniAccountManagementSystem.Pages.Vouchers
{
    [Authorize(Policy = "AccountManagement")]
    public class CreateVoucherModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateVoucherModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public Voucher Voucher { get; set; } = new Voucher();

        public IList<Account> Accounts { get; set; } = new List<Account>();

        public async Task OnGetAsync()
        {
            await InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            // Initialize Voucher if null
            if (Voucher == null)
            {
                Voucher = new Voucher();
            }

            // Set default values
            Voucher.VoucherDate = DateTime.Today;
            Voucher.Status = "Draft";

            // Initialize VoucherDetails if null
            if (Voucher.VoucherDetails == null)
            {
                Voucher.VoucherDetails = new List<VoucherDetail>();
            }

            // Add at least one empty detail for the form if none exist
            if (Voucher.VoucherDetails.Count == 0)
            {
                Voucher.VoucherDetails.Add(new VoucherDetail());
            }

            // Load accounts
            await LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            Accounts = new List<Account>();

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT AccountId, AccountCode, AccountName FROM Accounts WHERE IsActive = 1 ORDER BY AccountCode", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Accounts.Add(new Account
                                {
                                    AccountId = (int)reader["AccountId"],
                                    AccountCode = reader["AccountCode"]?.ToString() ?? "",
                                    AccountName = reader["AccountName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging
                ModelState.AddModelError("", $"Error loading accounts: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Ensure Voucher and VoucherDetails are not null
                if (Voucher == null)
                {
                    ModelState.AddModelError("", "Voucher data is required.");
                    await InitializePageAsync();
                    return Page();
                }

                if (Voucher.VoucherDetails == null)
                {
                    Voucher.VoucherDetails = new List<VoucherDetail>();
                }

                // Remove empty details (where AccountId is 0 or not selected)
                Voucher.VoucherDetails = Voucher.VoucherDetails
                    .Where(d => d != null && d.AccountId > 0)
                    .ToList();

                // Validate that at least one detail exists
                if (Voucher.VoucherDetails.Count == 0)
                {
                    ModelState.AddModelError("", "At least one voucher detail is required.");
                    await InitializePageAsync();
                    return Page();
                }

                // Validate debit/credit amounts
                foreach (var detail in Voucher.VoucherDetails)
                {
                    if ((detail.DebitAmount ?? 0) == 0 && (detail.CreditAmount ?? 0) == 0)
                    {
                        ModelState.AddModelError("", "Each detail must have either a debit or credit amount.");
                        await InitializePageAsync();
                        return Page();
                    }

                    if ((detail.DebitAmount ?? 0) > 0 && (detail.CreditAmount ?? 0) > 0)
                    {
                        ModelState.AddModelError("", "A detail cannot have both debit and credit amounts.");
                        await InitializePageAsync();
                        return Page();
                    }
                }

                //// Check if debits equal credits
                //var totalDebit = Voucher.VoucherDetails.Sum(d => d.DebitAmount ?? 0);
                //var totalCredit = Voucher.VoucherDetails.Sum(d => d.CreditAmount ?? 0);

                //if (totalDebit != totalCredit)
                //{
                //    ModelState.AddModelError("", $"Total debits ({totalDebit:C}) must equal total credits ({totalCredit:C}).");
                //    await InitializePageAsync();
                //    return Page();
                //}

                if (!ModelState.IsValid)
                {
                    await InitializePageAsync();
                    return Page();
                }

                // Load accounts to populate account info
                await LoadAccountsAsync();

                // Populate AccountCode and AccountName for display (not saved to DB)
                foreach (var detail in Voucher.VoucherDetails)
                {
                    var account = Accounts.FirstOrDefault(a => a.AccountId == detail.AccountId);
                    if (account != null)
                    {
                        detail.AccountCode = account.AccountCode;
                        detail.AccountName = account.AccountName;
                    }
                }

                // Save to database
                await SaveVoucherAsync();

                TempData["Success"] = "Voucher created successfully!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while saving the voucher: {ex.Message}");
                await InitializePageAsync();
                return Page();
            }
        }

        private async Task SaveVoucherAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_SaveVoucher", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters with proper null handling
                    command.Parameters.AddWithValue("@VoucherId",
                        Voucher.VoucherId == 0 ? DBNull.Value : Voucher.VoucherId);
                    command.Parameters.AddWithValue("@VoucherNo",
                        string.IsNullOrEmpty(Voucher.VoucherNo) ? DBNull.Value : Voucher.VoucherNo);
                    command.Parameters.AddWithValue("@VoucherType",
                        string.IsNullOrEmpty(Voucher.VoucherType) ? DBNull.Value : Voucher.VoucherType);
                    command.Parameters.AddWithValue("@VoucherDate", Voucher.VoucherDate);
                    command.Parameters.AddWithValue("@Reference",
                        string.IsNullOrEmpty(Voucher.Reference) ? DBNull.Value : Voucher.Reference);
                    command.Parameters.AddWithValue("@Description",
                        string.IsNullOrEmpty(Voucher.Description) ? DBNull.Value : Voucher.Description);
                    command.Parameters.AddWithValue("@Status",
                        string.IsNullOrEmpty(Voucher.Status) ? "Draft" : Voucher.Status);
                    command.Parameters.AddWithValue("@UserName",
                        User.Identity?.Name ?? "Unknown");

                    // Create DataTable for voucher details
                    var entriesTable = new DataTable();
                    entriesTable.Columns.Add("AccountId", typeof(int));
                    entriesTable.Columns.Add("Description", typeof(string));
                    entriesTable.Columns.Add("DebitAmount", typeof(decimal));
                    entriesTable.Columns.Add("CreditAmount", typeof(decimal));

                    foreach (var detail in Voucher.VoucherDetails)
                    {
                        entriesTable.Rows.Add(
                            detail.AccountId,
                            string.IsNullOrEmpty(detail.Description) ? DBNull.Value : detail.Description,
                            detail.DebitAmount ?? 0,
                            detail.CreditAmount ?? 0
                        );
                    }

                    var parameter = command.Parameters.AddWithValue("@VoucherDetails", entriesTable);
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "dbo.VoucherDetailType";

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}