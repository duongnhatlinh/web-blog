using PagedList.Core;
using web_blog.Data;

namespace web_blog.Models.AdminViewModels
{
    public class IndexViewModel
    {
        public IPagedList<Post> Posts { get; set; }
        public int PageNumber { get; set; }

        public string SearchString { get; set; }
    }
}
