namespace ZhuoFan.Wb.Common.Events.Articles;

/// <summary>
/// 文书修改事件
/// </summary>
public record EditActicleEvent(Guid ActicleId, string ActicleName, string ActicleContent) : IntegrationEvent;