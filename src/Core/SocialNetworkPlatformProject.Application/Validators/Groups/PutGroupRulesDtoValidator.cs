using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Validators.Groups;

public class PutGroupRulesDtoValidator : AbstractValidator<PutGroupRulesDto>
{
    public PutGroupRulesDtoValidator()
    {
        RuleFor(x => x.Rules)
            .NotNull()
            .Must(rules => rules.Count <= GroupLimits.MaxRules)
            .WithMessage(_ => Loc.T("val.group.rulesMax", GroupLimits.MaxRules));

        RuleForEach(x => x.Rules).ChildRules(rule =>
        {
            rule.RuleFor(r => r.Title)
                .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage(_ => Loc.T("val.group.ruleTitle"))
                .MaximumLength(GroupLimits.RuleTitle).WithMessage(_ => Loc.T("val.group.ruleTitleLength", GroupLimits.RuleTitle));

            rule.RuleFor(r => r.Text)
                .MaximumLength(GroupLimits.RuleText).WithMessage(_ => Loc.T("val.group.ruleTextLength", GroupLimits.RuleText));
        });
    }
}
