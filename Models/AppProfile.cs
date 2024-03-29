namespace NjuCsCmsHelper.Models;

using AutoMapper;
using NjuCsCmsHelper.Datas;

public class AppProfile : Profile
{
    public AppProfile()
    {
        CreateMap<Student, StudentDto>(MemberList.Destination);
        CreateMap<StudentDto, Student>(MemberList.Source);
        CreateMap<Mistake, MistakeDto>(MemberList.Destination);
        CreateMap<MistakeDto, Mistake>(MemberList.Source);
    }
}
