using System.ComponentModel.DataAnnotations;

namespace MiniAccountManagementSystem.Models
{
    public class Account
    {
        public int AccountId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Account Code")]
        public string AccountCode { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; }

        [Display(Name = "Parent Account")]
        public int? ParentAccountId { get; set; }

        [Display(Name = "Parent Account")]
        public string ParentAccountName { get; set; }

        [Display(Name = "Account Type")]
        public string AccountType { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<Account> ChildAccounts { get; set; } = new List<Account>();
    }
}
