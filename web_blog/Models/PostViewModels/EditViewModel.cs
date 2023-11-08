using System.ComponentModel.DataAnnotations;
using web_blog.Data;

namespace web_blog.Models.PostViewModels
{
    public class EditViewModel
    {
        [Display(Name = "Header Image")]
        public IFormFile HeaderImage { get; set; }
        public Post Post { get; set; }
    }
}
