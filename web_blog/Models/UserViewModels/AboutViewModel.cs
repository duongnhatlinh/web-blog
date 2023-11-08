using System.ComponentModel.DataAnnotations;
using web_blog.Data;

namespace web_blog.Models.UserViewModels
{
	public class AboutViewModel
	{
		[Required, Display(Name = "Profile Image")]
		public IFormFile ProfileImage { get; set; }
		public ApplicationUser User { get; set; }
	}
}
