using System;

namespace VibrantCode.HubQ.Web.Models
{
    public class Hyperlinks
    {
        public string? NextPage { get; set; }
        public string? PreviousPage { get; set; }

        public static Hyperlinks GeneratePagingLinks(Func<object, string> urlGenerator, int currentPage, int pageSize)
        {
            var nextLink = urlGenerator(new
            {
                pageNumber = currentPage + 1,
                pageSize,
            });

            var previousLink = currentPage > 1 ?
                urlGenerator(new
                {
                    pageNumber = currentPage - 1,
                    pageSize
                }) :
                null;

            return new Hyperlinks
            {
                NextPage = nextLink,
                PreviousPage = previousLink,
            };
        }
    }
}