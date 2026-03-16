using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Domain.Exceptions
{
    public class ServiceUnavailableException:Exception
    {
        public ServiceUnavailableException(string message):base(message)
        {
            
        }
    }
}
