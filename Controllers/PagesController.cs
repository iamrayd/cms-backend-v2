using Microsoft.AspNetCore.Mvc;
using ProjectCms.Models;
using ProjectCms.Services;
using ProjectCms.Api.Services;

namespace ProjectCms.Controllers
{
    [ApiController]
    [Route("api/Pages")]  // keep explicit – matches Angular
    public class PagesController : ControllerBase
    {
        private readonly PageService _pageService;
        private readonly IActivityLogService _activityLogService;
        private readonly ArchivedPageService _archivedPageService;   // ⭐ NEW

        public PagesController(
            PageService pageService,
            IActivityLogService activityLogService,
            ArchivedPageService archivedPageService)                  // ⭐ NEW
        {
            _pageService = pageService;
            _activityLogService = activityLogService;
            _archivedPageService = archivedPageService;               // ⭐ NEW
        }

        // ----------------------------
        // HEALTH CHECK
        // ----------------------------
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                await _pageService.GetAsync();
                return Ok(new { status = "ok", database = "connected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        // ----------------------------
        // COUNT
        // ----------------------------
        [HttpGet("count")]
        public async Task<ActionResult<int>> Count()
        {
            var pages = await _pageService.GetAsync();
            return Ok(pages.Count);
        }

        // ----------------------------
        // GET ALL
        // ----------------------------
        [HttpGet]
        public async Task<ActionResult<List<Page>>> GetAll()
        {
            var pages = await _pageService.GetAsync();
            return Ok(pages);
        }

        // ----------------------------
        // GET BY ID
        // ----------------------------
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Page>> GetById(string id)
        {
            var page = await _pageService.GetAsync(id);

            if (page is null)
                return NotFound();

            return Ok(page);
        }

        // ----------------------------
        // CREATE
        // ----------------------------
        [HttpPost]
        public async Task<ActionResult<Page>> Create([FromBody] Page newPage)
        {
            await _pageService.CreateAsync(newPage);

            await _activityLogService.LogAsync(
                userName: "Admin",
                action: "Created Page",
                contentType: "page",
                contentTitle: newPage.Title ?? string.Empty,
                contentId: newPage.Id ?? string.Empty,
                status: "Success"
            );

            return CreatedAtAction(nameof(GetById), new { id = newPage.Id }, newPage);
        }

        // ----------------------------
        // UPDATE
        // ----------------------------
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] Page updatedPage)
        {
            var existing = await _pageService.GetAsync(id);
            if (existing is null)
                return NotFound();

            updatedPage.Id = id;
            await _pageService.UpdateAsync(id, updatedPage);

            await _activityLogService.LogAsync(
                userName: "Admin",
                action: "Updated Page",
                contentType: "page",
                contentTitle: updatedPage.Title ?? string.Empty,
                contentId: id,
                status: "Success"
            );

            return NoContent();
        }

        // ----------------------------
        // DELETE -> ARCHIVE
        // ----------------------------
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _pageService.GetAsync(id);
            if (existing is null)
                return NotFound();

            // ⭐ 1) Map Page -> ArchivedPage
            var archived = new ArchivedPage
            {
                PageId = existing.Id ?? string.Empty,
                Title = existing.Title ?? string.Empty,
                Slug = existing.Slug ?? string.Empty,
                Description = existing.Description ?? string.Empty,
                Content = existing.Content ?? string.Empty,
                Status = existing.Status ?? string.Empty,
                FeaturedImage = existing.FeaturedImage ?? string.Empty,
                Tags = existing.Tags ?? new List<string>(),
                Category = existing.Category ?? string.Empty,
                PublishDate = existing.PublishDate,
                Author = existing.Author ?? string.Empty,
                ArchiveType = "Deleted",
                ArchivedAt = DateTime.UtcNow
            };

            // ⭐ 2) Save to ArchivedPages collection
            await _archivedPageService.CreateAsync(archived);

            // ⭐ 3) Remove from main Pages collection
            await _pageService.RemoveAsync(id);

            // ⭐ 4) Activity Log
            await _activityLogService.LogAsync(
                userName: "Admin",
                action: "Deleted Page",
                contentType: "page",
                contentTitle: existing.Title ?? string.Empty,
                contentId: id,
                status: "Success"
            );

            return NoContent();
        }
    }
}