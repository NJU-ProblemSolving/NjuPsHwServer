using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NjuCsCmsHelper.Models
{
    public class Student
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public int ReviewerId { get; set; }

        public virtual ICollection<Submission> Submissions { get; set; }
    }
}
