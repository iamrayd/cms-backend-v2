using Microsoft.AspNetCore.Mvc;
using ProjectCms.Models;
using ProjectCms.Services;
using ProjectCms.Api.Services;

namespace ProjectCms.Controllers
{
    [ApiController]
    [Route("api/Pages")]
    public class PagesController : ControllerBase
    {
        private readonly PageService _pageService;
        private readonly IActivityLogService _activityLogService;
        private readonly ArchivedPageService _archivedPageService;
        private readonly ILogger<PagesController> _logger;

        public PagesController(
            PageService pageService,
            IActivityLogService activityLogService,
            ArchivedPageService archivedPageService,
            ILogger<PagesController> logger)
        {
            _pageService = pageService;
            _activityLogService = activityLogService;
            _archivedPageService = archivedPageService;
            _logger = logger;
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
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Database connection failed"
                });
            }
        }

        // ----------------------------
        // COUNT
        // ----------------------------
        [HttpGet("count")]
        public async Task<ActionResult<int>> Count()
        {
            try
            {
                var pages = await _pageService.GetAsync();
                return Ok(pages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting pages");
                return StatusCode(500, "Error counting pages");
            }
        }

        // ----------------------------
        // GET ALL
        // ----------------------------
        [HttpGet]
        public async Task<ActionResult<List<Page>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Fetching all pages");
                var pages = await _pageService.GetAsync();
                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pages");
                return StatusCode(500, "Error fetching pages");
            }
        }

        // ----------------------------
        // GET BY ID
        // ----------------------------
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Page>> GetById(string id)
        {
            try
            {
                _logger.LogInformation("Fetching page with ID: {PageId}", id);
                var page = await _pageService.GetAsync(id);

                if (page is null)
                {
                    _logger.LogWarning("Page not found with ID: {PageId}", id);
                    return NotFound(new { message = "Page not found" });
                }

                return Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching page with ID: {PageId}", id);
                return StatusCode(500, "Error fetching page");
            }
        }

        // ----------------------------
        // CREATE
        // ----------------------------
        [HttpPost]
        public async Task<ActionResult<Page>> Create([FromBody] Page newPage)
        {
            try
            {
                // Validation
                if (newPage == null)
                {
                    _logger.LogWarning("Create page request received with null body");
                    return BadRequest(new { message = "Request body cannot be null" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for page creation");
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(newPage.Title))
                {
                    return BadRequest(new { message = "Title is required" });
                }

                if (string.IsNullOrWhiteSpace(newPage.Slug))
                {
                    return BadRequest(new { message = "Slug is required" });
                }

                // ⭐ CRITICAL FIX: Clear any invalid ID sent from client
                if (!string.IsNullOrWhiteSpace(newPage.Id) &&
                    (newPage.Id == "string" || newPage.Id.Length != 24))
                {
                    _logger.LogWarning("Invalid ID received in create request: {InvalidId}. Clearing it.", newPage.Id);
                    newPage.Id = string.Empty;
                }

                _logger.LogInformation("Creating new page: {PageTitle}", newPage.Title);
                await _pageService.CreateAsync(newPage);

                // Activity log
                try
                {
                    await _activityLogService.LogAsync(
                        userName: "Admin", // TODO: Get from authenticated user context
                        action: "Created Page",
                        contentType: "page",
                        contentTitle: newPage.Title,
                        contentId: newPage.Id,
                        status: "Success"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log activity for page creation");
                    // Don't fail the request if logging fails
                }

                _logger.LogInformation("Page created successfully with ID: {PageId}", newPage.Id);
                return CreatedAtAction(nameof(GetById), new { id = newPage.Id }, newPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page");
                return StatusCode(500, new { message = "Error creating page", details = ex.Message });
            }
        }

        // ----------------------------
        // UPDATE
        // ----------------------------
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] Page updatedPage)
        {
            try
            {
                // Validation
                if (updatedPage == null)
                {
                    return BadRequest(new { message = "Request body cannot be null" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(updatedPage.Title))
                {
                    return BadRequest(new { message = "Title is required" });
                }

                if (string.IsNullOrWhiteSpace(updatedPage.Slug))
                {
                    return BadRequest(new { message = "Slug is required" });
                }

                _logger.LogInformation("Updating page with ID: {PageId}", id);
                var existing = await _pageService.GetAsync(id);

                if (existing is null)
                {
                    _logger.LogWarning("Page not found for update with ID: {PageId}", id);
                    return NotFound(new { message = "Page not found" });
                }

                updatedPage.Id = id;
                await _pageService.UpdateAsync(id, updatedPage);

                // Activity log
                try
                {
                    await _activityLogService.LogAsync(
                        userName: "Admin", // TODO: Get from authenticated user context
                        action: "Updated Page",
                        contentType: "page",
                        contentTitle: updatedPage.Title,
                        contentId: id,
                        status: "Success"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log activity for page update");
                }

                _logger.LogInformation("Page updated successfully with ID: {PageId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page with ID: {PageId}", id);
                return StatusCode(500, new { message = "Error updating page", details = ex.Message });
            }
        }

        // ----------------------------
        // DELETE -> ARCHIVE
        // ----------------------------
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                _logger.LogInformation("Deleting page with ID: {PageId}", id);
                var existing = await _pageService.GetAsync(id);

                if (existing is null)
                {
                    _logger.LogWarning("Page not found for deletion with ID: {PageId}", id);
                    return NotFound(new { message = "Page not found" });
                }

                // Map Page -> ArchivedPage
                var archived = new ArchivedPage
                {
                    PageId = existing.Id,
                    Title = existing.Title,
                    Slug = existing.Slug,
                    Description = existing.Description ?? string.Empty,
                    Content = existing.Content ?? string.Empty,
                    Status = existing.Status,
                    FeaturedImage = existing.FeaturedImage ?? string.Empty,
                    Tags = existing.Tags ?? new List<string>(),
                    Category = existing.Category ?? string.Empty,
                    PublishDate = existing.PublishDate,
                    Author = existing.Author ?? string.Empty,
                    ArchiveType = "Deleted",
                    ArchivedAt = DateTime.UtcNow
                };

                // Save to ArchivedPages collection
                await _archivedPageService.CreateAsync(archived);
                _logger.LogInformation("Page archived with ID: {PageId}", id);

                // Remove from main Pages collection
                await _pageService.RemoveAsync(id);
                _logger.LogInformation("Page removed from main collection with ID: {PageId}", id);

                // Activity Log
                try
                {
                    await _activityLogService.LogAsync(
                        userName: "Admin", // TODO: Get from authenticated user context
                        action: "Deleted Page",
                        contentType: "page",
                        contentTitle: existing.Title,
                        contentId: id,
                        status: "Success"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log activity for page deletion");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page with ID: {PageId}", id);
                return StatusCode(500, new { message = "Error deleting page", details = ex.Message });
            }
        }
    }
}