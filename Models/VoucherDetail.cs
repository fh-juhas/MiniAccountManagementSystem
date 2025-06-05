using System.ComponentModel.DataAnnotations;

namespace MiniAccountManagementSystem.Models
{
    public class VoucherDetail
    {
        public int VoucherDetailId { get; set; }
        public int VoucherId { get; set; }
        public int AccountId { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal DebitAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal CreditAmount { get; set; }

        // For UI display
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
    }
}
