using System.ComponentModel.DataAnnotations;

namespace EClaim.Application.Models.ViewModel
{
    public class ClaimSubmissionViewModel
    {
        public int UserId { get; set; }

        [Required]
        public string ClaimType { get; set; }

        [Required]
        [StringLength(500)]
        [NoHtml(ErrorMessage = "HTML tags are not allowed.")]
        public string Description { get; set; }

        [Required]
        public List<IFormFile> Documents { get; set; } = new();
    }
}
