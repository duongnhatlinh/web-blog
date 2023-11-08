using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PagedList.Core;
using web_blog.Data;
using web_blog.Models.PostViewModels;

namespace web_blog.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;


        public AdminController(ApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            this.applicationDbContext = applicationDbContext;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index(string? SearchString, int? page)
        {
            int pageSize = 4;
            int pageNumber = page ?? 1;
            var str = SearchString ?? string.Empty;

            var posts = applicationDbContext.Posts
               .Include(post => post.Creator)
               .OrderByDescending(post => post.UpdatedOn)
               .Where(post => post.Title.Contains(str) || post.Content.Contains(str));

            // number of page
            double numberOfPages = posts.Count() / (double)pageSize;
            int roundNumberOfPages = (int)Math.Ceiling(numberOfPages);
            ViewBag.NumberOfPages = roundNumberOfPages;

            Models.AdminViewModels.IndexViewModel ivm = new Models.AdminViewModels.IndexViewModel
            {
                Posts = new StaticPagedList<Post>(posts.Skip((pageNumber - 1) * pageSize).Take(pageSize), pageNumber, pageSize, posts.Count()),
                SearchString = str,
                PageNumber = pageNumber,
            };
            return View(ivm);
        }

        public void FilterPost(Filter filter, ref IQueryable<Post> posts)
        {
            // filter category
            if (filter.categoryId != null)
            {
                posts = posts.Where(post => post.Category.Id == filter.categoryId);
            }

            // filter author
            if (filter.authorId != null)
            {
                posts = posts.Where(post => post.Creator.Id == filter.authorId);
            }

            // filter publish
            if (filter.publish != null)
            {
                if (filter.publish == "published")
                {
                    posts = posts.Where(post => post.Published == true);
                }
                else
                {
                    posts = posts.Where(post => post.Published == false);
                }
            }

            // filter sort order, order by
            if (filter.sortOrder == "desc")
            {
                if (filter.orderBy == "date")
                {
                    posts = posts.OrderByDescending(post => post.UpdatedOn);
                }
                else
                {
                    posts = posts.OrderByDescending(post => post.Title);
                }
            }

            if (filter.sortOrder == "asc")
            {
                if (filter.orderBy == "date")
                {
                    posts = posts.OrderBy(post => post.UpdatedOn);
                }
                else
                {
                    posts = posts.OrderBy(post => post.Title);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Approved(int? postId, bool? check)
        {
            if (postId is null || check is null)
                return new NotFoundResult();

            var post = applicationDbContext.Posts
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            if (check.Value == false)
            {
                post.Approved = true;
            }
            else
            {
                post.Approved = false;
            }

            applicationDbContext.Update(post);
            await applicationDbContext.SaveChangesAsync();

            return Json(new { status = true });
        }

        public IActionResult GetPost(int? id)
        {
            if (id is null)
                return new BadRequestResult();

            var postId = id.Value;

            var post = applicationDbContext.Posts
                .Include(post => post.Creator)
                .Include(post => post.Comments)
                    .ThenInclude(comment => comment.Author)
                .Include(post => post.Comments)
                        .ThenInclude(comment => comment.Comments)
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            PostViewModel pvm = new PostViewModel
            {
                Post = post
            };
            return View(pvm);
        }


        public IActionResult DashboardUser(string? SearchString, int? page)
        {
			int pageSize = 2;
			int pageNumber = page ?? 1;
			var str = SearchString ?? string.Empty;


			var users = applicationDbContext.Users
			   .Where(user => user.FirstName.Contains(str) || user.LastName.Contains(str));

			double numberOfPages = users.Count() / (double)pageSize;
			int roundNumberOfPages = (int)Math.Ceiling(numberOfPages);
			ViewBag.NumberOfPages = roundNumberOfPages;

			Models.AdminViewModels.UserViewModel uvm = new Models.AdminViewModels.UserViewModel
			{
				Users = new StaticPagedList<ApplicationUser>(users.Skip((pageNumber - 1) * pageSize).Take(pageSize), pageNumber, pageSize, users.Count()),
				SearchString = str,
				PageNumber = pageNumber,
			};
			return View(uvm);
		}
    }
}
