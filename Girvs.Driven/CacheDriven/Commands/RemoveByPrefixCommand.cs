namespace Girvs.Driven.CacheDriven.Commands;

public record RemoveByPrefixCommand(string Prefix) : Message;