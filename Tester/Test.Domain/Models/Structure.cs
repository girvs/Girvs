using System;
using System.Collections.Generic;

namespace Test.Domain.Models
{
    public class Structure
    {
        public Structure()
        {
            Childs = new List<Structure>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ImageUrl { get; set; }
        public Guid ParentID { get; set; }
        public bool IsPublic { get; set; }
        public bool IsIdentification { get; set; }
        public string PermissionDescString { get; set; }
        public List<Structure> Childs { get; set; }
    }
}