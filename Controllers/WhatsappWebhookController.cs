﻿using CustomerService.API.Dtos.RequestDtos;
using CustomerService.API.Dtos.RequestDtos.Wh;
using CustomerService.API.Hubs;
using CustomerService.API.Pipelines.Interfaces;
using CustomerService.API.Services.Interfaces;
using CustomerService.API.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerService.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class WhatsappWebhookController : ControllerBase
    {
        private readonly IMessagePipeline _pipeline;
        private readonly IWhatsAppService _whatsAppService;
        private readonly string _verifyToken;
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hub;

        public WhatsappWebhookController(
            IMessagePipeline pipeline,
            IWhatsAppService whatsAppService,
            IConfiguration config,
            IMessageService messageService,
            IHubContext<ChatHub> hub)
        {
            _pipeline = pipeline;
            _whatsAppService = whatsAppService;
            _verifyToken = config["WhatsApp:VerifyToken"]!;
            _messageService = messageService;
           _hub = hub;
        }

        [HttpGet("webhook", Name = "VerifyWhatsappWebhook")]
        [SwaggerOperation(
            Summary = "Verify WhatsApp webhook subscription",
            Description = "Valida hub.mode y hub.verify_token y devuelve hub.challenge si coinciden.",
            Tags = new[] { "WhatsApp Webhook" }
        )]
        [SwaggerResponse(200, "Webhook verified successfully", typeof(string))]
        [SwaggerResponse(403, "Invalid verification token")]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode"), SwaggerParameter("Expected 'subscribe'", Required = true)]
            string mode,

            [FromQuery(Name = "hub.verify_token"), SwaggerParameter("Your verify token", Required = true)]
            string token,

            [FromQuery(Name = "hub.challenge"), SwaggerParameter("Challenge to echo back", Required = true)]
            string challenge)
        {
            if (mode == "subscribe" && token == _verifyToken)
                return Content(challenge, "text/plain");

            return Forbid();
        }

        //[HttpPost("webhook", Name = "ReceiveWhatsappWebhook")]
        //[SwaggerOperation(
        //    Summary = "Receive WhatsApp messages",
        //    Description = "Procesa solo mensajes entrantes, ignora callbacks de estado.",
        //    Tags = new[] { "WhatsApp Webhook" }
        //)]
        //[Consumes("application/json")]
        //[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> ReceiveAsync([FromBody] WhatsAppUpdateRequest update, CancellationToken cancellation)
        //{
        //    if (update?.Entry == null || !update.Entry.Any() || update?.Entry.First().Changes.First().Value.Messages.Count() <= 0)
        //        return BadRequest(ApiResponse<object>.Fail("Invalid payload structure."));

        //    await _pipeline.ProcessIncomingAsync
        //        (
        //         update.Entry.First().Changes.First().Value,
        //         cancellation
        //        );

        //    return Ok();
        //}

        [HttpPost("webhook", Name = "ReceiveWhatsappWebhook")]
        [SwaggerOperation(
            Summary = "Receive WhatsApp messages and status callbacks",
            Description = "Procesa mensajes entrantes y callbacks de estado (delivered/read) en un solo endpoint.",
            Tags = new[] { "WhatsApp Webhook" })]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveAsync(
        [FromBody] WhatsAppUpdateRequest update,
        CancellationToken cancellation)
            {
            //Aplanar payload para obtener el ChangeValue
            var change = update.Entry
                .SelectMany(e => e.Changes)
                .Select(c => c.Value)
                .FirstOrDefault();

            if (change == null)
                return BadRequest(ApiResponse<object>.Fail("Payload inválido"));

            // Procesar callbacks de estado si existen
            if (change.Statuses?.Any() == true)
            {
                foreach (var st in change.Statuses)
                {
                    // Buscar mensaje por ExternalId (wamid)
                    var msg = await _messageService.GetByExternalIdAsync(st.MessageId, cancellation);
                    if (msg == null) continue;

                    var when = DateTimeOffset.FromUnixTimeSeconds(st.Timestamp);

                    // Actualizar estado en BD
                    switch (st.Status.ToLower())
                    {
                        case "delivered":
                            await _messageService.UpdateDeliveryStatusAsync(msg.MessageId, when, cancellation);
                            break;
                        case "read":
                            await _messageService.MarkAsReadAsync(msg.MessageId, when, cancellation);
                            break;
                            // otros estados opcionales: "sent", "failed", etc.
                    }

                    // (Opcional) Emitir evento SignalR para notificaciones en tiempo real
                    var evt = st.Status == "delivered" ? "MessageDelivered" : "MessageRead";

                    await _hub.Clients
                        .Group(msg.ConversationId.ToString())
                        .SendAsync(evt, new { msg.MessageId, when }, cancellation);
                }

                return Ok();
            }

            // Procesar mensajes entrantes (tu pipeline existente)
            if (change.Messages?.Any() == true)
            {
                await _pipeline.ProcessIncomingAsync(change, cancellation);
                return Ok();
            }

            // Sin datos relevantes
            return BadRequest(ApiResponse<object>.Fail("Sin contenido relevante"));
        }

        [HttpPost("{conversationId}/send")]
        [SwaggerOperation(
            Summary = "Envía un mensaje en el contexto de una conversación",
            Description = "Guarda el mensaje en la base de datos, lo envía por WhatsApp Cloud API y notifica a clientes SignalR."
        )]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendMessageAsync(
            int conversationId,
            [FromBody] SendMessageRequest req,
            CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(ApiResponse<object>.Fail("Los campos 'to' y 'body' son obligatorios."));

            // Aquí defines quién está enviando:
            //Hay que extraer del jwt el identificador del quien envia el mensaje. Temporalmente se definira al admin 1. No lo elvides...

            await _messageService.SendMessageAsync(req, false, cancellation);

            //await _whatsAppService
            //    .SendTextAsync(conversationId, BotUserId, req.Body, cancellation);

            return Ok(ApiResponse<object>.Ok(message: "Mensaje enviado y registrado correctamente."));
        }

        //[HttpPost("status/webhook")]
        //public async Task<IActionResult> ReceiveStatusAsync(
        //    [FromBody] WhatsAppStatusRequestDto update,
        //    CancellationToken ct = default)
        //{
        //    if (update?.Entry == null || !update.Entry.Any())
        //        return BadRequest("Invalid payload");

        //    foreach (var entry in update.Entry)
        //    {
        //        foreach (var change in entry.Changes)
        //        {
        //            foreach (var status in change.Value.Statuses)
        //            {
        //                var ts = DateTimeOffset.FromUnixTimeSeconds(status.Timestamp);

        //                switch (status.Status.ToLowerInvariant())
        //                {
        //                    case "delivered":
        //                        // actualiza deliveredAt + dispara SignalR "MessageDelivered"
        //                        await _messageService.UpdateDeliveryStatusAsync(
        //                            messageId: int.Parse(status.Id),
        //                            deliveredAt: ts,
        //                            cancellation: ct);
        //                        break;

        //                    case "read":
        //                        // actualiza readAt + dispara SignalR "MessageRead"
        //                        await _messageService.MarkAsReadAsync(
        //                            messageId: int.Parse(status.Id),
        //                            readAt: ts,
        //                            cancellation: ct);
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    return Ok();
        //}

        [HttpPost("{conversationId}/send/media")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendMediaAsync(
            int conversationId,
            IFormFile file,
            [FromForm] string? caption,
            CancellationToken cancellation
        )
                {

            var jwtToken = HttpContext.Request
                             .Headers["Authorization"]
                             .ToString()
                             .Split(' ')[1];

            if (file == null || file.Length == 0)
                        return BadRequest(ApiResponse<object>.Fail("No se envió archivo."));


                    // convierte IFormFile → byte[]
                    byte[] data;
                    await using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms, cancellation);
                        data = ms.ToArray();
                    }

                    var svcReq = new SendMediaRequest
                    {
                        ConversationId = conversationId,
                        Data = data,
                        FileName = file.FileName,
                        MimeType = file.ContentType,
                        Caption = caption
                    };

                    // llama al servicio
                    await _messageService.SendMediaAsync(svcReq, jwtToken, cancellation);

                    return Ok(ApiResponse<object>.Ok("Multimedia enviada correctamente."));
                }
    }
}