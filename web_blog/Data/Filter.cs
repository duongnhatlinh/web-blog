namespace web_blog.Data
{
    public class Filter
    {
        public int? categoryId { get; set; }
        public string? authorId { get; set; }
        public string? approve { get; set; }

        public string? publish { get; set; }

        public string? orderBy { get; set; }
        public string? sortOrder { get; set; }
    }
}
