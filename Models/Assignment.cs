using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NjuCsCmsHelper.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public DateTime DeadLine { get; set; }
    }
}
