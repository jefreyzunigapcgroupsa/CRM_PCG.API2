﻿using CustomerService.API.Utils.Enums;

namespace CustomerService.API.Models
{
    public class ContactLog
    {
        public int Id { get; set; }
        public string? WaName { get; set; } // String. The customer's name.
        public string? WaId { get; set; } // String. The customer's WhatsApp ID.
        public string? WaUserId { get; set; } // String.Additional unique, alphanumeric identifier for a WhatsApp user.
        public string? Phone { get; set; }
        public string? IdCard { get; set; }
        public string? ResidenceCard { get; set; }
        public string? Password { get; set; }
        public IdType? IdType { get; set; } //Tipo de documento de idetificación.
        public string? FullName { get; set; }

        public bool? IsProvidingData { get; set; }

        //Nombre que brinda el usuario. Solo se usa de referencia para que el admin pueda ver a la que pertenece cuando es admin.
        public string? CompanyName { get; set; }
        public int? CompanyId { get; set; }
        public ContactStatus Status { get; set; } = ContactStatus.New;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //Flag para verificar si los datos proporcionados por el usuario ha sido validado por el usuario.
        public bool? IsVerified { get; set; }

        public bool IsActive { get; set; }
        public byte[] RowVersion { get; set; } = null!;
        public virtual Company? Company { get; set; }
        public virtual ICollection<Conversation> ConversationClient { get; set; } = new List<Conversation>();

        public virtual ICollection<Message> MessagesSent { get; set; }
       = new List<Message>();

        public DateTime? VerifiedAt { get; set; }

        //Un usuairo verifica el conacto por lo que se guardar el id del usuario en el contactlog
        public int? VerifiedId { get; set; }
        public virtual User? VerifiedBy { get; set; }
    }
}
