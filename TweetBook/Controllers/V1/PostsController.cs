
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TweetBook.Contracts.V1;
using TweetBook.Contracts.V1.Requests;
using TweetBook.Contracts.V1.Responses;
using TweetBook.Domain;
using TweetBook.Extensions;
using TweetBook.Services;

namespace TweetBook.Controllers.V1
{
    [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        public PostsController(IPostService postService)
        {
            this._postService = postService;
        }

        [HttpGet(ApiRoutes.Posts.Get)]
        //this is end point level authorization fo rusers to have tagg.view claim apart from normal claims
       // [Authorize(Policy ="TagViewer")]
        public async Task<IActionResult> Get([FromRoute] Guid postId)
        {
            var post =await _postService.GetPostByIdAsync(postId);
            if (post == null)
                return NotFound();
            return Ok(post);
        }


        [HttpDelete(ApiRoutes.Posts.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid postId)
        {

            bool userOwnsPost = await _postService.UerOwnsPostAsync(postId, HttpContext.GetUserId());
            if (!userOwnsPost)
            {
                return BadRequest(new
                {
                    Error = "You don't own this post"
                });
            }

            var deleted =await _postService.DeletePostAsync(postId);
            if (deleted)
                return NoContent();

            return NotFound();
        }

        [HttpPut(ApiRoutes.Posts.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostRequest updatePostRequest)
        {
            //USer shoyld own this post to update it
            bool userOwnsPost = await _postService.UerOwnsPostAsync(postId, HttpContext.GetUserId() );
            if (!userOwnsPost) {
                return BadRequest(new {
                    Error = "You don't own this post"
                });
            }
            var post = await _postService.GetPostByIdAsync(postId);
            post.Name = updatePostRequest.Name;
            var updated =await _postService.UpdatePostAsync(post);

            if (updated)
                return Ok(post);

            return NotFound();
        }

        [HttpGet(ApiRoutes.Posts.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _postService.GetPostsAsync());
        }

        [HttpPost(ApiRoutes.Posts.Create)]

        public async Task<IActionResult> Create([FromBody] CreatePostRequest postRequest)
        {
            var post = new Post {
                Name = postRequest.Name,
                 UserId=HttpContext.GetUserId()//ext method for httpContext, we are tying post with logged inuser
            };

           await _postService.CreatePostAsync(post);

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
            var locationUri = baseUrl + "/" + ApiRoutes.Posts.Get.Replace("postId", post.Id.ToString());
            var response = new PostResponse { Id = post.Id, Name = post.Name };
            return Created(locationUri, response);
        }
    }
}