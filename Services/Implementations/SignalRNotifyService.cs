﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CustomerService.API.Services.Interfaces;
using CustomerService.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using CustomerService.API.Dtos.RequestDtos;
using CustomerService.API.Dtos.ResponseDtos;

namespace CustomerService.API.Services.Implementations
{
    public class SignalRNotifyService : ISignalRNotifyService
    {
        private readonly IHubContext<NotificationsHub> _hub;

        public SignalRNotifyService(IHubContext<NotificationsHub> hub)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        }

        public Task NotifyNewContactCreatedAsync(NewContactCreatedDto dto, CancellationToken cancellation = default)
        {
            return _hub.Clients
                       .Group("Admins")
                       .SendAsync("NewContactCreated", dto, cancellation);
        }

        public Task NotifySupportRequestedAsync(SupportRequestedDto dto, CancellationToken cancellation = default)
        {
            return _hub.Clients
                       .Group("Admins")
                       .SendAsync("SupportRequested", dto, cancellation);
        }

        public Task NotifyConversationAssignedAsync(ConversationAssignedDto dto, CancellationToken cancellation = default)
        {
            // Notify the assigned agent
            return _hub.Clients
                       .User(dto.AssignedAgentId.ToString())
                       .SendAsync("ConversationAssigned", dto, cancellation);
        }

        public Task NotifyUserAsync(int userId, string method, object payload, CancellationToken cancellation = default)
        {
            return _hub.Clients
                       .User(userId.ToString())
                       .SendAsync(method, payload, cancellation);
        }
        public Task SendNotificationToUsersAsync(IEnumerable<int> userIds, NotificationResponseDto dto, CancellationToken ct = default)
        {
            var tasks = userIds.Select(id =>
              _hub.Clients.Group(id.ToString())
                         .SendAsync("Notification", dto, ct)
            );

            tasks = tasks.Append(
              _hub.Clients.Group("Admins").SendAsync("Notification", dto, ct)
            );

            return Task.WhenAll(tasks);

        }
    }
}