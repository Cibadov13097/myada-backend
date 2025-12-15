using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem.Admin
{
    public class AdminCreateFoundItem
    {
        public string ImageUrl { get; set; }

        
        public string Description { get; set; }
        
        public string LocationFound { get; set; }

    }
}
