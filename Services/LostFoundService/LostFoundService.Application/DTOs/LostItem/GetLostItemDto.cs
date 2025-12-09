using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.LostItem
{
    public class GetLostItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public string LocationFound { get; set; }
        public string ImageUrl { get; set; }

        public string PostedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }   
    }

}
