namespace NjuCsCmsHelper.Server.Services;

using MailKit.Net.Smtp;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using NjuCsCmsHelper.Models;

public class MailingService
{
    private readonly ILogger<MailingService> logger;

    private readonly AppDbContext dbContext;
    private readonly IMemoryCache cache;
    private readonly IConfigurationSection smtpConfig;
    private readonly IConfigurationSection metaConfig;

    public MailingService(ILogger<MailingService> logger, AppDbContext dbContext, IMemoryCache cache, IConfiguration configuration)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.cache = cache;
        this.smtpConfig = configuration.GetSection("Smtp");
        this.metaConfig = configuration.GetSection("Meta");
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

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpConfig["Host"]);
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
}

