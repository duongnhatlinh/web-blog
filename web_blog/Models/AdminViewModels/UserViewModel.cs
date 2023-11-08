using PagedList.Core;
using web_blog.Data;

namespace web_blog.Models.AdminViewModels
{
	public class UserViewModel
	{
		public IPagedList<ApplicationUser> Users { get; set; }
		public int PageNumber { get; set; }

		public string SearchString { get; set; }
	}
}
