using Microsoft.AspNetCore.Mvc;

namespace web_blog.ViewComponents
{
    public class UserFooterViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            return await Task.Factory.StartNew(() => { return View(); });
        }
    }
}
