using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PagedList.Core;
using System.Drawing;
using System.Drawing.Printing;
using System.Security.Policy;
using web_blog.Data;
using web_blog.Models.HomeViewModels;

namespace web_blog.ViewComponents
{

    public class SideBodyViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly UserManager<ApplicationUser> userManager;

        public SideBodyViewComponent(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            applicationDbContext = dbContext;
            this.userManager = userManager;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            Filter filter = new Filter
            {
                categoryId = null,
                authorId = null,
                sortOrder = null,
                orderBy = null,
            };
            ViewBag.Filter = filter;

            List<Category> categories = new List<Category>();
            categories = applicationDbContext.Categories.ToList();
            ViewBag.Categories = categories;

            List<ApplicationUser> users = new List<ApplicationUser>();
            users = userManager.Users.ToList();
            ViewBag.Users = users;

            List<Post> posts = new List<Post>();
            posts = applicationDbContext.Posts
               .Include(post => post.Creator)
               .Include(post => post.Category)
               .Include(post => post.Comments)
               .OrderByDescending(post => post.UpdatedOn)
               .Take(5)
               .ToList();
            ViewBag.Posts = posts;

            return await Task.Factory.StartNew(() => { return View(); });
        }
    }
}


