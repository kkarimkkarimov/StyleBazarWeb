namespace Stylebazar.BLL.Exceptions;

// BİZNES XƏTASI — biznes qaydası pozulanda atılır (məs. "5 məhsul limitini keçdin").
// API-də 422 (Unprocessable Entity) cavabına çevrilir.
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
