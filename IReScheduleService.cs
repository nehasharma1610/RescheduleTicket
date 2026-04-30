using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEMApps.DTOs.Booking;
using TEMPApps.DTOs.ReSchedulingDto;

namespace TEMApps.Interfaces
{
    public interface IReScheduleService
    {
        Task GetBookingDetailsAsync(Guid id, Guid userId);
        public Task<ReScheduleResponseDto> RescheduleTicketAsync(ReScheduleTicketDto dto);
      
    }
}
