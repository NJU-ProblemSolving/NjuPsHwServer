using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NjuCsCmsHelper.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public Grade Grade { get; set; }
        [Required]
        public string Comment { get; set; }
        [Required]
        public string Track { get; set; }

        public virtual Student Student { get; set; }
        public virtual Assignment Assignment { get; set; }

        public virtual ICollection<Mistake> NeedCorrection { get; set; }
        public virtual ICollection<Mistake> HasCorrected { get; set; }
    }

    public enum Grade
    {
        None,
        A,
        Aminus,
        B,
        Bminus,
        C,
        D,
    }
}
