
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TweetBook.Services
{
    public interface IPostService
    {
        Task<List<Domain.Post>> GetPostsAsync();

        Task<Domain.Post> GetPostByIdAsync(Guid postId);

        Task<bool> UpdatePostAsync(Domain.Post postToUpdate);

        Task<bool> DeletePostAsync(Guid postId);

        Task<bool> CreatePostAsync(Domain.Post post);
        Task<bool> UerOwnsPostAsync(Guid post, string getUserId);
        

    }
}
