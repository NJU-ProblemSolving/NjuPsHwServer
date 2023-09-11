namespace NjuCsCmsHelper.Server.Services;

using System.Text;
using MailKit.Net.Smtp;
using MailKit.Net.Proxy.HttpProxyClient;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using NjuCsCmsHelper.Datas;
using NjuCsCmsHelper.Models;

public class MailingService
{
    private readonly ILogger<MailingService> logger;

    private readonly AppDbContext dbContext;
    private readonly IMemoryCache cache;
    private readonly IConfigurationSection smtpConfig;
    private readonly IConfigurationSection metaConfig;
    private readonly MyAppService myAppService;

    public MailingService(ILogger<MailingService> logger, AppDbContext dbContext, IMemoryCache cache, IConfiguration configuration, MyAppService myAppService)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.cache = cache;
        this.smtpConfig = configuration.GetSection("Smtp");
        this.metaConfig = configuration.GetSection("Meta");
        this.myAppService = myAppService;
    }

    public async Task SendMail(MimeMessage message)
    {
        try
        {
            using var client = new SmtpClient();
            if (smtpConfig["Proxy"] != null && smtpConfig["ProxyPort"] != null) 
                client.ProxyClient = new HttpProxyClient(smtpConfig["Proxy"], smtpConfig.GetValue<int>("ProxyPort"));
            await client.ConnectAsync(smtpConfig["Host"], smtpConfig.GetValue<int>("HostPort"));
            await client.AuthenticateAsync(smtpConfig["Username"], smtpConfig["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发送邮件失败");
            throw new HttpResponseException(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    public async Task SendToken(int studentId)
    {
        var studentName = await dbContext.Students.Where(student => student.Id == studentId).Select(student => student.Name).SingleOrDefaultAsync();
        if (studentName == null) throw new HttpResponseException(StatusCodes.Status404NotFound, "Student not found");

        var token = await dbContext.Tokens.Where(t => t.StudentId == studentId).Select(t => t.Id).FirstOrDefaultAsync();
        if (token == null) throw new HttpResponseException(StatusCodes.Status404NotFound, "Token not found");

        if (cache.TryGetValue(studentId, out _))
            throw new HttpResponseException(StatusCodes.Status429TooManyRequests, "在最近一小时内已发送过邮件，请稍后再试。");
        cache.Set(studentId, true, TimeSpan.FromHours(1));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(metaConfig["Organization"], smtpConfig["Username"]));
        message.To.Add(new MailboxAddress(studentName, $"{studentId}@smail.nju.edu.cn"));
        message.Subject = "【问题求解】作业系统 Token";
        var text = $"\n登录 Token 已被重置为 {token} 。\n前往问题求解作业平台 {metaConfig["Website"]} 以继续。\n\n";
        message.Body = new TextPart("plain") { Text = text };

        await SendMail(message);
    }

    public async Task SendSummaryMail(int studentId)
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(student => student.Id == studentId);
        if (student == null) throw new KeyNotFoundException($"Student ID {studentId} not exists");

        var submissions = await dbContext.Submissions
            .Where(submission => submission.StudentId == studentId)
            .OrderBy(submission => submission.AssignmentId)
            .Select(submission => new SubmissionDto
            {
                AssignmentId = submission.AssignmentId,
                AssignmentName = submission.Assignment.Name,
                Grade = submission.Grade,
                SubmittedAt = submission.SubmittedAt,
                NeedCorrection = submission.NeedCorrection
                    .OrderBy(mistake => mistake.ProblemId)
                    .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                HasCorrected = submission.HasCorrected
                    .OrderBy(mistake => mistake.AssignmentId)
                    .ThenBy(mistake => mistake.ProblemId)
                    .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                    .ToList(),
                Comment = submission.Comment,
            })
                              .ToListAsync();
        foreach (var s in submissions)
        {
            s.AssignmentName = await myAppService.GetAssignmentNameById(s.AssignmentId);
            await Task.WhenAll(s.NeedCorrection.Select(x => myAppService.FillProblemDTO(x)));
            await Task.WhenAll(s.HasCorrected.Select(x => myAppService.FillProblemDTO(x)));
        }

        var notCorrected =
            await dbContext.Mistakes.Where(mistake => mistake.StudentId == studentId && mistake.CorrectedIn == null)
                .OrderBy(mistake => mistake.AssignmentId)
                .ThenBy(mistake => mistake.ProblemId)
                .Select(m => new MistakeDto { AssignmentId = m.AssignmentId, ProblemId = m.ProblemId })
                .ToListAsync();
        await Task.WhenAll(notCorrected.Select(x => myAppService.FillProblemDTO(x)));

        var content = new StringBuilder();
        content.Append(CultureInfo.InvariantCulture, $"\n{student.Name}同学你好，你一共提交了{submissions.Count}次作业。\n\n");
        foreach (var submission in submissions)
        {
            content.Append(CultureInfo.InvariantCulture, $"在作业{submission.AssignmentName}中，你的评分是 {submission.Grade.GetDescription()} 。");
            if (submission.NeedCorrection.Count > 0)
            {
                content.Append($"其中，你需要订正题目");
                content.AppendJoin('、', submission.NeedCorrection);
                content.Append('。');
            }
            else
            {
                content.Append("此次作业你无需进行订正。");
            }
            if (submission.HasCorrected.Count > 0)
            {
                content.Append($"附录中对题目");
                content.AppendJoin('、', submission.HasCorrected);
                content.Append($"的订正已被接受。");
            }
            content.Append('\n');
        }
        content.Append('\n');

        if (notCorrected.Count > 0)
        {
            content.Append("目前，你还没有订正题目");
            content.AppendJoin('、', notCorrected);
            content.Append("。请及时进行订正。\n");
        }
        else
        {
            content.Append("恭喜，你目前没有需要订正的题目。\n");
        }

        content.Append('\n');
        content.Append("此邮件为脚本自动发送，请勿直接回复。");
        content.Append(CultureInfo.InvariantCulture, $"在需要时你也可以访问 {metaConfig["Website"]} 查询和补交作业。\n");
        content.Append("如果你对某次提交有疑问，请及时联系助教\n");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(metaConfig["Organization"], smtpConfig["Username"]));
        message.To.Add(new MailboxAddress(student.Name, $"{student.Id}@smail.nju.edu.cn"));
        message.Subject = "【问题求解】作业情况反馈";
        message.Body = new TextPart() { Text = content.ToString() };

        await SendMail(message);
    }
}
