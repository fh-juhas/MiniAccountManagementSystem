using System.ComponentModel.DataAnnotations;

namespace MiniAccountManagementSystem.Models
{
    public class Voucher
    {
        public int VoucherId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Voucher No")]
        public string VoucherNo { get; set; }

        [Required]
        [Display(Name = "Voucher Type")]
        public string VoucherType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime VoucherDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string Reference { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Display(Name = "Total Amount")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Draft";

        public List<VoucherDetail> VoucherDetails { get; set; } = new List<VoucherDetail>();
    }
}
