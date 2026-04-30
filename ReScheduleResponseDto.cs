using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEMPApps.DTOs.ReSchedulingDto
{
    public class ReScheduleResponseDto
{
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public DateTimeOffset OldDate { get; set; }
        public DateTimeOffset NewDate { get; set; } 
        public string Message { get; set; } = string.Empty;
    }
}
