namespace Stylebazar.BLL.Exceptions;

// İCAZƏ XƏTASI — istifadəçi giriş etməyibsə və ya bu əməliyyata icazəsi yoxdursa atılır.
// API-də 401 (Unauthorized) cavabına çevrilir.
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
