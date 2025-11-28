using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectCms.Models
{
    public class Page
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;  // ⭐ FIXED: Non-nullable

        [BsonElement("title")]
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("slug")]
        [Required(ErrorMessage = "Slug is required")]
        [StringLength(200, ErrorMessage = "Slug cannot exceed 200 characters")]
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "Slug must be lowercase alphanumeric with hyphens")]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [BsonElement("status")]
        [RegularExpression("^(draft|published|archived)$",
            ErrorMessage = "Status must be 'draft', 'published', or 'archived'")]
        public string Status { get; set; } = "draft";

        [BsonElement("content")]
        public string? Content { get; set; }

        [BsonElement("featuredImage")]
        [Url(ErrorMessage = "Featured image must be a valid URL")]
        public string? FeaturedImage { get; set; }

        [BsonElement("tags")]
        public List<string>? Tags { get; set; }

        [BsonElement("category")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string? Category { get; set; }

        [BsonElement("publishDate")]
        public DateTime? PublishDate { get; set; }

        [BsonElement("author")]
        [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters")]
        public string? Author { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}