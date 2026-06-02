namespace Stylebazar.MVC.Models
{
    // ErrorViewModel — xəta səhifəsinə (Error.cshtml) ötürülən kiçik məlumat modeli.
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }   // Sorğunun izləmə id-si (problemi tapmaqda kömək edir)

        // RequestId varsa səhifədə göstər, boşdursa gizlət.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
