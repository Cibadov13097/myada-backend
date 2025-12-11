using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem.Admin
{
    public class AdminFoundItemDto : GetFoundItemDto
    {
        [JsonPropertyOrder(8)]
        public bool IsDeleted { get; set; }
        [JsonPropertyOrder(9)]
        public string FoundByUserId { get; set; }
        [JsonPropertyOrder(10)]
        public string CreatedByUserId { get; set; }
        [JsonPropertyOrder(11)]
        public string ModifiedByUserId { get; set; }
    }

}
