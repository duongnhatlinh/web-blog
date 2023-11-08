using Microsoft.AspNetCore.Identity;

namespace web_blog.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Job { set; get; }
        public string? Education { set; get; }
		public string? Location { set; get; }
		public string? Skills { set; get; }
		public string? Notes { set; get; }
	}
}
