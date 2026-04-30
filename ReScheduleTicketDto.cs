using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEMPApps.DTOs.ReSchedulingDto
{
    public class ReScheduleTicketDto
{
        public Guid TicketId { get; set; }
        public DateTimeOffset NewDate { get; set; }
        public string Reason { get; set; } 
    }
}
