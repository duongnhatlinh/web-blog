using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;
using System.Diagnostics;
using web_blog.Data;
using web_blog.Models;
using web_blog.Models.PostViewModels;
using web_blog.Models.HomeViewModels;

namespace web_blog.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; 
        private readonly ApplicationDbContext applicationDbContext;
        private readonly UserManager<ApplicationUser> userManager;


        public HomeController(ILogger<HomeController> logger,
            ApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            this.applicationDbContext = applicationDbContext;
            this.userManager = userManager;
        }


        public IActionResult Index(string? SearchString, int? page, int? categoryId, string? authorId, string? sortOrder, string? orderBy)
        {
            int pageSize = 6;
            int pageNumber = page ?? 1;
            var str = SearchString ?? string.Empty;


            var posts = applicationDbContext.Posts
               .Include(post => post.Creator)
               .Include(post => post.Category)
               .Include(post => post.Comments)
               .OrderByDescending(post => post.UpdatedOn)
               .Where(post => post.Title.Contains(str) || post.Content.Contains(str));

            // filter
            Filter filter = new Filter();

            filter.categoryId = categoryId ?? null;
            filter.authorId = authorId ?? null;
            filter.sortOrder = sortOrder ?? null;
            filter.orderBy = orderBy ?? null;

            FilterPost(filter, ref posts);
            ViewBag.Filter = filter;
            // number of page
            double numberOfPages = posts.Count() / (double)pageSize;
            int roundNumberOfPages = (int)Math.Ceiling(numberOfPages);
            ViewBag.NumberOfPages = roundNumberOfPages;

            IndexViewModel ivm = new IndexViewModel
            {
                Posts = new StaticPagedList<Post>(posts.Skip((pageNumber - 1) * pageSize).Take(pageSize), pageNumber, pageSize, posts.Count()),
                SearchString = str,
                PageNumber = pageNumber,
            };
            return PartialView("_IndexPartial", ivm);
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
        public IActionResult GetPost(int? id)
        {
            if (id is null)
                return new BadRequestResult();

            var postId = id.Value;

            var post = applicationDbContext.Posts
                .Include(post => post.Creator)
                .Include(post => post.Category)
                .Include(post => post.Comments)
                    .ThenInclude(comment => comment.Author)
                .Include(post => post.Comments)
                        .ThenInclude(comment => comment.Comments)
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            List<Post> posts = new List<Post>();
            posts = applicationDbContext.Posts
               .Include(post => post.Creator)
               .Include(post => post.Category)
               .Include(post => post.Comments)
               .OrderByDescending(post => post.UpdatedOn)
               .Take(5)
               .ToList();
            ViewBag.Posts = posts;

            List<ApplicationUser> users = new List<ApplicationUser>();
            users = applicationDbContext.Users
               .Take(5)
               .ToList();
            ViewBag.Users = users;

            PostViewModel pvm = new PostViewModel
            {
                Post = post
            };
            return View(pvm);
        }

        [HttpPost]
        public async Task<IActionResult> Comment(PostViewModel postViewModel)
        {
            if (postViewModel.Post is null || postViewModel.Post.Id == 0)
                return new BadRequestResult();



            var post = applicationDbContext.Posts
                .Include(post => post.Creator)
                .Include(post => post.Comments)
                    .ThenInclude(comment => comment.Author)
                .Include(post => post.Comments)
                        .ThenInclude(reply => reply.Parent)
                .FirstOrDefault(post => post.Id == postViewModel.Post.Id);

            if (post is null)
                return new NotFoundResult();

            var comment = postViewModel.Comment;

            comment.Author = await userManager.GetUserAsync(User);
            comment.Post = post;
            comment.CreatedOn = DateTime.Now;


            if (comment.Parent != null)
            {
                var cmtId = comment.Parent.Id;
                var cmt = applicationDbContext.Comments
                .Include(comment => comment.Author)
                .Include(comment => comment.Post)
                .Include(comment => comment.Parent)
                .FirstOrDefault(comment => comment.Id == cmtId);

                if (cmt is null)
                {
                    return new NotFoundResult();
                }
                comment.Parent = cmt;
            }

            applicationDbContext.Add(comment);
            await applicationDbContext.SaveChangesAsync();

            return RedirectToAction("GetPost", "Home", new { postViewModel.Post.Id });
        }

        public IActionResult GetAuthor()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}