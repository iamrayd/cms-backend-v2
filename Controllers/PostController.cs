using Microsoft.AspNetCore.Mvc;
using ProjectCms.Models;
using ProjectCms.Services;
using ProjectCms.Api.Services;

namespace ProjectCms.Controllers
{
    [ApiController]
    [Route("api/Posts")]
    public class PostsController : ControllerBase
    {
        private readonly PostService _postService;
        private readonly IActivityLogService _activityLogService;

        public PostsController(PostService postService, IActivityLogService activityLogService)
        {
            _postService = postService;
            _activityLogService = activityLogService;
        }

        // GET /api/Posts
        [HttpGet]
        public async Task<ActionResult<List<Post>>> GetAll()
        {
            var posts = await _postService.GetAsync();
            return Ok(posts);
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> Count()
        {
            var posts = await _postService.GetAsync();
            return Ok(posts.Count);
        }

        // GET /api/Posts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Post>> GetById(string id)
        {
            var post = await _postService.GetAsync(id);
            return post is null ? NotFound() : Ok(post);
        }

        // POST /api/Posts
        [HttpPost]
        public async Task<ActionResult<Post>> Create([FromBody] Post newPost)
        {
            if (string.IsNullOrWhiteSpace(newPost.Title))
                return BadRequest("Title is required.");

            await _postService.CreateAsync(newPost);

            // Optional logging
            try
            {
                await _activityLogService.LogAsync(
                    userName: "Admin",
                    action: "Created Post",
                    contentType: "post",
                    contentTitle: newPost.Title,
                    contentId: newPost.Id ?? string.Empty,
                    status: "Success"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Activity Log failed: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetById), new { id = newPost.Id }, newPost);
        }

        // PUT /api/Posts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Post updatedPost)
        {
            var existing = await _postService.GetAsync(id);
            if (existing is null) return NotFound();

            updatedPost.Id = id;
            await _postService.UpdateAsync(id, updatedPost);

            try
            {
                await _activityLogService.LogAsync(
                    userName: "Admin",
                    action: "Updated Post",
                    contentType: "post",
                    contentTitle: updatedPost.Title,
                    contentId: id,
                    status: "Success"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Activity Log failed: {ex.Message}");
            }

            return NoContent();
        }

        // DELETE /api/Posts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var existing = await _postService.GetAsync(id);
                if (existing is null) return NotFound();

                await _postService.RemoveAsync(id);

                try
                {
                    await _activityLogService.LogAsync(
                        userName: "Admin",
                        action: "Deleted Post",
                        contentType: "post",
                        contentTitle: existing.Title,
                        contentId: id,
                        status: "Success"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Activity Log failed: {ex.Message}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Delete Post failed: {ex}");
                return StatusCode(500, "Error deleting post");
            }
        }
    }
}
