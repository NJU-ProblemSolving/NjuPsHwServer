using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NjuCsCmsHelper.Models
{
    public class Mistake
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int AssignmentId { get; set; }
        public int ProblemId { get; set; }
        public int MakedInId { get; set; }
        public int? CorrectedInId { get; set; }

        public virtual Student Student { get; set; }
        public virtual Assignment Assignment { get; set; }
        public virtual Submission MakedIn { get; set; }
        public virtual Submission CorrectedIn { get; set; }
    }
}
