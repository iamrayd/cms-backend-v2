using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ProjectCms.Models
{
    public class Page
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = null!;

        [BsonElement("slug")]
        public string Slug { get; set; } = null!;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "draft";   // draft | published

        [BsonElement("content")]
        public string? Content { get; set; }

        [BsonElement("featuredImage")]
        public string? FeaturedImage { get; set; }

        [BsonElement("tags")]
        public List<string>? Tags { get; set; }

        [BsonElement("category")]
        public string? Category { get; set; }   // Home, About Us, etc.

        [BsonElement("publishDate")]
        public DateTime? PublishDate { get; set; }

        [BsonElement("author")]
        public string? Author { get; set; }
    }
}