using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Interfaces
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
