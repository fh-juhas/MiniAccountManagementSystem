using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MiniAccountManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
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
        public Voucher Voucher { get; set; }
        public IList<Account> Accounts { get; set; }

        public async Task OnGetAsync()
        {
            Voucher = new Voucher { VoucherDate = DateTime.Today, Status = "Draft" };
            Accounts = new List<Account>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT AccountId, AccountCode, AccountName FROM Accounts WHERE IsActive = 1", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Accounts.Add(new Account
                            {
                                AccountId = (int)reader["AccountId"],
                                AccountCode = reader["AccountCode"].ToString(),
                                AccountName = reader["AccountName"].ToString()
                            });
                        }
                    }
                }
            }
            // Initialize at least one detail for the form
            Voucher.VoucherDetails.Add(new VoucherDetail());
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

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

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.sp_SaveVoucher", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@VoucherId", Voucher.VoucherId == 0 ? (object)DBNull.Value : Voucher.VoucherId);
                    command.Parameters.AddWithValue("@VoucherNo", Voucher.VoucherNo);
                    command.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType);
                    command.Parameters.AddWithValue("@VoucherDate", Voucher.VoucherDate);
                    command.Parameters.AddWithValue("@Reference", Voucher.Reference ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Description", Voucher.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Status", Voucher.Status);
                    command.Parameters.AddWithValue("@UserName", User.Identity.Name);

                    var entriesTable = new DataTable();
                    entriesTable.Columns.Add("AccountId", typeof(int));
                    entriesTable.Columns.Add("Description", typeof(string));
                    entriesTable.Columns.Add("DebitAmount", typeof(decimal));
                    entriesTable.Columns.Add("CreditAmount", typeof(decimal));

                    foreach (var detail in Voucher.VoucherDetails)
                    {
                        entriesTable.Rows.Add(detail.AccountId, detail.Description, detail.DebitAmount, detail.CreditAmount);
                    }

                    var parameter = command.Parameters.AddWithValue("@VoucherDetails", entriesTable);
                    parameter.SqlDbType = System.Data.SqlDbType.Structured;
                    parameter.TypeName = "dbo.VoucherDetailType";

                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToPage();
        }
    }
}