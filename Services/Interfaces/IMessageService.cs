﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CustomerService.API.Dtos.RequestDtos;
using CustomerService.API.Dtos.RequestDtos.Wh;
using CustomerService.API.Dtos.ResponseDtos;

namespace CustomerService.API.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageResponseDto> SendMessageAsync(SendMessageRequest request, bool isContact = false, CancellationToken cancellation = default);
        Task<IEnumerable<MessageResponseDto>> GetByConversationAsync(int conversationId, CancellationToken cancellation = default);
        Task UpdateDeliveryStatusAsync(int messageId, DateTimeOffset deliveredAt, CancellationToken cancellation = default);
        Task MarkAsReadAsync(int messageId, DateTimeOffset readAt, CancellationToken cancellation = default);

        Task<MessageResponseDto> SendMediaAsync(
           SendMediaRequest request,
           string jwtToken,
           CancellationToken cancellation = default
       );

        Task<MessageResponseDto?> GetByExternalIdAsync(string externalId, CancellationToken ct);
    }
}