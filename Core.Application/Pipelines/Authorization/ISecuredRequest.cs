using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Pipelines.Authorization
{
    // Marker (nişan) interfeysi — gələcəkdə "bu sorğu icazə (authorization) tələb edir"
    // demək üçün nəzərdə tutulub (CQRS/MediatR pipeline-larında istifadə olunur).
    // Hələlik boşdur və layihədə fəal istifadə olunmur — şablon olaraq qalıb.
    internal interface ISecuredRequest
    {
    }
}
