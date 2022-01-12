using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace NjuCsCmsHelper.Server.Controllers
{
    using Models;

    [ApiController]
    [Route("[controller]")]
    public class AssignmentController : ControllerBase
    {
        private readonly ILogger<AssignmentController> logger;
        private readonly Models.ApplicationDbContext dbContext;

        public AssignmentController(ILogger<AssignmentController> logger, Models.ApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Assignment assignment)
        {
            if (await dbContext.Assignments.AnyAsync(a => a.Id == assignment.Id))
                return Conflict("Assignament ID already exists");
            dbContext.Assignments.Add(assignment);
            await dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}