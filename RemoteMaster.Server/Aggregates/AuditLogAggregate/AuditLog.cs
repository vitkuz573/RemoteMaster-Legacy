// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Aggregates.AuditLogAggregate;

public class AuditLog : IAggregateRoot
{
    private AuditLog() { }

    public AuditLog(string actionType, string userName, DateTime actionTime, string details)
    {
        ActionType = actionType;
        UserName = userName;
        ActionTime = actionTime;
        Details = details;
    }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; private set; }

    public string ActionType { get; private set; }
    
    public string UserName { get; private set; }
    
    public DateTime ActionTime { get; private set; }
    
    public string Details { get; private set; }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public static AuditLog Create(string actionType, string userName, string details)
    {
        return new AuditLog(actionType, userName, DateTime.UtcNow, details);
    }
}

