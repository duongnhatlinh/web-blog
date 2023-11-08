using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PagedList.Core;
using System.Security.Claims;
using web_blog.Data;
using web_blog.Models.HomeViewModels;
using web_blog.Models.PostViewModels;
using web_blog.Models.UserViewModels;

namespace web_blog.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment webHostEnvironment;


        public UserController(ApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            this.applicationDbContext = applicationDbContext;
            this.userManager = userManager;
            this.webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index(string? SearchString, int? page, int? categoryId, string? authorId, string? publish, string? sortOrder, string? orderBy)
        {
            int pageSize = 7;
            int pageNumber = page ?? 1;
            var str = SearchString ?? string.Empty;
         

            var posts = applicationDbContext.Posts
               .Include(post => post.Creator)
               .OrderByDescending(post => post.UpdatedOn)
               .Where(post => post.Title.Contains(str) || post.Content.Contains(str));

            // filter
            Filter filter = new Filter();

            filter.categoryId = categoryId ?? null;
            filter.authorId = authorId ?? null;
            filter.publish = publish ?? null;
            filter.sortOrder = sortOrder ?? null;
            filter.orderBy = orderBy ?? null;

            FilterPost(filter, ref posts);

            ViewBag.Filter = filter;

            List<Category> categories = new List<Category>();
            categories = applicationDbContext.Categories.ToList();
            ViewBag.Categories = categories;

            List<ApplicationUser> users = new List<ApplicationUser>();
            users = userManager.Users.ToList();
            ViewBag.Users = users;

            // number of page
            double numberOfPages = posts.Count() / (double)pageSize;
            int roundNumberOfPages = (int)Math.Ceiling(numberOfPages);
            ViewBag.NumberOfPages = roundNumberOfPages;

            Models.UserViewModels.IndexViewModel ivm = new Models.UserViewModels.IndexViewModel
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
        public async Task<IActionResult> Published(int? postId, bool? check)
        {
            if (postId is null || check is null)
                return new NotFoundResult();

            var post = applicationDbContext.Posts
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            if (check.Value == false)
            {
                post.Published = true;
            }
            else
            {
                post.Published = false;
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

        [HttpGet]
        public IActionResult CreatePost()
        {

            List<Category> categories = new List<Category>();
            categories = applicationDbContext.Categories.ToList();
            ViewBag.Categories = categories;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(CreateViewModel createViewModel, int? categoryId)
        {
            var category = applicationDbContext.Categories.Where(x => x.Id == categoryId).FirstOrDefault();

            Post post = createViewModel.Post;
            post.Category = category;
            post.Creator = await userManager.GetUserAsync(User);
            post.CreatedOn = DateTime.Now;
            post.UpdatedOn = DateTime.Now;

            applicationDbContext.Add(post);
            await applicationDbContext.SaveChangesAsync();

            string webRootPath = webHostEnvironment.WebRootPath;
            string pathToImage = $@"{webRootPath}\UserFiles\Posts\{post.Id}\HeaderImage.jpg";

            EnsureFolder(pathToImage);

            using (var fileStream = new FileStream(pathToImage, FileMode.Create))
            {
                await createViewModel.HeaderImage.CopyToAsync(fileStream);
            }

            return RedirectToAction("CreatePost");

        }

        [HttpGet]
        public IActionResult EditPost(int? id)
        {
            List<SelectListItem> values = (from x in applicationDbContext.Categories.ToList()
                                           select new SelectListItem
                                           {
                                               Text = x.Name,
                                               Value = x.Id.ToString()
                                           }).ToList();

            ViewBag.value = values;

            if (id is null)
                return new BadRequestResult();

            var postId = id.Value;

            var post = applicationDbContext.Posts
                .Include(post => post.Creator)
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            EditViewModel evm = new EditViewModel
            {
                Post = post
            };

            return View(evm);
        }

        [HttpPost]
        public async Task<IActionResult> EditPost(EditViewModel editViewModel)
        {
            if (editViewModel.Post is null)
                return new NotFoundResult();

            var category = applicationDbContext.Categories.Where(x => x.Id == editViewModel.Post.Category.Id).FirstOrDefault();

            if (category is null)
                return new NotFoundResult();

            var post = applicationDbContext.Posts
               .Include(post => post.Creator)
               .FirstOrDefault(post => post.Id == editViewModel.Post.Id);

            if (post is null)
                return new NotFoundResult();

            post.UpdatedOn = DateTime.Now;
            post.Title = editViewModel.Post.Title;
            post.Category = category;
            post.Creator = await userManager.GetUserAsync(User);
            post.Content = editViewModel.Post.Content;
            post.Published = editViewModel.Post.Published;


            if (editViewModel.HeaderImage != null)
            {
                string webRootPath = webHostEnvironment.WebRootPath;
                string pathToImage = $@"{webRootPath}\UserFiles\Posts\{editViewModel.Post.Id}\HeaderImage.jpg";

                EnsureFolder(pathToImage);

                using (var fileStream = new FileStream(pathToImage, FileMode.Create))
                {
                    await editViewModel.HeaderImage.CopyToAsync(fileStream);
                }
            }

            applicationDbContext.Update(post);
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("EditPost");
        }
        [HttpPost]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null)
                return new BadRequestResult();

            var postId = id.Value;

            var post = applicationDbContext.Posts
                .FirstOrDefault(post => post.Id == postId);

            if (post is null)
                return new NotFoundResult();

            applicationDbContext.Remove(post);

            await applicationDbContext.SaveChangesAsync();

            return RedirectToAction("Index", "User");
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

            return RedirectToAction("GetPost", "User", new { postViewModel.Post.Id });
        }

        public async Task<IActionResult> AboutMe()
        {
			ApplicationUser user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

			AboutViewModel avm = new AboutViewModel
			{
				User = user,
			};

			return View(avm);
		}

        [HttpPost]
		public async Task<IActionResult> EditProfile(AboutViewModel aboutViewModel)
		{
			if (aboutViewModel.ProfileImage != null)
			{
				string webRootPath = webHostEnvironment.WebRootPath;
				string pathToImage = $@"{webRootPath}\UserImages\Profile\{aboutViewModel.User.Id}\ProfilePicture.jpg";

				EnsureFolder(pathToImage);

				using (var fileStream = new FileStream(pathToImage, FileMode.Create))
				{
					await aboutViewModel.ProfileImage.CopyToAsync(fileStream);
				}
			}

			applicationDbContext.Update(aboutViewModel.User);
			await applicationDbContext.SaveChangesAsync();
			return View("AboutMe");
		}
		private ActionResult DetermineActionResult(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.Identity.IsAuthenticated)
                return new ForbidResult();
            else
                return new ChallengeResult();
        }

        private void EnsureFolder(string path)
        {
            string directoryName = Path.GetDirectoryName(path);
            if (directoryName.Length > 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
        }

    }
}
