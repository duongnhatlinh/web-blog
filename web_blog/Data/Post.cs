using System.ComponentModel.DataAnnotations;

namespace web_blog.Data
{
    public class Post
    {
        public int Id { get; set; }
        public ApplicationUser Creator { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Published { get; set; }
        public bool Approved { get; set; }

        public Category Category { get; set; }

        public virtual IEnumerable<Comment> Comments { get; set; }
    }
}
