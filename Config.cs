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
        {1, "李晗"},
        {2, "桑百惠"},
        {3, "赵超懿"},
        {4, "姚梦雨"},
    };
}
