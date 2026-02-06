using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class Size : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    }
}
