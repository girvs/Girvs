using Girvs.EventBus;

namespace Sample.ServiceB.Events;

// 继承 IntegrationEvent（record），不指定 base 初始化即调用其无参构造（生成 Id/CreationDate）
public record SampleMessage(string Text) : IntegrationEvent;
