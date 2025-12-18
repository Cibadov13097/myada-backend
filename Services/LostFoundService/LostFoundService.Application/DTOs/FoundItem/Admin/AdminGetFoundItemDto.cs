using LostFoundService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem.Admin
{
    public class AdminGetFoundItemDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }

        public string Description { get; set; } = null!;
        public string LocationFound { get; set; } = null!;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        public string FoundByUserId { get; set; } = null!;
        public Status Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        public string CreatedByUserId { get; set; } = null!;
        public string? ModifiedByUserId { get; set; }
    }

}
