using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Admin;

namespace SocialNetworkPlatformProject.Application.Validators.Admin;

public class PostReportDtoValidator : AbstractValidator<PostReportDto>
{
    public PostReportDtoValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty().WithMessage(_ => Loc.T("report.targetRequired"));

        RuleFor(x => x.TargetType).IsInEnum().WithMessage(_ => Loc.T("report.targetTypeInvalid"));
        RuleFor(x => x.Reason).IsInEnum().WithMessage(_ => Loc.T("report.reasonInvalid"));

        RuleFor(x => x.Details).MaximumLength(1000).WithMessage(_ => Loc.T("report.detailsTooLong"));
    }
}

public class ResolveReportDtoValidator : AbstractValidator<ResolveReportDto>
{
    public ResolveReportDtoValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500).WithMessage(_ => Loc.T("admin.noteTooLong"));
    }
}

public class SuspendUserDtoValidator : AbstractValidator<SuspendUserDto>
{
    public SuspendUserDtoValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(300).WithMessage(_ => Loc.T("admin.noteTooLong"));
    }
}

public class RemoveContentDtoValidator : AbstractValidator<RemoveContentDto>
{
    public RemoveContentDtoValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500).WithMessage(_ => Loc.T("admin.noteTooLong"));
    }
}
