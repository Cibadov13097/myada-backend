using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events.LostFound
{
    public class LostItemCreatedEvent
    {
        public int LostItemId { get; set; }
        public string Name { get; set; } = default!;
        public string Location { get; set; } = default!;
    }
}
