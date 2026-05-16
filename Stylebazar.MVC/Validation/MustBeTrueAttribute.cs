using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Stylebazar.MVC.Validation;

/// <summary>
/// Validation attribute that passes only when the bound value is the boolean
/// <c>true</c>. Designed specifically for "I accept the terms" checkboxes
/// where <see cref="RangeAttribute"/> with <c>typeof(bool)</c> falls apart on
/// the client side — jQuery Validation's range adapter parses min/max as
/// floats, gets <c>NaN</c> from "True", and ends up emitting the error message
/// even when the box is ticked. This attribute emits a single bespoke
/// <c>data-val-mustbetrue</c> rule that the client-side adapter in
/// <c>site.js</c> reads as a plain <c>element.checked</c> check.
///
/// Apply to <c>bool?</c> (nullable) so ASP.NET does NOT also auto-add a
/// <c>data-val-required</c> rule with an English default message.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public MustBeTrueAttribute()
    {
        ErrorMessage = "Bu sahə işarələnməlidir.";
    }

    public override bool IsValid(object? value) => value is bool b && b;

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context is null) return;

        // Standard jQuery unobtrusive contract — `data-val="true"` activates
        // validation for the field, then each rule has its own message
        // attribute keyed by rule name.
        if (!context.Attributes.ContainsKey("data-val"))
            context.Attributes.Add("data-val", "true");

        string message = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        if (!context.Attributes.ContainsKey("data-val-mustbetrue"))
            context.Attributes.Add("data-val-mustbetrue", message);
    }
}
