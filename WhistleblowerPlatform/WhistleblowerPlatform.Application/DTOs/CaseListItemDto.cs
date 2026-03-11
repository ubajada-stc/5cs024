namespace WhistleblowerPlatform.Application.DTOs;

public class CaseListItemDto
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AcknowledgementDueAt { get; set; }
    public DateTime FeedbackDueAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public int AttachmentCount { get; set; }
}
