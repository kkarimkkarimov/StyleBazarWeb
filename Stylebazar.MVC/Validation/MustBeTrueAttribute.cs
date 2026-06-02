using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Stylebazar.MVC.Validation;

// MustBeTrueAttribute — XÜSUSİ yoxlama atributu. Yalnız dəyər "true" olanda keçir.
// Əsasən "Şərtləri qəbul edirəm" checkbox-u üçün: istifadəçi qutunu işarələməsə, forma təsdiqlənməsin.
// Həm serverdə (IsValid), həm brauzerdə (data-val-mustbetrue → site.js) işləyir.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public MustBeTrueAttribute()
    {
        ErrorMessage = "Bu sahə işarələnməlidir.";   // istifadəçiyə göstərilən xəta mesajı
    }

    // SERVER tərəfi yoxlama: dəyər bool olmalı və true olmalıdır.
    public override bool IsValid(object? value) => value is bool b && b;

    // BRAUZER (client) tərəfi yoxlama üçün lazımi HTML data-atributlarını əlavə edir
    // (jQuery unobtrusive validation bunları oxuyur).
    public void AddValidation(ClientModelValidationContext context)
    {
        if (context is null) return;

        // data-val="true" → bu sahə üçün client validasiyanı aktivləşdirir.
        if (!context.Attributes.ContainsKey("data-val"))
            context.Attributes.Add("data-val", "true");

        // Qaydanın öz mesajı (qayda adına görə açarlanır).
        string message = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        if (!context.Attributes.ContainsKey("data-val-mustbetrue"))
            context.Attributes.Add("data-val-mustbetrue", message);
    }
}
