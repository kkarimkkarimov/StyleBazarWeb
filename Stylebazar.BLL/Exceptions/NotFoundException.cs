namespace Stylebazar.BLL.Exceptions;

// TAPILMADI XƏTASI — axtarılan obyekt (məs. məhsul/mağaza) bazada olmayanda atılır.
// API-də 404 (Not Found) cavabına çevrilir.
public class NotFoundException : Exception
{
    // Entity adı + açar ilə avtomatik mesaj qurur ("Product with key '5' was not found.").
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }

    // Hazır mesajla.
    public NotFoundException(string message) : base(message) { }
}
