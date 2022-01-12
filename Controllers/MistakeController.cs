using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace NjuCsCmsHelper.Server.Controllers
{
    using Models;

    [ApiController]
    [Route("[controller]")]
    public class MistakeController : ControllerBase
    {
        private readonly ILogger<MistakeController> logger;
        private readonly ApplicationDbContext dbContext;

        public MistakeController(ILogger<MistakeController> logger, ApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int? studentId)
        {
            var list = await dbContext.Mistakes
                .Where(m => (studentId == null || m.StudentId == studentId) && m.CorrectedIn == null)
                .Select(m => new { StudentId = m.StudentId, Mistake = $"{m.AssignmentId}-{m.ProblemId}" })
                .ToListAsync();
            var res = list
                .GroupBy(m => m.StudentId)
                .Select(g => new { StudentId = g.Key, Mistakes = g.Select(m => m.Mistake).ToList() })
                .ToList();
            return Ok(res);
        }
    }
}