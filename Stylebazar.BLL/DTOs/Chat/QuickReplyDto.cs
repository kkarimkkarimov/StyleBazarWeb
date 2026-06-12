namespace Stylebazar.BLL.DTOs.Chat;

// QuickReplyDto — satıcının bir sürətli cavab şablonu (composer menyusunda göstərilir).
public class QuickReplyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
