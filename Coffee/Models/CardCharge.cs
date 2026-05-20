using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coffee.Models
{
    public class CardCharge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [StringLength(20)]
        public string Telco { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(50)]
        public string Serial { get; set; }

        [Required]
        public decimal DeclaredValue { get; set; }

        public decimal? RealValue { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestId { get; set; } // Mã GD hệ thống mình sinh ra

        public string TransId { get; set; } // Mã GD bên TSR trả về

        // 99: Đang chờ, 1: Thành công, 2: Sai mệnh giá, 3: Thẻ lỗi, 4: Bảo trì, 100: Thất bại gửi
        public int Status { get; set; } = 99; 

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
