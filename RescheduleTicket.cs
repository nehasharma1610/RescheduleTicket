using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEMApps.Models.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TEMApps.Models;

namespace TEMPApps.Models.Models
{
    [Table("RescheduleTickets")]
    public class RescheduleTicket : BaseModel
    {
        [Key]
        [Column("RescheduleId")]
        public Guid Id { get; set; }
        [Column]
        public Guid TicketId { get; set; } // Foreign key to Booking
        [Column]
        public DateTimeOffset OldDate { get; set; }
        [Column]
        public DateTimeOffset NewDate { get; set; }
        [Column]
        public string Reason { get; set; } = string.Empty;
        [Column]
        public DateTimeOffset ReScheduledOn { get; set; } 
        [Column]
        public Booking Booking { get; set; }  // Navigation property to Booking
    }
}
