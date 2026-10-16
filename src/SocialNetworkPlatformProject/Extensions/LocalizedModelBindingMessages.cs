using Microsoft.AspNetCore.Mvc;

namespace SocialNetworkPlatformProject.Extensions;

// What ASP.NET says when a request's values cannot be read (a letter where a number should be, a missing body...) is
// English by default; the page shows it as it is, so it comes from the language files, in the request's language.
public static class LocalizedModelBindingMessages
{
    public static void Apply(MvcOptions options)
    {
        var messages = options.ModelBindingMessageProvider;

        messages.SetValueIsInvalidAccessor(value => Loc.T("binding.valueInvalid", value));
        messages.SetValueMustBeANumberAccessor(field => Loc.T("binding.mustBeNumber", field));
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => Loc.T("binding.valueInvalidFor", value, field));
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => Loc.T("binding.valueInvalid", value));
        messages.SetNonPropertyValueMustBeANumberAccessor(() => Loc.T("binding.numberRequired"));
        messages.SetUnknownValueIsInvalidAccessor(field => Loc.T("binding.unknownInvalid", field));
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => Loc.T("binding.unknownInvalidNoField"));
        messages.SetMissingBindRequiredValueAccessor(field => Loc.T("binding.required", field));
        messages.SetMissingKeyOrValueAccessor(() => Loc.T("binding.valueRequired"));
        messages.SetMissingRequestBodyRequiredValueAccessor(() => Loc.T("binding.bodyRequired"));
        messages.SetValueMustNotBeNullAccessor(field => Loc.T("binding.notNull", field));
    }
}
