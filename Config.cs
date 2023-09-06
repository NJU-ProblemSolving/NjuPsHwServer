namespace NjuCsCmsHelper.Server;

public static class AppUserClaims
{
    public const string StudentName = "studentName";
    public const string StudentId = "studentId";
}

public static class AppConfig
{
    public const int AttachmentSizeLimit = 10 * 1024 * 1024;
    public const string Revision = "unknown";
    public static readonly Dictionary<int, String> ReviewerName = new Dictionary<int, string> {
        {1, "陈子元"},
        {2, "付博"},
        {3, "缪天顺"},
        {4, "赵欣玥"},
    };
}
